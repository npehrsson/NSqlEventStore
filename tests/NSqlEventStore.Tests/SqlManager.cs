using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace NSqlEventStore.Tests
{
    public static class SqlManager {
        private const string DropDatabaseQuery = "ALTER DATABASE [{0}] SET OFFLINE WITH ROLLBACK IMMEDIATE ALTER DATABASE [{0}] SET ONLINE WITH ROLLBACK IMMEDIATE; DROP DATABASE [{0}]";
        private const string ConnectionStringTemplate = "DATA SOURCE={0};USER ID={1};PASSWORD={2};pooling=false";
        private const string CreateDatabaseQueryTemplate = "CREATE DATABASE [{0}]";

        private const string ContainsDatabaseQueryTemplate = "SELECT COUNT(*) FROM sys.databases WHERE name = '{0}'";

        public static bool Contains(string connectionString, string databaseName) {
            using (var dbConnection = new SqlConnection(connectionString)) {
                dbConnection.Open();
                using (var command = dbConnection.CreateCommand()) {
                    command.CommandText = string.Format(CultureInfo.InvariantCulture, ContainsDatabaseQueryTemplate, databaseName);
                    return command.ExecuteScalar().ToString() == "1";
                }
            }
        }

        public static void Create(string connectionString, string databaseName) {
            RunCommand(connectionString, string.Format(CultureInfo.InvariantCulture, CreateDatabaseQueryTemplate, databaseName));
            SqlConnection.ClearAllPools();
        }


        public static void RemoveIfExisting(string connectionString, string databaseName) {
            if (!Contains(connectionString, databaseName)) {
                return;
            }

            RunCommand(connectionString, string.Format(CultureInfo.InvariantCulture, DropDatabaseQuery, databaseName));
        }


        private static void RunCommand(string connectionString, string commandText) {
            using (var dbConnection = new SqlConnection(connectionString)) {
                dbConnection.Open();
                RunCommand(dbConnection, commandText);
            }
        }


        private static void RunCommand(IDbConnection dbConnection, string commandText) {
            using (var command = dbConnection.CreateCommand()) {
                command.CommandText = commandText;
                command.ExecuteNonQuery();
            }
        }

        public static void Recreate(string connectionString, string database) {
            RemoveIfExisting(connectionString, database);
            Create(connectionString, database);
        }

        public static void Recreate(string connectionString) {
            ExecuteDbAction(connectionString, Recreate);
        }

        public static void Create(string connectionString) {
            ExecuteDbAction(connectionString, Create);
        }


        public static void RemoveIfExisting(string connectionString) {
            ExecuteDbAction(connectionString, RemoveIfExisting);
        }

        private static void ExecuteDbAction(string connectionString, Action<string, string> dbAction) {
            if (dbAction == null) throw new ArgumentNullException("dbAction");
            var info = SqlManageInfo.FromConnectionString(connectionString);
            dbAction(info.ConnectionStringWithoutInitialCatalog, info.DatabaseName);
        }

        private class SqlManageInfo {
            public string ConnectionString { get; set; }
            public string ConnectionStringWithoutInitialCatalog { get; set; }
            public string DatabaseName { get; set; }

            public static SqlManageInfo FromConnectionString(string connectionString) {
                var result = new SqlManageInfo { ConnectionString = connectionString };
                var builder = new SqlConnectionStringBuilder(connectionString);
                result.DatabaseName = builder.InitialCatalog;
                builder.InitialCatalog = string.Empty;
                result.ConnectionStringWithoutInitialCatalog = builder.ConnectionString;
                return result;
            }
        }
    }
}