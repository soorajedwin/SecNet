using SecNetCore.Attributes;
using SecNetCore.Models;

namespace SecNetApi.Tests.Infrastructure;

/// <summary>
/// A test entity used only in endpoint tests.
/// Registered through the mock ISecEntityRegistry — no database required.
/// </summary>
[SecEntity(Crud = SecCrud.All)]
public sealed class TestProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// A read-only test entity (Create/Update/Delete disabled).
/// </summary>
[SecEntity(Crud = SecCrud.Read)]
public sealed class ReadOnlyEntity
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}
