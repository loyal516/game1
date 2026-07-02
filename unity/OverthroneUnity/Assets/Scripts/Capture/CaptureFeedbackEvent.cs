using UnityEngine;

namespace Overthrone
{
    public readonly struct CaptureFeedbackEvent
    {
        public CaptureFeedbackEvent(CaptureFeedbackType type, GameObject source, GameObject target, Vector3 position)
        {
            Type = type;
            Source = source;
            Target = target;
            Position = position;
        }

        public CaptureFeedbackType Type { get; }
        public GameObject Source { get; }
        public GameObject Target { get; }
        public Vector3 Position { get; }
    }
}
