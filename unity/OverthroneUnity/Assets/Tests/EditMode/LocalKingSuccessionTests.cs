using System.Reflection;
using NUnit.Framework;
using Overthrone;
using UnityEngine;

public sealed class LocalKingSuccessionTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void MatchManagerPromotesOnlyHighestCaptureCountCandidateToKing()
    {
        var fixture = CreateFixture();
        var quietRunner = CreateParticipant("Quiet Runner", TeamId.Blue, 0, 5f, 10);
        var captor = CreateParticipant("Recent Captor", TeamId.Blue, 1, 0f, 1);
        var contributor = CreateParticipant("Point Contributor", TeamId.Blue, 0, 20f, 99);

        try
        {
            fixture.Manager.Configure(fixture.Points, new[] { quietRunner.Team, captor.Team, contributor.Team });
            fixture.Manager.ApplyMatchRules(0f);

            Assert.AreEqual(MovementState.Neutral, quietRunner.StateController.PersistentState);
            Assert.AreEqual(MovementState.King, captor.StateController.PersistentState);
            Assert.AreEqual(MovementState.Neutral, contributor.StateController.PersistentState);
        }
        finally
        {
            quietRunner.Destroy();
            captor.Destroy();
            contributor.Destroy();
            fixture.Destroy();
        }
    }

    [Test]
    public void MatchManagerUsesPointContributionWhenCaptureCountsTie()
    {
        var fixture = CreateFixture();
        var lowContributor = CreateParticipant("Low Contributor", TeamId.Blue, 0, 0.2f, 99);
        var highContributor = CreateParticipant("High Contributor", TeamId.Blue, 0, 0.8f, 1);

        try
        {
            fixture.Manager.Configure(fixture.Points, new[] { lowContributor.Team, highContributor.Team });
            fixture.Manager.ApplyMatchRules(0f);

            Assert.AreEqual(MovementState.Neutral, lowContributor.StateController.PersistentState);
            Assert.AreEqual(MovementState.King, highContributor.StateController.PersistentState);
        }
        finally
        {
            lowContributor.Destroy();
            highContributor.Destroy();
            fixture.Destroy();
        }
    }

    [Test]
    public void CompleteCaptureIncrementsKingPriorityCaptureCount()
    {
        var king = CreateAgent("Priority King", TeamId.Blue, MovementState.King);
        var target = CreateAgent("Priority Target", TeamId.Red, MovementState.Neutral);

        try
        {
            Assert.IsTrue(king.Agent.TryHold(target.Agent));
            Assert.IsTrue(king.Agent.CompleteCapture(target.Agent));

            Assert.AreEqual(1, king.Team.FinalCaptureCount);
        }
        finally
        {
            king.Destroy();
            target.Destroy();
        }
    }

    [Test]
    public void CapturePointAwardsPointContributionToActiveCapturers()
    {
        var pointObject = new GameObject("Contribution Point");
        var point = pointObject.AddComponent<CapturePoint>();
        var blue = CreateParticipant("Blue Contributor", TeamId.Blue, 0, 0f, 1);

        try
        {
            AddOccupant(point, blue.Team);

            point.Tick(1f);

            Assert.AreEqual(CapturePointProgress.OnePlayerCaptureRatePerSecond, blue.Team.PointContribution, 0.0001f);
        }
        finally
        {
            blue.Destroy();
            Object.DestroyImmediate(pointObject);
        }
    }

    private static MatchFixture CreateFixture()
    {
        var points = new CapturePoint[3];
        for (var index = 0; index < points.Length; index++)
        {
            var pointObject = new GameObject($"King Point {index + 1}");
            var point = pointObject.AddComponent<CapturePoint>();
            point.Configure(((char)('A' + index)).ToString(), 5f);
            SetPointOwner(point, index < 2 ? TeamId.Blue : TeamId.None);
            points[index] = point;
        }

        var managerObject = new GameObject("King Succession Match Manager");
        var manager = managerObject.AddComponent<LocalMatchManager>();
        manager.Configure(points);
        return new MatchFixture(managerObject, manager, points);
    }

    private static ParticipantFixture CreateParticipant(
        string name,
        TeamId team,
        int captures,
        float contribution,
        int tieBreaker)
    {
        var gameObject = new GameObject(name);
        var localTeam = gameObject.AddComponent<LocalPlayerTeam>();
        localTeam.Configure(team);
        localTeam.ConfigureKingPriority(captures, contribution, tieBreaker);
        var stateController = gameObject.AddComponent<PlayerStateController>();
        stateController.SetPersistentState(MovementState.Neutral);
        return new ParticipantFixture(gameObject, localTeam, stateController);
    }

    private static AgentFixture CreateAgent(string name, TeamId team, MovementState state)
    {
        var gameObject = new GameObject(name);
        var localTeam = gameObject.AddComponent<LocalPlayerTeam>();
        localTeam.Configure(team);
        var stateController = gameObject.AddComponent<PlayerStateController>();
        stateController.SetPersistentState(state);
        var agent = gameObject.AddComponent<PlayerCaptureAgent>();
        agent.Configure(stateController);
        return new AgentFixture(gameObject, localTeam, stateController, agent);
    }

    private static void SetPointOwner(CapturePoint point, TeamId team)
    {
        var field = typeof(CapturePoint).GetField("progress", PrivateInstance);
        Assert.IsNotNull(field, "CapturePoint.progress should exist for this king succession test.");
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

    private static void AddOccupant(CapturePoint point, LocalPlayerTeam occupant)
    {
        var field = typeof(CapturePoint).GetField("occupants", PrivateInstance);
        Assert.IsNotNull(field, "CapturePoint.occupants should exist for this contribution test.");
        var occupants = (System.Collections.Generic.HashSet<LocalPlayerTeam>)field.GetValue(point);
        occupants.Add(occupant);
    }

    private readonly struct MatchFixture
    {
        public MatchFixture(GameObject gameObject, LocalMatchManager manager, CapturePoint[] points)
        {
            GameObject = gameObject;
            Manager = manager;
            Points = points;
        }

        public GameObject GameObject { get; }
        public LocalMatchManager Manager { get; }
        public CapturePoint[] Points { get; }

        public void Destroy()
        {
            Object.DestroyImmediate(GameObject);
            foreach (var point in Points)
            {
                if (point != null)
                {
                    Object.DestroyImmediate(point.gameObject);
                }
            }
        }
    }

    private readonly struct ParticipantFixture
    {
        public ParticipantFixture(GameObject gameObject, LocalPlayerTeam team, PlayerStateController stateController)
        {
            GameObject = gameObject;
            Team = team;
            StateController = stateController;
        }

        public GameObject GameObject { get; }
        public LocalPlayerTeam Team { get; }
        public PlayerStateController StateController { get; }

        public void Destroy()
        {
            Object.DestroyImmediate(GameObject);
        }
    }

    private readonly struct AgentFixture
    {
        public AgentFixture(
            GameObject gameObject,
            LocalPlayerTeam team,
            PlayerStateController stateController,
            PlayerCaptureAgent agent)
        {
            GameObject = gameObject;
            Team = team;
            StateController = stateController;
            Agent = agent;
        }

        public GameObject GameObject { get; }
        public LocalPlayerTeam Team { get; }
        public PlayerStateController StateController { get; }
        public PlayerCaptureAgent Agent { get; }

        public void Destroy()
        {
            Object.DestroyImmediate(GameObject);
        }
    }
}
