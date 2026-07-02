namespace Overthrone
{
    public readonly struct LocalMatchFlowEvent
    {
        public LocalMatchFlowEvent(
            LocalMatchFlowEventType type,
            TeamId team,
            TeamId defenderTeam,
            float remainingSeconds)
        {
            Type = type;
            Team = team;
            DefenderTeam = defenderTeam;
            RemainingSeconds = remainingSeconds;
        }

        public LocalMatchFlowEventType Type { get; }
        public TeamId Team { get; }
        public TeamId DefenderTeam { get; }
        public float RemainingSeconds { get; }
    }
}
