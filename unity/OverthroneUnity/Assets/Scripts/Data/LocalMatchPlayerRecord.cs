namespace Overthrone
{
    public readonly struct LocalMatchPlayerRecord
    {
        public LocalMatchPlayerRecord(
            string matchId,
            string profileId,
            TeamId team,
            int captures,
            int rescues,
            int capturedCount,
            float pointContribution,
            int mmrChange,
            bool wasMvp)
        {
            MatchId = matchId;
            ProfileId = profileId;
            Team = team;
            Captures = captures;
            Rescues = rescues;
            CapturedCount = capturedCount;
            PointContribution = pointContribution;
            MmrChange = mmrChange;
            WasMvp = wasMvp;
        }

        public string MatchId { get; }
        public string ProfileId { get; }
        public TeamId Team { get; }
        public int Captures { get; }
        public int Rescues { get; }
        public int CapturedCount { get; }
        public float PointContribution { get; }
        public int MmrChange { get; }
        public bool WasMvp { get; }
    }
}
