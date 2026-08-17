using System.ComponentModel.DataAnnotations;
using Xunit;
using SecNetData.Crud;

namespace SecNetData.Tests.Crud;

public sealed class SecValidationServiceTests
{
    private readonly ISecValidationService _validator = new SecValidationService();

    [Fact]
    public void Validate_ValidObject_ReturnsTrue()
    {
        var model = new TestModel { Name = "John", Email = "john@example.com" };
        var result = _validator.Validate(model, out var errors);

        Assert.True(result);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MissingRequiredField_ReturnsFalse()
    {
        var model = new TestModel { Name = "", Email = "john@example.com" };
        var result = _validator.Validate(model, out var errors);

        Assert.False(result);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_ExceedsMaxLength_ReturnsFalse()
    {
        var model = new TestModel { Name = new string('a', 200), Email = "john@example.com" };
        var result = _validator.Validate(model, out var errors);

        Assert.False(result);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_InvalidEmail_ReturnsFalse()
    {
        var model = new TestModel { Name = "John", Email = "not-an-email" };
        var result = _validator.Validate(model, out var errors);

        Assert.False(result);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_NullObject_ReturnsFalse()
    {
        var result = _validator.Validate(null!, out var errors);

        Assert.False(result);
        Assert.NotEmpty(errors);
    }

    private class TestModel
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }
    }
}
