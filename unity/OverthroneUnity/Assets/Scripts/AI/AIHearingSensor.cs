using UnityEngine;

namespace Overthrone
{
    public sealed class AIHearingSensor : MonoBehaviour
    {
        [SerializeField] private float hearingMultiplier = 1f;
        [SerializeField] private float memorySeconds = 4f;

        private float memoryTimer;

        public bool HasHeardNoise => memoryTimer > 0f;
        public Vector3 LastHeardPosition { get; private set; }
        public GameObject LastHeardSource { get; private set; }

        private void OnEnable()
        {
            NoiseSystem.NoiseEmitted += OnNoiseEmitted;
        }

        private void OnDisable()
        {
            NoiseSystem.NoiseEmitted -= OnNoiseEmitted;
        }

        private void Update()
        {
            memoryTimer = Mathf.Max(0f, memoryTimer - Time.deltaTime);
        }

        private void OnNoiseEmitted(NoiseEvent noiseEvent)
        {
            if (noiseEvent.Source == gameObject)
            {
                return;
            }

            var maxDistance = noiseEvent.Radius * hearingMultiplier;
            if (Vector3.Distance(transform.position, noiseEvent.Position) > maxDistance)
            {
                return;
            }

            LastHeardSource = noiseEvent.Source;
            LastHeardPosition = noiseEvent.Position;
            memoryTimer = memorySeconds;
        }

        private void OnDrawGizmosSelected()
        {
            if (!HasHeardNoise)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(LastHeardPosition, 0.45f);
            Gizmos.DrawLine(transform.position, LastHeardPosition);
        }
    }
}
