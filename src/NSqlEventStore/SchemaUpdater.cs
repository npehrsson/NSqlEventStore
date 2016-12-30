using System;
using System.Data;
using System.Data.SqlClient;

namespace NSqlEventStore {
    public class SchemaUpdater {
        private readonly Func<IDbConnection> _connectionFactory;
        private const int ThereIsAlreadyAnObjectNamedXXXXInTheDatabase = 2714;
        private static bool _hasUpdated;
        private const string Schema = @"
IF OBJECT_ID('Events', 'U') IS NULL
BEGIN
    CREATE TABLE [Events](
	    [EventId] [uniqueidentifier] CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED NOT NULL,
	    [StreamId] [uniqueidentifier] NOT NULL,
	    [EventType] [nvarchar](250) NOT NULL,
	    [EventData] [varbinary](max) NOT NULL,
	    [StreamPosition] [int] NOT NULL,
	    [StorePosition] [bigint] IDENTITY(1,1) NOT NULL,
	    [CreatedEpoch] [bigint] NOT NULL
    )

    CREATE UNIQUE INDEX IX_StreamId_SequenceNumber ON [Events] ([StreamId], [StreamPosition])
END

IF OBJECT_ID('StreamMetaData', 'U') IS NULL
BEGIN
    CREATE TABLE [StreamMetaData](
	    [Id] [uniqueidentifier] CONSTRAINT [PK_StreamMetaData] PRIMARY KEY CLUSTERED NOT NULL,
	    [StreamId] [uniqueidentifier] NOT NULL,
	    [Key] [nvarchar](250) NOT NULL,
	    [Value] [nvarchar](450) NOT NULL
    )

    CREATE INDEX IX_StreamId ON [StreamMetaData] ([StreamId])
    CREATE INDEX IX_Key_Value ON [StreamMetaData] ([Key], [Value])
    CREATE UNIQUE INDEX IX_StreamId_Key ON [StreamMetaData] ([StreamId], [Key])
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