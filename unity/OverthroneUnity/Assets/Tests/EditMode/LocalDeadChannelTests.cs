using NUnit.Framework;
using Overthrone;
using UnityEngine;

public sealed class LocalDeadChannelTests
{
    [Test]
    public void DeadChannelAcceptsCapturedTeamMessagesOnly()
    {
        var blueCaptured = CreateAgent("Blue Captured", TeamId.Blue, MovementState.Neutral);
        var blueAlive = CreateAgent("Blue Alive", TeamId.Blue, MovementState.Neutral);
        var redCaptured = CreateAgent("Red Captured", TeamId.Red, MovementState.Neutral);
        var blueKing = CreateAgent("Blue King", TeamId.Blue, MovementState.King);
        var redKing = CreateAgent("Red King", TeamId.Red, MovementState.King);
        var blueHolder = CreateAgent("Blue Holder", TeamId.Blue, MovementState.Attacker);
        var redHolder = CreateAgent("Red Holder", TeamId.Red, MovementState.Attacker);
        var channelObject = new GameObject("Dead Channel");

        try
        {
            Capture(redHolder.Agent, redKing.Agent, blueCaptured.Agent);
            Capture(blueHolder.Agent, blueKing.Agent, redCaptured.Agent);
            var channel = channelObject.AddComponent<LocalDeadChannel>();

            Assert.IsFalse(channel.TryPost(blueAlive.Agent, "alive should not post"));
            Assert.IsTrue(channel.TryPost(blueCaptured.Agent, "hold point A"));
            Assert.IsTrue(channel.TryPost(redCaptured.Agent, "red only"));

            Assert.AreEqual(1, channel.CountVisibleMessages(blueCaptured.Agent));
            Assert.AreEqual(1, channel.CountVisibleMessages(redCaptured.Agent));
            Assert.AreEqual(0, channel.CountVisibleMessages(blueAlive.Agent));

            var blueLog = channel.BuildVisibleLog(blueCaptured.Agent);
            StringAssert.Contains("DEAD CHANNEL", blueLog);
            StringAssert.Contains("Blue team-only", blueLog);
            StringAssert.Contains("Blue Captured: hold point A", blueLog);
            Assert.IsFalse(blueLog.Contains("red only"));

            var redLog = channel.BuildVisibleLog(redCaptured.Agent);
            StringAssert.Contains("Red Captured: red only", redLog);
            Assert.IsFalse(redLog.Contains("hold point A"));
        }
        finally
        {
            Object.DestroyImmediate(channelObject);
            Object.DestroyImmediate(blueCaptured.GameObject);
            Object.DestroyImmediate(blueAlive.GameObject);
            Object.DestroyImmediate(redCaptured.GameObject);
            Object.DestroyImmediate(blueKing.GameObject);
            Object.DestroyImmediate(redKing.GameObject);
            Object.DestroyImmediate(blueHolder.GameObject);
            Object.DestroyImmediate(redHolder.GameObject);
        }
    }

    [Test]
    public void LocalCaptureSystemPostsJoinMessageAfterFinalCapture()
    {
        var king = CreateAgent("Blue King", TeamId.Blue, MovementState.King);
        var holder = CreateAgent("Blue Holder", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Red Target", TeamId.Red, MovementState.Neutral);
        var channelObject = new GameObject("Dead Channel");
        var systemObject = new GameObject("Capture System");

        king.GameObject.transform.position = Vector3.zero;
        holder.GameObject.transform.position = Vector3.forward * 0.5f;
        target.GameObject.transform.position = Vector3.forward;

        try
        {
            var channel = channelObject.AddComponent<LocalDeadChannel>();
            var captureSystem = systemObject.AddComponent<LocalCaptureSystem>();
            captureSystem.Configure(king.Agent, new[] { king.Agent, holder.Agent, target.Agent }, channel);

            Assert.IsTrue(holder.Agent.TryHold(target.Agent));
            Assert.IsTrue(captureSystem.TickFinalCapture(king.Agent, CaptureInteractionRules.CaptureHoldSeconds));

            Assert.AreEqual(CaptureStatus.Captured, target.Agent.Status);
            Assert.AreEqual(1, channel.CountVisibleMessages(target.Agent));
            StringAssert.Contains("System: Red Target joined dead channel", channel.BuildVisibleLog(target.Agent));
            Assert.AreEqual(0, channel.CountVisibleMessages(king.Agent));
        }
        finally
        {
            Object.DestroyImmediate(systemObject);
            Object.DestroyImmediate(channelObject);
            Object.DestroyImmediate(king.GameObject);
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(target.GameObject);
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
        return new AgentFixture(gameObject, motor, teamComponent, agent);
    }

    private static void Capture(PlayerCaptureAgent holder, PlayerCaptureAgent king, PlayerCaptureAgent target)
    {
        Assert.IsTrue(holder.TryHold(target));
        Assert.IsTrue(king.CompleteCapture(target));
    }

    private readonly struct AgentFixture
    {
        public AgentFixture(GameObject gameObject, PlayerMotor motor, LocalPlayerTeam team, PlayerCaptureAgent agent)
        {
            GameObject = gameObject;
            Motor = motor;
            Team = team;
            Agent = agent;
        }

        public GameObject GameObject { get; }
        public PlayerMotor Motor { get; }
        public LocalPlayerTeam Team { get; }
        public PlayerCaptureAgent Agent { get; }
    }
}
