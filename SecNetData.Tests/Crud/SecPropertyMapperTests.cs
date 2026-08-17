using System;
using System.ComponentModel.DataAnnotations;
using Xunit;
using SecNetCore.Attributes;
using SecNetData.Crud;
using SecNetData.Services;

namespace SecNetData.Tests.Crud;

public sealed class SecPropertyMapperTests
{
    private ISecPropertyMapper _mapper = null!;
    private ISecEntityRegistry _registry = null!;
    private ISecTypeConverter _converter = null!;

    public SecPropertyMapperTests()
    {
        var discovery = new SecEntityDiscovery(typeof(TestEntity).Assembly);
        _registry = new SecEntityRegistry(discovery);
        _mapper = new SecPropertyMapper();
        _converter = new SecTypeConverter();
    }

    [Fact]
    public void MapProperties_SimpleProperties_MapsSuccessfully()
    {
        var source = new { Name = "John", Email = "john@example.com" };
        var target = new TestEntity();

        var result = _mapper.MapProperties(source, target, _registry, _converter, out var errors);

        Assert.True(result);
        Assert.Empty(errors);
        Assert.Equal("John", target.Name);
        Assert.Equal("john@example.com", target.Email);
    }

    [Fact]
    public void MapProperties_IgnoresSecIgnoreProperties()
    {
        var source = new { Name = "John", Secret = "should-be-ignored" };
        var target = new TestEntity();

        _mapper.MapProperties(source, target, _registry, _converter, out var errors);

        Assert.Null(target.Secret); // Should not be mapped
    }

    [Fact]
    public void MapProperties_IgnoresPrimaryKey()
    {
        var source = new { Id = 999, Name = "John" };
        var target = new TestEntity { Id = 1 };

        _mapper.MapProperties(source, target, _registry, _converter, out var errors);

        Assert.Equal(1, target.Id); // ID should not be changed
        Assert.Equal("John", target.Name);
    }

    [Fact]
    public void MapProperties_IgnoresUnknownProperties()
    {
        var source = new { Name = "John", UnknownProperty = "value" };
        var target = new TestEntity();

        var result = _mapper.MapProperties(source, target, _registry, _converter, out var errors);

        Assert.True(result); // Should succeed silently
        Assert.Equal("John", target.Name);
    }

    [Fact]
    public void MapProperties_ConvertTypes()
    {
        var source = new { Name = "John", IsActive = "true" };
        var target = new TestEntity();

        var result = _mapper.MapProperties(source, target, _registry, _converter, out var errors);

        Assert.True(result);
        Assert.Equal("John", target.Name);
        Assert.True(target.IsActive);
    }

    [Fact]
    public void MapProperties_NullSource_ReturnsFalse()
    {
        var target = new TestEntity();

        var result = _mapper.MapProperties(null!, target, _registry, _converter, out var errors);

        Assert.False(result);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void MapProperties_NullTarget_ReturnsFalse()
    {
        var source = new { Name = "John" };

        var result = _mapper.MapProperties(source, null!, _registry, _converter, out var errors);

        Assert.False(result);
        Assert.NotEmpty(errors);
    }

    [SecEntity]
    public class TestEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public bool IsActive { get; set; }

        [SecIgnore]
        public string? Secret { get; set; }
    }
}
