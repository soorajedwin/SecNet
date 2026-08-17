using SecNetCore.Attributes;
using System.Reflection;

namespace SecNetData.Services
{
    public sealed class SecEntityDiscovery
    {
        private readonly Assembly[] _assemblies;

        public SecEntityDiscovery(params Assembly[] assemblies)
        {
            _assemblies = assemblies
                .Distinct()
                .ToArray();
        }

        public IReadOnlyList<Type> GetEntities()
        {
            return _assemblies
                .SelectMany(GetLoadableTypes)
                .Where(type =>
                    type is { IsClass: true, IsAbstract: false } &&
                    type.GetCustomAttribute<SecEntityAttribute>() != null)
                .Distinct()
                .ToList();
        }

        private static IEnumerable<Type> GetLoadableTypes(
            Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types
                    .Where(type => type != null)
                    .Cast<Type>();
            }
        }
    }
}
