using UnityEngine;
using UnityEngine.InputSystem;

namespace Overthrone
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("References")]
        public Transform cameraPivot;
        public MovementProfileSet movementProfiles;

        [Header("Look")]
        public float mouseSensitivity = 0.11f;
        public float gamepadLookSensitivityDegreesPerSecond = 15f;
        public float gamepadLookDeadzone = 0.08f;
        public float minPitch = -35f;
        public float maxPitch = 65f;
        public bool lockCursorOnStart = true;
        public bool lockCursorOnClick = true;

        [Header("Gravity")]
        public float gravity = -24f;
        public float groundedStickForce = -2f;

        [Header("Jump")]
        public float jumpHeight = 2f;

        [Header("Crouch")]
        public float crouchSpeed = 2.5f;

        [Header("Slime Shape")]
        public float slimeControllerHeight = 1.26f;
        public float slimeControllerRadius = 0.25f;
        public float slimeVisualScaleXZ = 1.2f;
        public float slimeVisualScaleY = 0.62f;
        public float slimeGroundFrictionMultiplier = 0.3f;

        [Header("Stamina")]
        public float maxStamina = 100f;
        public float currentStamina = 100f;
        public float sprintStaminaCostPerSecond = 10f;
        public float idleStaminaRecoveryPerSecond = 15f;
        public float movingStaminaRecoveryPerSecond = 5f;
        public float crouchStaminaRecoveryPerSecond = 10f;

        [Header("Dash")]
        public float dashStaminaCost = 25f;
        public float dashCooldownSeconds = 5f;
        public float dashDurationSeconds = 0.5f;
        public float dashSpeedMultiplier = 2f;
        public float sprintDoubleTapDashWindowSeconds = 0.32f;

        private CharacterController controller;
        private PlayerInputReader input;
        private float verticalVelocity;
        private float yaw;
        private float pitch = 18f;
        private Vector3 horizontalVelocity;
        private MovementState state = MovementState.Neutral;
        private bool isSprinting;
        private bool isCrouching;
        private float dashTimeRemaining;
        private float dashCooldownRemaining;
        private float lastSprintTapTime = float.NegativeInfinity;
        private Vector3 dashDirection = Vector3.forward;
        private float defaultControllerHeight;
        private float defaultControllerRadius;
        private Vector3 defaultControllerCenter;
        private Vector3 defaultVisualScale;
        private bool hasShapeDefaults;

        public MovementState State => state;
        public bool IsGrounded => controller != null && controller.isGrounded;
        public bool IsMoving => input != null && new Vector2(input.Move.x, input.Move.y).sqrMagnitude > 0.01f && CurrentProfile.canMove;
        public bool IsSprinting => isSprinting;
        public bool IsCrouching => isCrouching;
        public bool IsDashing => dashTimeRemaining > 0f;
        public float DashCooldownRemaining => Mathf.Max(0f, dashCooldownRemaining);
        public float CurrentHorizontalSpeed => new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;
        public float NormalizedStamina => maxStamina > 0f ? Mathf.Clamp01(currentStamina / maxStamina) : 0f;
        public MovementProfile CurrentProfile => movementProfiles != null ? movementProfiles.Get(state) : new MovementProfile();

        public void SetState(MovementState nextState)
        {
            state = nextState;
            ApplyStateShape();
        }

        public bool HasStamina(float amount)
        {
            return currentStamina + 0.0001f >= Mathf.Max(0f, amount);
        }

        public bool TrySpendStamina(float amount)
        {
            amount = Mathf.Max(0f, amount);
            if (!HasStamina(amount))
            {
                return false;
            }

            currentStamina -= amount;
            ClampStamina();
            return true;
        }

        public bool TryStartDash(Vector2 moveInput)
        {
            return TryStartDash(CurrentProfile, Vector2.ClampMagnitude(moveInput, 1f));
        }

        public bool RegisterSprintTapForDash(float timeSeconds, Vector2 moveInput)
        {
            var withinWindow = timeSeconds - lastSprintTapTime <= Mathf.Max(0f, sprintDoubleTapDashWindowSeconds);
            if (!withinWindow)
            {
                lastSprintTapTime = timeSeconds;
                return false;
            }

            lastSprintTapTime = float.NegativeInfinity;
            return TryStartDash(moveInput);
        }

        public Vector2 ResolveLookDelta(Vector2 rawLook, bool isGamepadLook, float deltaTime)
        {
            if (!isGamepadLook)
            {
                return rawLook * Mathf.Max(0f, mouseSensitivity);
            }

            if (rawLook.sqrMagnitude < gamepadLookDeadzone * gamepadLookDeadzone)
            {
                return Vector2.zero;
            }

            return rawLook * Mathf.Max(0f, gamepadLookSensitivityDegreesPerSecond) * Mathf.Max(0f, deltaTime);
        }

        public float ResolveGroundAcceleration(MovementProfile profile, bool hasMoveInput)
        {
            if (profile == null)
            {
                return 0f;
            }

            var acceleration = Mathf.Max(0f, profile.acceleration);
            if (state == MovementState.Slime && !hasMoveInput)
            {
                return acceleration * Mathf.Clamp01(slimeGroundFrictionMultiplier);
            }

            return acceleration;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            yaw = transform.eulerAngles.y;
            CaptureShapeDefaults();

            if (cameraPivot != null)
            {
                pitch = cameraPivot.localEulerAngles.x;
            }

            ClampStamina();
        }

        private void Start()
        {
            if (lockCursorOnStart)
            {
                SetCursorLocked(true);
            }
        }

        private void Update()
        {
            UpdateCursorLock();
            ApplyLook();
            ApplyMovement(Time.deltaTime);
        }

        private void UpdateCursorLock()
        {
            if (!lockCursorOnClick)
            {
                return;
            }

            if (Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                SetCursorLocked(true);
            }

            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                SetCursorLocked(false);
            }
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void ApplyLook()
        {
            var look = ResolveLookDelta(input.Look, input.IsGamepadLook, Time.deltaTime);
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void ApplyMovement(float deltaTime)
        {
            var profile = CurrentProfile;
            var moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            var desiredDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            var hasMoveInput = moveInput.sqrMagnitude > 0.01f;
            TickDashCooldown(deltaTime);
            if (input.DashPressed)
            {
                TryStartDash(profile, moveInput);
            }
            else if (input.SprintPressed)
            {
                RegisterSprintTapForDash(Time.time, moveInput);
            }

            isCrouching = input.CrouchHeld && profile.canMove;
            isSprinting = !IsDashing && input.SprintHeld && profile.canSprint && profile.canMove && hasMoveInput && !isCrouching && currentStamina > 0f;
            var desiredSpeed = GetDesiredSpeed(profile, isSprinting, isCrouching);
            var wasDashing = IsDashing;
            if (wasDashing)
            {
                horizontalVelocity = dashDirection * profile.MaxSpeed(true) * Mathf.Max(1f, dashSpeedMultiplier);
                TickDashDuration(deltaTime);
            }
            else if (profile.canMove)
            {
                var desiredVelocity = desiredDirection * desiredSpeed;
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    desiredVelocity,
                    ResolveGroundAcceleration(profile, hasMoveInput) * deltaTime
                );
            }
            else
            {
                horizontalVelocity = Vector3.zero;
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickForce;
            }

            if (input.JumpPressed && controller.isGrounded && profile.canMove)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * Mathf.Abs(gravity) * 2f);
            }

            UpdateStamina(deltaTime, isSprinting, hasMoveInput, isCrouching, wasDashing);
            verticalVelocity += gravity * deltaTime;
            var velocity = horizontalVelocity + Vector3.up * verticalVelocity;
            controller.Move(velocity * deltaTime);
        }

        private float GetDesiredSpeed(MovementProfile profile, bool sprinting, bool crouching)
        {
            if (sprinting)
            {
                return profile.MaxSpeed(true);
            }

            return crouching ? Mathf.Min(profile.walkSpeed, crouchSpeed) : profile.MaxSpeed(false);
        }

        private bool TryStartDash(MovementProfile profile, Vector2 moveInput)
        {
            if (IsDashing || dashCooldownRemaining > 0f || profile == null || !profile.canMove || !profile.canSprint)
            {
                return false;
            }

            if (!TrySpendStamina(dashStaminaCost))
            {
                return false;
            }

            var worldDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            dashDirection = worldDirection.sqrMagnitude > 0.01f ? worldDirection.normalized : transform.forward;
            dashTimeRemaining = Mathf.Max(0f, dashDurationSeconds);
            dashCooldownRemaining = Mathf.Max(0f, dashCooldownSeconds);
            return dashTimeRemaining > 0f;
        }

        private void TickDashCooldown(float deltaTime)
        {
            if (dashCooldownRemaining <= 0f)
            {
                dashCooldownRemaining = 0f;
                return;
            }

            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - Mathf.Max(0f, deltaTime));
        }

        private void TickDashDuration(float deltaTime)
        {
            if (dashTimeRemaining <= 0f)
            {
                dashTimeRemaining = 0f;
                return;
            }

            dashTimeRemaining = Mathf.Max(0f, dashTimeRemaining - Mathf.Max(0f, deltaTime));
        }

        private void UpdateStamina(float deltaTime, bool sprinting, bool hasMoveInput, bool crouching, bool dashing)
        {
            if (sprinting)
            {
                currentStamina -= sprintStaminaCostPerSecond * deltaTime;
            }
            else if (dashing)
            {
                ClampStamina();
                return;
            }
            else
            {
                currentStamina += GetStaminaRecoveryRate(hasMoveInput, crouching) * deltaTime;
            }

            ClampStamina();
        }

        private float GetStaminaRecoveryRate(bool hasMoveInput, bool crouching)
        {
            if (crouching)
            {
                return crouchStaminaRecoveryPerSecond;
            }

            return hasMoveInput ? movingStaminaRecoveryPerSecond : idleStaminaRecoveryPerSecond;
        }

        private void ClampStamina()
        {
            maxStamina = Mathf.Max(0f, maxStamina);
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }

        private void CaptureShapeDefaults()
        {
            if (hasShapeDefaults)
            {
                return;
            }

            controller ??= GetComponent<CharacterController>();
            if (controller == null)
            {
                return;
            }

            defaultControllerHeight = controller.height;
            defaultControllerRadius = controller.radius;
            defaultControllerCenter = controller.center;
            defaultVisualScale = transform.localScale;
            hasShapeDefaults = true;
        }

        private void ApplyStateShape()
        {
            CaptureShapeDefaults();
            if (!hasShapeDefaults || controller == null)
            {
                return;
            }

            if (state == MovementState.Slime)
            {
                var height = Mathf.Max(0.1f, Mathf.Min(defaultControllerHeight, slimeControllerHeight));
                var radius = Mathf.Max(0.05f, Mathf.Min(defaultControllerRadius, slimeControllerRadius));
                controller.height = height;
                controller.radius = radius;
                controller.center = new Vector3(defaultControllerCenter.x, height * 0.5f, defaultControllerCenter.z);
                transform.localScale = new Vector3(
                    defaultVisualScale.x * Mathf.Max(0.01f, slimeVisualScaleXZ),
                    defaultVisualScale.y * Mathf.Max(0.01f, slimeVisualScaleY),
                    defaultVisualScale.z * Mathf.Max(0.01f, slimeVisualScaleXZ)
                );
                return;
            }

            controller.height = defaultControllerHeight;
            controller.radius = defaultControllerRadius;
            controller.center = defaultControllerCenter;
            transform.localScale = defaultVisualScale;
        }

        private void OnValidate()
        {
            mouseSensitivity = Mathf.Max(0f, mouseSensitivity);
            gamepadLookSensitivityDegreesPerSecond = Mathf.Max(0f, gamepadLookSensitivityDegreesPerSecond);
            gamepadLookDeadzone = Mathf.Max(0f, gamepadLookDeadzone);
            slimeControllerHeight = Mathf.Max(0.1f, slimeControllerHeight);
            slimeControllerRadius = Mathf.Max(0.05f, slimeControllerRadius);
            slimeVisualScaleXZ = Mathf.Max(0.01f, slimeVisualScaleXZ);
            slimeVisualScaleY = Mathf.Max(0.01f, slimeVisualScaleY);
            slimeGroundFrictionMultiplier = Mathf.Clamp01(slimeGroundFrictionMultiplier);
            ClampStamina();
        }
    }
}
