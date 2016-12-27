using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace NSqlEventStore {
    internal class EventAppender {
        private readonly Func<IDbConnection> _connectionFactory;

        public EventAppender(Func<IDbConnection> connectionFactory) {
            _connectionFactory = connectionFactory;
        }

        public void Append(Guid streamId, long expectedVersion, params EventData[] events) {
            var retryStrategy = expectedVersion == ExpectedVersion.Any
                ? new UniqueIndexViolationRetryStrategy(5)
                : DoNotRetryStrategy.Singleton;

            ExecuteCommand(connection => {
                var command = connection.CreateCommand();

                AppendStreamId(command, streamId);

                if (expectedVersion == ExpectedVersion.Any) {
                    PrepareCommandWithCurrentSequenceNumber(command, streamId);
                }
                else {
                    PrepareCommandWithExplicitSetSequenceNumber(command, expectedVersion);
                }

                PrepareCommandWithEvents(command, events);

                return command;
            }, retryStrategy, events.Length > 1);
        }

        private void ExecuteCommand(Func<IDbConnection, IDbCommand> commandBuilder, IRetryStrategy retryStrategy, bool wrapInTransaction) {
            try {
                using (var connection = _connectionFactory()) {
                    using (var command = commandBuilder(connection)) {
                        connection.Open();

                        retryStrategy.Execute(() => {
                            if (wrapInTransaction) {
                                using (var transaction = connection.BeginTransaction()) {
                                    command.Transaction = transaction;
                                    transaction.Commit();
                                }
                            }
                            else {
                                command.ExecuteNonQuery();
                            }
                        });
                    }
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

        public static void PrepareCommandWithEvents(IDbCommand command, EventData[] events) {
            command.AddParameter("@createdEpoch", DbType.Int64, GetEpoch());
            command.CommandText += "INSERT INTO Events(EventId, StreamId, StreamPosition, EventType, EventData, CreatedEpoch) VALUES";

            var valueInserts = new List<string>();

            for (var i = 0; i < events.Length; i++) {
                var @event = events[i];
                valueInserts.Add(string.Format(EventInsertValueFormat, i));

                command.AddParameter("@eventId_" + i, @event.EventId);
                command.AddParameter("@eventType_" + i, @event.EventType);
                command.AddParameter("@eventData_" + i, DbType.Binary, @event.Data);
                command.AddParameter("@eventNumber_" + i, DbType.Int64, i + 1);
            }

            command.CommandText += " " + string.Join(", ", valueInserts) + ";";
        }

        private static readonly DateTimeOffset DateTimeOffsetStartTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan());

        private static long GetEpoch() {
            var epochTimeSpan = DateTime.UtcNow - DateTimeOffsetStartTime;
            return epochTimeSpan.Milliseconds;
        }
    }
}