using System;
using UnityEngine;

namespace Overthrone
{
    [DisallowMultipleComponent]
    public sealed class LocalCaptureSystem : MonoBehaviour
    {
        [SerializeField] private PlayerCaptureAgent localPlayer;
        [SerializeField] private PlayerCaptureAgent[] agents = Array.Empty<PlayerCaptureAgent>();
        [SerializeField] private LocalDeadChannel deadChannel;

        private PlayerInputReader input;
        private PlayerMotor motor;
        private TackleHitbox localTackleHitbox;
        private PlayerCaptureAgent captureTarget;
        private float captureHoldProgress;

        public float CaptureHoldProgress01 => CaptureInteractionRules.CaptureHoldSeconds > 0f
            ? Mathf.Clamp01(captureHoldProgress / CaptureInteractionRules.CaptureHoldSeconds)
            : 0f;
        public PlayerCaptureAgent CaptureHoldTarget => captureTarget;

        public void Configure(PlayerCaptureAgent player, PlayerCaptureAgent[] captureAgents, LocalDeadChannel localDeadChannel = null)
        {
            localPlayer = player;
            agents = captureAgents ?? Array.Empty<PlayerCaptureAgent>();
            deadChannel = localDeadChannel;
            ResolveLocalReferences();
        }

        private void Awake()
        {
            ResolveLocalReferences();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);
            localPlayer?.TickTackleCooldown(deltaTime, this);

            if (localPlayer == null || input == null)
            {
                return;
            }

            if (input.TacklePressed)
            {
                TryTackle(localPlayer);
            }

            if (input.CaptureHeld)
            {
                TickFinalCapture(localPlayer, deltaTime);
            }
            else
            {
                ResetCaptureHold();
            }

            TryRescueNearby(localPlayer);
        }

        public bool TryTackle(PlayerCaptureAgent attacker)
        {
            if (attacker == null || attacker.IsTackleOnCooldown || attacker.IsInteractionStunned)
            {
                return false;
            }

            var attackerMotor = ResolveMotor(attacker);
            if (attackerMotor == null || !CaptureInteractionRules.CanTackle(attacker.PersistentState, attacker.Status, attackerMotor.currentStamina))
            {
                return false;
            }

            ApplyTackleAttemptPenalty(attacker, attackerMotor);
            var target = FindTackleTarget(attacker);
            if (target == null)
            {
                ApplyTackleMissPenalty(attacker);
                EmitFeedback(CaptureFeedbackType.TackleMiss, attacker, null, attacker.transform.position + attacker.transform.forward * 1.2f);
                return false;
            }

            var feedbackType = target.Status == CaptureStatus.Holding
                ? CaptureFeedbackType.HolderInterrupted
                : CaptureFeedbackType.TackleHit;
            var succeeded = target.Status == CaptureStatus.Holding
                ? target.ReleaseHoldWithHolderStun()
                : attacker.TryHold(target);
            if (!succeeded)
            {
                ApplyTackleMissPenalty(attacker);
                EmitFeedback(CaptureFeedbackType.TackleMiss, attacker, target, target.transform.position);
                return false;
            }

            EmitFeedback(feedbackType, attacker, target, target.transform.position);
            ResetCaptureHold();
            return true;
        }

        public bool TickFinalCapture(PlayerCaptureAgent captor, float deltaTime)
        {
            if (captor == null || captor.IsInteractionStunned)
            {
                ResetCaptureHold();
                return false;
            }

            var target = FindFinalCaptureTarget(captor);
            if (target == null)
            {
                ResetCaptureHold();
                return false;
            }

            if (captureTarget != target)
            {
                captureTarget = target;
                captureHoldProgress = 0f;
            }

            captureHoldProgress += Mathf.Max(0f, deltaTime);
            if (captureHoldProgress < CaptureInteractionRules.CaptureHoldSeconds)
            {
                return false;
            }

            var completed = captor.CompleteCapture(target);
            if (completed)
            {
                EmitFeedback(CaptureFeedbackType.FinalCapture, captor, target, target.transform.position);
                deadChannel?.PostSystemMessage(target.Team.Team, $"{target.name} joined dead channel");
            }

            ResetCaptureHold();
            return completed;
        }

        public bool TryRescueNearby(PlayerCaptureAgent rescuer)
        {
            if (rescuer == null || rescuer.Team == null || rescuer.IsInteractionStunned)
            {
                return false;
            }

            foreach (var target in agents)
            {
                if (target == null || target == rescuer || target.Team == null)
                {
                    continue;
                }

                if (!CaptureInteractionRules.CanRescue(rescuer.Team.Team, target.Team.Team, rescuer.Status, target.Status))
                {
                    continue;
                }

                if (!CaptureInteractionRules.IsInRange(rescuer.transform.position, target.transform.position, CaptureInteractionRules.RescueRange))
                {
                    continue;
                }

                if (!target.ReleaseHoldWithHolderStun())
                {
                    continue;
                }

                EmitFeedback(CaptureFeedbackType.Rescue, rescuer, target, target.transform.position);
                ResetCaptureHold();
                return true;
            }

            return false;
        }

        private PlayerCaptureAgent FindTackleTarget(PlayerCaptureAgent attacker)
        {
            var hitbox = ResolveTackleHitbox(attacker);
            if (hitbox != null)
            {
                return hitbox.FindBestTarget(attacker);
            }

            var range = attacker.PersistentState == MovementState.King
                ? CaptureInteractionRules.KingTackleRange
                : CaptureInteractionRules.TackleRange;
            PlayerCaptureAgent bestTarget = null;
            var bestDistance = float.MaxValue;

            foreach (var target in agents)
            {
                if (!IsTackleTarget(attacker, target))
                {
                    continue;
                }

                var distance = Vector3.Distance(attacker.transform.position, target.transform.position);
                if (distance > range || distance >= bestDistance)
                {
                    continue;
                }

                if (!CaptureInteractionRules.IsInsideForwardCone(attacker.transform, target.transform.position, CaptureInteractionRules.TackleAngleDegrees))
                {
                    continue;
                }

                bestDistance = distance;
                bestTarget = target;
            }

            return bestTarget;
        }

        private PlayerCaptureAgent FindFinalCaptureTarget(PlayerCaptureAgent captor)
        {
            if (captor == null)
            {
                return null;
            }

            if (captor.HeldTarget != null && IsFinalCaptureTarget(captor, captor.HeldTarget))
            {
                return captor.HeldTarget;
            }

            foreach (var target in agents)
            {
                if (IsFinalCaptureTarget(captor, target))
                {
                    return target;
                }
            }

            return null;
        }

        private static bool IsTackleTarget(PlayerCaptureAgent attacker, PlayerCaptureAgent target)
        {
            return attacker != null
                && target != null
                && target != attacker
                && attacker.Team != null
                && target.Team != null
                && CaptureInteractionRules.CanTackleTarget(attacker.Team.Team, target.Team.Team, target.Status);
        }

        private static bool IsFinalCaptureTarget(PlayerCaptureAgent captor, PlayerCaptureAgent target)
        {
            return captor != null
                && target != null
                && target != captor
                && captor.Team != null
                && target.Team != null
                && captor.Team.Team != TeamId.None
                && target.Team.Team != TeamId.None
                && captor.Team.Team != target.Team.Team
                && CaptureInteractionRules.CanFinalCapture(captor.CaptureAuthorityState, captor.Status, target.Status)
                && CaptureInteractionRules.IsInRange(captor.transform.position, target.transform.position, CaptureInteractionRules.CaptureRange);
        }

        private void ResetCaptureHold()
        {
            captureTarget = null;
            captureHoldProgress = 0f;
        }

        private static PlayerMotor ResolveMotor(PlayerCaptureAgent agent)
        {
            return agent != null ? agent.GetComponent<PlayerMotor>() : null;
        }

        private TackleHitbox ResolveTackleHitbox(PlayerCaptureAgent agent)
        {
            if (agent == null)
            {
                return null;
            }

            if (agent == localPlayer)
            {
                if (localTackleHitbox == null)
                {
                    localTackleHitbox = agent.GetComponent<TackleHitbox>();
                }

                return localTackleHitbox;
            }

            return agent.GetComponent<TackleHitbox>();
        }

        private void ApplyTackleAttemptPenalty(PlayerCaptureAgent attacker, PlayerMotor attackerMotor)
        {
            if (attackerMotor != null)
            {
                attackerMotor.currentStamina = Mathf.Max(0f, attackerMotor.currentStamina - CaptureInteractionRules.TackleStaminaCost);
            }

            attacker.StartTackleCooldown();
        }

        private void ApplyTackleMissPenalty(PlayerCaptureAgent attacker)
        {
            attacker.ApplyTackleMissStun();
            ResetCaptureHold();
        }

        private static void EmitFeedback(
            CaptureFeedbackType type,
            PlayerCaptureAgent source,
            PlayerCaptureAgent target,
            Vector3 position)
        {
            CaptureFeedbackSystem.Emit(new CaptureFeedbackEvent(
                type,
                source != null ? source.gameObject : null,
                target != null ? target.gameObject : null,
                position
            ));
        }

        private void ResolveLocalReferences()
        {
            if (localPlayer == null)
            {
                return;
            }

            input = localPlayer.GetComponent<PlayerInputReader>();
            motor = localPlayer.GetComponent<PlayerMotor>();
            localTackleHitbox = localPlayer.GetComponent<TackleHitbox>();
        }
    }
}
