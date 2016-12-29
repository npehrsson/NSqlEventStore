using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NSqlEventStore {
    public class StreamHeaderLookup {
        private readonly IDictionary<Guid, StreamHeader> _metaDataCache;

        public StreamHeaderLookup() {
            _metaDataCache = new Dictionary<Guid, StreamHeader>();
        }

        public StreamHeader this[Guid value] => _metaDataCache[value];

        public void LoadStreams(IDbConnection connection, IList<Guid> requiredStreamIds) {
            var notLoaded = new List<Guid>();

            foreach (var streamId in requiredStreamIds) {
                if (!_metaDataCache.ContainsKey(streamId)) {
                    notLoaded.Add(streamId);
                }
            }

            if (!notLoaded.Any()) {
                return;
            }

            using (var command = connection.CreateCommand()) {
                command.CommandText = "SELECT StreamId, [Key], [Value] FROM StreamMetaData WHERE StreamId IN ('" + string.Join("', '", notLoaded) + "') ORDER BY StreamId";

                using (var reader = command.ExecuteReader()) {
                    FillCache(reader);
                }
            }

            foreach (var streamId in notLoaded) {
                if (!_metaDataCache.ContainsKey(streamId)) {
                    _metaDataCache.Add(streamId, new StreamHeader(streamId, new Dictionary<string, string>()));
                }
            }
        }

        private void FillCache(IDataReader reader) {
            var currentStreamId = Guid.Empty;
            var dictionary = new Dictionary<string, string>();

            while (reader.Read()) {
                var streamId = reader.GetGuid(0);

                if (streamId != currentStreamId) {
                    if (currentStreamId != Guid.Empty) {
                        _metaDataCache.Add(currentStreamId, new StreamHeader(currentStreamId, dictionary));
                        dictionary = new Dictionary<string, string>();
                    }
                    currentStreamId = streamId;
                }

                dictionary.Add(reader.GetString(1), reader.GetString(2));
            }

            if (currentStreamId != Guid.Empty) {
                _metaDataCache.Add(currentStreamId, new StreamHeader(currentStreamId, dictionary));
            }
        }
    }
}