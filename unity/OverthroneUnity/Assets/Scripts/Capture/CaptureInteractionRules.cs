using UnityEngine;

namespace Overthrone
{
    public static class CaptureInteractionRules
    {
        public const float TackleRange = 3f;
        public const float KingTackleRange = 3.6f;
        public const float TackleAngleDegrees = 60f;
        public const float TackleStaminaCost = 30f;
        public const float TackleCooldownSeconds = 2f;
        public const float TackleMissStunSeconds = 0.5f;
        public const float CaptureRange = 2f;
        public const float CaptureHoldSeconds = 1.5f;
        public const float RescueRange = 1.5f;
        public const float HolderReleaseStunSeconds = 1f;

        public static bool CanTackle(MovementState state, CaptureStatus status, float stamina)
        {
            return status == CaptureStatus.Free
                && stamina >= TackleStaminaCost
                && (state == MovementState.Attacker || state == MovementState.King);
        }

        public static bool CanHold(MovementState state, CaptureStatus captorStatus, CaptureStatus targetStatus)
        {
            return captorStatus == CaptureStatus.Free
                && targetStatus == CaptureStatus.Free
                && (state == MovementState.Attacker || state == MovementState.King);
        }

        public static bool CanTackleTarget(TeamId attackerTeam, TeamId targetTeam, CaptureStatus targetStatus)
        {
            return attackerTeam != TeamId.None
                && targetTeam != TeamId.None
                && attackerTeam != targetTeam
                && (targetStatus == CaptureStatus.Free || targetStatus == CaptureStatus.Holding);
        }

        public static bool CanFinalCapture(MovementState state, CaptureStatus captorStatus, CaptureStatus targetStatus)
        {
            return state == MovementState.King
                && captorStatus == CaptureStatus.Free
                && targetStatus == CaptureStatus.Held;
        }

        public static bool CanFinalCapture(MovementState state, CaptureStatus captorStatus, CaptureStatus targetStatus, bool captorIsTargetHolder)
        {
            return !captorIsTargetHolder
                && CanFinalCapture(state, captorStatus, targetStatus);
        }

        public static bool CanRescue(TeamId rescuerTeam, TeamId heldTeam, CaptureStatus rescuerStatus, CaptureStatus heldStatus)
        {
            return rescuerStatus == CaptureStatus.Free
                && heldStatus == CaptureStatus.Held
                && rescuerTeam != TeamId.None
                && rescuerTeam == heldTeam;
        }

        public static bool CanSlimeEscape(CaptureStatus status, bool slimeEscapeUsed)
        {
            return status == CaptureStatus.Held && !slimeEscapeUsed;
        }

        public static bool IsInRange(Vector3 source, Vector3 target, float range)
        {
            return Vector3.Distance(source, target) <= Mathf.Max(0f, range);
        }

        public static bool IsInsideForwardCone(Transform source, Vector3 targetPosition, float angleDegrees)
        {
            var toTarget = targetPosition - source.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            var forward = source.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            var dot = Vector3.Dot(forward.normalized, toTarget.normalized);
            var halfAngle = Mathf.Max(0f, angleDegrees) * 0.5f;
            return dot >= Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        }
    }
}
