using System;
using System.Data;
using System.Data.SqlClient;

namespace NSqlEventStore {
    internal class SchemaUpdater {
        private readonly Func<IDbConnection> _connectionFactory;
        private const int ThereIsAlreadyAnObjectNamedXXXXInTheDatabase = 2714;
        private static bool _hasUpdated;
        private const string Schema = @"
IF OBJECT_ID('Evemts', 'U') IS NULL
BEGIN
    CREATE TABLE [Events](
	    [EventId] [uniqueidentifier] CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED NOT NULL,
	    [StreamId] [uniqueidentifier] NOT NULL,
	    [EventType] [nvarchar](250) NOT NULL,
	    [EventData] [varbinary](max) NOT NULL,
	    [StreamPosition] [int] NOT NULL,
	    [StorePosition] [int] IDENTITY(1,1) NOT NULL,
	    [CreatedEpoch] [int] NOT NULL
    )

    CREATE UNIQUE INDEX IX_StreamId_SequenceNumber ON [Events] ([StreamId], [StreamPosition])
END";

        public SchemaUpdater(Func<IDbConnection> connectionFactory) {
            _connectionFactory = connectionFactory;
        }

        public void Execute() {
            if (_hasUpdated) {
                return;
            }
            try {
                using (var connection = _connectionFactory()) {
                    connection.Open();

                    using (var command = connection.CreateCommand()) {
                        command.CommandText = Schema;
                        command.ExecuteNonQuery();
                    }
                }

                _hasUpdated = true;
            }
            catch (SqlException e) when (e.Number == ThereIsAlreadyAnObjectNamedXXXXInTheDatabase) {
            }
        }
    }
}