using UnityEngine;

namespace Overthrone
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LocalPlayerTeam))]
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerCaptureAgent : MonoBehaviour
    {
        [SerializeField] private PlayerStateController stateController;

        private MovementState stateBeforeCaptureInteraction = MovementState.Neutral;
        private bool slimeEscapeUsed;
        private bool releaseStunActive;
        private bool tackleMissStunActive;
        private float tackleCooldownRemaining;
        private UnityEngine.Object tackleCooldownTickOwner;

        public CaptureStatus Status { get; private set; }
        public LocalPlayerTeam Team { get; private set; }
        public PlayerCaptureAgent HeldBy { get; private set; }
        public PlayerCaptureAgent HeldTarget { get; private set; }
        public MovementState PersistentState => stateController != null ? stateController.PersistentState : MovementState.Neutral;
        public MovementState CaptureAuthorityState => Status == CaptureStatus.Holding ? stateBeforeCaptureInteraction : PersistentState;
        public bool HasUsedSlimeEscape => slimeEscapeUsed;
        public bool IsReleaseStunned => releaseStunActive;
        public bool IsInteractionStunned => releaseStunActive || tackleMissStunActive;
        internal bool IsTackleOnCooldown => tackleCooldownRemaining > 0f;

        public void Configure(PlayerStateController controller)
        {
            stateController = controller;
            EnsureReferences();
        }

        private void Awake()
        {
            EnsureReferences();
        }

        public bool TryHold(PlayerCaptureAgent target)
        {
            EnsureReferences();
            if (target == null || target == this || IsInteractionStunned)
            {
                return false;
            }

            target.EnsureReferences();
            if (!CaptureInteractionRules.CanHold(PersistentState, Status, target.Status))
            {
                return false;
            }

            if (Team == null || target.Team == null || Team.Team == TeamId.None || target.Team.Team == TeamId.None || Team.Team == target.Team.Team)
            {
                return false;
            }

            stateBeforeCaptureInteraction = PersistentState;
            Status = CaptureStatus.Holding;
            HeldTarget = target;
            SetPersistentState(MovementState.Holding);
            target.BecomeHeldBy(this);
            return true;
        }

        public bool ReleaseHold()
        {
            return ReleaseHold(false);
        }

        public bool ReleaseHoldWithHolderStun()
        {
            return ReleaseHold(true);
        }

        public bool TrySlimeEscape()
        {
            EnsureReferences();
            if (!CaptureInteractionRules.CanSlimeEscape(Status, slimeEscapeUsed))
            {
                return false;
            }

            var holder = HeldBy;
            var escapePosition = transform.position;
            if (!ReleaseHoldWithHolderStun())
            {
                return false;
            }

            slimeEscapeUsed = true;
            CaptureFeedbackSystem.Emit(new CaptureFeedbackEvent(
                CaptureFeedbackType.SlimeEscape,
                gameObject,
                holder != null ? holder.gameObject : null,
                escapePosition
            ));
            return true;
        }

        private bool ReleaseHold(bool stunHolder)
        {
            if (Status == CaptureStatus.Holding && HeldTarget != null)
            {
                var target = HeldTarget;
                HeldTarget = null;
                Status = CaptureStatus.Free;
                RestoreStateAfterHolding(stunHolder);
                target.ReleaseFromHeld();
                return true;
            }

            if (Status == CaptureStatus.Held && HeldBy != null)
            {
                return HeldBy.ReleaseHold(stunHolder);
            }

            return false;
        }

        public bool CompleteCapture(PlayerCaptureAgent target)
        {
            EnsureReferences();
            if (target == null || IsInteractionStunned || target.Status != CaptureStatus.Held)
            {
                return false;
            }

            target.EnsureReferences();
            if (Team == null || target.Team == null || Team.Team == TeamId.None || target.Team.Team == TeamId.None || Team.Team == target.Team.Team)
            {
                return false;
            }

            if (!CaptureInteractionRules.CanFinalCapture(CaptureAuthorityState, Status, target.Status))
            {
                return false;
            }

            var holder = target.HeldBy;
            if (holder != null)
            {
                holder.HeldTarget = null;
                holder.Status = CaptureStatus.Free;
                holder.RestoreStateAfterHolding(false);
            }

            target.HeldBy = null;
            target.HeldTarget = null;
            target.Status = CaptureStatus.Captured;
            target.SetPersistentState(MovementState.Captured);
            Team.RegisterFinalCapture();
            return true;
        }

        private void BecomeHeldBy(PlayerCaptureAgent holder)
        {
            stateBeforeCaptureInteraction = PersistentState;
            HeldBy = holder;
            Status = CaptureStatus.Held;
            SetPersistentState(MovementState.Held);
        }

        private void ReleaseFromHeld()
        {
            HeldBy = null;
            Status = CaptureStatus.Free;
            SetPersistentState(stateBeforeCaptureInteraction);
        }

        private void RestoreStateAfterHolding(bool stunHolder)
        {
            SetPersistentState(stateBeforeCaptureInteraction);
            releaseStunActive = stunHolder;
            if (stunHolder)
            {
                RequestTimedState(MovementState.Holding, CaptureInteractionRules.HolderReleaseStunSeconds);
            }
        }

        internal void ApplyTackleMissStun()
        {
            EnsureReferences();
            tackleMissStunActive = true;
            RequestTimedState(MovementState.Holding, CaptureInteractionRules.TackleMissStunSeconds);
        }

        internal void StartTackleCooldown()
        {
            tackleCooldownRemaining = CaptureInteractionRules.TackleCooldownSeconds;
            tackleCooldownTickOwner = null;
        }

        internal void TickTackleCooldown(float deltaTime, UnityEngine.Object tickOwner)
        {
            if (tackleCooldownRemaining <= 0f)
            {
                tackleCooldownTickOwner = null;
                return;
            }

            if (!CanTickTackleCooldown(tickOwner))
            {
                return;
            }

            tackleCooldownTickOwner = tickOwner;
            tackleCooldownRemaining = Mathf.Max(0f, tackleCooldownRemaining - Mathf.Max(0f, deltaTime));
            if (tackleCooldownRemaining <= 0f)
            {
                tackleCooldownTickOwner = null;
            }
        }

        private bool CanTickTackleCooldown(UnityEngine.Object tickOwner)
        {
            if (tickOwner == null)
            {
                return false;
            }

            if (tackleCooldownTickOwner == null || tackleCooldownTickOwner == tickOwner)
            {
                return true;
            }

            return tackleCooldownTickOwner is Behaviour behaviour && !behaviour.isActiveAndEnabled;
        }

        internal void ClearReleaseStun()
        {
            releaseStunActive = false;
        }

        internal void ClearInteractionStun()
        {
            releaseStunActive = false;
            tackleMissStunActive = false;
        }

        private void SetPersistentState(MovementState state)
        {
            if (stateController != null)
            {
                stateController.SetPersistentState(state);
            }
        }

        private void RequestTimedState(MovementState state, float duration)
        {
            if (stateController != null)
            {
                stateController.RequestTimedState(state, duration);
            }
        }

        private void EnsureReferences()
        {
            if (Team == null)
            {
                Team = GetComponent<LocalPlayerTeam>();
            }

            if (stateController == null)
            {
                stateController = GetComponent<PlayerStateController>();
            }
        }
    }
}
