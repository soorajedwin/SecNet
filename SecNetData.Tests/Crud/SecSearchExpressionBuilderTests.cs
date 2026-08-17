using System;
using System.Linq;
using Xunit;
using SecNetCore.Attributes;
using SecNetData.Crud;
using SecNetData.Services;

namespace SecNetData.Tests.Crud;

public sealed class SecSearchExpressionBuilderTests
{
    private ISecSearchExpressionBuilder _builder = null!;
    private ISecEntityRegistry _registry = null!;

    public SecSearchExpressionBuilderTests()
    {
        var discovery = new SecEntityDiscovery(typeof(TestEntity).Assembly);
        _registry = new SecEntityRegistry(discovery);
        _builder = new SecSearchExpressionBuilder();
    }

    [Fact]
    public void ApplySearch_NullSearchTerm_ReturnsOriginalQuery()
    {
        var items = new List<TestEntity>
        {
            new TestEntity { Name = "John", Email = "john@example.com" }
        };
        var query = items.AsQueryable();

        var result = _builder.ApplySearch(query, null!, typeof(TestEntity), _registry);

        Assert.Equal(query.Count(), result.Count());
    }

    [Fact]
    public void ApplySearch_EmptySearchTerm_ReturnsOriginalQuery()
    {
        var items = new List<TestEntity>
        {
            new TestEntity { Name = "John", Email = "john@example.com" }
        };
        var query = items.AsQueryable();

        var result = _builder.ApplySearch(query, "", typeof(TestEntity), _registry);

        Assert.Equal(query.Count(), result.Count());
    }

    [Fact]
    public void ApplySearch_SearchInName_FindsMatch()
    {
        var items = new List<TestEntity>
        {
            new TestEntity { Name = "John Doe", Email = "john@example.com" },
            new TestEntity { Name = "Jane Smith", Email = "jane@example.com" },
            new TestEntity { Name = "Johnny App", Email = "johnny@example.com" }
        };
        var query = items.AsQueryable();

        var result = _builder.ApplySearch(query, "John", typeof(TestEntity), _registry);
        var matches = result.Cast<TestEntity>().ToList();

        Assert.Equal(2, matches.Count); // "John Doe" and "Johnny App"
    }

    [Fact]
    public void ApplySearch_SearchInEmail_FindsMatch()
    {
        var items = new List<TestEntity>
        {
            new TestEntity { Name = "John", Email = "john@example.com" },
            new TestEntity { Name = "Jane", Email = "jane@example.com" }
        };
        var query = items.AsQueryable();

        var result = _builder.ApplySearch(query, "example", typeof(TestEntity), _registry);
        var matches = result.Cast<TestEntity>().ToList();

        Assert.Equal(2, matches.Count);
    }

    [Fact]
    public void ApplySearch_NoMatches_ReturnsEmpty()
    {
        var items = new List<TestEntity>
        {
            new TestEntity { Name = "John", Email = "john@example.com" }
        };
        var query = items.AsQueryable();

        var result = _builder.ApplySearch(query, "xyz123", typeof(TestEntity), _registry);
        var matches = result.Cast<TestEntity>().ToList();

        Assert.Empty(matches);
    }

    [SecEntity]
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
