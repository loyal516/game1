using UnityEngine;

namespace Overthrone
{
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerNoiseEmitter : MonoBehaviour
    {
        [SerializeField] private AudioClip footstepClip;
        [SerializeField] private float footstepVolume = 0.42f;
        [SerializeField] private float minimumMoveSpeed = 0.5f;

        private PlayerMotor motor;
        private AudioSource audioSource;
        private float stepTimer;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;

            if (footstepClip == null)
            {
                footstepClip = CreateProceduralFootstep();
            }
        }

        private void Update()
        {
            var profile = motor.CurrentProfile;
            var shouldEmitNoise = motor.IsSprinting && motor.CurrentHorizontalSpeed > minimumMoveSpeed && profile.noiseRadius > 0f;
            if (!shouldEmitNoise)
            {
                stepTimer = 0f;
                return;
            }

            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f)
            {
                return;
            }

            stepTimer = Mathf.Max(0.08f, profile.footstepInterval);
            audioSource.maxDistance = profile.noiseRadius;
            audioSource.PlayOneShot(footstepClip, footstepVolume);
            NoiseSystem.Emit(new NoiseEvent(gameObject, transform.position, profile.noiseRadius, motor.State));
        }

        private static AudioClip CreateProceduralFootstep()
        {
            const int sampleRate = 22050;
            const float duration = 0.08f;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i += 1)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Exp(-42f * t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 120f * t) * envelope * 0.5f;
            }

            var clip = AudioClip.Create("ProceduralFootstep", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
