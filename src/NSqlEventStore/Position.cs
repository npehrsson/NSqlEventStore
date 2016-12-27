namespace NSqlEventStore {
    public class Position {
        public Position(long streamPosition, long globalPosition) {
            StreamPosition = streamPosition;
            GlobalPosition = globalPosition;
        }

        public long StreamPosition { get; }
        public long GlobalPosition { get; }
    }
}