using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using SecNetApi.Extensions;
using SecNetCore.Entities;

namespace SecNetData.Tests.Integration;

/// <summary>
/// Integration test verifying Phase 5 implementation works end-to-end.
/// This demonstrates the intended usage pattern for new applications.
/// </summary>
public sealed class Phase5IntegrationTests
{
    [Fact]
    public void NewApplication_CanIntegrateSecNet_WithMinimalConfiguration()
    {
        // This demonstrates the INTENDED usage pattern from the requirements:
        // A new application should be able to register SecNet with just:
        // 1. ApplicationName
        // 2. Database credentials
        // 3. Entity assemblies

        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());

        // Act - This is how a new application uses SecNet
        services.AddSecNet(options =>
        {
            // Required configuration
            options.ApplicationName = "MyNewApplication";
            options.Database.Server = "localhost";
            options.Database.Username = "root";
            options.Database.DatabaseName = "mynewapp";

            // Entity registration
            options.EntityAssemblies.Add(typeof(Customer).Assembly);
        });

        var provider = services.BuildServiceProvider();

        // Assert - Verify all SecNet services are available
        Assert.NotNull(provider.GetRequiredService<SecOptions>());
        Assert.NotNull(provider.GetRequiredService<ISecApplicationInfo>());

        var appInfo = provider.GetRequiredService<ISecApplicationInfo>();
        Assert.Equal("MyNewApplication", appInfo.ApplicationName);
        Assert.NotEmpty(appInfo.SecNetVersion);
    }

    [Fact]
    public void NewApplication_CanConfigure_AllSchemaOptions()
    {
        // Advanced scenario: Full schema configuration

        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());

        // Act
        services.AddSecNet(options =>
        {
            // Basic
            options.ApplicationName = "AdvancedApp";
            options.Database.Server = "prod-db.example.com";
            options.Database.Port = 3306;
            options.Database.Username = "appuser";
            options.Database.Password = "SecurePassword123!";
            options.Database.DatabaseName = "advanced_prod";

            // Schema tuning
            options.Schema.AutoCreateDatabase = false;  // Manual in production
            options.Schema.AutoCreateTables = true;
            options.Schema.AutoAddColumns = true;
            options.Schema.AutoCreateIndexes = false;   // Future phase
            options.Schema.AutoSynchronizeOnStartup = false;  // Manual in production

            // Multiple assemblies
            options.EntityAssemblies.Add(typeof(Customer).Assembly);
            // options.EntityAssemblies.Add(typeof(Order).Assembly);
            // options.EntityAssemblies.Add(typeof(Inventory).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<SecOptions>();

        // Assert
        Assert.Equal("AdvancedApp", options.ApplicationName);
        Assert.Equal("prod-db.example.com", options.Database.Server);
        Assert.Equal(3306u, options.Database.Port);
        Assert.Equal("appuser", options.Database.Username);
        Assert.Equal("SecurePassword123!", options.Database.Password);
        Assert.Equal("advanced_prod", options.Database.DatabaseName);

        Assert.False(options.Schema.AutoCreateDatabase);
        Assert.True(options.Schema.AutoCreateTables);
        Assert.True(options.Schema.AutoAddColumns);
        Assert.False(options.Schema.AutoCreateIndexes);
        Assert.False(options.Schema.AutoSynchronizeOnStartup);
    }

    [Fact]
    public void ApplicationConfiguration_CanBeLoadedFromSettings()
    {
        // Real-world scenario: Configuration from appsettings.json

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Sec:ApplicationName", "ConfiguredApp" },
                { "Sec:Database:Server", "configured-db" },
                { "Sec:Database:Port", "3307" },
                { "Sec:Database:Username", "configuser" },
                { "Sec:Database:DatabaseName", "configured_db" },
                { "Sec:Schema:AutoSynchronizeOnStartup", "false" },
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());

        // Act
        services.AddSecNet(options =>
        {
            // Would bind from configuration in real app
            configuration.GetSection("Sec").Bind(options);
        });

        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<SecOptions>();

        // Assert
        Assert.Equal("ConfiguredApp", opts.ApplicationName);
        Assert.Equal("configured-db", opts.Database.Server);
        Assert.Equal(3307u, opts.Database.Port);
        Assert.Equal("configuser", opts.Database.Username);
        Assert.Equal("configured_db", opts.Database.DatabaseName);
        Assert.False(opts.Schema.AutoSynchronizeOnStartup);
    }

    /// <summary>
    /// Test implementation of IHostEnvironment.
    /// </summary>
    private class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
    }

    private class NullFileProvider : IFileProvider
    {
        public IFileInfo GetFileInfo(string subpath) => new NullFileInfo();
        public IDirectoryContents GetDirectoryContents(string subpath) => new NullDirectoryContents();
        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }

    private class NullFileInfo : IFileInfo
    {
        public bool Exists => false;
        public long Length => 0;
        public string PhysicalPath => "";
        public string Name => "";
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public bool IsDirectory => false;
        public Stream CreateReadStream() => Stream.Null;
    }

    private class NullDirectoryContents : IDirectoryContents, IEnumerable<IFileInfo>
    {
        public bool Exists => false;
        public IEnumerator<IFileInfo> GetEnumerator() => Enumerable.Empty<IFileInfo>().GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
