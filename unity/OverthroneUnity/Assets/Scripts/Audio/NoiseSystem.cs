using System;

namespace Overthrone
{
    public static class NoiseSystem
    {
        public static event Action<NoiseEvent> NoiseEmitted;

        public static void Emit(NoiseEvent noiseEvent)
        {
            NoiseEmitted?.Invoke(noiseEvent);
        }
    }
}
