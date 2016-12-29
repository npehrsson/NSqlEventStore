using System;
using System.Collections;
using System.Collections.Generic;

namespace NSqlEventStore {
    internal class DoNotRetryStrategy : IRetryStrategy {
        public static IRetryStrategy Singleton { get; } = new DoNotRetryStrategy();

        public void Execute(Action action) {
            action();
        }
    }

    public class StreamHeader {
        public StreamHeader(Guid streamId, IDictionary<string, string> metaData) {
            StreamId = streamId;
            MetaData = metaData;
        }

        public Guid StreamId { get; }
        public IDictionary<string, string> MetaData { get; }
    }
}