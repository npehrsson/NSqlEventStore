using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NSqlEventStore {
    internal class StreamContinuationToken : IStreamContinuationToken {
        private readonly Func<IDbConnection> _connectionFactory;
        private readonly Action<IDbCommand> _dbCommandPreparer;
        private readonly string _query;
        private readonly StreamHeaderLookup _streamMetalookup = new StreamHeaderLookup();
        private const string QueryEventCommandText = @"
SELECT
    EventId,
    StreamId,
    EventType,
    EventData,
    StreamPosition,
    StorePosition,
    CreatedEpoch
FROM Events
{0}
ORDER BY {1} ASC
OFFSET @skip ROWS
FETCH NEXT @take ROWS ONLY";

        public StreamContinuationToken(Func<IDbConnection> connectionFactory, long startPosition, string orderByColumn) {
            _connectionFactory = connectionFactory;
            CurrentPosition = startPosition;
            _query = string.Format(QueryEventCommandText, string.Empty, orderByColumn);
        }

        public StreamContinuationToken(Func<IDbConnection> connectionFactory, long startPosition, string orderByColumn, string whereExpression, Action<IDbCommand> dbCommandPreparer) {
            _connectionFactory = connectionFactory;
            _dbCommandPreparer = dbCommandPreparer;
            CurrentPosition = startPosition;
            _query = string.Format(QueryEventCommandText, whereExpression, orderByColumn);
        }

        public int Take { get; set; } = 100;
        public long CurrentPosition { get; private set; }

        public bool HasMore { get; private set; } = true;
        public IEnumerable<RecordedEvent> GetNext() {
            var results = new List<RecordedEvent>();

            using (var connection = _connectionFactory()) {
                connection.Open();

                using (var command = connection.CreateCommand()) {
                    command.CommandText = _query;
                    _dbCommandPreparer?.Invoke(command);
                    command.AddParameter("@skip", DbType.Int64, CurrentPosition);
                    command.AddParameter("@take", DbType.Int64, Take);

                    using (var reader = command.ExecuteReader()) {
                        while (reader.Read()) {
                            results.Add(Map(reader));
                        }
                    }
                }

                _streamMetalookup.LoadStreams(connection, results.Select(x => x.StreamId).Distinct().ToList());
            }

            foreach (var item in results) {
                item.StreamHeader = _streamMetalookup[item.StreamId];
            }

            HasMore = results.Count == Take;
            CurrentPosition = CurrentPosition + results.Count;

            return results;
        }

        private static RecordedEvent Map(IDataRecord dataRecord) {
            return new RecordedEvent(
                dataRecord.GetGuid(0),
                dataRecord.GetGuid(1),
                new Position(dataRecord.GetInt32(4), dataRecord.GetInt32(5)),
                dataRecord.GetString(2),
                (byte[])dataRecord.GetValue(3),
                dataRecord.GetInt32(6));
        }
    }
}
