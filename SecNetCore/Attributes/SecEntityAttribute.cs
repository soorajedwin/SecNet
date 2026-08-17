using SecNetCore.Models;

namespace SecNetCore.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class SecEntityAttribute : Attribute
    {
        public string? TableName { get; }

        public SecCrud Crud { get; set; } = SecCrud.All;

        public SecEntityAttribute(string? tableName = null)
        {
            TableName = tableName;
        }
    }
}
