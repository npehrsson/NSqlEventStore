using System.Data.SqlClient;
using System.Linq;

namespace NSqlEventStore {
    internal static class ExpectedVersionExceptionIdentifier {
        private const string IndexName = "IX_StreamId_SequenceNumber";

        public static bool IsExpectedVersionException(SqlException exception) {
            return exception
                .Errors
                .Cast<SqlError>()
                .Any(e => e.Class == 14 && e.Number == 2601)
                   && exception.Message.Contains(IndexName);
        }
    }
}