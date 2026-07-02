using System.Reflection;
using UnityEditor;
using NUnit.Framework;
using Overthrone;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class MovementProfileTests
{
    private const string InputActionsPath = "Assets/Input/OverthroneControls.inputactions";
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void InputActionsExposePhaseOneMovementActions()
    {
        var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        Assert.IsNotNull(controls);

        var map = controls.FindActionMap("Player", throwIfNotFound: false);
        Assert.IsNotNull(map);

        var jump = map.FindAction("Jump", throwIfNotFound: false);
        var crouch = map.FindAction("Crouch", throwIfNotFound: false);
        var dash = map.FindAction("Dash", throwIfNotFound: false);
        var ping = map.FindAction("Ping", throwIfNotFound: false);
        var spectatePrevious = map.FindAction("SpectatePrevious", throwIfNotFound: false);
        var spectateNext = map.FindAction("SpectateNext", throwIfNotFound: false);
        Assert.IsNotNull(jump);
        Assert.IsNotNull(crouch);
        Assert.IsNotNull(dash);
        Assert.IsNotNull(ping);
        Assert.IsNotNull(spectatePrevious);
        Assert.IsNotNull(spectateNext);
        AssertHasBinding(jump, "<Keyboard>/space");
        AssertHasBinding(jump, "<Gamepad>/buttonSouth");
        AssertHasBinding(crouch, "<Keyboard>/leftCtrl");
        AssertHasBinding(crouch, "<Gamepad>/buttonEast");
        AssertHasBinding(dash, "<Keyboard>/q");
        AssertHasBinding(dash, "<Gamepad>/buttonWest");
        AssertHasBinding(ping, "<Keyboard>/g");
        AssertHasBinding(ping, "<Gamepad>/dpad/up");
        AssertHasBinding(spectatePrevious, "<Keyboard>/q");
        AssertHasBinding(spectatePrevious, "<Gamepad>/leftShoulder");
        AssertHasBinding(spectateNext, "<Keyboard>/tab");
        AssertHasBinding(spectateNext, "<Gamepad>/rightShoulder");
    }

    [Test]
    public void PlayerMotorExposesPhaseOneStaminaState()
    {
        var player = new GameObject("Stamina Test Player");
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();

            Assert.AreEqual(100f, motor.maxStamina);
            Assert.AreEqual(100f, motor.currentStamina);
            Assert.AreEqual(1f, motor.NormalizedStamina);

            motor.currentStamina = 25f;
            Assert.AreEqual(0.25f, motor.NormalizedStamina);

            motor.currentStamina = 250f;
            Assert.AreEqual(1f, motor.NormalizedStamina);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void LookDeltaUsesMousePixelsWithoutDeltaTimeScaling()
    {
        var player = new GameObject("Mouse Look Test Player");
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            motor.mouseSensitivity = 0.11f;

            var first = motor.ResolveLookDelta(new Vector2(10f, -5f), false, 1f / 30f);
            var second = motor.ResolveLookDelta(new Vector2(10f, -5f), false, 1f / 120f);

            Assert.AreEqual(first, second);
            Assert.AreEqual(new Vector2(1.1f, -0.55f), first);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void LookDeltaUsesFrameRateIndependentGamepadDegreesPerSecond()
    {
        var player = new GameObject("Gamepad Look Test Player");
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            motor.gamepadLookSensitivityDegreesPerSecond = 15f;
            motor.gamepadLookDeadzone = 0.08f;

            var lookDelta = motor.ResolveLookDelta(new Vector2(6f, -3f), true, 0.5f);

            Assert.AreEqual(new Vector2(45f, -22.5f), lookDelta);
            Assert.AreEqual(Vector2.zero, motor.ResolveLookDelta(new Vector2(0.02f, 0.02f), true, 0.5f));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void DashSpendsStaminaAndStartsCooldown()
    {
        var player = new GameObject("Dash Cost Test Player");
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            motor.currentStamina = 100f;

            Assert.IsTrue(motor.TryStartDash(Vector2.up));
            Assert.IsTrue(motor.IsDashing);
            Assert.AreEqual(75f, motor.currentStamina, 0.0001f);
            Assert.AreEqual(5f, motor.DashCooldownRemaining, 0.0001f);

            Assert.IsFalse(motor.TryStartDash(Vector2.up));
            Assert.AreEqual(75f, motor.currentStamina, 0.0001f);

            InvokePrivate(motor, "TickDashCooldown", 5f);
            InvokePrivate(motor, "TickDashDuration", 0.51f);
            Assert.IsTrue(motor.TryStartDash(Vector2.right));
            Assert.AreEqual(50f, motor.currentStamina, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void DashRequiresStaminaAndSprintCapableProfile()
    {
        var player = new GameObject("Dash Gate Test Player");
        var profileSet = ScriptableObject.CreateInstance<MovementProfileSet>();
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            motor.movementProfiles = profileSet;
            profileSet.SetProfiles(new[]
            {
                new MovementProfile
                {
                    state = MovementState.Neutral,
                    canMove = true,
                    canSprint = true
                },
                new MovementProfile
                {
                    state = MovementState.Slime,
                    canMove = true,
                    canSprint = false
                }
            });

            motor.currentStamina = 24f;
            Assert.IsFalse(motor.TryStartDash(Vector2.up));
            Assert.AreEqual(24f, motor.currentStamina, 0.0001f);

            motor.currentStamina = 100f;
            motor.SetState(MovementState.Slime);
            Assert.IsFalse(motor.TryStartDash(Vector2.up));
            Assert.AreEqual(100f, motor.currentStamina, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(profileSet);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void SprintDoubleTapStartsDashWithinWindow()
    {
        var player = new GameObject("Sprint Double Tap Dash Test Player");
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            motor.currentStamina = 100f;

            Assert.IsFalse(motor.RegisterSprintTapForDash(1f, Vector2.up));
            Assert.IsFalse(motor.IsDashing);
            Assert.AreEqual(100f, motor.currentStamina, 0.0001f);

            Assert.IsTrue(motor.RegisterSprintTapForDash(1.2f, Vector2.up));
            Assert.IsTrue(motor.IsDashing);
            Assert.AreEqual(75f, motor.currentStamina, 0.0001f);
            Assert.AreEqual(5f, motor.DashCooldownRemaining, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void SprintDoubleTapOutsideWindowDoesNotDash()
    {
        var player = new GameObject("Slow Sprint Double Tap Test Player");
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            motor.currentStamina = 100f;

            Assert.IsFalse(motor.RegisterSprintTapForDash(1f, Vector2.up));
            Assert.IsFalse(motor.RegisterSprintTapForDash(1.5f, Vector2.up));

            Assert.IsFalse(motor.IsDashing);
            Assert.AreEqual(100f, motor.currentStamina, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void SlimeInputSpendsStaminaAndStartsCooldown()
    {
        var player = new GameObject("Slime Cost Test Player");
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            var states = player.AddComponent<PlayerStateController>();
            InvokePrivate(states, "Awake");

            motor.currentStamina = 100f;
            InvokePrivate(states, "HandleSlimePressed");

            Assert.AreEqual(MovementState.Slime, motor.State);
            Assert.AreEqual(50f, motor.currentStamina, 0.0001f);
            Assert.AreEqual(15f, states.SlimeCooldownRemaining, 0.0001f);

            InvokePrivate(states, "TickTimedState", 3.1f);
            Assert.AreEqual(MovementState.Neutral, motor.State);

            InvokePrivate(states, "HandleSlimePressed");
            Assert.AreEqual(
                MovementState.Neutral,
                motor.State,
                "Slime should not reactivate while the 15s cooldown is still running."
            );
            Assert.AreEqual(50f, motor.currentStamina, 0.0001f);

            InvokePrivate(states, "TickSlimeCooldown", 15f);
            InvokePrivate(states, "HandleSlimePressed");

            Assert.AreEqual(MovementState.Slime, motor.State);
            Assert.AreEqual(0f, motor.currentStamina, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void SlimeInputRequiresEnoughStamina()
    {
        var player = new GameObject("Slime Stamina Gate Test Player");
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            var states = player.AddComponent<PlayerStateController>();
            InvokePrivate(states, "Awake");

            motor.currentStamina = 49f;
            InvokePrivate(states, "HandleSlimePressed");

            Assert.AreEqual(MovementState.Neutral, motor.State);
            Assert.AreEqual(49f, motor.currentStamina, 0.0001f);
            Assert.AreEqual(0f, states.SlimeCooldownRemaining, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void SlimeStateShrinksControllerAndSquashesVisual()
    {
        var player = new GameObject("Slime Shape Test Player");
        try
        {
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.36f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            player.transform.localScale = new Vector3(1f, 1.1f, 1.2f);
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();

            motor.SetState(MovementState.Slime);

            Assert.Less(controller.height, 1.8f);
            Assert.Less(controller.radius, 0.36f);
            Assert.AreEqual(controller.height * 0.5f, controller.center.y, 0.0001f);
            Assert.Greater(player.transform.localScale.x, 1f);
            Assert.Less(player.transform.localScale.y, 1.1f);
            Assert.Greater(player.transform.localScale.z, 1.2f);

            motor.SetState(MovementState.Neutral);

            Assert.AreEqual(1.8f, controller.height, 0.0001f);
            Assert.AreEqual(0.36f, controller.radius, 0.0001f);
            Assert.AreEqual(new Vector3(0f, 0.9f, 0f), controller.center);
            Assert.AreEqual(new Vector3(1f, 1.1f, 1.2f), player.transform.localScale);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void SlimeStateReducesNoInputGroundFriction()
    {
        var player = new GameObject("Slime Friction Test Player");
        var neutralProfile = new MovementProfile { state = MovementState.Neutral, acceleration = 20f };
        var slimeProfile = new MovementProfile { state = MovementState.Slime, acceleration = 20f };
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();

            Assert.AreEqual(20f, motor.ResolveGroundAcceleration(neutralProfile, false), 0.0001f);

            motor.SetState(MovementState.Slime);

            Assert.AreEqual(20f, motor.ResolveGroundAcceleration(slimeProfile, true), 0.0001f);
            Assert.AreEqual(6f, motor.ResolveGroundAcceleration(slimeProfile, false), 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void PlayerHudReadsMotorStaminaState()
    {
        var player = new GameObject("Hud Player");
        var hudObject = new GameObject("Hud");
        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));

        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            motor.currentStamina = 50f;

            var fill = fillObject.GetComponent<Image>();
            var statusText = textObject.GetComponent<Text>();
            var hud = hudObject.AddComponent<PlayerHud>();
            hud.Configure(motor, fill, statusText);
            hud.Refresh();

            Assert.AreEqual(0.5f, fill.fillAmount);
            StringAssert.Contains("STA 50/100", statusText.text);
        }
        finally
        {
            Object.DestroyImmediate(textObject);
            Object.DestroyImmediate(fillObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void PlayerHudShowsSlimeCooldownAndStaminaGate()
    {
        var cooldownPlayer = new GameObject("Hud Slime Cooldown Player");
        var staminaPlayer = new GameObject("Hud Slime Stamina Player");
        var cooldownHudObject = new GameObject("Cooldown Hud");
        var staminaHudObject = new GameObject("Stamina Hud");
        var cooldownTextObject = new GameObject("Cooldown Text", typeof(RectTransform), typeof(Text));
        var staminaTextObject = new GameObject("Stamina Text", typeof(RectTransform), typeof(Text));

        try
        {
            cooldownPlayer.AddComponent<CharacterController>();
            cooldownPlayer.AddComponent<PlayerInputReader>();
            var cooldownMotor = cooldownPlayer.AddComponent<PlayerMotor>();
            var cooldownStates = cooldownPlayer.AddComponent<PlayerStateController>();
            InvokePrivate(cooldownStates, "Awake");
            InvokePrivate(cooldownStates, "HandleSlimePressed");

            var cooldownText = cooldownTextObject.GetComponent<Text>();
            var cooldownHud = cooldownHudObject.AddComponent<PlayerHud>();
            cooldownHud.Configure(cooldownMotor, null, cooldownText);
            cooldownHud.Refresh();

            StringAssert.Contains("SLIME CD 15s", cooldownText.text);

            staminaPlayer.AddComponent<CharacterController>();
            staminaPlayer.AddComponent<PlayerInputReader>();
            var staminaMotor = staminaPlayer.AddComponent<PlayerMotor>();
            staminaMotor.currentStamina = 30f;
            staminaPlayer.AddComponent<PlayerStateController>();

            var staminaText = staminaTextObject.GetComponent<Text>();
            var staminaHud = staminaHudObject.AddComponent<PlayerHud>();
            staminaHud.Configure(staminaMotor, null, staminaText);
            staminaHud.Refresh();

            StringAssert.Contains("SLIME STA 50", staminaText.text);
        }
        finally
        {
            Object.DestroyImmediate(staminaTextObject);
            Object.DestroyImmediate(cooldownTextObject);
            Object.DestroyImmediate(staminaHudObject);
            Object.DestroyImmediate(cooldownHudObject);
            Object.DestroyImmediate(staminaPlayer);
            Object.DestroyImmediate(cooldownPlayer);
        }
    }

    [Test]
    public void PlayerHudShowsDashCooldownAndStaminaGate()
    {
        var cooldownPlayer = new GameObject("Hud Dash Cooldown Player");
        var staminaPlayer = new GameObject("Hud Dash Stamina Player");
        var cooldownHudObject = new GameObject("Dash Cooldown Hud");
        var staminaHudObject = new GameObject("Dash Stamina Hud");
        var cooldownTextObject = new GameObject("Dash Cooldown Text", typeof(RectTransform), typeof(Text));
        var staminaTextObject = new GameObject("Dash Stamina Text", typeof(RectTransform), typeof(Text));

        try
        {
            cooldownPlayer.AddComponent<CharacterController>();
            cooldownPlayer.AddComponent<PlayerInputReader>();
            var cooldownMotor = cooldownPlayer.AddComponent<PlayerMotor>();
            Assert.IsTrue(cooldownMotor.TryStartDash(Vector2.up));

            var cooldownText = cooldownTextObject.GetComponent<Text>();
            var cooldownHud = cooldownHudObject.AddComponent<PlayerHud>();
            cooldownHud.Configure(cooldownMotor, null, cooldownText);
            cooldownHud.Refresh();

            StringAssert.Contains("DASH CD 5s", cooldownText.text);

            staminaPlayer.AddComponent<CharacterController>();
            staminaPlayer.AddComponent<PlayerInputReader>();
            var staminaMotor = staminaPlayer.AddComponent<PlayerMotor>();
            staminaMotor.currentStamina = 20f;

            var staminaText = staminaTextObject.GetComponent<Text>();
            var staminaHud = staminaHudObject.AddComponent<PlayerHud>();
            staminaHud.Configure(staminaMotor, null, staminaText);
            staminaHud.Refresh();

            StringAssert.Contains("DASH STA 25", staminaText.text);
        }
        finally
        {
            Object.DestroyImmediate(staminaTextObject);
            Object.DestroyImmediate(cooldownTextObject);
            Object.DestroyImmediate(staminaHudObject);
            Object.DestroyImmediate(cooldownHudObject);
            Object.DestroyImmediate(staminaPlayer);
            Object.DestroyImmediate(cooldownPlayer);
        }
    }

    [Test]
    public void MovementProfileBlocksHeldMovement()
    {
        var profile = new MovementProfile
        {
            state = MovementState.Held,
            canMove = false,
            canSprint = false,
            walkSpeed = 0f,
            runSpeed = 0f
        };

        Assert.AreEqual(0f, profile.MaxSpeed(false));
        Assert.AreEqual(0f, profile.MaxSpeed(true));
    }

    [TestCase(MovementState.Held)]
    [TestCase(MovementState.Captured)]
    [TestCase(MovementState.Holding)]
    public void PlayerMotorClearsHorizontalVelocityImmediatelyWhenProfileCannotMove(MovementState blockedState)
    {
        var player = new GameObject($"Blocked Velocity Test Player {blockedState}");
        var profileSet = ScriptableObject.CreateInstance<MovementProfileSet>();

        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            motor.lockCursorOnStart = false;
            motor.movementProfiles = profileSet;
            profileSet.SetProfiles(new[]
            {
                new MovementProfile
                {
                    state = MovementState.Neutral,
                    canMove = true,
                    walkSpeed = 4f,
                    runSpeed = 7f,
                    acceleration = 18f
                },
                new MovementProfile
                {
                    state = blockedState,
                    canMove = false,
                    canSprint = false,
                    walkSpeed = 0f,
                    runSpeed = 0f,
                    acceleration = 0f
                }
            });

            InvokePrivate(motor, "Awake");
            SetPrivateField(motor, "horizontalVelocity", new Vector3(6f, 0f, -4f));
            motor.SetState(blockedState);

            InvokePrivate(motor, "ApplyMovement", 1f / 60f);

            Assert.AreEqual(
                0f,
                motor.CurrentHorizontalSpeed,
                0.0001f,
                $"{blockedState} must cancel existing horizontal velocity immediately; blocked players cannot glide for an acceleration frame."
            );
        }
        finally
        {
            Object.DestroyImmediate(profileSet);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void CaptureActionUsesRawHeldInputWithoutDurationInteractions()
    {
        var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        Assert.IsNotNull(controls);

        var map = controls.FindActionMap("Player", throwIfNotFound: false);
        Assert.IsNotNull(map);

        var capture = map.FindAction("Capture", throwIfNotFound: false);
        Assert.IsNotNull(capture);

        var keyboardBinding = FindBinding(capture, "<Keyboard>/f");
        Assert.IsTrue(
            string.IsNullOrWhiteSpace(capture.interactions),
            $"Capture action must stay raw held input; found action interaction '{capture.interactions}'."
        );
        Assert.IsTrue(
            string.IsNullOrWhiteSpace(keyboardBinding.interactions),
            $"Capture <Keyboard>/f binding must not require gameplay duration; found binding interaction '{keyboardBinding.interactions}'."
        );
    }

    [Test]
    public void MovementProfileUsesRunSpeedOnlyWhenSprintingAllowed()
    {
        var profile = new MovementProfile
        {
            state = MovementState.Attacker,
            canMove = true,
            canSprint = true,
            walkSpeed = 4f,
            runSpeed = 7f
        };

        Assert.AreEqual(4f, profile.MaxSpeed(false));
        Assert.AreEqual(7f, profile.MaxSpeed(true));
    }

    [Test]
    public void NoiseSystemBroadcastsRunNoise()
    {
        NoiseEvent? received = null;
        void Handler(NoiseEvent noiseEvent) => received = noiseEvent;

        var source = new GameObject("Noise Source");
        try
        {
            NoiseSystem.NoiseEmitted += Handler;
            NoiseSystem.Emit(new NoiseEvent(source, Vector3.one, 8f, MovementState.Attacker));

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(source, received.Value.Source);
            Assert.AreEqual(8f, received.Value.Radius);
            Assert.AreEqual(MovementState.Attacker, received.Value.State);
        }
        finally
        {
            NoiseSystem.NoiseEmitted -= Handler;
            Object.DestroyImmediate(source);
        }
    }

    [Test]
    public void PlayerStateControllerUpdatesMotorState()
    {
        var player = new GameObject("State Test Player");
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            var states = player.AddComponent<PlayerStateController>();

            Assert.AreEqual(MovementState.Neutral, motor.State);

            states.RequestTimedState(MovementState.Slime, 1f);
            Assert.AreEqual(MovementState.Slime, motor.State);

            states.SetPersistentState(MovementState.Held);
            Assert.AreEqual(MovementState.Held, motor.State);

            states.ReturnToDefault();
            Assert.AreEqual(MovementState.Neutral, motor.State);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void TimedStateReturnsToPersistentMatchState()
    {
        var player = new GameObject("Timed Persistent State Player");
        try
        {
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            var states = player.AddComponent<PlayerStateController>();
            InvokePrivate(states, "Awake");

            states.SetPersistentState(MovementState.King);
            states.RequestTimedState(MovementState.Slime, 0.1f);
            Assert.AreEqual(MovementState.Slime, motor.State);

            InvokePrivate(states, "TickTimedState", 0.2f);
            Assert.AreEqual(MovementState.King, motor.State);
            Assert.AreEqual(MovementState.King, states.PersistentState);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    private static void AssertHasBinding(InputAction action, string path)
    {
        FindBinding(action, path);
    }

    private static InputBinding FindBinding(InputAction action, string path)
    {
        foreach (var binding in action.bindings)
        {
            if (binding.path == path)
            {
                return binding;
            }
        }

        Assert.Fail($"{action.name} is missing binding {path}.");
        return default;
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, PrivateInstance);
        Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} should exist for this regression test.");
        method.Invoke(target, args);
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        var field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} should exist for this regression test.");
        field.SetValue(target, value);
    }
}
