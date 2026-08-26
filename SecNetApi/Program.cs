using SecNetApi.Extensions;
using SecNetCore.Entities;
using SecNetData.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSecData(options =>
{
    options.Database.ApplicationName = "SecTest";

    options.Database.Server = "localhost";
    options.Database.Port = 3306;
    options.Database.Username = "root";
    options.Database.Password = "mysql@123";
    options.Database.DatabaseName = "sectest";

    options.EntityAssemblies =
    [
        typeof(Customer).Assembly
    ];
});

builder.Services.AddSecApi(options =>
{
    options.EnableDevelopmentErrors =
        builder.Environment.IsDevelopment();
});

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseSecExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapSecEndpoints();

app.Run();

public partial class Program { }

