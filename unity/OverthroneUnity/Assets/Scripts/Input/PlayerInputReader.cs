using UnityEngine;
using UnityEngine.InputSystem;

namespace Overthrone
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private string actionMapName = "Player";

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction dashAction;
        private InputAction jumpAction;
        private InputAction crouchAction;
        private InputAction slimeAction;
        private InputAction tackleAction;
        private InputAction captureAction;
        private InputAction pingAction;
        private InputAction spectatePreviousAction;
        private InputAction spectateNextAction;
        private bool actionsBound;

        public enum LookInputSource
        {
            None,
            Pointer,
            Gamepad
        }

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public LookInputSource CurrentLookInputSource { get; private set; }
        public bool IsGamepadLook => CurrentLookInputSource == LookInputSource.Gamepad;
        public bool SprintHeld { get; private set; }
        public bool SprintPressed => sprintAction != null && sprintAction.WasPressedThisFrame();
        public bool DashPressed => dashAction != null && dashAction.WasPressedThisFrame();
        public bool JumpPressed => jumpAction != null && jumpAction.WasPressedThisFrame();
        public bool CrouchHeld { get; private set; }
        public bool CaptureHeld { get; private set; }
        public bool SlimePressed => slimeAction != null && slimeAction.WasPressedThisFrame();
        public bool TacklePressed => tackleAction != null && tackleAction.WasPressedThisFrame();
        public bool PingPressed => pingAction != null && pingAction.WasPressedThisFrame();
        public bool PingHeld => pingAction != null && pingAction.IsPressed();
        public bool PingReleased => pingAction != null && pingAction.WasReleasedThisFrame();
        public bool SpectatePreviousPressed => spectatePreviousAction != null && spectatePreviousAction.WasPressedThisFrame();
        public bool SpectateNextPressed => spectateNextAction != null && spectateNextAction.WasPressedThisFrame();

        public void Configure(InputActionAsset inputActions)
        {
            if (controls == inputActions && actionsBound)
            {
                return;
            }

            UnbindActions();
            controls = inputActions;
            BindActions();

            if (isActiveAndEnabled)
            {
                EnableActions(true);
            }
        }

        private void Awake()
        {
            BindActions();
        }

        private void OnEnable()
        {
            EnableActions(true);
        }

        private void OnDisable()
        {
            EnableActions(false);
        }

        private void OnDestroy()
        {
            UnbindActions();
        }

        private void BindActions()
        {
            if (actionsBound)
            {
                return;
            }

            if (controls == null)
            {
                return;
            }

            var map = controls.FindActionMap(actionMapName, throwIfNotFound: false);
            if (map == null)
            {
                Debug.LogError($"Input action map '{actionMapName}' was not found.", this);
                return;
            }

            moveAction = map.FindAction("Move");
            lookAction = map.FindAction("Look");
            sprintAction = map.FindAction("Sprint");
            dashAction = map.FindAction("Dash");
            jumpAction = map.FindAction("Jump");
            crouchAction = map.FindAction("Crouch");
            slimeAction = map.FindAction("Slime");
            tackleAction = map.FindAction("Tackle");
            captureAction = map.FindAction("Capture");
            pingAction = map.FindAction("Ping");
            spectatePreviousAction = map.FindAction("SpectatePrevious");
            spectateNextAction = map.FindAction("SpectateNext");

            if (moveAction == null
                || lookAction == null
                || sprintAction == null
                || dashAction == null
                || jumpAction == null
                || crouchAction == null
                || captureAction == null
                || pingAction == null
                || spectatePreviousAction == null
                || spectateNextAction == null)
            {
                Debug.LogError("One or more required player input actions are missing.", this);
                ClearActions();
                return;
            }

            moveAction.performed += OnMovePerformed;
            moveAction.canceled += OnMoveCanceled;
            lookAction.performed += OnLookPerformed;
            lookAction.canceled += OnLookCanceled;
            sprintAction.performed += OnSprintPerformed;
            sprintAction.canceled += OnSprintCanceled;
            crouchAction.performed += OnCrouchPerformed;
            crouchAction.canceled += OnCrouchCanceled;
            captureAction.performed += OnCapturePerformed;
            captureAction.canceled += OnCaptureCanceled;
            actionsBound = true;
        }

        private void EnableActions(bool enabled)
        {
            if (controls == null)
            {
                return;
            }

            if (enabled)
            {
                controls.Enable();
            }
            else
            {
                controls.Disable();
            }
        }

        private void UnbindActions()
        {
            if (!actionsBound)
            {
                ClearActions();
                return;
            }

            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
            lookAction.performed -= OnLookPerformed;
            lookAction.canceled -= OnLookCanceled;
            sprintAction.performed -= OnSprintPerformed;
            sprintAction.canceled -= OnSprintCanceled;
            crouchAction.performed -= OnCrouchPerformed;
            crouchAction.canceled -= OnCrouchCanceled;
            captureAction.performed -= OnCapturePerformed;
            captureAction.canceled -= OnCaptureCanceled;
            ClearActions();
        }

        private void ClearActions()
        {
            moveAction = null;
            lookAction = null;
            sprintAction = null;
            dashAction = null;
            jumpAction = null;
            crouchAction = null;
            slimeAction = null;
            tackleAction = null;
            captureAction = null;
            pingAction = null;
            spectatePreviousAction = null;
            spectateNextAction = null;
            Move = Vector2.zero;
            Look = Vector2.zero;
            CurrentLookInputSource = LookInputSource.None;
            SprintHeld = false;
            CrouchHeld = false;
            CaptureHeld = false;
            actionsBound = false;
        }

        private void OnMovePerformed(InputAction.CallbackContext context) => Move = context.ReadValue<Vector2>();
        private void OnMoveCanceled(InputAction.CallbackContext context) => Move = Vector2.zero;
        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            Look = context.ReadValue<Vector2>();
            CurrentLookInputSource = context.control?.device is Gamepad ? LookInputSource.Gamepad : LookInputSource.Pointer;
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            Look = Vector2.zero;
            CurrentLookInputSource = LookInputSource.None;
        }

        private void OnSprintPerformed(InputAction.CallbackContext context) => SprintHeld = true;
        private void OnSprintCanceled(InputAction.CallbackContext context) => SprintHeld = false;
        private void OnCrouchPerformed(InputAction.CallbackContext context) => CrouchHeld = true;
        private void OnCrouchCanceled(InputAction.CallbackContext context) => CrouchHeld = false;
        private void OnCapturePerformed(InputAction.CallbackContext context) => CaptureHeld = true;
        private void OnCaptureCanceled(InputAction.CallbackContext context) => CaptureHeld = false;
    }
}
