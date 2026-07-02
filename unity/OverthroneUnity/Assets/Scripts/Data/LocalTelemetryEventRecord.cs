namespace Overthrone
{
    public readonly struct LocalTelemetryEventRecord
    {
        public LocalTelemetryEventRecord(
            string eventId,
            string matchId,
            string profileId,
            string eventName,
            string occurredAt,
            string payloadJson)
        {
            EventId = eventId;
            MatchId = matchId;
            ProfileId = profileId;
            EventName = eventName;
            OccurredAt = occurredAt;
            PayloadJson = payloadJson;
        }

        public string EventId { get; }
        public string MatchId { get; }
        public string ProfileId { get; }
        public string EventName { get; }
        public string OccurredAt { get; }
        public string PayloadJson { get; }
    }
}
