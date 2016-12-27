using System;

namespace NSqlEventStore {
    public class EventData {
        public EventData() {
            EventId = Guid.NewGuid();
        }

        public Guid EventId { get; }
        public string EventType { get; set; }
        public byte[] Data { get; set; }
    }
}