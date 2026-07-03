using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Overthrone;
using UnityEngine;

public sealed class PlayerNoiseEmitterFootstepTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void MovingWithoutSprintDoesNotEmitFootstepNoise()
    {
        using var scenario = NoiseScenario.Create(baseNoiseRadius: 6f);
        var received = new List<NoiseEvent>();
        void Handler(NoiseEvent noiseEvent) => received.Add(noiseEvent);

        scenario.Audio.maxDistance = 99f;
        SetMotorMovement(scenario.Motor, isSprinting: false, horizontalSpeed: 4.5f);

        try
        {
            NoiseSystem.NoiseEmitted += Handler;
            scenario.Emitter.Tick(0f);

            Assert.IsEmpty(received, "Walking movement must stay silent for AI hearing.");
            Assert.AreEqual(99f, scenario.Audio.maxDistance, 0.0001f);
        }
        finally
        {
            NoiseSystem.NoiseEmitted -= Handler;
        }
    }

    [Test]
    public void SprintingWithoutSurfaceInfoUsesOneXFootstepAndNoiseMultipliers()
    {
        using var scenario = NoiseScenario.Create(baseNoiseRadius: 8f);
        var received = new List<NoiseEvent>();
        void Handler(NoiseEvent noiseEvent) => received.Add(noiseEvent);

        scenario.Audio.maxDistance = 1f;
        scenario.Audio.pitch = 1f;
        SetMotorMovement(scenario.Motor, isSprinting: true, horizontalSpeed: 7.2f);

        try
        {
            NoiseSystem.NoiseEmitted += Handler;
            scenario.Emitter.Tick(0f);

            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(scenario.Player, received[0].Source);
            Assert.AreEqual(MovementState.Neutral, received[0].State);
            Assert.AreEqual(8f, received[0].Radius, 0.0001f);
            Assert.AreEqual(8f, scenario.Audio.maxDistance, 0.0001f);
            Assert.AreEqual(1f, scenario.Audio.pitch, 0.0001f);
            Assert.AreEqual(1f, scenario.Emitter.ResolveSurface().VolumeScale, 0.0001f);
        }
        finally
        {
            NoiseSystem.NoiseEmitted -= Handler;
        }
    }

    [Test]
    public void SprintingInsideFootstepIntervalDoesNotDuplicateNoiseEvents()
    {
        using var scenario = NoiseScenario.Create(baseNoiseRadius: 8f, footstepInterval: 0.5f);
        var received = new List<NoiseEvent>();
        void Handler(NoiseEvent noiseEvent) => received.Add(noiseEvent);

        SetMotorMovement(scenario.Motor, isSprinting: true, horizontalSpeed: 7.2f);

        try
        {
            NoiseSystem.NoiseEmitted += Handler;
            scenario.Emitter.Tick(0f);
            scenario.Emitter.Tick(0f);

            Assert.AreEqual(1, received.Count);
        }
        finally
        {
            NoiseSystem.NoiseEmitted -= Handler;
        }
    }

    [Test]
    public void SurfaceNoiseRadiusMultiplierIsAppliedToEmittedNoiseRadius()
    {
        using var scenario = NoiseScenario.Create(baseNoiseRadius: 8f);
        using var surface = FootstepSurfaceFixture.CreateUnder(scenario.Player);
        surface.Component.VolumeMultiplier = 0.5f;
        surface.Component.PitchMultiplier = 1.25f;
        surface.Component.NoiseRadiusMultiplier = 1.75f;
        scenario.GroundOnSurface();

        var received = new List<NoiseEvent>();
        void Handler(NoiseEvent noiseEvent) => received.Add(noiseEvent);

        scenario.Audio.maxDistance = 1f;
        scenario.Audio.pitch = 1f;
        SetMotorMovement(scenario.Motor, isSprinting: true, horizontalSpeed: 7.2f);

        try
        {
            NoiseSystem.NoiseEmitted += Handler;
            scenario.Emitter.Tick(0f);

            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(14f, received[0].Radius, 0.0001f);
            Assert.AreEqual(14f, scenario.Audio.maxDistance, 0.0001f);
            Assert.AreEqual(1.25f, scenario.Audio.pitch, 0.0001f);
            Assert.AreEqual(0.5f, scenario.Emitter.ResolveSurface().VolumeScale, 0.0001f);
        }
        finally
        {
            NoiseSystem.NoiseEmitted -= Handler;
        }
    }

    [Test]
    public void AirbornePlayerDoesNotResolveDistantSurfaceBelow()
    {
        using var scenario = NoiseScenario.Create(baseNoiseRadius: 8f);
        using var surface = FootstepSurfaceFixture.CreateUnder(scenario.Player);
        surface.Component.VolumeMultiplier = 0.5f;
        surface.Component.PitchMultiplier = 1.25f;
        surface.Component.NoiseRadiusMultiplier = 1.75f;

        var received = new List<NoiseEvent>();
        void Handler(NoiseEvent noiseEvent) => received.Add(noiseEvent);

        SetMotorMovement(scenario.Motor, isSprinting: true, horizontalSpeed: 7.2f);

        try
        {
            NoiseSystem.NoiseEmitted += Handler;
            scenario.Emitter.Tick(0f);

            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(8f, received[0].Radius, 0.0001f);
            Assert.AreEqual(1f, scenario.Audio.pitch, 0.0001f);
            Assert.AreEqual(1f, scenario.Emitter.ResolveSurface().VolumeScale, 0.0001f);
        }
        finally
        {
            NoiseSystem.NoiseEmitted -= Handler;
        }
    }

    [Test]
    public void GroundedOnPlainColliderDoesNotResolveSurfaceBelowIt()
    {
        using var scenario = NoiseScenario.Create(baseNoiseRadius: 8f);
        using var lowerSurface = FootstepSurfaceFixture.CreateUnder(scenario.Player, yOffset: -0.125f, height: 0.05f);
        using var plainGround = PlainGroundFixture.CreateUnder(scenario.Player);
        lowerSurface.Component.VolumeMultiplier = 0.5f;
        lowerSurface.Component.PitchMultiplier = 1.25f;
        lowerSurface.Component.NoiseRadiusMultiplier = 1.75f;
        scenario.GroundOnSurface();

        var received = new List<NoiseEvent>();
        void Handler(NoiseEvent noiseEvent) => received.Add(noiseEvent);

        SetMotorMovement(scenario.Motor, isSprinting: true, horizontalSpeed: 7.2f);

        try
        {
            NoiseSystem.NoiseEmitted += Handler;
            scenario.Emitter.Tick(0f);

            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(8f, received[0].Radius, 0.0001f);
            Assert.AreEqual(1f, scenario.Audio.pitch, 0.0001f);
            Assert.AreEqual(1f, scenario.Emitter.ResolveSurface().VolumeScale, 0.0001f);
        }
        finally
        {
            NoiseSystem.NoiseEmitted -= Handler;
        }
    }

    [Test]
    public void SurfaceMultipliersAreClampedToAudibleAndNonNegativeRanges()
    {
        var multipliers = new FootstepSurfaceMultipliers(-1f, -2f, -3f);

        Assert.AreEqual(0f, multipliers.VolumeScale);
        Assert.AreEqual(0.01f, multipliers.PitchScale);
        Assert.AreEqual(0f, multipliers.NoiseRadiusScale);
    }

    private static void SetMotorMovement(PlayerMotor motor, bool isSprinting, float horizontalSpeed)
    {
        SetPrivateField(motor, "isSprinting", isSprinting);
        SetPrivateField(motor, "horizontalVelocity", Vector3.forward * horizontalSpeed);
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

    private sealed class NoiseScenario : IDisposable
    {
        private NoiseScenario(GameObject player, MovementProfileSet profiles, CharacterController controller, PlayerMotor motor, PlayerNoiseEmitter emitter, AudioSource audio)
        {
            Player = player;
            Profiles = profiles;
            Controller = controller;
            Motor = motor;
            Emitter = emitter;
            Audio = audio;
        }

        public GameObject Player { get; }
        public MovementProfileSet Profiles { get; }
        public CharacterController Controller { get; }
        public PlayerMotor Motor { get; }
        public PlayerNoiseEmitter Emitter { get; }
        public AudioSource Audio { get; }

        public static NoiseScenario Create(float baseNoiseRadius, float footstepInterval = 0.5f)
        {
            var player = new GameObject("Footstep Noise Test Player");
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.36f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>();
            var audio = player.AddComponent<AudioSource>();
            var emitter = player.AddComponent<PlayerNoiseEmitter>();
            var profiles = ScriptableObject.CreateInstance<MovementProfileSet>();
            profiles.SetProfiles(new[]
            {
                new MovementProfile
                {
                    state = MovementState.Neutral,
                    canMove = true,
                    canSprint = true,
                    walkSpeed = 4.5f,
                    runSpeed = 7.2f,
                    acceleration = 100f,
                    noiseRadius = baseNoiseRadius,
                    footstepInterval = footstepInterval
                }
            });

            motor.movementProfiles = profiles;
            motor.SetState(MovementState.Neutral);
            InvokePrivate(motor, "Awake");
            InvokePrivate(emitter, "Awake");
            return new NoiseScenario(player, profiles, controller, motor, emitter, audio);
        }

        public void GroundOnSurface()
        {
            Physics.SyncTransforms();
            Controller.Move(Vector3.down * 0.05f);
            Physics.SyncTransforms();
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Profiles);
            UnityEngine.Object.DestroyImmediate(Player);
        }
    }

    private sealed class FootstepSurfaceFixture : IDisposable
    {
        private FootstepSurfaceFixture(GameObject ground, FootstepSurface component)
        {
            Ground = ground;
            Component = component;
        }

        private GameObject Ground { get; }
        public FootstepSurface Component { get; }

        public static FootstepSurfaceFixture CreateUnder(GameObject player, float yOffset = -0.025f, float height = 0.05f)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Noisy Footstep Surface";
            ground.transform.position = player.transform.position + Vector3.up * yOffset;
            ground.transform.localScale = new Vector3(8f, height, 8f);
            var fixture = new FootstepSurfaceFixture(ground, ground.AddComponent<FootstepSurface>());
            Physics.SyncTransforms();
            return fixture;
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Ground);
        }
    }

    private sealed class PlainGroundFixture : IDisposable
    {
        private PlainGroundFixture(GameObject ground)
        {
            Ground = ground;
        }

        private GameObject Ground { get; }

        public static PlainGroundFixture CreateUnder(GameObject player)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Plain Footstep Ground";
            ground.transform.position = player.transform.position + Vector3.down * 0.025f;
            ground.transform.localScale = new Vector3(8f, 0.05f, 8f);
            var fixture = new PlainGroundFixture(ground);
            Physics.SyncTransforms();
            return fixture;
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Ground);
        }
    }
}
