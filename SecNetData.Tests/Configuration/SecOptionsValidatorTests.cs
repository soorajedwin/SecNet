using SecNetData.Configuration;

namespace SecNetData.Tests.Configuration;

/// <summary>
/// Tests for SecOptionsValidator configuration validation logic.
/// </summary>
public sealed class SecOptionsValidatorTests
{
    [Fact]
    public void Validate_WithValidConfiguration_Succeeds()
    {
        // Arrange
        var options = new SecOptions
        {
            ApplicationName = "TestApp",
            Database = new SecDatabaseOptions
            {
                Server = "localhost",
                Username = "root",
                DatabaseName = "testdb"
            }
        };

        // Act & Assert - should not throw
        SecOptionsValidator.Validate(options);
    }

    [Fact]
    public void Validate_WithNullOptions_Throws()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            SecOptionsValidator.Validate(null!));
    }

    [Fact]
    public void Validate_WithMissingApplicationName_Throws()
    {
        // Arrange
        var options = new SecOptions
        {
            ApplicationName = "", // Empty
            Database = new SecDatabaseOptions
            {
                Server = "localhost",
                Username = "root",
                DatabaseName = "testdb"
            }
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            SecOptionsValidator.Validate(options));
        Assert.Contains("ApplicationName", ex.Message);
    }

    [Fact]
    public void Validate_WithMissingServer_Throws()
    {
        // Arrange
        var options = new SecOptions
        {
            ApplicationName = "TestApp",
            Database = new SecDatabaseOptions
            {
                Server = "", // Empty
                Username = "root",
                DatabaseName = "testdb"
            }
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            SecOptionsValidator.Validate(options));
        Assert.Contains("Server", ex.Message);
    }

    [Fact]
    public void Validate_WithMissingUsername_Throws()
    {
        // Arrange
        var options = new SecOptions
        {
            ApplicationName = "TestApp",
            Database = new SecDatabaseOptions
            {
                Server = "localhost",
                Username = "", // Empty
                DatabaseName = "testdb"
            }
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            SecOptionsValidator.Validate(options));
        Assert.Contains("Username", ex.Message);
    }

    [Fact]
    public void Validate_WithMissingDatabaseName_Throws()
    {
        // Arrange
        var options = new SecOptions
        {
            ApplicationName = "TestApp",
            Database = new SecDatabaseOptions
            {
                Server = "localhost",
                Username = "root",
                DatabaseName = "" // Empty
            }
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            SecOptionsValidator.Validate(options));
        Assert.Contains("DatabaseName", ex.Message);
    }

    [Fact]
    public void Validate_WithDefaultPort_Succeeds()
    {
        // Arrange - Port has default value of 3306
        var options = new SecOptions
        {
            ApplicationName = "TestApp",
            Database = new SecDatabaseOptions
            {
                Server = "localhost",
                Username = "root",
                DatabaseName = "testdb"
                // Port not set, uses default
            }
        };

        // Act & Assert
        SecOptionsValidator.Validate(options);
        Assert.Equal(3306u, options.Database.Port);
    }

    [Fact]
    public void Validate_WithCustomPort_Succeeds()
    {
        // Arrange
        var options = new SecOptions
        {
            ApplicationName = "TestApp",
            Database = new SecDatabaseOptions
            {
                Server = "localhost",
                Port = 3307,
                Username = "root",
                DatabaseName = "testdb"
            }
        };

        // Act & Assert
        SecOptionsValidator.Validate(options);
        Assert.Equal(3307u, options.Database.Port);
    }

    [Fact]
    public void Validate_WithEmptyPassword_Succeeds()
    {
        // Arrange - Password is optional
        var options = new SecOptions
        {
            ApplicationName = "TestApp",
            Database = new SecDatabaseOptions
            {
                Server = "localhost",
                Username = "root",
                DatabaseName = "testdb",
                Password = "" // Empty password is valid
            }
        };

        // Act & Assert
        SecOptionsValidator.Validate(options);
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var options = new SecOptions
        {
            ApplicationName = "", // Missing
            Database = new SecDatabaseOptions
            {
                Server = "", // Missing
                Username = "", // Missing
                DatabaseName = "" // Missing
            }
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            SecOptionsValidator.Validate(options));

        Assert.Contains("ApplicationName", ex.Message);
        Assert.Contains("Server", ex.Message);
        Assert.Contains("Username", ex.Message);
        Assert.Contains("DatabaseName", ex.Message);
    }
}
