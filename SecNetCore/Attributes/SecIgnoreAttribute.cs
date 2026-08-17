namespace SecNetCore.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SecIgnoreAttribute : Attribute
    {
    }
}
