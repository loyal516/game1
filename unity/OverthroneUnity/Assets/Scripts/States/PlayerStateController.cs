using UnityEngine;

namespace Overthrone
{
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerStateController : MonoBehaviour
    {
        [SerializeField] private MovementState defaultState = MovementState.Neutral;
        [SerializeField] private float slimeDuration = 3f;
        [SerializeField] private float slimeStaminaCost = 50f;
        [SerializeField] private float slimeCooldownSeconds = 15f;

        private PlayerInputReader input;
        private PlayerMotor motor;
        private PlayerCaptureAgent captureAgent;
        private float stateTimer;
        private bool timedStateActive;
        private MovementState timedState;
        private MovementState persistentState;
        private float slimeCooldownRemaining;

        public MovementState CurrentState
        {
            get
            {
                EnsureReferences();
                return motor != null ? motor.State : defaultState;
            }
        }

        public MovementState PersistentState => persistentState;
        public float SlimeCooldownRemaining => Mathf.Max(0f, slimeCooldownRemaining);
        public float SlimeStaminaCost => Mathf.Max(0f, slimeStaminaCost);

        private void Awake()
        {
            EnsureReferences();
            persistentState = defaultState;
            motor.SetState(persistentState);
        }

        private void Update()
        {
            EnsureReferences();

            if (input.SlimePressed)
            {
                HandleSlimePressed();
            }

            TickSlimeCooldown(Time.deltaTime);
            TickTimedState(Time.deltaTime);
        }

        public void SetPersistentState(MovementState nextState)
        {
            EnsureReferences();
            persistentState = nextState;
            if (ShouldKeepTimedState() || IsCaptureInteractionStunned())
            {
                motor.SetState(MovementState.Holding);
                return;
            }

            timedStateActive = false;
            stateTimer = 0f;
            timedState = default;
            motor.SetState(persistentState);
        }

        public void RequestTimedState(MovementState nextState, float duration)
        {
            EnsureReferences();
            if (IsCaptureInteractionStunned() && nextState != MovementState.Holding)
            {
                return;
            }

            motor.SetState(nextState);
            stateTimer = Mathf.Max(0f, duration);
            timedStateActive = stateTimer > 0f;
            timedState = timedStateActive ? nextState : default;
        }

        public void ReturnToDefault()
        {
            SetPersistentState(defaultState);
        }

        private void TickTimedState(float deltaTime)
        {
            if (!timedStateActive)
            {
                return;
            }

            stateTimer -= deltaTime;
            if (stateTimer > 0f)
            {
                return;
            }

            var expiredTimedState = timedState;
            timedStateActive = false;
            stateTimer = 0f;
            timedState = default;
            if (expiredTimedState == MovementState.Holding)
            {
                captureAgent?.ClearInteractionStun();
            }

            motor.SetState(persistentState);
        }

        private void TickSlimeCooldown(float deltaTime)
        {
            if (slimeCooldownRemaining <= 0f)
            {
                slimeCooldownRemaining = 0f;
                return;
            }

            slimeCooldownRemaining = Mathf.Max(0f, slimeCooldownRemaining - Mathf.Max(0f, deltaTime));
        }

        private bool ShouldKeepTimedState()
        {
            return timedStateActive && timedState == MovementState.Holding;
        }

        private bool IsCaptureInteractionStunned()
        {
            return captureAgent != null && captureAgent.IsInteractionStunned;
        }

        private void HandleSlimePressed()
        {
            if (IsCaptureInteractionStunned())
            {
                return;
            }

            if (slimeCooldownRemaining > 0f || motor == null || !motor.HasStamina(slimeStaminaCost))
            {
                return;
            }

            var wasHeld = captureAgent != null && captureAgent.Status == CaptureStatus.Held;
            if (wasHeld && !captureAgent.TrySlimeEscape())
            {
                return;
            }

            if (!motor.TrySpendStamina(slimeStaminaCost))
            {
                return;
            }

            slimeCooldownRemaining = Mathf.Max(0f, slimeCooldownSeconds);
            RequestTimedState(MovementState.Slime, slimeDuration);
        }

        private void EnsureReferences()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }

            if (captureAgent == null)
            {
                captureAgent = GetComponent<PlayerCaptureAgent>();
            }
        }
    }
}
