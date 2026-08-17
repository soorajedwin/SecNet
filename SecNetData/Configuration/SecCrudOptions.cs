namespace SecNetData.Configuration;

/// <summary>
/// Configuration options for the CRUD engine.
/// </summary>
public sealed class SecCrudOptions
{
    /// <summary>
    /// Default page size when not specified.
    /// Default: 20
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// Maximum allowed page size to prevent clients from requesting unbounded data.
    /// Default: 500
    /// </summary>
    public int MaxPageSize { get; set; } = 500;

    /// <summary>
    /// Minimum page size (validation).
    /// Default: 1
    /// </summary>
    public int MinPageSize { get; set; } = 1;
}
