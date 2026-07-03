using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Overthrone;
using UnityEngine;

public sealed class LocalMatchFlowTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void VictoryCountdownStartCreatesDefenderReentryWindow()
    {
        var fixture = CreateMatchFixture(TeamId.Blue, TeamId.Blue, TeamId.Blue);
        var events = new List<LocalMatchFlowEvent>();

        try
        {
            fixture.Manager.FlowChanged += events.Add;
            fixture.Manager.ApplyMatchRules(0f);

            Assert.AreEqual(LocalMatchPhase.VictoryCountdown, fixture.Manager.Phase);
            Assert.AreEqual(TeamId.Blue, fixture.Manager.VictoryCountdownTeam);
            Assert.AreEqual(TeamId.Red, fixture.Manager.DefenderTeam);
            Assert.IsTrue(fixture.Manager.IsDefenderReentryWindowActive);
            Assert.AreEqual(LocalMatchRules.VictoryCountdownSeconds, fixture.Manager.DefenderReentryTimeRemaining);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(LocalMatchFlowEventType.VictoryCountdownStarted, events[0].Type);
            Assert.AreEqual(TeamId.Blue, events[0].Team);
            Assert.AreEqual(TeamId.Red, events[0].DefenderTeam);
        }
        finally
        {
            fixture.Manager.FlowChanged -= events.Add;
            fixture.Destroy();
        }
    }

    [Test]
    public void DefenderBreaksFullControlAndInterruptsVictoryCountdown()
    {
        var fixture = CreateMatchFixture(TeamId.Blue, TeamId.Blue, TeamId.Blue);
        var events = new List<LocalMatchFlowEvent>();

        try
        {
            fixture.Manager.FlowChanged += events.Add;
            fixture.Manager.ApplyMatchRules(0f);
            fixture.Manager.ApplyMatchRules(5f);

            SetPointOwner(fixture.Points[2], TeamId.None);
            fixture.Manager.ApplyMatchRules(0f);

            Assert.AreEqual(LocalMatchPhase.Playing, fixture.Manager.Phase);
            Assert.AreEqual(TeamId.None, fixture.Manager.VictoryCountdownTeam);
            Assert.AreEqual(TeamId.None, fixture.Manager.DefenderTeam);
            Assert.IsFalse(fixture.Manager.IsDefenderReentryWindowActive);
            Assert.AreEqual(LocalMatchRules.VictoryCountdownSeconds, fixture.Manager.VictoryTimeRemaining);
            Assert.AreEqual(2, events.Count);
            Assert.AreEqual(LocalMatchFlowEventType.VictoryCountdownInterrupted, events[1].Type);
            Assert.AreEqual(TeamId.Blue, events[1].Team);
            Assert.AreEqual(25f, events[1].RemainingSeconds);
        }
        finally
        {
            fixture.Manager.FlowChanged -= events.Add;
            fixture.Destroy();
        }
    }

    [Test]
    public void CountdownCompletionMovesMatchToResult()
    {
        var fixture = CreateMatchFixture(TeamId.Red, TeamId.Red, TeamId.Red);
        var events = new List<LocalMatchFlowEvent>();

        try
        {
            fixture.Manager.FlowChanged += events.Add;
            fixture.Manager.ApplyMatchRules(0f);
            fixture.Manager.ApplyMatchRules(LocalMatchRules.VictoryCountdownSeconds);

            Assert.AreEqual(LocalMatchPhase.Result, fixture.Manager.Phase);
            Assert.AreEqual(TeamId.Red, fixture.Manager.Winner);
            Assert.AreEqual(TeamId.None, fixture.Manager.DefenderTeam);
            Assert.IsFalse(fixture.Manager.IsDefenderReentryWindowActive);
            Assert.AreEqual(2, events.Count);
            Assert.AreEqual(LocalMatchFlowEventType.RoundEnded, events[1].Type);
            Assert.AreEqual(TeamId.Red, events[1].Team);
            Assert.AreEqual(0f, events[1].RemainingSeconds);
            Assert.AreEqual(events[1].Type, fixture.Manager.LastFlowEvent.Type);
        }
        finally
        {
            fixture.Manager.FlowChanged -= events.Add;
            fixture.Destroy();
        }
    }

    [Test]
    public void AllCapturedOpponentsImmediatelyEndRound()
    {
        var blue = CreateAgent("Blue King", TeamId.Blue, MovementState.King);
        var red = CreateAgent("Red Target", TeamId.Red, MovementState.Neutral);
        var managerObject = new GameObject("All Captured Match Manager");
        var manager = managerObject.AddComponent<LocalMatchManager>();
        var events = new List<LocalMatchFlowEvent>();

        try
        {
            Assert.IsTrue(blue.Agent.TryHold(red.Agent));
            Assert.IsTrue(blue.Agent.CompleteCapture(red.Agent));

            manager.Configure(System.Array.Empty<CapturePoint>(), new[] { blue.Team, red.Team });
            manager.FlowChanged += events.Add;
            manager.ApplyMatchRules(0f);

            Assert.AreEqual(LocalMatchPhase.Result, manager.Phase);
            Assert.AreEqual(TeamId.Blue, manager.Winner);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(LocalMatchFlowEventType.RoundEnded, events[0].Type);
            Assert.AreEqual(TeamId.Blue, events[0].Team);
        }
        finally
        {
            manager.FlowChanged -= events.Add;
            Object.DestroyImmediate(managerObject);
            blue.Destroy();
            red.Destroy();
        }
    }

    private static MatchFixture CreateMatchFixture(params TeamId[] owners)
    {
        var points = new CapturePoint[owners.Length];
        for (var index = 0; index < owners.Length; index++)
        {
            var gameObject = new GameObject($"Flow Point {index + 1}");
            var point = gameObject.AddComponent<CapturePoint>();
            point.Configure(((char)('A' + index)).ToString(), 5f);
            SetPointOwner(point, owners[index]);
            points[index] = point;
        }

        var managerObject = new GameObject("Flow Match Manager");
        var manager = managerObject.AddComponent<LocalMatchManager>();
        manager.Configure(points);
        return new MatchFixture(managerObject, manager, points);
    }

    private static void SetPointOwner(CapturePoint point, TeamId team)
    {
        var field = typeof(CapturePoint).GetField("progress", PrivateInstance);
        Assert.IsNotNull(field, "CapturePoint.progress should exist for this local match flow test.");
        var progress = (CapturePointProgress)field.GetValue(point);
        progress.Reset();

        if (team == TeamId.Blue)
        {
            progress.Tick(3, 0, 10f);
        }
        else if (team == TeamId.Red)
        {
            progress.Tick(0, 3, 10f);
        }
    }

    private static AgentFixture CreateAgent(string name, TeamId team, MovementState state)
    {
        var gameObject = new GameObject(name);
        gameObject.AddComponent<CharacterController>();
        gameObject.AddComponent<PlayerInputReader>();
        gameObject.AddComponent<PlayerMotor>();
        var localTeam = gameObject.AddComponent<LocalPlayerTeam>();
        localTeam.Configure(team);
        var stateController = gameObject.AddComponent<PlayerStateController>();
        stateController.SetPersistentState(state);
        var agent = gameObject.AddComponent<PlayerCaptureAgent>();
        agent.Configure(stateController);
        return new AgentFixture(gameObject, localTeam, agent);
    }

    private readonly struct MatchFixture
    {
        public MatchFixture(GameObject managerObject, LocalMatchManager manager, CapturePoint[] points)
        {
            ManagerObject = managerObject;
            Manager = manager;
            Points = points;
        }

        public GameObject ManagerObject { get; }
        public LocalMatchManager Manager { get; }
        public CapturePoint[] Points { get; }

        public void Destroy()
        {
            Object.DestroyImmediate(ManagerObject);
            foreach (var point in Points)
            {
                if (point != null)
                {
                    Object.DestroyImmediate(point.gameObject);
                }
            }
        }
    }

    private readonly struct AgentFixture
    {
        public AgentFixture(GameObject gameObject, LocalPlayerTeam team, PlayerCaptureAgent agent)
        {
            GameObject = gameObject;
            Team = team;
            Agent = agent;
        }

        public GameObject GameObject { get; }
        public LocalPlayerTeam Team { get; }
        public PlayerCaptureAgent Agent { get; }

        public void Destroy()
        {
            Object.DestroyImmediate(GameObject);
        }
    }
}
