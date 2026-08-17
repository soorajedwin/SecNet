namespace SecNetCore.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SecColumnAttribute : Attribute
    {
        public string? Name { get; }

        public SecColumnAttribute(string? name = null)
        {
            Name = name;
        }
    }
}
