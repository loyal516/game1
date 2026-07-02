namespace Overthrone
{
    public readonly struct LocalMatchRecord
    {
        public LocalMatchRecord(
            string id,
            string mode,
            string map,
            string startedAt,
            string endedAt,
            TeamId winningTeam,
            int durationSec)
        {
            Id = id;
            Mode = mode;
            Map = map;
            StartedAt = startedAt;
            EndedAt = endedAt;
            WinningTeam = winningTeam;
            DurationSec = durationSec;
        }

        public string Id { get; }
        public string Mode { get; }
        public string Map { get; }
        public string StartedAt { get; }
        public string EndedAt { get; }
        public TeamId WinningTeam { get; }
        public int DurationSec { get; }
    }
}
