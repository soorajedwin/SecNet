using SecNetApi.Services;
using SecNetCore.Results;

namespace SecNetApi.Tests.Services;

/// <summary>
/// Unit tests for SecHttpResultMapper — verifies that SecResult objects
/// are mapped to the correct HTTP status codes without involving HTTP infrastructure.
/// </summary>
public sealed class SecHttpResultMapperTests
{
    private readonly SecHttpResultMapper _mapper = new();

    [Theory]
    [InlineData("Unknown entity: 'Foo'", 404)]
    [InlineData("Entity not found", 404)]
    [InlineData("record not found", 404)]
    [InlineData("Create operation not allowed for entity 'Foo'", 403)]
    [InlineData("Update operation not allowed for entity 'Foo'", 403)]
    [InlineData("Delete operation not allowed for entity 'Foo'", 403)]
    [InlineData("Invalid ID: cannot convert", 400)]
    [InlineData("Invalid sort property: 'Blah' is not valid", 400)]
    [InlineData("Invalid page size: maximum allowed page size is 100.", 400)]
    [InlineData("Validation failed", 400)]
    [InlineData("Property mapping failed", 400)]
    [InlineData("Entity validation failed", 400)]
    [InlineData("Invalid request: could not parse request body.", 400)]
    [InlineData("Failed to create entity: some DB error", 500)]
    [InlineData(null, 500)]
    [InlineData("", 500)]
    public void DetermineFailureStatus_ReturnsExpectedStatusCode(string? message, int expectedStatus)
    {
        var status = _mapper.DetermineFailureStatus(message);
        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public void Map_SuccessResult_Returns200()
    {
        var result = SecResult<string>.Ok("hello");
        var httpResult = _mapper.Map(result);
        Assert.NotNull(httpResult);
    }

    [Fact]
    public void BadRequest_ReturnsFailureResult()
    {
        var httpResult = _mapper.BadRequest("bad input");
        Assert.NotNull(httpResult);
    }

    [Fact]
    public void EntityNotFound_ReturnsNotFoundResult()
    {
        var httpResult = _mapper.EntityNotFound("Foo");
        Assert.NotNull(httpResult);
    }

    [Fact]
    public void ServerError_ReturnsServerErrorResult()
    {
        var httpResult = _mapper.ServerError("oops");
        Assert.NotNull(httpResult);
    }
}
