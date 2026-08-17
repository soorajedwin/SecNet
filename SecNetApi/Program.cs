using SecNetApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register SecNet API services (result mapper, options).
// SecNetData services (database, CRUD, entity registry) are registered via AddSecData when
// a concrete data-backed host configures them. In this minimal bootstrap we register the
// API-layer services that do not require a database so the app starts cleanly.
builder.Services.AddSecApi(options =>
{
    options.EnableDevelopmentErrors = builder.Environment.IsDevelopment();
});

// Register authorization services so UseAuthorization middleware can be wired in.
// Actual authorization policies are added by the hosting application.
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

var app = builder.Build();

// Global exception handler — must be first in the pipeline.
app.UseSecExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map generic SecNet entity endpoints ( /api/sec/{entity} ).
// Full functionality requires ISecEntityRegistry and ISecCrudService registered via AddSecData.
app.MapSecEndpoints();

app.Run();

// Expose Program for WebApplicationFactory in tests.
public partial class Program { }

