using System.Collections.Generic;
using UnityEngine;

namespace Overthrone
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCaptureAgent))]
    public sealed class TackleHitbox : MonoBehaviour
    {
        private const int MaxHits = 32;

        [SerializeField] private float radius = 0.75f;
        [SerializeField] private float verticalOffset = 0.9f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        private readonly Collider[] hits = new Collider[MaxHits];
        private readonly HashSet<PlayerCaptureAgent> seenAgents = new HashSet<PlayerCaptureAgent>();

        public PlayerCaptureAgent FindBestTarget(PlayerCaptureAgent attacker)
        {
            if (attacker == null || attacker.Team == null)
            {
                return null;
            }

            Physics.SyncTransforms();
            var range = attacker.PersistentState == MovementState.King
                ? CaptureInteractionRules.KingTackleRange
                : CaptureInteractionRules.TackleRange;
            var start = attacker.transform.position + Vector3.up * Mathf.Max(0f, verticalOffset);
            var end = start + attacker.transform.forward * Mathf.Max(0f, range);
            var hitCount = Physics.OverlapCapsuleNonAlloc(
                start,
                end,
                Mathf.Max(0f, radius),
                hits,
                targetLayers,
                triggerInteraction
            );

            seenAgents.Clear();
            PlayerCaptureAgent bestTarget = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = hits[i];
                hits[i] = null;
                if (hit == null)
                {
                    continue;
                }

                var target = hit.GetComponentInParent<PlayerCaptureAgent>();
                if (!IsCandidate(attacker, target) || !seenAgents.Add(target))
                {
                    continue;
                }

                var distance = Vector3.Distance(attacker.transform.position, target.transform.position);
                if (distance >= bestDistance)
                {
                    continue;
                }

                if (!CaptureInteractionRules.IsInsideForwardCone(attacker.transform, target.transform.position, CaptureInteractionRules.TackleAngleDegrees))
                {
                    continue;
                }

                bestTarget = target;
                bestDistance = distance;
            }

            seenAgents.Clear();
            return bestTarget;
        }

        private static bool IsCandidate(PlayerCaptureAgent attacker, PlayerCaptureAgent target)
        {
            return attacker != null
                && target != null
                && target != attacker
                && target.Team != null
                && CaptureInteractionRules.CanTackleTarget(attacker.Team.Team, target.Team.Team, target.Status);
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0f, radius);
            verticalOffset = Mathf.Max(0f, verticalOffset);
        }
    }
}
