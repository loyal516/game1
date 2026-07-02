using NUnit.Framework;
using Overthrone;
using UnityEngine;
using UnityEngine.UI;

public sealed class LocalMatchFlowPresenterTests
{
    [Test]
    public void CountdownStartShowsBannerAndReentersFreeDefenders()
    {
        var defender = CreateAgent("Red Defender", TeamId.Red, MovementState.Neutral);
        var presenterObject = new GameObject("Flow Presenter");
        var bannerObject = new GameObject("Flow Banner", typeof(RectTransform), typeof(Text));
        var overlayObject = new GameObject("Flow Overlay", typeof(RectTransform), typeof(Image));
        var spawnObject = new GameObject("Red Spawn");

        spawnObject.transform.SetPositionAndRotation(new Vector3(4f, 1f, -3f), Quaternion.Euler(0f, 90f, 0f));
        defender.Motor.currentStamina = 12f;

        try
        {
            var presenter = presenterObject.AddComponent<LocalMatchFlowPresenter>();
            var banner = bannerObject.GetComponent<Text>();
            var overlay = overlayObject.GetComponent<Image>();
            presenter.Configure(
                null,
                banner,
                overlay,
                new[] { defender.Team },
                null,
                spawnObject.transform,
                4f
            );

            presenter.PlayFlowEvent(new LocalMatchFlowEvent(
                LocalMatchFlowEventType.VictoryCountdownStarted,
                TeamId.Blue,
                TeamId.Red,
                30f
            ));

            Assert.IsTrue(banner.gameObject.activeSelf);
            StringAssert.Contains("OVERTURN", banner.text);
            StringAssert.Contains("Blue controls all points", banner.text);
            StringAssert.Contains("Red re-entry 30s", banner.text);
            Assert.IsTrue(overlay.gameObject.activeSelf);
            Assert.AreEqual(1, presenter.LastReenteredCount);
            Assert.AreEqual(spawnObject.transform.position, defender.GameObject.transform.position);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), defender.GameObject.transform.rotation), 0.01f);
            Assert.AreEqual(defender.Motor.maxStamina, defender.Motor.currentStamina);
            Assert.AreEqual(MovementState.Attacker, defender.StateController.CurrentState);
        }
        finally
        {
            Object.DestroyImmediate(spawnObject);
            Object.DestroyImmediate(overlayObject);
            Object.DestroyImmediate(bannerObject);
            Object.DestroyImmediate(presenterObject);
            Object.DestroyImmediate(defender.GameObject);
        }
    }

    [Test]
    public void CapturedDefenderDoesNotReenter()
    {
        var king = CreateAgent("Blue King", TeamId.Blue, MovementState.King);
        var defender = CreateAgent("Captured Red Defender", TeamId.Red, MovementState.Neutral);
        var presenterObject = new GameObject("Flow Presenter");
        var bannerObject = new GameObject("Flow Banner", typeof(RectTransform), typeof(Text));
        var spawnObject = new GameObject("Red Spawn");

        defender.GameObject.transform.position = Vector3.left;
        spawnObject.transform.position = Vector3.right * 6f;

        try
        {
            Assert.IsTrue(king.Agent.TryHold(defender.Agent));
            Assert.IsTrue(king.Agent.CompleteCapture(defender.Agent));

            var presenter = presenterObject.AddComponent<LocalMatchFlowPresenter>();
            presenter.Configure(
                null,
                bannerObject.GetComponent<Text>(),
                null,
                new[] { defender.Team },
                null,
                spawnObject.transform
            );

            presenter.PlayFlowEvent(new LocalMatchFlowEvent(
                LocalMatchFlowEventType.VictoryCountdownStarted,
                TeamId.Blue,
                TeamId.Red,
                30f
            ));

            Assert.AreEqual(0, presenter.LastReenteredCount);
            Assert.AreEqual(Vector3.left, defender.GameObject.transform.position);
            Assert.AreEqual(MovementState.Captured, defender.StateController.CurrentState);
        }
        finally
        {
            Object.DestroyImmediate(spawnObject);
            Object.DestroyImmediate(bannerObject);
            Object.DestroyImmediate(presenterObject);
            Object.DestroyImmediate(king.GameObject);
            Object.DestroyImmediate(defender.GameObject);
        }
    }

    [Test]
    public void InterruptAndRoundEndUseDistinctBanners()
    {
        var presenterObject = new GameObject("Flow Presenter");
        var bannerObject = new GameObject("Flow Banner", typeof(RectTransform), typeof(Text));

        try
        {
            var presenter = presenterObject.AddComponent<LocalMatchFlowPresenter>();
            var banner = bannerObject.GetComponent<Text>();
            presenter.Configure(null, banner, null, System.Array.Empty<LocalPlayerTeam>(), null, null);

            presenter.PlayFlowEvent(new LocalMatchFlowEvent(
                LocalMatchFlowEventType.VictoryCountdownInterrupted,
                TeamId.Blue,
                TeamId.None,
                12f
            ));
            StringAssert.Contains("DEFENDER BREAK", banner.text);
            StringAssert.Contains("Blue countdown stopped", banner.text);

            presenter.PlayFlowEvent(new LocalMatchFlowEvent(
                LocalMatchFlowEventType.RoundEnded,
                TeamId.Red,
                TeamId.None,
                0f
            ));
            StringAssert.Contains("ROUND END", banner.text);
            StringAssert.Contains("Red wins", banner.text);
        }
        finally
        {
            Object.DestroyImmediate(bannerObject);
            Object.DestroyImmediate(presenterObject);
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
        return new AgentFixture(gameObject, motor, teamComponent, stateController, agent);
    }

    private readonly struct AgentFixture
    {
        public AgentFixture(
            GameObject gameObject,
            PlayerMotor motor,
            LocalPlayerTeam team,
            PlayerStateController stateController,
            PlayerCaptureAgent agent)
        {
            GameObject = gameObject;
            Motor = motor;
            Team = team;
            StateController = stateController;
            Agent = agent;
        }

        public GameObject GameObject { get; }
        public PlayerMotor Motor { get; }
        public LocalPlayerTeam Team { get; }
        public PlayerStateController StateController { get; }
        public PlayerCaptureAgent Agent { get; }
    }
}
