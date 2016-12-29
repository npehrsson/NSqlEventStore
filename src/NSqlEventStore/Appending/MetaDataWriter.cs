using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NSqlEventStore {
    public class MetaDataWriter {
        private readonly Func<IDbConnection> _connectionFactory;

        public MetaDataWriter(Func<IDbConnection> connectionFactory) {
            _connectionFactory = connectionFactory;
        }

        public void Write(Guid streamId, IDictionary<string, string> metaData) {
            using (var connection = _connectionFactory()) {
                using (var command = connection.CreateCommand()) {
                    command.AddParameter("@streamId", streamId);
                    command.CommandText = "DELETE FROM StreamMetaData WHERE StreamId = @streamId;";
                    AppendMetaDataInsertToCommand(command, metaData);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AppendMetaDataInsertToCommand(IDbCommand command, IDictionary<string, string> metaData) {
            if (!metaData.Any()) {
                return;
            }

            command.CommandText += "INSERT INTO StreamMetaData(Id, StreamId, [Key], [Value]) VALUES ";
            var i = 0;
            var values = new List<string>();

            foreach (var keyValuePair in metaData) {
                values.Add($"(NEWID(), @streamId, @key_{i}, @value_{i})");
                command.AddParameter("@key_" + i, keyValuePair.Key);
                command.AddParameter("@value_" + i, keyValuePair.Value);
                i = i + 1;
            }

            command.CommandText += string.Join(", ", values) + ";";
        }
    }
}