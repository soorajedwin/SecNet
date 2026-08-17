namespace SecNetCore.Models
{
    [Flags]
    public enum SecCrud
    {
        None = 0,
        Create = 1,
        Read = 2,
        Update = 4,
        Delete = 8,

        ReadWrite = Create | Read | Update,
        All = Create | Read | Update | Delete
    }
}
