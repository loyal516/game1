using UnityEngine;

namespace Overthrone
{
    public readonly struct FootstepSurfaceMultipliers
    {
        public FootstepSurfaceMultipliers(float volumeScale, float pitchScale, float noiseRadiusScale)
        {
            VolumeScale = Mathf.Max(0f, volumeScale);
            PitchScale = Mathf.Max(0.01f, pitchScale);
            NoiseRadiusScale = Mathf.Max(0f, noiseRadiusScale);
        }

        public float VolumeScale { get; }
        public float PitchScale { get; }
        public float NoiseRadiusScale { get; }
    }

    public sealed class FootstepSurface : MonoBehaviour
    {
        [SerializeField] private float volumeMultiplier = 1f;
        [SerializeField] private float pitchMultiplier = 1f;
        [SerializeField] private float noiseRadiusMultiplier = 1f;

        public float VolumeMultiplier
        {
            get => volumeMultiplier;
            set => volumeMultiplier = value;
        }

        public float PitchMultiplier
        {
            get => pitchMultiplier;
            set => pitchMultiplier = value;
        }

        public float NoiseRadiusMultiplier
        {
            get => noiseRadiusMultiplier;
            set => noiseRadiusMultiplier = value;
        }

        public FootstepSurfaceMultipliers Multipliers =>
            new(volumeMultiplier, pitchMultiplier, noiseRadiusMultiplier);
    }
}
