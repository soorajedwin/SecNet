namespace SecNetData.Configuration
{
    public sealed class SecDatabaseOptions
    {
        public string ApplicationName { get; set; } = string.Empty;

        public string Server { get; set; } = "localhost";

        public uint Port { get; set; } = 3306;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string DatabaseName { get; set; } = string.Empty;

        public string Charset { get; set; } = "utf8mb4";

        public string Collation { get; set; } = "utf8mb4_unicode_ci";
    }
}
