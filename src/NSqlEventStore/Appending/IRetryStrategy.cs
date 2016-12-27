using System;

namespace NSqlEventStore {
    internal interface IRetryStrategy {
        void Execute(Action action);
    }
}