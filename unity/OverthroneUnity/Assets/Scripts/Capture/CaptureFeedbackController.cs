using UnityEngine;

namespace Overthrone
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class CaptureFeedbackController : MonoBehaviour
    {
        [SerializeField] private float effectLifetime = 0.65f;
        [SerializeField] private float effectScale = 1.4f;
        [SerializeField] private float volume = 0.7f;

        private AudioSource audioSource;
        private Material particleMaterial;

        public int PlayedFeedbackCount { get; private set; }
        public CaptureFeedbackEvent LastFeedback { get; private set; }
        public GameObject LastEffect { get; private set; }

        public void PlayFeedback(CaptureFeedbackEvent feedbackEvent)
        {
            EnsureReferences();
            LastFeedback = feedbackEvent;
            PlayedFeedbackCount += 1;
            LastEffect = CreateEffect(feedbackEvent);
            audioSource.PlayOneShot(CreateProceduralClip(feedbackEvent.Type), volume);
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            CaptureFeedbackSystem.FeedbackEmitted += PlayFeedback;
        }

        private void OnDisable()
        {
            CaptureFeedbackSystem.FeedbackEmitted -= PlayFeedback;
        }

        private void EnsureReferences()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.35f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }

        private GameObject CreateEffect(CaptureFeedbackEvent feedbackEvent)
        {
            var effect = new GameObject($"Capture Feedback {feedbackEvent.Type}");
            effect.transform.position = feedbackEvent.Position;
            var particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = 0.18f;
            main.loop = false;
            main.startLifetime = effectLifetime;
            main.startSpeed = SpeedFor(feedbackEvent.Type);
            main.startSize = SizeFor(feedbackEvent.Type);
            main.startColor = ColorFor(feedbackEvent.Type);

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, CountFor(feedbackEvent.Type)) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f * effectScale;

            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetParticleMaterial();
            particles.Play();

            if (Application.isPlaying)
            {
                Destroy(effect, effectLifetime + 0.2f);
            }

            return effect;
        }

        private Material GetParticleMaterial()
        {
            if (particleMaterial != null)
            {
                return particleMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            particleMaterial = new Material(shader);
            return particleMaterial;
        }

        private static Color ColorFor(CaptureFeedbackType type)
        {
            return type switch
            {
                CaptureFeedbackType.TackleHit => new Color(1f, 0.34f, 0.2f, 1f),
                CaptureFeedbackType.TackleMiss => new Color(0.75f, 0.85f, 1f, 0.75f),
                CaptureFeedbackType.Rescue => new Color(0.35f, 1f, 0.7f, 1f),
                CaptureFeedbackType.FinalCapture => new Color(1f, 0.9f, 0.18f, 1f),
                CaptureFeedbackType.HolderInterrupted => new Color(1f, 0.55f, 0.1f, 1f),
                CaptureFeedbackType.SlimeEscape => new Color(0.2f, 1f, 0.35f, 1f),
                _ => Color.white
            };
        }

        private static float SizeFor(CaptureFeedbackType type)
        {
            return type == CaptureFeedbackType.FinalCapture ? 0.48f : 0.32f;
        }

        private static float SpeedFor(CaptureFeedbackType type)
        {
            return type == CaptureFeedbackType.TackleMiss ? 1.6f : 2.6f;
        }

        private static short CountFor(CaptureFeedbackType type)
        {
            return type == CaptureFeedbackType.FinalCapture ? (short)30 : (short)18;
        }

        private static AudioClip CreateProceduralClip(CaptureFeedbackType type)
        {
            const int sampleRate = 22050;
            var duration = type == CaptureFeedbackType.FinalCapture ? 0.28f : 0.16f;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            var frequency = FrequencyFor(type);
            for (var i = 0; i < sampleCount; i += 1)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Exp(-DecayFor(type) * t);
                var tone = Mathf.Sin(2f * Mathf.PI * frequency * t);
                var overtone = Mathf.Sin(2f * Mathf.PI * frequency * 1.85f * t) * 0.42f;
                samples[i] = (tone + overtone) * envelope * 0.45f;
            }

            var clip = AudioClip.Create($"CaptureFeedback_{type}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float FrequencyFor(CaptureFeedbackType type)
        {
            return type switch
            {
                CaptureFeedbackType.TackleHit => 96f,
                CaptureFeedbackType.TackleMiss => 260f,
                CaptureFeedbackType.Rescue => 520f,
                CaptureFeedbackType.FinalCapture => 74f,
                CaptureFeedbackType.HolderInterrupted => 130f,
                CaptureFeedbackType.SlimeEscape => 180f,
                _ => 220f
            };
        }

        private static float DecayFor(CaptureFeedbackType type)
        {
            return type == CaptureFeedbackType.FinalCapture ? 8f : 18f;
        }
    }
}
