using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace NSqlEventStore {
    internal class EventAppender {
        private readonly Func<IDbConnection> _connectionFactory;
        private const int MaxBatchSize = 500;

        public EventAppender(Func<IDbConnection> connectionFactory) {
            _connectionFactory = connectionFactory;
        }

        public void Append(Guid streamId, long expectedVersion, params EventData[] events) {
            var retryStrategy = expectedVersion == ExpectedVersion.Any
                ? new UniqueIndexViolationRetryStrategy(5)
                : DoNotRetryStrategy.Singleton;

            ExecuteCommand(connection => {
                var commands = new List<IDbCommand>();
                var processedEvents = 0;

                while (events.Length > processedEvents) {
                    var command = connection.CreateCommand();
                    AppendStreamId(command, streamId);

                    if (expectedVersion == ExpectedVersion.Any) {
                        PrepareCommandWithCurrentSequenceNumber(command, streamId);
                    }
                    else {
                        PrepareCommandWithExplicitSetSequenceNumber(command, expectedVersion);
                    }

                    PrepareCommandWithEvents(command, events
                        .Skip(processedEvents)
                        .Take(MaxBatchSize)
                        .ToList());

                    processedEvents = processedEvents + MaxBatchSize;

                    commands.Add(command);
                }

                return commands;
            }, retryStrategy, events.Length > 1);
        }

        private void ExecuteCommand(Func<IDbConnection, IList<IDbCommand>> commandBuilder, IRetryStrategy retryStrategy, bool wrapInTransaction) {
            try {
                using (var connection = _connectionFactory()) {
                    var commands = commandBuilder(connection);
                    connection.Open();

                    retryStrategy.Execute(() => {
                        if (!wrapInTransaction) {
                            commands.First().ExecuteNonQuery();
                            return;
                        }

                        using (var transaction = connection.BeginTransaction()) {
                            foreach (var command in commands) {
                                command.Transaction = transaction;
                                command.ExecuteNonQuery();
                            }
                            transaction.Commit();
                        }
                    });
                }
            }
            catch (SqlException e) {
                if (ExpectedVersionExceptionIdentifier.IsExpectedVersionException(e)) {
                    throw new WrongExpectedVersionException();
                }

                throw;
            }
        }

        private const string EventInsertValueFormat =
           "(@eventId_{0}, @streamId, @StreamPosition + @eventNumber_{0}, @eventType_{0}, @eventData_{0}, @createdEpoch)";

        public static void AppendStreamId(IDbCommand command, Guid streamId) {
            command.AddParameter("@streamId", streamId);
        }

        public static void PrepareCommandWithCurrentSequenceNumber(IDbCommand command, Guid streamId) {
            command.CommandText +=
                @"
DECLARE @StreamPosition INT = (SELECT MAX(StreamPosition) FROM Events WHERE StreamId = @streamId);
SET @StreamPosition = CASE WHEN @StreamPosition IS NULL THEN 0 ELSE @StreamPosition END;";
        }

        public static void PrepareCommandWithExplicitSetSequenceNumber(IDbCommand command, long expected) {
            command.AddParameter("@StreamPosition", DbType.Int64, expected);
        }

        public static void PrepareCommandWithEvents(IDbCommand command, IList<EventData> events) {
            command.AddParameter("@createdEpoch", DbType.Int64, GetEpoch());
            command.CommandText += "INSERT INTO Events(EventId, StreamId, StreamPosition, EventType, EventData, CreatedEpoch) SELECT * FROM (VALUES";

            var valueInserts = new List<string>();

            for (var i = 0; i < events.Count; i++) {
                var @event = events[i];
                valueInserts.Add(string.Format(EventInsertValueFormat, i));

                command.AddParameter("@eventId_" + i, @event.EventId);
                command.AddParameter("@eventType_" + i, @event.EventType);
                command.AddParameter("@eventData_" + i, DbType.Binary, @event.Data);
                command.AddParameter("@eventNumber_" + i, DbType.Int64, i + 1);
            }

            command.CommandText += " " + string.Join(", ", valueInserts) + ") events(eventid, streamid, streamposition, eventtype, eventdata, createdepoch) order by streamposition asc;";
        }

        private static readonly DateTimeOffset DateTimeOffsetStartTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan());

        private static long GetEpoch() {
            var epochTimeSpan = DateTime.UtcNow - DateTimeOffsetStartTime;
            return epochTimeSpan.Milliseconds;
        }
    }
}