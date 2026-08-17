using System;
using Xunit;
using SecNetCore.Attributes;
using SecNetCore.Models;
using SecNetData.Crud;
using SecNetData.Schema;
using SecNetData.Services;

namespace SecNetData.Tests.Crud;

public sealed class SecEntityRegistryTests
{
    private static ISecEntityRegistry CreateRegistry()
    {
        var discovery = new SecEntityDiscovery(typeof(TestCustomer).Assembly);
        return new SecEntityRegistry(discovery);
    }

    [Fact]
    public void GetAllEntityTypes_ReturnsRegisteredEntities()
    {
        var registry = CreateRegistry();
        var entities = registry.GetAllEntityTypes();

        Assert.NotEmpty(entities);
        Assert.Contains(typeof(TestCustomer), entities);
    }

    [Fact]
    public void TryGetEntityType_WithValidName_ReturnsTrue()
    {
        var registry = CreateRegistry();
        var result = registry.TryGetEntityType("TestCustomer", out var entityType);

        Assert.True(result);
        Assert.Equal(typeof(TestCustomer), entityType);
    }

    [Fact]
    public void TryGetEntityType_WithInvalidName_ReturnsFalse()
    {
        var registry = CreateRegistry();
        var result = registry.TryGetEntityType("NonExistentEntity", out var entityType);

        Assert.False(result);
        Assert.Null(entityType);
    }

    [Fact]
    public void GetPrimaryKeyProperty_ReturnsIdProperty()
    {
        var registry = CreateRegistry();
        var pkProperty = registry.GetPrimaryKeyProperty(typeof(TestCustomer));

        Assert.NotNull(pkProperty);
        Assert.Equal("Id", pkProperty.PropertyName);
    }

    [Fact]
    public void IsOperationAllowed_WithAllowedCrud_ReturnsTrue()
    {
        var registry = CreateRegistry();
        var allowed = registry.IsOperationAllowed(typeof(TestCustomer), SecCrud.Create);

        Assert.True(allowed);
    }

    [Fact]
    public void GetEntityMetadata_ReturnsValidMetadata()
    {
        var registry = CreateRegistry();
        var metadata = registry.GetEntityMetadata(typeof(TestCustomer));

        Assert.NotNull(metadata);
        Assert.Equal("TestCustomer", metadata.TableName);
        Assert.NotEmpty(metadata.Properties);
    }

    // Test entity
    [SecEntity]
    public class TestCustomer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
