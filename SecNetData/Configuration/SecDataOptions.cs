using System.Reflection;

namespace SecNetData.Configuration
{
    public sealed class SecDataOptions
    {
        public SecDatabaseOptions Database { get; set; } = new();

        public Assembly[] EntityAssemblies { get; set; } = [];
    }
}
