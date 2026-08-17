using System.Text.Json;
using SecNetCore.Models;
using SecNetData.Crud;
using SecNetApi.Configuration;
using SecNetApi.Services;

namespace SecNetApi.Endpoints;

/// <summary>
/// Registers the generic SecNet entity API endpoints.
/// A single set of five routes handles all registered [SecEntity] types.
/// </summary>
public static class SecEntityEndpoints
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Maps the five generic SecNet entity endpoints onto the provided route builder.
    /// </summary>
    public static IEndpointRouteBuilder MapSecEntityEndpoints(
        this IEndpointRouteBuilder app,
        SecApiOptions options)
    {
        var prefix = options.RoutePrefix.Trim('/');

        // GET /api/sec/{entity} — list with optional paging, sorting, search
        app.MapGet($"/{prefix}/{{entity}}", HandleGetList)
            .WithName("SecGetList")
            .WithTags("SecNet")
            .WithSummary("List entities")
            .WithDescription("Returns a paged list of entities. Supports ?page, ?pageSize, ?search, and ?sort query parameters.");

        // GET /api/sec/{entity}/{id} — get by primary key
        app.MapGet($"/{prefix}/{{entity}}/{{id}}", HandleGetById)
            .WithName("SecGetById")
            .WithTags("SecNet")
            .WithSummary("Get entity by ID")
            .WithDescription("Returns a single entity by its primary key.");

        // POST /api/sec/{entity} — create
        app.MapPost($"/{prefix}/{{entity}}", HandleCreate)
            .WithName("SecCreate")
            .WithTags("SecNet")
            .WithSummary("Create entity")
            .WithDescription("Creates a new entity. Returns HTTP 201 with a Location header on success.");

        // PUT /api/sec/{entity}/{id} — update
        app.MapPut($"/{prefix}/{{entity}}/{{id}}", HandleUpdate)
            .WithName("SecUpdate")
            .WithTags("SecNet")
            .WithSummary("Update entity")
            .WithDescription("Updates an existing entity by its primary key.");

        // DELETE /api/sec/{entity}/{id} — delete
        app.MapDelete($"/{prefix}/{{entity}}/{{id}}", HandleDelete)
            .WithName("SecDelete")
            .WithTags("SecNet")
            .WithSummary("Delete entity")
            .WithDescription("Deletes an entity by its primary key.");

        return app;
    }

    // -----------------------------------------------------------------------
    // GET LIST
    // -----------------------------------------------------------------------

    private static async Task<IResult> HandleGetList(
        string entity,
        HttpContext context,
        ISecEntityRegistry registry,
        ISecCrudService crudService,
        SecHttpResultMapper mapper,
        SecApiOptions options,
        CancellationToken cancellationToken)
    {
        if (!registry.TryGetEntityType(entity, out _))
            return mapper.EntityNotFound(entity);

        // Parse query parameters
        var queryString = context.Request.Query;

        if (!TryParsePageRequest(queryString, options, out var pageRequest, out var pageError))
            return mapper.BadRequest(pageError!);

        var sorts = new List<SecSort>();
        if (!TryParseSorts(queryString, registry, entity, out sorts, out var sortError))
            return mapper.BadRequest(sortError!);

        var search = queryString["search"].FirstOrDefault();

        var request = new SecQueryRequest
        {
            Page = pageRequest,
            Search = search,
            Sorts = sorts
        };

        var result = await crudService.GetListAsync(entity, request, cancellationToken);
        return mapper.Map(result);
    }

    // -----------------------------------------------------------------------
    // GET BY ID
    // -----------------------------------------------------------------------

    private static async Task<IResult> HandleGetById(
        string entity,
        string id,
        ISecEntityRegistry registry,
        ISecCrudService crudService,
        SecHttpResultMapper mapper,
        CancellationToken cancellationToken)
    {
        if (!registry.TryGetEntityType(entity, out _))
            return mapper.EntityNotFound(entity);

        if (string.IsNullOrWhiteSpace(id))
            return mapper.BadRequest("Invalid request: ID must not be empty.");

        var result = await crudService.GetByIdAsync(entity, id, cancellationToken);
        return mapper.Map(result);
    }

    // -----------------------------------------------------------------------
    // POST — CREATE
    // -----------------------------------------------------------------------

    private static async Task<IResult> HandleCreate(
        string entity,
        HttpContext context,
        ISecEntityRegistry registry,
        ISecCrudService crudService,
        SecHttpResultMapper mapper,
        SecApiOptions options,
        CancellationToken cancellationToken)
    {
        if (!registry.TryGetEntityType(entity, out var entityType) || entityType == null)
            return mapper.EntityNotFound(entity);

        var model = await DeserializeBodyAsync(context.Request.Body, entityType, cancellationToken);
        if (model == null)
            return mapper.BadRequest("Invalid request: could not parse request body.");

        var result = await crudService.CreateAsync(entity, model, cancellationToken);

        if (!result.Success)
            return mapper.Map(result);

        // Build Location header for the created resource
        var prefix = options.RoutePrefix.Trim('/');
        var pkProperty = registry.GetPrimaryKeyProperty(entityType);
        var idValue = result.Data != null
            ? entityType.GetProperty(pkProperty.PropertyName)?.GetValue(result.Data) ?? string.Empty
            : string.Empty;
        var locationUri = $"/{prefix}/{entity}/{idValue}";

        return mapper.MapCreated(result, locationUri);
    }

    // -----------------------------------------------------------------------
    // PUT — UPDATE
    // -----------------------------------------------------------------------

    private static async Task<IResult> HandleUpdate(
        string entity,
        string id,
        HttpContext context,
        ISecEntityRegistry registry,
        ISecCrudService crudService,
        SecHttpResultMapper mapper,
        CancellationToken cancellationToken)
    {
        if (!registry.TryGetEntityType(entity, out var entityType) || entityType == null)
            return mapper.EntityNotFound(entity);

        if (string.IsNullOrWhiteSpace(id))
            return mapper.BadRequest("Invalid request: ID must not be empty.");

        var model = await DeserializeBodyAsync(context.Request.Body, entityType, cancellationToken);
        if (model == null)
            return mapper.BadRequest("Invalid request: could not parse request body.");

        var result = await crudService.UpdateAsync(entity, id, model, cancellationToken);
        return mapper.Map(result);
    }

    // -----------------------------------------------------------------------
    // DELETE
    // -----------------------------------------------------------------------

    private static async Task<IResult> HandleDelete(
        string entity,
        string id,
        ISecEntityRegistry registry,
        ISecCrudService crudService,
        SecHttpResultMapper mapper,
        CancellationToken cancellationToken)
    {
        if (!registry.TryGetEntityType(entity, out _))
            return mapper.EntityNotFound(entity);

        if (string.IsNullOrWhiteSpace(id))
            return mapper.BadRequest("Invalid request: ID must not be empty.");

        var result = await crudService.DeleteAsync(entity, id, cancellationToken);
        return mapper.MapNoContent(result);
    }

    // -----------------------------------------------------------------------
    // HELPERS
    // -----------------------------------------------------------------------

    /// <summary>
    /// Parses page/pageSize query parameters, enforcing the configured MaxPageSize.
    /// </summary>
    private static bool TryParsePageRequest(
        IQueryCollection query,
        SecApiOptions options,
        out SecPageRequest pageRequest,
        out string? error)
    {
        pageRequest = new SecPageRequest();
        error = null;

        if (query.TryGetValue("page", out var pageValues))
        {
            if (!int.TryParse(pageValues.FirstOrDefault(), out var page) || page < 1)
            {
                error = "Invalid page: must be a positive integer.";
                return false;
            }

            pageRequest.Page = page;
        }

        if (query.TryGetValue("pageSize", out var pageSizeValues))
        {
            if (!int.TryParse(pageSizeValues.FirstOrDefault(), out var pageSize) || pageSize < 1)
            {
                error = "Invalid page size: must be a positive integer.";
                return false;
            }

            if (pageSize > options.MaxPageSize)
            {
                error = $"Invalid page size: maximum allowed page size is {options.MaxPageSize}.";
                return false;
            }

            pageRequest.PageSize = pageSize;
        }

        return true;
    }

    /// <summary>
    /// Parses sort query parameters in the form ?sort=PropertyName or ?sort=PropertyName:desc.
    /// Only properties registered on the entity type are accepted.
    /// </summary>
    private static bool TryParseSorts(
        IQueryCollection query,
        ISecEntityRegistry registry,
        string entityName,
        out List<SecSort> sorts,
        out string? error)
    {
        sorts = new List<SecSort>();
        error = null;

        var sortValues = query["sort"];
        if (sortValues.Count == 0)
            return true;

        if (!registry.TryGetEntityType(entityName, out var entityType) || entityType == null)
            return true; // entity not found is handled upstream

        var entityMetadata = registry.GetEntityMetadata(entityType);
        var validPropertyNames = new HashSet<string>(
            entityMetadata.Properties.Select(p => p.PropertyName),
            StringComparer.OrdinalIgnoreCase);

        foreach (var sortValue in sortValues)
        {
            if (string.IsNullOrWhiteSpace(sortValue))
                continue;

            var parts = sortValue!.Split(':', 2);
            var propertyName = parts[0].Trim();
            var descending = parts.Length > 1 &&
                             parts[1].Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);

            if (!validPropertyNames.Contains(propertyName))
            {
                error = $"Invalid sort property: '{propertyName}' is not a valid property of '{entityName}'.";
                return false;
            }

            // Use the canonical casing from metadata
            var canonicalName = entityMetadata.Properties
                .First(p => p.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                .PropertyName;

            sorts.Add(new SecSort { Property = canonicalName, Descending = descending });
        }

        return true;
    }

    /// <summary>
    /// Deserializes the HTTP request body into the specified entity type.
    /// Returns null if the body is empty or cannot be parsed.
    /// </summary>
    private static async Task<object?> DeserializeBodyAsync(
        Stream body,
        Type entityType,
        CancellationToken cancellationToken)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync(body, entityType, _jsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
