using NUnit.Framework;
using Overthrone;
using UnityEngine;

public sealed class LocalBotControllerTests
{
    [Test]
    public void BotChoosesCapturePointAndInjectsMovementInput()
    {
        var bot = CreateAgent("Point Bot", TeamId.Blue, MovementState.Neutral, Vector3.zero);
        var pointObject = new GameObject("Bot Target Point");
        var point = pointObject.AddComponent<CapturePoint>();
        point.Configure("A", 5f);
        pointObject.transform.position = new Vector3(0f, 0f, 10f);

        try
        {
            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(new[] { point }, new[] { bot.Team }, new[] { bot.Agent }, null);

            controller.Tick(0.1f);

            Assert.AreEqual(point, controller.CurrentPointTarget);
            Assert.Greater(bot.Input.Move.sqrMagnitude, 0.01f);
            Assert.IsTrue(bot.Input.SprintHeld);
        }
        finally
        {
            bot.Destroy();
            Object.DestroyImmediate(pointObject);
        }
    }

    [Test]
    public void AttackerBotCanTackleEnemyInFront()
    {
        var bot = CreateAgent("Attacker Bot", TeamId.Blue, MovementState.Attacker, Vector3.zero);
        var target = CreateAgent("Bot Target", TeamId.Red, MovementState.Neutral, Vector3.forward * 2f);
        var captureSystemObject = new GameObject("Bot Capture System");
        var captureSystem = captureSystemObject.AddComponent<LocalCaptureSystem>();

        try
        {
            var agents = new[] { bot.Agent, target.Agent };
            captureSystem.Configure(bot.Agent, agents);
            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(System.Array.Empty<CapturePoint>(), new[] { bot.Team, target.Team }, agents, captureSystem);

            controller.Tick(0.1f);

            Assert.AreEqual(CaptureStatus.Holding, bot.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);
            Assert.AreEqual(target.Agent, controller.CurrentAgentTarget);
        }
        finally
        {
            Object.DestroyImmediate(captureSystemObject);
            bot.Destroy();
            target.Destroy();
        }
    }

    private static AgentFixture CreateAgent(string name, TeamId team, MovementState state, Vector3 position)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.position = position;
        gameObject.AddComponent<CharacterController>();
        var input = gameObject.AddComponent<PlayerInputReader>();
        var motor = gameObject.AddComponent<PlayerMotor>();
        motor.currentStamina = 100f;
        var localTeam = gameObject.AddComponent<LocalPlayerTeam>();
        localTeam.Configure(team);
        var stateController = gameObject.AddComponent<PlayerStateController>();
        stateController.SetPersistentState(state);
        var agent = gameObject.AddComponent<PlayerCaptureAgent>();
        agent.Configure(stateController);
        return new AgentFixture(gameObject, input, localTeam, agent);
    }

    private readonly struct AgentFixture
    {
        public AgentFixture(GameObject gameObject, PlayerInputReader input, LocalPlayerTeam team, PlayerCaptureAgent agent)
        {
            GameObject = gameObject;
            Input = input;
            Team = team;
            Agent = agent;
        }

        public GameObject GameObject { get; }
        public PlayerInputReader Input { get; }
        public LocalPlayerTeam Team { get; }
        public PlayerCaptureAgent Agent { get; }

        public void Destroy()
        {
            Object.DestroyImmediate(GameObject);
        }
    }
}
