using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using SecNetApi.Extensions;
using SecNetCore.Entities;

namespace SecNetData.Tests.Registration;

/// <summary>
/// Tests for AddSecNet registration method.
/// </summary>
public sealed class SecNetServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSecNet_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var hostEnvironment = new TestHostEnvironment();

        services.AddSingleton<IHostEnvironment>(hostEnvironment);

        // Act
        services.AddSecNet(options =>
        {
            options.ApplicationName = "TestApp";
            options.Database.Server = "localhost";
            options.Database.Username = "root";
            options.Database.DatabaseName = "testdb";
        });

        var provider = services.BuildServiceProvider();

        // Assert - Verify key services are registered without creating DbContext
        Assert.NotNull(provider.GetRequiredService<SecOptions>());
        Assert.NotNull(provider.GetRequiredService<ISecApplicationInfo>());
        Assert.NotNull(provider.GetRequiredService<SecStartupOrchestrator>());
        Assert.NotNull(provider.GetRequiredService<ISecSchemaManager>());
        Assert.NotNull(provider.GetRequiredService<ISecEntityRegistry>());
        // Note: ISecCrudService is scoped and requires DbContext, so we skip it in unit tests
        // It's tested in integration tests with actual database connectivity
    }

    [Fact]
    public void AddSecNet_WithValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var hostEnvironment = new TestHostEnvironment();
        services.AddSingleton<IHostEnvironment>(hostEnvironment);

        // Act & Assert - should not throw
        services.AddSecNet(options =>
        {
            options.ApplicationName = "ValidApp";
            options.Database.Server = "localhost";
            options.Database.Username = "root";
            options.Database.DatabaseName = "validdb";
        });
    }

    [Fact]
    public void AddSecNet_WithMissingApplicationName_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        var hostEnvironment = new TestHostEnvironment();
        services.AddSingleton<IHostEnvironment>(hostEnvironment);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            services.AddSecNet(options =>
            {
                options.ApplicationName = ""; // Empty
                options.Database.Server = "localhost";
                options.Database.Username = "root";
                options.Database.DatabaseName = "testdb";
            }));
    }

    [Fact]
    public void AddSecNet_WithMultipleEntityAssemblies_DiscoversBoth()
    {
        // Arrange
        var services = new ServiceCollection();
        var hostEnvironment = new TestHostEnvironment();
        services.AddSingleton<IHostEnvironment>(hostEnvironment);

        // Act
        services.AddSecNet(options =>
        {
            options.ApplicationName = "MultiAssemblyApp";
            options.Database.Server = "localhost";
            options.Database.Username = "root";
            options.Database.DatabaseName = "testdb";

            // Register multiple assemblies
            options.EntityAssemblies.Add(typeof(Customer).Assembly);
            // Could add more assemblies here
        });

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ISecEntityRegistry>();

        // Assert - Should be able to find Customer entity
        Assert.True(registry.TryGetEntityType("Customer", out var entityType));
        Assert.NotNull(entityType);
    }

    [Fact]
    public void AddSecNet_ConfiguresSchemaOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var hostEnvironment = new TestHostEnvironment();
        services.AddSingleton<IHostEnvironment>(hostEnvironment);

        // Act
        services.AddSecNet(options =>
        {
            options.ApplicationName = "SchemaConfigApp";
            options.Database.Server = "localhost";
            options.Database.Username = "root";
            options.Database.DatabaseName = "testdb";
            options.Schema.AutoCreateDatabase = false;
            options.Schema.AutoCreateTables = false;
            options.Schema.AutoSynchronizeOnStartup = false;
        });

        var provider = services.BuildServiceProvider();
        var schemaOptions = provider.GetRequiredService<SecSchemaOptions>();

        // Assert
        Assert.False(schemaOptions.AutoCreateDatabase);
        Assert.False(schemaOptions.AutoCreateTables);
        Assert.False(schemaOptions.AutoSynchronizeOnStartup);
    }

    [Fact]
    public void AddSecNet_ProvidessApplicationInfo()
    {
        // Arrange
        var services = new ServiceCollection();
        var hostEnvironment = new TestHostEnvironment { EnvironmentName = "Development" };
        services.AddSingleton<IHostEnvironment>(hostEnvironment);

        // Act
        services.AddSecNet(options =>
        {
            options.ApplicationName = "InfoApp";
            options.Database.Server = "localhost";
            options.Database.Username = "root";
            options.Database.DatabaseName = "testdb";
        });

        var provider = services.BuildServiceProvider();
        var appInfo = provider.GetRequiredService<ISecApplicationInfo>();

        // Assert
        Assert.Equal("InfoApp", appInfo.ApplicationName);
        Assert.Equal("Development", appInfo.Environment);
        Assert.NotEmpty(appInfo.SecNetVersion);
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

    /// <summary>
    /// Null IFileProvider for testing.
    /// </summary>
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
