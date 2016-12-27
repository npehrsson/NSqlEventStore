using System;
using System.Data.SqlClient;

namespace NSqlEventStore {
    internal class UniqueIndexViolationRetryStrategy : IRetryStrategy {
        private readonly int _maxAttempts;
        private int _performedAttempts;

        public UniqueIndexViolationRetryStrategy(int maxAttempts) {
            _maxAttempts = maxAttempts;
        }

        public void Execute(Action action) {
            while (true) {
                try {
                    action();
                    return;
                }
                catch (SqlException e) {
                    if (ExpectedVersionExceptionIdentifier.IsExpectedVersionException(e) && _performedAttempts > _maxAttempts) {
                        throw;
                    }

                    _performedAttempts = _performedAttempts + 1;
                }
            }
        }
    }
}