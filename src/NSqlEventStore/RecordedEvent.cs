using System;
using System.Collections.Generic;

namespace NSqlEventStore {
    public class RecordedEvent {
        public RecordedEvent(Guid eventId, Guid streamId, Position position, string eventType, byte[] data, long createdEpoch) {
            EventId = eventId;
            StreamId = streamId;
            Position = position;
            EventType = eventType;
            Data = data;
            CreatedEpoch = createdEpoch;
        }

        public Guid EventId { get; }
        public Guid StreamId { get; }
        public Position Position { get; }
        public string EventType { get; }
        public byte[] Data { get; }
        public long CreatedEpoch { get; }
        public StreamHeader StreamHeader { get; internal set; }
    }
}