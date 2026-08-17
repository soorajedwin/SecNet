using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Moq;
using SecNetApi.Tests.Infrastructure;
using SecNetCore.Models;
using SecNetCore.Results;

namespace SecNetApi.Tests.Endpoints;

/// <summary>
/// Integration tests for the generic SecNet entity endpoints.
/// All tests run against an in-process test server using mock data services;
/// no MySQL database is required.
/// </summary>
public sealed class SecEntityEndpointsTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static TestWebApplicationFactory CreateFactory()
    {
        var factory = new TestWebApplicationFactory();
        factory.SetupDefaultRegistry();
        return factory;
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    private static SecResult<SecPagedResult<object>> MakePagedResult(int totalRecords = 2)
    {
        var paged = new SecPagedResult<object>
        {
            Items = new List<object> { new { id = 1, name = "Alpha" }, new { id = 2, name = "Beta" } },
            Page = 1,
            PageSize = 20,
            TotalRecords = totalRecords
        };
        return SecResult<SecPagedResult<object>>.Ok(paged);
    }

    // -----------------------------------------------------------------------
    // 1. GET list — returns 200 with SecResult<SecPagedResult>
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetList_KnownEntity_Returns200()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.GetListAsync("TestProduct", It.IsAny<SecQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakePagedResult());

        var response = await client.GetAsync("/api/sec/TestProduct");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJson(response);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // 2. GET list — paging parameters are forwarded
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetList_WithPaging_ForwardsPagingToService()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        SecQueryRequest? captured = null;
        factory.CrudServiceMock
            .Setup(s => s.GetListAsync("TestProduct", It.IsAny<SecQueryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, SecQueryRequest, CancellationToken>((_, req, _) => captured = req)
            .ReturnsAsync(MakePagedResult());

        await client.GetAsync("/api/sec/TestProduct?page=3&pageSize=5");

        Assert.NotNull(captured);
        Assert.Equal(3, captured!.Page.Page);
        Assert.Equal(5, captured.Page.PageSize);
    }

    // -----------------------------------------------------------------------
    // 3. GET list — sort parameter is forwarded
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetList_WithValidSort_ForwardsSortToService()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        SecQueryRequest? captured = null;
        factory.CrudServiceMock
            .Setup(s => s.GetListAsync("TestProduct", It.IsAny<SecQueryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, SecQueryRequest, CancellationToken>((_, req, _) => captured = req)
            .ReturnsAsync(MakePagedResult());

        await client.GetAsync("/api/sec/TestProduct?sort=Name:desc");

        Assert.NotNull(captured);
        Assert.Single(captured!.Sorts);
        Assert.Equal("Name", captured.Sorts[0].Property);
        Assert.True(captured.Sorts[0].Descending);
    }

    // -----------------------------------------------------------------------
    // 4. GET list — search parameter is forwarded
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetList_WithSearch_ForwardsSearchToService()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        SecQueryRequest? captured = null;
        factory.CrudServiceMock
            .Setup(s => s.GetListAsync("TestProduct", It.IsAny<SecQueryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, SecQueryRequest, CancellationToken>((_, req, _) => captured = req)
            .ReturnsAsync(MakePagedResult());

        await client.GetAsync("/api/sec/TestProduct?search=alpha");

        Assert.NotNull(captured);
        Assert.Equal("alpha", captured!.Search);
    }

    // -----------------------------------------------------------------------
    // 5. GET by ID — returns 200 with entity
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingEntity_Returns200()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.GetByIdAsync("TestProduct", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<object?>.Ok(new TestProduct { Id = 1, Name = "Alpha", IsActive = true }));

        var response = await client.GetAsync("/api/sec/TestProduct/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJson(response);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // 6. GET unknown entity — returns 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetList_UnknownEntity_Returns404()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/sec/NonExistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJson(response);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetById_UnknownEntity_Returns404()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/sec/NonExistent/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 7. GET by ID — missing record returns 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetById_MissingRecord_Returns404()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.GetByIdAsync("TestProduct", "999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<object?>.Fail("Entity not found"));

        var response = await client.GetAsync("/api/sec/TestProduct/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await ReadJson(response);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // 8. GET by ID — invalid ID format returns 400
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetById_InvalidId_Returns400()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.GetByIdAsync("TestProduct", "abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<object?>.Fail("Invalid ID: cannot convert 'abc' to Int32"));

        var response = await client.GetAsync("/api/sec/TestProduct/abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 9. POST — valid create returns 201 with Location header
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Post_ValidEntity_Returns201WithLocation()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.CreateAsync("TestProduct", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<object?>.Ok(new TestProduct { Id = 42, Name = "New", IsActive = true }));

        var body = new StringContent(
            """{"name":"New","isActive":true}""",
            Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/sec/TestProduct", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("TestProduct/42", response.Headers.Location!.ToString());
    }

    // -----------------------------------------------------------------------
    // 10. POST — validation failure returns 400
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Post_ValidationFailure_Returns400()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var failResult = SecResult<object?>.Fail("Validation failed");
        failResult.Errors = new List<string> { "Name is required." };
        factory.CrudServiceMock
            .Setup(s => s.CreateAsync("TestProduct", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failResult);

        var body = new StringContent("""{}""", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/sec/TestProduct", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJson(response);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // 11. POST — entity without Create permission returns 403
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Post_EntityWithoutCreatePermission_Returns403()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.CreateAsync("ReadOnlyEntity", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<object?>.Fail("Create operation not allowed for entity 'ReadOnlyEntity'"));

        var body = new StringContent("""{"value":"x"}""", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/sec/ReadOnlyEntity", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 12. PUT — valid update returns 200
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Put_ValidUpdate_Returns200()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.UpdateAsync("TestProduct", "1", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<object?>.Ok(new TestProduct { Id = 1, Name = "Updated", IsActive = false }));

        var body = new StringContent(
            """{"name":"Updated","isActive":false}""",
            Encoding.UTF8, "application/json");
        var response = await client.PutAsync("/api/sec/TestProduct/1", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await ReadJson(response);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // 13. PUT — missing record returns 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Put_MissingRecord_Returns404()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.UpdateAsync("TestProduct", "999", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<object?>.Fail("Entity not found"));

        var body = new StringContent("""{"name":"X"}""", Encoding.UTF8, "application/json");
        var response = await client.PutAsync("/api/sec/TestProduct/999", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 14. PUT — invalid ID returns 400
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Put_InvalidId_Returns400()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.UpdateAsync("TestProduct", "abc", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<object?>.Fail("Invalid ID: cannot convert 'abc' to Int32"));

        var body = new StringContent("""{"name":"X"}""", Encoding.UTF8, "application/json");
        var response = await client.PutAsync("/api/sec/TestProduct/abc", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 15. PUT — validation failure returns 400
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Put_ValidationFailure_Returns400()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var failResult = SecResult<object?>.Fail("Validation failed");
        failResult.Errors = new List<string> { "Name cannot be empty." };
        factory.CrudServiceMock
            .Setup(s => s.UpdateAsync("TestProduct", "1", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failResult);

        var body = new StringContent("""{"name":""}""", Encoding.UTF8, "application/json");
        var response = await client.PutAsync("/api/sec/TestProduct/1", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 16. PUT — entity without Update permission returns 403
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Put_EntityWithoutUpdatePermission_Returns403()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.UpdateAsync("ReadOnlyEntity", "1", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<object?>.Fail("Update operation not allowed for entity 'ReadOnlyEntity'"));

        var body = new StringContent("""{"value":"x"}""", Encoding.UTF8, "application/json");
        var response = await client.PutAsync("/api/sec/ReadOnlyEntity/1", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 17. DELETE — successful delete returns 204 No Content
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingEntity_Returns204()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.DeleteAsync("TestProduct", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<bool>.Ok(true));

        var response = await client.DeleteAsync("/api/sec/TestProduct/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 18. DELETE — missing record returns 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_MissingRecord_Returns404()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.DeleteAsync("TestProduct", "999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<bool>.Fail("Entity not found"));

        var response = await client.DeleteAsync("/api/sec/TestProduct/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 19. DELETE — entity without Delete permission returns 403
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_EntityWithoutDeletePermission_Returns403()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.DeleteAsync("ReadOnlyEntity", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecResult<bool>.Fail("Delete operation not allowed for entity 'ReadOnlyEntity'"));

        var response = await client.DeleteAsync("/api/sec/ReadOnlyEntity/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 20. Invalid sort property returns 400
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetList_InvalidSortProperty_Returns400()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // No mock needed — sorting validation happens before calling the service
        var response = await client.GetAsync("/api/sec/TestProduct?sort=NonExistentProperty");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJson(response);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        var message = doc.RootElement.GetProperty("message").GetString();
        Assert.Contains("NonExistentProperty", message);
    }

    // -----------------------------------------------------------------------
    // 21. Invalid page size returns 400
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetList_PageSizeExceedsMax_Returns400()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // Default MaxPageSize is 100
        var response = await client.GetAsync("/api/sec/TestProduct?pageSize=999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJson(response);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetList_NonIntegerPageSize_Returns400()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/sec/TestProduct?pageSize=notanumber");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // 22. Unexpected exception is handled by middleware — returns 500
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetList_ServiceThrowsException_Returns500WithSafeMessage()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        factory.CrudServiceMock
            .Setup(s => s.GetListAsync("TestProduct", It.IsAny<SecQueryRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("internal database error with secret"));

        var response = await client.GetAsync("/api/sec/TestProduct");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        // The raw exception message must not appear in the response
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("internal database error with secret", body);
    }

    // -----------------------------------------------------------------------
    // 23. Standard error response format is always consistent
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ErrorResponse_AlwaysHasStandardSecResultShape()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // Trigger a 404 for unknown entity
        var response = await client.GetAsync("/api/sec/Nonexistent");

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // Must have success, message, errors
        Assert.True(doc.RootElement.TryGetProperty("success", out _));
        Assert.True(doc.RootElement.TryGetProperty("message", out _));
        Assert.True(doc.RootElement.TryGetProperty("errors", out _));
        Assert.Equal(JsonValueKind.False, doc.RootElement.GetProperty("success").ValueKind);
    }

    // -----------------------------------------------------------------------
    // 24. Entity names are restricted to registered entities
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AnyVerb_ArbitraryEntityName_Returns404NotArbitraryType()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // These entity names are not registered — all must return 404
        var paths = new[]
        {
            "/api/sec/System.IO.File",
            "/api/sec/__proto__",
            "/api/sec/../../etc/passwd",
        };

        foreach (var path in paths)
        {
            var response = await client.GetAsync(path);
            // Either 404 (entity not found) or 400 (bad route) — never 200
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }

    // -----------------------------------------------------------------------
    // 25. Request properties cannot bypass Phase 3 property security
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Post_ExtraPropertiesInBody_ArePassedToServiceNotDirectlyToDb()
    {
        // This test verifies the API layer passes the deserialized model to the CRUD service
        // without performing its own property filtering. The CRUD service (Phase 3) is
        // responsible for ignoring/rejecting properties that are not writable.
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        object? capturedModel = null;
        factory.CrudServiceMock
            .Setup(s => s.CreateAsync("TestProduct", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, object, CancellationToken>((_, model, _) => capturedModel = model)
            .ReturnsAsync(SecResult<object?>.Ok(new TestProduct { Id = 1 }));

        // Send a body that has a well-known property plus an extra property.
        // The endpoint must forward the deserialized model to the service — not to the DB directly.
        var body = new StringContent(
            """{"name":"Test","isActive":true,"internalSecret":"should-be-ignored-by-service"}""",
            Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/sec/TestProduct", body);

        // The service was called (service is responsible for filtering, not the endpoint)
        factory.CrudServiceMock.Verify(
            s => s.CreateAsync("TestProduct", It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Additional — POST with unparseable body returns 400
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Post_InvalidJsonBody_Returns400()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var body = new StringContent("this is not json", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/sec/TestProduct", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Additional — ascending sort (no :desc suffix)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetList_AscendingSort_ForwardsSortCorrectly()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        SecQueryRequest? captured = null;
        factory.CrudServiceMock
            .Setup(s => s.GetListAsync("TestProduct", It.IsAny<SecQueryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, SecQueryRequest, CancellationToken>((_, req, _) => captured = req)
            .ReturnsAsync(MakePagedResult());

        await client.GetAsync("/api/sec/TestProduct?sort=Name");

        Assert.NotNull(captured);
        Assert.Single(captured!.Sorts);
        Assert.Equal("Name", captured.Sorts[0].Property);
        Assert.False(captured.Sorts[0].Descending);
    }

    // -----------------------------------------------------------------------
    // Additional — multiple sort fields
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetList_MultipleSortFields_ForwardsAllSorts()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        SecQueryRequest? captured = null;
        factory.CrudServiceMock
            .Setup(s => s.GetListAsync("TestProduct", It.IsAny<SecQueryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, SecQueryRequest, CancellationToken>((_, req, _) => captured = req)
            .ReturnsAsync(MakePagedResult());

        await client.GetAsync("/api/sec/TestProduct?sort=Name&sort=Id:desc");

        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Sorts.Count);
        Assert.Equal("Name", captured.Sorts[0].Property);
        Assert.Equal("Id", captured.Sorts[1].Property);
        Assert.True(captured.Sorts[1].Descending);
    }

    // -----------------------------------------------------------------------
    // Additional — DELETE unknown entity returns 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_UnknownEntity_Returns404()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/sec/NonExistent/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
