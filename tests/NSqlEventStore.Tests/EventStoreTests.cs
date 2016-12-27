using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NSqlEventStore.Tests {
    public class EventStoreTests {
        private const string ConnectionString = "Server=(local);Database=EventSourcingTests;Trusted_Connection=True;";

        [Fact]
        public void Append_OneEvent_Succeds() {
            var eventStore = CreateEventStore();

            var streamId = Guid.Parse("0FD6214D-A2A4-4898-BFD8-5B89678B387E");

            eventStore.Append(streamId, 1, new EventData() {
                Data = Encoding.ASCII.GetBytes("Niclas is King, adasasdasf dfgdfgdf dgdfgdfg d gd d gdfgdf df "),
                EventType = "StringMessage"
            },
            new EventData() {
                Data = Encoding.ASCII.GetBytes("Niclas is King, adasasdasf dfgdfgdf dgdfgdfg d gd d gdfgdf df "),
                EventType = "StringMessage"
            });
        }

        [Fact]
        public void AllEventStream_OneEvent_Succeds() {
            var eventStore = CreateEventStore();

            var streamId = Guid.Parse("0FD6214D-A2A4-4898-BFD8-5B89678B387E");

            var stream = eventStore.GetAllStream(0);

            var results = new List<RecordedEvent>();
            while (stream.HasMore) {
                var items = stream.GetNext();
                results.AddRange(items);
            }
        }

        [Fact]
        public void SingleEventStream() {
            var eventStore = CreateEventStore();

            var streamId = Guid.Parse("0FD6214D-A2A4-4898-BFD8-5B89678B387E");

            var stream = eventStore.GetStream(streamId, 0);

            var results = new List<RecordedEvent>();
            while (stream.HasMore) {
                var items = stream.GetNext();
                results.AddRange(items);
            }
        }

        [Fact]
        public void LoadTest() {
            var eventStore = CreateEventStore();
            var streamId = Guid.Parse("0FD6214D-A2A4-4898-BFD8-5B89678B387E");

            Parallel.For(0, 10, x => {
                var events = new List<EventData>();

                for (var i = 0; i < 800; i++) {
                    events.Add(new EventData() {
                        Data = Encoding.ASCII.GetBytes("Niclas is King, adasasdasf dfgdfgdf dgdfgdfg d gd d gdfgdf df "),
                        EventType = "StringMessage"
                    });
                }

                eventStore.Append(streamId, ExpectedVersion.Any, events.ToArray());
            });
        }

        private EventStore CreateEventStore() {
            //SqlManager.Recreate(ConnectionString);
            return new EventStore(ConnectionString);
        }
    }
}
