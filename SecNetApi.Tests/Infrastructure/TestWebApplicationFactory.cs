using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SecNetApi.Configuration;
using SecNetApi.Extensions;
using SecNetApi.Services;
using SecNetData.Crud;
using SecNetData.Schema;

namespace SecNetApi.Tests.Infrastructure;

/// <summary>
/// WebApplicationFactory that replaces data-layer services with mocks,
/// enabling endpoint tests without a database connection.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<ISecCrudService> CrudServiceMock { get; } = new();
    public Mock<ISecEntityRegistry> RegistryMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove any existing registry / crud service registrations
            RemoveRegistration<ISecEntityRegistry>(services);
            RemoveRegistration<ISecCrudService>(services);
            RemoveRegistration<SecApiOptions>(services);
            RemoveRegistration<SecHttpResultMapper>(services);

            // Register mocks
            services.AddSingleton(RegistryMock.Object);
            services.AddScoped(_ => CrudServiceMock.Object);

            // Register API layer services
            services.AddSecApi(opt => opt.EnableDevelopmentErrors = true);
        });

        builder.UseEnvironment("Development");
    }

    /// <summary>
    /// Configures the registry mock to recognize TestProduct and ReadOnlyEntity.
    /// </summary>
    public void SetupDefaultRegistry()
    {
        var testProductMeta = SecEntityMetadata.FromEntityType(typeof(TestProduct));
        var readOnlyMeta = SecEntityMetadata.FromEntityType(typeof(ReadOnlyEntity));

        // TestProduct
        RegistryMock
            .Setup(r => r.TryGetEntityType("TestProduct", out It.Ref<Type?>.IsAny))
            .Returns((string _, out Type? t) => { t = typeof(TestProduct); return true; });

        RegistryMock
            .Setup(r => r.GetEntityMetadata(typeof(TestProduct)))
            .Returns(testProductMeta);

        RegistryMock
            .Setup(r => r.GetPrimaryKeyProperty(typeof(TestProduct)))
            .Returns(testProductMeta.KeyProperty!);

        RegistryMock
            .Setup(r => r.IsOperationAllowed(typeof(TestProduct), It.IsAny<SecNetCore.Models.SecCrud>()))
            .Returns(true);

        // ReadOnlyEntity
        RegistryMock
            .Setup(r => r.TryGetEntityType("ReadOnlyEntity", out It.Ref<Type?>.IsAny))
            .Returns((string _, out Type? t) => { t = typeof(ReadOnlyEntity); return true; });

        RegistryMock
            .Setup(r => r.GetEntityMetadata(typeof(ReadOnlyEntity)))
            .Returns(readOnlyMeta);

        RegistryMock
            .Setup(r => r.GetPrimaryKeyProperty(typeof(ReadOnlyEntity)))
            .Returns(readOnlyMeta.KeyProperty!);

        RegistryMock
            .Setup(r => r.IsOperationAllowed(typeof(ReadOnlyEntity), It.IsAny<SecNetCore.Models.SecCrud>()))
            .Returns(false);

        // Unknown entities
        RegistryMock
            .Setup(r => r.TryGetEntityType(
                It.Is<string>(s => s != "TestProduct" && s != "ReadOnlyEntity"),
                out It.Ref<Type?>.IsAny))
            .Returns((string _, out Type? t) => { t = null; return false; });
    }

    private static void RemoveRegistration<T>(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
            services.Remove(descriptor);
    }
}
