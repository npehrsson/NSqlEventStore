using System;
using System.Data;
using System.Data.SqlClient;

namespace NSqlEventStore {
    public class EventStore {
        private readonly Func<IDbConnection> _connectionFactory;
        private readonly SchemaUpdater _schemaUpdater;
        private readonly EventAppender _appender;

        public EventStore(Func<IDbConnection> connectionFactory) {
            _connectionFactory = connectionFactory;
            _schemaUpdater = new SchemaUpdater(connectionFactory);
            _appender = new EventAppender(connectionFactory);
        }

        public EventStore(string connectionString) : this(() => new SqlConnection(connectionString)) {}

        public void Append(Guid streamId, long expectedVersion, params EventData[] events) {
            _schemaUpdater.Execute();
            _appender.Append(streamId, expectedVersion, events);
        }

        public IStreamContinuationToken GetAllStream(long startPosition) {
            _schemaUpdater.Execute();
            return new StreamContinuationToken(_connectionFactory, startPosition, "StorePosition");
        }

        public IStreamContinuationToken GetStream(Guid streamId, long startPosition) {
            _schemaUpdater.Execute();
            return new StreamContinuationToken(
                _connectionFactory,
                startPosition,
                "StreamPosition",
                "WHERE StreamId = @streamId",
                x => x.AddParameter("@streamId", streamId)
            );
        }

        // Todo
        //public void SetStreamMetaData(Guid streamId, IDictionary<string, string> metaData)
    }
}