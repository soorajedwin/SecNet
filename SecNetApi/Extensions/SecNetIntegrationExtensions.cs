using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecNetApi.Configuration;
using SecNetApi.Health;
using SecNetData.Configuration;
using SecNetData.Context;
using SecNetData.Crud;
using SecNetData.Schema;
using SecNetData.Services;

namespace SecNetApi.Extensions;

/// <summary>
/// Extension methods for unified SecNet integration via AddSecNet().
/// This is the primary registration method for new applications.
/// Advanced scenarios can use the lower-level AddSecData() and AddSecApi() methods directly.
/// </summary>
public static class SecNetIntegrationExtensions
{
    /// <summary>
    /// Registers the complete SecNet framework in the dependency injection container.
    /// This is the primary integration point for new applications.
    /// 
    /// Call this method to register:
    /// - SecNet data services (EF Core, schema management, entity discovery, CRUD)
    /// - SecNet API services (endpoint mapping, error handling, options)
    /// - Application info service (ISecApplicationInfo)
    /// - Startup orchestrator
    /// 
    /// After registration, call UseSecNet() in the application startup to initialize.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration callback to set SecOptions (ApplicationName, Database, EntityAssemblies, etc.)</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// builder.Services.AddSecNet(options =>
    /// {
    ///     options.ApplicationName = "MyApplication";
    ///     options.Database.Server = "localhost";
    ///     options.Database.Port = 3306;
    ///     options.Database.Username = "root";
    ///     options.Database.Password = "password";
    ///     options.Database.DatabaseName = "my_application";
    ///     options.EntityAssemblies.Add(typeof(Customer).Assembly);
    /// });
    /// 
    /// var app = builder.Build();
    /// app.UseSecNet();
    /// app.MapSecEndpoints();
    /// app.Run();
    /// </code>
    /// </example>
    public static IServiceCollection AddSecNet(
        this IServiceCollection services,
        Action<SecOptions> configure)
    {
        var options = new SecOptions();
        configure(options);

        // Register unified SecOptions
        services.AddSingleton(options);

        // Validate configuration early
        try
        {
            SecOptionsValidator.Validate(options);
        }
        catch (InvalidOperationException ex)
        {
            // Configuration errors should fail during registration, not later at startup
            throw new InvalidOperationException("SecNet configuration validation failed. " +
                "Ensure ApplicationName, Database.Server, Database.Username, and Database.DatabaseName are set.", ex);
        }

        // Register Application Info service
        services.AddSingleton<ISecApplicationInfo>(sp =>
            new SecApplicationInfo(
                options.ApplicationName,
                sp.GetRequiredService<IHostEnvironment>()));

        // Register core data services using SecDataOptions adapter
        var dataOptions = new SecDataOptions
        {
            Database = options.Database,
            EntityAssemblies = options.EntityAssemblies.ToArray()
        };

        // For backward compatibility, also add to SecDataOptions
        services.AddSingleton(dataOptions);

        // Register connection string builder
        services.AddSingleton<SecConnectionStringBuilder>();

        // Register entity discovery
        var entityDiscovery = new SecEntityDiscovery(
            options.EntityAssemblies.ToArray());

        services.AddSingleton(entityDiscovery);
        services.AddSingleton(new SecModelConfigurer(entityDiscovery));

        // Register schema manager components
        services.AddSingleton<MySQLTypeMapper>();
        services.AddSingleton(sp =>
            new SecDatabaseManager(
                options.Database,
                sp.GetRequiredService<SecConnectionStringBuilder>()));

        services.AddSingleton(sp =>
            new SecTableManager(
                options.Database,
                sp.GetRequiredService<SecConnectionStringBuilder>()));

        services.AddSingleton<SecSchemaComparator>();

        // Register schema options from SecOptions
        var schemaOptions = new SecSchemaOptions
        {
            DatabaseName = options.Database.DatabaseName,
            AutoCreateDatabase = options.Schema.AutoCreateDatabase,
            AutoCreateTables = options.Schema.AutoCreateTables,
            AutoAddColumns = options.Schema.AutoAddColumns,
            AutoCreateIndexes = options.Schema.AutoCreateIndexes,
            AutoSynchronizeOnStartup = options.Schema.AutoSynchronizeOnStartup
        };

        services.AddSingleton(schemaOptions);

        // Register schema manager
        services.AddSingleton<ISecSchemaManager>(sp =>
            new SecSchemaManager(
                sp.GetRequiredService<SecEntityDiscovery>(),
                sp.GetRequiredService<SecDatabaseManager>(),
                sp.GetRequiredService<SecTableManager>(),
                schemaOptions));

        // Register DbContext
        var connectionBuilder = new SecConnectionStringBuilder();
        var connectionString = connectionBuilder.BuildDatabaseConnectionString(options.Database);

        services.AddDbContext<SecDbContext>((sp, dbOptions) =>
        {
            var modelConfigurer = sp.GetRequiredService<SecModelConfigurer>();

            dbOptions.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString));
        });

        // Register CRUD services
        services.AddSingleton(options.Crud);

        services.AddSingleton<ISecEntityRegistry>(sp =>
            new SecEntityRegistry(sp.GetRequiredService<SecEntityDiscovery>()));

        services.AddSingleton<ISecValidationService, SecValidationService>();
        services.AddSingleton<ISecTypeConverter, SecTypeConverter>();
        services.AddSingleton<ISecPropertyMapper, SecPropertyMapper>();
        services.AddSingleton<ISecSortingExpressionBuilder, SecSortingExpressionBuilder>();
        services.AddSingleton<ISecSearchExpressionBuilder, SecSearchExpressionBuilder>();

        services.AddScoped<ISecCrudService>(sp =>
            new SecCrudService(
                sp.GetRequiredService<SecDbContext>(),
                sp.GetRequiredService<ISecEntityRegistry>(),
                sp.GetRequiredService<ISecValidationService>(),
                sp.GetRequiredService<ISecTypeConverter>(),
                sp.GetRequiredService<ISecPropertyMapper>(),
                sp.GetRequiredService<ISecSortingExpressionBuilder>(),
                sp.GetRequiredService<ISecSearchExpressionBuilder>(),
                options.Crud,
                sp.GetRequiredService<ILogger<SecCrudService>>()));

        // Register API services
        services.AddSecApi(opt =>
        {
            opt.RoutePrefix = "api/sec";
            opt.MaxPageSize = 100;
            opt.EnableDevelopmentErrors = false;
            opt.EnableOpenApi = true;
        });

        // Register startup orchestrator
        services.AddSingleton<SecStartupOrchestrator>(sp =>
            new SecStartupOrchestrator(
                options,
                sp.GetRequiredService<SecEntityDiscovery>(),
                sp.GetRequiredService<ISecSchemaManager>(),
                sp.GetRequiredService<ISecApplicationInfo>()));

        // Register authorization for API layer
        services.AddAuthorization();

        // Register health checks
        services.AddHealthChecks()
            .AddCheck<SecNetHealthCheck>("secnet");

        return services;
    }
}
