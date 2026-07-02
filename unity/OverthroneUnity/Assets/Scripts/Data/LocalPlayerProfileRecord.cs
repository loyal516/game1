namespace Overthrone
{
    public readonly struct LocalPlayerProfileRecord
    {
        public LocalPlayerProfileRecord(
            string id,
            string displayName,
            int mmr,
            string rankTier,
            string createdAt,
            string lastSeenAt)
        {
            Id = id;
            DisplayName = displayName;
            Mmr = mmr;
            RankTier = rankTier;
            CreatedAt = createdAt;
            LastSeenAt = lastSeenAt;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int Mmr { get; }
        public string RankTier { get; }
        public string CreatedAt { get; }
        public string LastSeenAt { get; }
    }
}
