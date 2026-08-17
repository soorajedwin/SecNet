using Xunit;
using SecNetData.Crud;

namespace SecNetData.Tests.Crud;

public sealed class SecTypeConverterTests
{
    private readonly ISecTypeConverter _converter = new SecTypeConverter();

    [Fact]
    public void TryConvert_StringToString_Succeeds()
    {
        var result = _converter.TryConvert("hello", typeof(string), out var converted, out var error);

        Assert.True(result);
        Assert.Equal("hello", converted);
        Assert.Null(error);
    }

    [Fact]
    public void TryConvert_StringToInt_Succeeds()
    {
        var result = _converter.TryConvert("42", typeof(int), out var converted, out var error);

        Assert.True(result);
        Assert.Equal(42, converted);
        Assert.Null(error);
    }

    [Fact]
    public void TryConvert_InvalidStringToInt_Fails()
    {
        var result = _converter.TryConvert("not a number", typeof(int), out var converted, out var error);

        Assert.False(result);
        Assert.Null(converted);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryConvert_StringToBool_Succeeds()
    {
        var result = _converter.TryConvert("true", typeof(bool), out var converted, out var error);

        Assert.True(result);
        Assert.Equal(true, converted);
    }

    [Fact]
    public void TryConvert_NumberToBool_Succeeds()
    {
        var result = _converter.TryConvert("1", typeof(bool), out var converted, out var error);

        Assert.True(result);
        Assert.Equal(true, converted);
    }

    [Fact]
    public void TryConvert_StringToGuid_Succeeds()
    {
        var guid = Guid.NewGuid();
        var result = _converter.TryConvert(guid.ToString(), typeof(Guid), out var converted, out var error);

        Assert.True(result);
        Assert.Equal(guid, converted);
    }

    [Fact]
    public void TryConvert_StringToDateTime_Succeeds()
    {
        var dateStr = "2024-01-15";
        var result = _converter.TryConvert(dateStr, typeof(DateTime), out var converted, out var error);

        Assert.True(result);
        Assert.IsType<DateTime>(converted);
    }

    [Fact]
    public void TryConvert_NullToNullableInt_Succeeds()
    {
        var result = _converter.TryConvert(null, typeof(int?), out var converted, out var error);

        Assert.True(result);
        Assert.Null(converted);
    }

    [Fact]
    public void TryConvert_NullToNonNullableInt_Fails()
    {
        var result = _converter.TryConvert(null, typeof(int), out var converted, out var error);

        Assert.False(result);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryConvert_StringToDecimal_Succeeds()
    {
        var result = _converter.TryConvert("123.45", typeof(decimal), out var converted, out var error);

        Assert.True(result);
        Assert.Equal(123.45m, converted);
    }

    [Fact]
    public void TryConvert_AlreadyCorrectType_Succeeds()
    {
        var result = _converter.TryConvert(42, typeof(int), out var converted, out var error);

        Assert.True(result);
        Assert.Equal(42, converted);
    }
}
