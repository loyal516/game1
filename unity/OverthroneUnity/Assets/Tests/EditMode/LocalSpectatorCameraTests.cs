using NUnit.Framework;
using Overthrone;
using UnityEngine;
using UnityEngine.UI;

public sealed class LocalSpectatorCameraTests
{
    [Test]
    public void CapturedLocalPlayerSpectatesFirstAliveAlly()
    {
        var local = CreateAgent("Captured Local", TeamId.Blue, MovementState.Neutral);
        var ally = CreateAgent("Alive Ally", TeamId.Blue, MovementState.Neutral);
        var enemyKing = CreateAgent("Enemy King", TeamId.Red, MovementState.King);
        var cameraFixture = CreateSpectatorCamera();

        ally.GameObject.transform.position = new Vector3(3f, 1f, 2f);

        try
        {
            Capture(enemyKing.Agent, local.Agent);

            cameraFixture.Spectator.Configure(local.Agent, new[] { local.Agent, ally.Agent, enemyKing.Agent });

            Assert.IsTrue(cameraFixture.Spectator.IsSpectating);
            Assert.AreEqual(ally.Agent, cameraFixture.Spectator.SpectatorTarget);
            Assert.IsNull(cameraFixture.CameraObject.transform.parent);
            AssertVectorApproximately(
                ally.GameObject.transform.TransformPoint(cameraFixture.Spectator.FollowOffset),
                cameraFixture.CameraObject.transform.position
            );
        }
        finally
        {
            cameraFixture.Destroy();
            Object.DestroyImmediate(local.GameObject);
            Object.DestroyImmediate(ally.GameObject);
            Object.DestroyImmediate(enemyKing.GameObject);
        }
    }

    [Test]
    public void CapturedLocalPlayerFallsBackToSelfWhenNoLiveAllyExists()
    {
        var local = CreateAgent("Captured Local Fallback", TeamId.Blue, MovementState.Neutral);
        var ally = CreateAgent("Captured Ally", TeamId.Blue, MovementState.Neutral);
        var enemyKing = CreateAgent("Enemy King", TeamId.Red, MovementState.King);
        var cameraFixture = CreateSpectatorCamera();

        local.GameObject.transform.position = new Vector3(-2f, 1f, 4f);

        try
        {
            Capture(enemyKing.Agent, local.Agent);
            Capture(enemyKing.Agent, ally.Agent);

            cameraFixture.Spectator.Configure(local.Agent, new[] { local.Agent, ally.Agent, enemyKing.Agent });

            Assert.IsTrue(cameraFixture.Spectator.IsSpectating);
            Assert.AreEqual(local.Agent, cameraFixture.Spectator.SpectatorTarget);
            AssertVectorApproximately(
                local.GameObject.transform.TransformPoint(cameraFixture.Spectator.FollowOffset),
                cameraFixture.CameraObject.transform.position
            );
        }
        finally
        {
            cameraFixture.Destroy();
            Object.DestroyImmediate(local.GameObject);
            Object.DestroyImmediate(ally.GameObject);
            Object.DestroyImmediate(enemyKing.GameObject);
        }
    }

    [Test]
    public void SpectatorTargetCyclesAcrossLiveAlliesOnly()
    {
        var local = CreateAgent("Captured Local Cycle", TeamId.Blue, MovementState.Neutral);
        var firstAlly = CreateAgent("First Ally", TeamId.Blue, MovementState.Neutral);
        var secondAlly = CreateAgent("Second Ally", TeamId.Blue, MovementState.Neutral);
        var enemy = CreateAgent("Enemy", TeamId.Red, MovementState.Neutral);
        var enemyKing = CreateAgent("Enemy King", TeamId.Red, MovementState.King);
        var cameraFixture = CreateSpectatorCamera();

        try
        {
            Capture(enemyKing.Agent, local.Agent);

            cameraFixture.Spectator.Configure(
                local.Agent,
                new[] { local.Agent, firstAlly.Agent, enemy.Agent, secondAlly.Agent }
            );

            Assert.AreEqual(firstAlly.Agent, cameraFixture.Spectator.SpectatorTarget);
            Assert.AreEqual(secondAlly.Agent, cameraFixture.Spectator.CycleTarget(1));
            Assert.AreEqual(firstAlly.Agent, cameraFixture.Spectator.CycleTarget(1));
            Assert.AreEqual(secondAlly.Agent, cameraFixture.Spectator.CycleTarget(-1));
        }
        finally
        {
            cameraFixture.Destroy();
            Object.DestroyImmediate(local.GameObject);
            Object.DestroyImmediate(firstAlly.GameObject);
            Object.DestroyImmediate(secondAlly.GameObject);
            Object.DestroyImmediate(enemy.GameObject);
            Object.DestroyImmediate(enemyKing.GameObject);
        }
    }

    [Test]
    public void SpectatorInputHandlerCyclesTargetsOnlyWhileCaptured()
    {
        var local = CreateAgent("Captured Local Input Cycle", TeamId.Blue, MovementState.Neutral);
        var firstAlly = CreateAgent("First Input Ally", TeamId.Blue, MovementState.Neutral);
        var secondAlly = CreateAgent("Second Input Ally", TeamId.Blue, MovementState.Neutral);
        var enemyKing = CreateAgent("Enemy King", TeamId.Red, MovementState.King);
        var cameraFixture = CreateSpectatorCamera();

        try
        {
            Assert.IsNull(cameraFixture.Spectator.HandleSpectatorInput(false, true));

            Capture(enemyKing.Agent, local.Agent);
            cameraFixture.Spectator.Configure(
                local.Agent,
                new[] { local.Agent, firstAlly.Agent, secondAlly.Agent }
            );

            Assert.AreEqual(firstAlly.Agent, cameraFixture.Spectator.SpectatorTarget);
            Assert.AreEqual(secondAlly.Agent, cameraFixture.Spectator.HandleSpectatorInput(false, true));
            Assert.AreEqual(firstAlly.Agent, cameraFixture.Spectator.HandleSpectatorInput(true, false));
        }
        finally
        {
            cameraFixture.Destroy();
            Object.DestroyImmediate(local.GameObject);
            Object.DestroyImmediate(firstAlly.GameObject);
            Object.DestroyImmediate(secondAlly.GameObject);
            Object.DestroyImmediate(enemyKing.GameObject);
        }
    }

    [Test]
    public void PlayerHudShowsSpectatorOverlayWhileCaptured()
    {
        var local = CreateAgent("Captured Local Hud", TeamId.Blue, MovementState.Neutral);
        var ally = CreateAgent("Alive Hud Ally", TeamId.Blue, MovementState.Neutral);
        var enemyKing = CreateAgent("Enemy King", TeamId.Red, MovementState.King);
        var cameraFixture = CreateSpectatorCamera();
        var hudObject = new GameObject("Spectator Hud");
        var statusObject = new GameObject("Status Text", typeof(RectTransform), typeof(Text));
        var spectatorObject = new GameObject("Spectator Text", typeof(RectTransform), typeof(Text));
        var deadChannelObject = new GameObject("Dead Channel");

        try
        {
            Capture(enemyKing.Agent, local.Agent);
            cameraFixture.Spectator.Configure(local.Agent, new[] { local.Agent, ally.Agent });
            var deadChannel = deadChannelObject.AddComponent<LocalDeadChannel>();
            Assert.IsTrue(deadChannel.TryPost(local.Agent, "watch left point"));

            var statusText = statusObject.GetComponent<Text>();
            var spectatorText = spectatorObject.GetComponent<Text>();
            var hud = hudObject.AddComponent<PlayerHud>();
            hud.Configure(
                local.Motor,
                null,
                statusText,
                playerCaptureAgent: local.Agent,
                localSpectatorCamera: cameraFixture.Spectator,
                spectatorOverlayText: spectatorText,
                localDeadChannel: deadChannel
            );

            hud.Refresh();

            Assert.IsTrue(spectatorText.gameObject.activeSelf);
            StringAssert.Contains("SPECTATING", spectatorText.text);
            StringAssert.Contains(ally.GameObject.name, spectatorText.text);
            StringAssert.Contains("Q Previous", spectatorText.text);
            StringAssert.Contains("Tab Next", spectatorText.text);
            StringAssert.Contains("DEAD CHANNEL", spectatorText.text);
            StringAssert.Contains("watch left point", spectatorText.text);
        }
        finally
        {
            Object.DestroyImmediate(deadChannelObject);
            Object.DestroyImmediate(spectatorObject);
            Object.DestroyImmediate(statusObject);
            Object.DestroyImmediate(hudObject);
            cameraFixture.Destroy();
            Object.DestroyImmediate(local.GameObject);
            Object.DestroyImmediate(ally.GameObject);
            Object.DestroyImmediate(enemyKing.GameObject);
        }
    }

    private static void Capture(PlayerCaptureAgent king, PlayerCaptureAgent target)
    {
        var holderFixture = CreateAgent($"{king.name} Holder", king.Team.Team, MovementState.Attacker);
        try
        {
            Assert.IsTrue(holderFixture.Agent.TryHold(target));
            Assert.IsTrue(king.CompleteCapture(target));
        }
        finally
        {
            Object.DestroyImmediate(holderFixture.GameObject);
        }
    }

    private static AgentFixture CreateAgent(string name, TeamId team, MovementState state)
    {
        var gameObject = new GameObject(name);
        gameObject.AddComponent<CharacterController>();
        gameObject.AddComponent<PlayerInputReader>();
        var motor = gameObject.AddComponent<PlayerMotor>();
        var teamComponent = gameObject.AddComponent<LocalPlayerTeam>();
        teamComponent.Configure(team);
        var stateController = gameObject.AddComponent<PlayerStateController>();
        var agent = gameObject.AddComponent<PlayerCaptureAgent>();
        stateController.SetPersistentState(state);
        agent.Configure(stateController);
        return new AgentFixture(gameObject, motor, stateController, agent);
    }

    private static CameraFixture CreateSpectatorCamera()
    {
        var pivot = new GameObject("Default Camera Parent");
        var cameraObject = new GameObject("Spectator Camera");
        cameraObject.transform.SetParent(pivot.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1f, -4f);
        cameraObject.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        return new CameraFixture(pivot, cameraObject, cameraObject.AddComponent<LocalSpectatorCamera>());
    }

    private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
    {
        Assert.LessOrEqual(Vector3.Distance(expected, actual), 0.001f);
    }

    private readonly struct AgentFixture
    {
        public AgentFixture(GameObject gameObject, PlayerMotor motor, PlayerStateController stateController, PlayerCaptureAgent agent)
        {
            GameObject = gameObject;
            Motor = motor;
            StateController = stateController;
            Agent = agent;
        }

        public GameObject GameObject { get; }
        public PlayerMotor Motor { get; }
        public PlayerStateController StateController { get; }
        public PlayerCaptureAgent Agent { get; }
    }

    private readonly struct CameraFixture
    {
        public CameraFixture(GameObject pivot, GameObject cameraObject, LocalSpectatorCamera spectator)
        {
            Pivot = pivot;
            CameraObject = cameraObject;
            Spectator = spectator;
        }

        public GameObject Pivot { get; }
        public GameObject CameraObject { get; }
        public LocalSpectatorCamera Spectator { get; }

        public void Destroy()
        {
            Object.DestroyImmediate(CameraObject);
            Object.DestroyImmediate(Pivot);
        }
    }
}
