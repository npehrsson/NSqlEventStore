using System;

namespace NSqlEventStore {
    internal class DoNotRetryStrategy : IRetryStrategy {
        public static IRetryStrategy Singleton { get; } = new DoNotRetryStrategy();

        public void Execute(Action action) {
            action();
        }
    }
}