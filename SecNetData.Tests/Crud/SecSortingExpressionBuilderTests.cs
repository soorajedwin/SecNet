using System;
using System.Linq;
using Xunit;
using SecNetCore.Attributes;
using SecNetCore.Models;
using SecNetData.Crud;
using SecNetData.Services;

namespace SecNetData.Tests.Crud;

public sealed class SecSortingExpressionBuilderTests
{
    private ISecSortingExpressionBuilder _builder = null!;
    private ISecEntityRegistry _registry = null!;

    public SecSortingExpressionBuilderTests()
    {
        var discovery = new SecEntityDiscovery(typeof(TestEntity).Assembly);
        _registry = new SecEntityRegistry(discovery);
        _builder = new SecSortingExpressionBuilder();
    }

    [Fact]
    public void ApplySorting_NoSorts_ReturnsOriginalQuery()
    {
        var query = Enumerable.Empty<TestEntity>().AsQueryable();
        var sorts = new List<SecSort>();

        var result = _builder.ApplySorting(query, sorts, typeof(TestEntity), _registry, out var errors);

        Assert.NotNull(result);
        Assert.Empty(errors);
    }

    [Fact]
    public void ApplySorting_ValidSort_AppliesSuccessfully()
    {
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Zebra" },
            new TestEntity { Id = 2, Name = "Apple" },
            new TestEntity { Id = 3, Name = "Banana" }
        };
        var query = items.AsQueryable();
        var sorts = new List<SecSort> { new SecSort { Property = "Name", Descending = false } };

        var result = _builder.ApplySorting(query, sorts, typeof(TestEntity), _registry, out var errors);

        Assert.Empty(errors);
        var sorted = result.Cast<TestEntity>().ToList();
        Assert.Equal("Apple", sorted[0].Name);
        Assert.Equal("Banana", sorted[1].Name);
        Assert.Equal("Zebra", sorted[2].Name);
    }

    [Fact]
    public void ApplySorting_DescendingSort_SortsDescending()
    {
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Zebra" },
            new TestEntity { Id = 2, Name = "Apple" }
        };
        var query = items.AsQueryable();
        var sorts = new List<SecSort> { new SecSort { Property = "Name", Descending = true } };

        var result = _builder.ApplySorting(query, sorts, typeof(TestEntity), _registry, out var errors);

        Assert.Empty(errors);
        var sorted = result.Cast<TestEntity>().ToList();
        Assert.Equal("Zebra", sorted[0].Name);
        Assert.Equal("Apple", sorted[1].Name);
    }

    [Fact]
    public void ApplySorting_InvalidProperty_RecordsError()
    {
        var query = Enumerable.Empty<TestEntity>().AsQueryable();
        var sorts = new List<SecSort> { new SecSort { Property = "NonExistentProperty", Descending = false } };

        var result = _builder.ApplySorting(query, sorts, typeof(TestEntity), _registry, out var errors);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ApplySorting_SecIgnoreProperty_RecordsError()
    {
        var query = Enumerable.Empty<TestEntity>().AsQueryable();
        var sorts = new List<SecSort> { new SecSort { Property = "Secret", Descending = false } };

        var result = _builder.ApplySorting(query, sorts, typeof(TestEntity), _registry, out var errors);

        Assert.NotEmpty(errors);
    }

    [SecEntity]
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [SecIgnore]
        public string? Secret { get; set; }
    }
}
