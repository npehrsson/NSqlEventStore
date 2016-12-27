using System.Collections.Generic;

namespace NSqlEventStore {
    public interface IStreamContinuationToken {
        long CurrentPosition { get; }
        bool HasMore { get; }
        IEnumerable<RecordedEvent> GetNext();
    }
}