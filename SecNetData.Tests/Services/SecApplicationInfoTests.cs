using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace SecNetData.Tests.Services;

/// <summary>
/// Tests for ISecApplicationInfo and SecApplicationInfo implementation.
/// </summary>
public sealed class SecApplicationInfoTests
{
    [Fact]
    public void SecApplicationInfo_WithValidParameters_Initializes()
    {
        // Arrange
        var hostEnvironment = new TestHostEnvironment { EnvironmentName = "Production" };

        // Act
        var appInfo = new SecApplicationInfo("MyApp", hostEnvironment);

        // Assert
        Assert.Equal("MyApp", appInfo.ApplicationName);
        Assert.Equal("Production", appInfo.Environment);
        Assert.NotEmpty(appInfo.SecNetVersion);
    }

    [Fact]
    public void SecApplicationInfo_WithNullApplicationName_Throws()
    {
        // Arrange
        var hostEnvironment = new TestHostEnvironment();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new SecApplicationInfo(null!, hostEnvironment));
    }

    [Fact]
    public void SecApplicationInfo_WithEmptyApplicationName_Throws()
    {
        // Arrange
        var hostEnvironment = new TestHostEnvironment();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new SecApplicationInfo("", hostEnvironment));
    }

    [Fact]
    public void SecApplicationInfo_WithNullHostEnvironment_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecApplicationInfo("MyApp", null!));
    }

    [Fact]
    public void SecApplicationInfo_PropertiesReflectConstructorArguments()
    {
        // Arrange
        var hostEnvironment = new TestHostEnvironment { EnvironmentName = "Staging" };
        var appInfo = new SecApplicationInfo("StagingApp", hostEnvironment);

        // Act & Assert
        Assert.Equal("StagingApp", appInfo.ApplicationName);
        Assert.Equal("Staging", appInfo.Environment);
    }

    [Fact]
    public void SecApplicationInfo_SecNetVersionIsNotEmpty()
    {
        // Arrange
        var hostEnvironment = new TestHostEnvironment();
        var appInfo = new SecApplicationInfo("VersionTest", hostEnvironment);

        // Act & Assert
        Assert.NotNull(appInfo.SecNetVersion);
        Assert.NotEmpty(appInfo.SecNetVersion);
        Assert.True(appInfo.SecNetVersion.Contains("."));
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
