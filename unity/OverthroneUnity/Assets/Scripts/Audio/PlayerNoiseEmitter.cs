using UnityEngine;

namespace Overthrone
{
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerNoiseEmitter : MonoBehaviour
    {
        [SerializeField] private AudioClip footstepClip;
        [SerializeField] private float footstepVolume = 0.42f;
        [SerializeField] private float footstepPitch = 1f;
        [SerializeField] private float minimumMoveSpeed = 0.5f;
        [SerializeField] private float groundProbeOriginOffset = 0.2f;
        [SerializeField] private float groundProbeDistance = 0.35f;

        private PlayerMotor motor;
        private CharacterController controller;
        private AudioSource audioSource;
        private readonly RaycastHit[] groundHits = new RaycastHit[4];
        private float stepTimer;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            controller = GetComponent<CharacterController>();
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
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            var profile = motor.CurrentProfile;
            var shouldEmitNoise = motor.IsSprinting && motor.CurrentHorizontalSpeed > minimumMoveSpeed && profile.noiseRadius > 0f;
            if (!shouldEmitNoise)
            {
                stepTimer = 0f;
                return;
            }

            stepTimer -= deltaTime;
            if (stepTimer > 0f)
            {
                return;
            }

            var surfaceMultipliers = ResolveSurface();
            var noiseRadius = profile.noiseRadius * surfaceMultipliers.NoiseRadiusScale;
            stepTimer = Mathf.Max(0.08f, profile.footstepInterval);
            audioSource.maxDistance = noiseRadius;
            audioSource.pitch = footstepPitch * surfaceMultipliers.PitchScale;
            audioSource.PlayOneShot(footstepClip, footstepVolume * surfaceMultipliers.VolumeScale);
            NoiseSystem.Emit(new NoiseEvent(gameObject, transform.position, noiseRadius, motor.State));
        }

        public FootstepSurfaceMultipliers ResolveSurface()
        {
            var groundSurface = ResolveGroundSurface();
            return groundSurface != null
                ? groundSurface.Multipliers
                : new FootstepSurfaceMultipliers(1f, 1f, 1f);
        }

        private FootstepSurface ResolveGroundSurface()
        {
            if (controller != null && !controller.isGrounded)
            {
                return null;
            }

            var origin = transform.position + Vector3.up * Mathf.Max(0f, groundProbeOriginOffset);
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                Mathf.Max(0f, groundProbeOriginOffset + groundProbeDistance),
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            Collider closestCollider = null;
            var closestDistance = float.PositiveInfinity;

            for (var index = 0; index < hitCount; index += 1)
            {
                var hit = groundHits[index];
                if (hit.collider == null || hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hit.distance >= closestDistance)
                {
                    continue;
                }

                closestCollider = hit.collider;
                closestDistance = hit.distance;
            }

            return closestCollider != null ? closestCollider.GetComponentInParent<FootstepSurface>() : null;
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
