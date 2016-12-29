using System;
using System.Data;

namespace NSqlEventStore {
    internal static class DbExtensions {
        public static long ExecuteNonQuery(this IDbConnection connection, string commandText) {
            using (var command = connection.CreateCommand()) {
                command.CommandText = commandText;
                return command.ExecuteNonQuery();
            }
        }

        public static IDbCommand AddParameter(this IDbCommand command, string name, DbType dbType, object value) {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            parameter.DbType = dbType;

            command.Parameters.Add(parameter);
            return command;
        }

        public static IDbCommand AddParameter(this IDbCommand command, string name, Guid value) {
            return command.AddParameter(name, DbType.Guid, value);
        }

        public static IDbCommand AddParameter(this IDbCommand command, string name, byte[] value) {
            return command.AddParameter(name, DbType.Binary, value);
        }

        public static IDbCommand AddParameter(this IDbCommand command, string name, long value) {
            return command.AddParameter(name, DbType.Int64, value);
        }

        public static IDbCommand AddParameter(this IDbCommand command, string name, string value) {
            return command.AddParameter(name, DbType.String, value);
        }
    }
}