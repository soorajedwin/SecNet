using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecNetData.Configuration;
using SecNetData.Context;
using SecNetData.Crud;
using SecNetData.Schema;
using SecNetData.Services;

namespace SecNetData.Extensions;

public static class SecDataServiceCollectionExtensions
{
    /// <summary>
    /// Adds SecNet data services to the dependency injection container.
    /// Includes EF Core, schema management, and entity discovery.
    /// </summary>
    public static IServiceCollection AddSecData(
        this IServiceCollection services,
        Action<SecDataOptions> configure)
    {
        return AddSecData(services, configure, null);
    }

    /// <summary>
    /// Adds SecNet data services with optional schema synchronization configuration.
    /// </summary>
    public static IServiceCollection AddSecData(
        this IServiceCollection services,
        Action<SecDataOptions> configure,
        Action<SecSchemaOptions>? configureSchema)
    {
        var options = new SecDataOptions();

        configure(options);

        // Register database options
        services.AddSingleton(options.Database);
        services.AddSingleton(options);

        // Register connection string builder
        services.AddSingleton<SecConnectionStringBuilder>();

        // Register entity discovery
        var entityDiscovery = new SecEntityDiscovery(
            options.EntityAssemblies);

        services.AddSingleton(entityDiscovery);
        services.AddSingleton(new SecModelConfigurer(entityDiscovery));

        // Register schema manager components
        services.AddSingleton<MySQLTypeMapper>();
        services.AddSingleton(sp =>
            new SecDatabaseManager(
                sp.GetRequiredService<SecDatabaseOptions>(),
                sp.GetRequiredService<Services.SecConnectionStringBuilder>()));

        services.AddSingleton(sp =>
            new SecTableManager(
                sp.GetRequiredService<SecDatabaseOptions>(),
                sp.GetRequiredService<Services.SecConnectionStringBuilder>()));

        services.AddSingleton<SecSchemaComparator>();

        // Register schema options
        var schemaOptions = new SecSchemaOptions
        {
            DatabaseName = options.Database.DatabaseName,
        };

        if (configureSchema != null)
        {
            configureSchema(schemaOptions);
        }

        services.AddSingleton(schemaOptions);

        // Register schema manager
        services.AddSingleton<ISecSchemaManager>(sp =>
            new SecSchemaManager(
                sp.GetRequiredService<SecEntityDiscovery>(),
                sp.GetRequiredService<SecDatabaseManager>(),
                sp.GetRequiredService<SecTableManager>(),
                sp.GetRequiredService<SecSchemaOptions>()));

        // Register DbContext
        var connectionBuilder = new SecConnectionStringBuilder();

        var connectionString =
            connectionBuilder.BuildDatabaseConnectionString(
                options.Database);

        services.AddDbContext<SecDbContext>((sp, dbOptions) =>
        {
            var modelConfigurer = sp.GetRequiredService<SecModelConfigurer>();

            dbOptions.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString));
        });

        // Register CRUD services
        var crudOptions = new SecCrudOptions();
        services.AddSingleton(crudOptions);

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
                sp.GetRequiredService<SecCrudOptions>(),
                sp.GetRequiredService<ILogger<SecCrudService>>()));

        return services;
    }
}
