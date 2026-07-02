using UnityEngine;

namespace Overthrone
{
    public readonly struct LocalPingEvent
    {
        public LocalPingEvent(LocalPingType type, TeamId team, Vector3 position, string label, float duration)
        {
            Type = type;
            Team = team;
            Position = position;
            Label = string.IsNullOrWhiteSpace(label) ? type.ToString() : label;
            Duration = Mathf.Max(0f, duration);
        }

        public LocalPingType Type { get; }
        public TeamId Team { get; }
        public Vector3 Position { get; }
        public string Label { get; }
        public float Duration { get; }
    }
}
