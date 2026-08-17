using MySqlConnector;
using SecNetData.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecNetData.Services
{
    public sealed class SecConnectionStringBuilder
    {
        public string BuildServerConnectionString(
            SecDatabaseOptions options)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = options.Server,
                Port = options.Port,
                UserID = options.Username,
                Password = options.Password,

                CharacterSet = options.Charset,

                AllowUserVariables = true,
                SslMode = MySqlSslMode.Preferred
            };

            return builder.ConnectionString;
        }

        public string BuildDatabaseConnectionString(
            SecDatabaseOptions options)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = options.Server,
                Port = options.Port,
                UserID = options.Username,
                Password = options.Password,

                Database = options.DatabaseName,

                CharacterSet = options.Charset,

                AllowUserVariables = true,
                SslMode = MySqlSslMode.Preferred
            };

            return builder.ConnectionString;
        }
    }
}
