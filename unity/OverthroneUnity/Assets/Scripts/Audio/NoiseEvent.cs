using UnityEngine;

namespace Overthrone
{
    public readonly struct NoiseEvent
    {
        public NoiseEvent(GameObject source, Vector3 position, float radius, MovementState state)
        {
            Source = source;
            Position = position;
            Radius = radius;
            State = state;
        }

        public GameObject Source { get; }
        public Vector3 Position { get; }
        public float Radius { get; }
        public MovementState State { get; }
    }
}
