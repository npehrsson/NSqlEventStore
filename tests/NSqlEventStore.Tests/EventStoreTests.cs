﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NSqlEventStore.Tests {
    public class EventStoreTests {
        private const string AppVeyorConenctionString = @"Server=(local)\SQL2016;Database=EventSourcingTests;User ID=sa;Password=Password12!";
        private const string LocalConnectionString = "Server=(local);Database=EventSourcingTests;Trusted_Connection=True;";

        private static string ConnectionString => Environment.GetEnvironmentVariable("APPVEYOR") == "True" ? AppVeyorConenctionString : LocalConnectionString;

        [Fact]
        public void Append_OneEvent_Succeds() {
            var eventStore = CreateEventStore();

            var streamId = Guid.Parse("0FD6214D-A2A4-4898-BFD8-5B89678B387E");

            eventStore.Append(streamId, ExpectedVersion.Any, new EventData() {
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
        public void WriteStreamMetaData() {
            var eventStore = CreateEventStore();

            var streamId = Guid.Parse("0FD6214D-A2A4-4898-BFD8-5B89678B387E");

            eventStore.SetStreamMetaData(streamId, new Dictionary<string, string>() {
                {
                    "Name", "Niclas"
                }});
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

        [Fact(Skip = "To heavy")]
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
            SqlManager.Recreate(ConnectionString);
            SchemaUpdater.ResetCachedUpdater();
            return new EventStore(ConnectionString);
        }
    }
}
