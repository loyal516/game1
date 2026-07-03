using System.Collections.Generic;
using NUnit.Framework;
using Overthrone;
using UnityEngine;
using UnityEngine.AI;

public sealed class LocalBotControllerTests
{
    private const float DirectionTolerance = 0.01f;

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
    public void HeardEnemyNoiseTakesPriorityOverCapturePoint()
    {
        var bot = CreateAgent("Hearing Bot", TeamId.Blue, MovementState.Neutral, Vector3.zero);
        var enemy = CreateAgent("Noisy Enemy", TeamId.Red, MovementState.Attacker, new Vector3(12f, 0f, 0f));
        var pointObject = CreateCapturePoint("Hearing Capture Point", new Vector3(0f, 0f, 10f));
        AIHearingSensor hearingSensor = null;

        try
        {
            hearingSensor = AddEnabledHearingSensor(bot.GameObject);
            var point = pointObject.GetComponent<CapturePoint>();
            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(new[] { point }, new[] { bot.Team, enemy.Team }, new[] { bot.Agent, enemy.Agent }, null);

            var heardPosition = enemy.GameObject.transform.position;
            Assert.IsTrue(hearingSensor.TryRememberNoise(new NoiseEvent(enemy.GameObject, heardPosition, 25f, MovementState.Attacker)));
            Assert.IsTrue(hearingSensor.HasHeardNoise);
            controller.Tick(0f);

            Assert.IsTrue(controller.IsInvestigatingNoise);
            AssertVectorApproximately(heardPosition, controller.CurrentNoiseTarget);
            Assert.IsNull(controller.CurrentPointTarget);
            Assert.AreEqual(LocalBotMoveMode.DirectFallback, controller.LastMoveMode);
            AssertVectorApproximately(heardPosition, controller.LastSteeringTarget);
            Assert.Greater(bot.Input.Move.x, 0.99f);
            Assert.AreEqual(0f, bot.Input.Move.y, DirectionTolerance);
            Assert.IsTrue(bot.Input.SprintHeld);
        }
        finally
        {
            bot.Destroy();
            enemy.Destroy();
            Object.DestroyImmediate(pointObject);
        }
    }

    [Test]
    public void SameTeamNoiseIsIgnoredForCapturePointSelection()
    {
        var bot = CreateAgent("Friendly Hearing Bot", TeamId.Blue, MovementState.Neutral, Vector3.zero);
        var ally = CreateAgent("Noisy Ally", TeamId.Blue, MovementState.Attacker, new Vector3(12f, 0f, 0f));
        var pointObject = CreateCapturePoint("Friendly Noise Capture Point", new Vector3(0f, 0f, 10f));
        AIHearingSensor hearingSensor = null;

        try
        {
            hearingSensor = AddEnabledHearingSensor(bot.GameObject);
            var point = pointObject.GetComponent<CapturePoint>();
            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(new[] { point }, new[] { bot.Team, ally.Team }, new[] { bot.Agent, ally.Agent }, null);

            Assert.IsTrue(hearingSensor.TryRememberNoise(new NoiseEvent(ally.GameObject, ally.GameObject.transform.position, 25f, MovementState.Attacker)));
            Assert.IsTrue(hearingSensor.HasHeardNoise);
            controller.Tick(0f);

            Assert.IsFalse(controller.IsInvestigatingNoise);
            AssertVectorApproximately(bot.GameObject.transform.position, controller.CurrentNoiseTarget);
            Assert.AreEqual(point, controller.CurrentPointTarget);
            Assert.AreEqual(LocalBotMoveMode.DirectFallback, controller.LastMoveMode);
            AssertVectorApproximately(point.transform.position, controller.LastSteeringTarget);
            Assert.AreEqual(0f, bot.Input.Move.x, DirectionTolerance);
            Assert.Greater(bot.Input.Move.y, 0.99f);
            Assert.IsTrue(bot.Input.SprintHeld);
        }
        finally
        {
            bot.Destroy();
            ally.Destroy();
            Object.DestroyImmediate(pointObject);
        }
    }

    [Test]
    public void FriendlyNoiseDoesNotOverwriteRememberedEnemyNoise()
    {
        var bot = CreateAgent("Enemy Memory Bot", TeamId.Blue, MovementState.Neutral, Vector3.zero);
        var enemy = CreateAgent("Remembered Enemy", TeamId.Red, MovementState.Attacker, new Vector3(12f, 0f, 0f));
        var ally = CreateAgent("Distracting Ally", TeamId.Blue, MovementState.Attacker, new Vector3(0f, 0f, 12f));
        var pointObject = CreateCapturePoint("Enemy Memory Capture Point", new Vector3(0f, 0f, 10f));
        AIHearingSensor hearingSensor = null;

        try
        {
            hearingSensor = AddEnabledHearingSensor(bot.GameObject);
            var point = pointObject.GetComponent<CapturePoint>();
            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(new[] { point }, new[] { bot.Team, enemy.Team, ally.Team }, new[] { bot.Agent, enemy.Agent, ally.Agent }, null);

            var enemyPosition = enemy.GameObject.transform.position;
            Assert.IsTrue(hearingSensor.TryRememberNoise(new NoiseEvent(enemy.GameObject, enemyPosition, 25f, MovementState.Attacker)));
            controller.Tick(0f);
            Assert.IsTrue(controller.IsInvestigatingNoise);

            Assert.IsTrue(hearingSensor.TryRememberNoise(new NoiseEvent(ally.GameObject, ally.GameObject.transform.position, 25f, MovementState.Attacker)));
            controller.Tick(0f);

            Assert.IsTrue(controller.IsInvestigatingNoise);
            AssertVectorApproximately(enemyPosition, controller.CurrentNoiseTarget);
            Assert.IsNull(controller.CurrentPointTarget);
        }
        finally
        {
            bot.Destroy();
            enemy.Destroy();
            ally.Destroy();
            Object.DestroyImmediate(pointObject);
        }
    }

    [Test]
    public void DirectFallbackUsesDestinationVectorWhenNoNavMeshPathExists()
    {
        var bot = CreateAgent("Direct Fallback Bot", TeamId.Blue, MovementState.Neutral, Vector3.zero);
        var pointObject = CreateCapturePoint("Fallback Target", new Vector3(8f, 0f, 0f));

        try
        {
            var point = pointObject.GetComponent<CapturePoint>();
            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(new[] { point }, new[] { bot.Team }, new[] { bot.Agent }, null);

            controller.Tick(0f);

            Assert.AreEqual(point, controller.CurrentPointTarget);
            Assert.AreEqual(LocalBotMoveMode.DirectFallback, controller.LastMoveMode);
            AssertVectorApproximately(point.transform.position, controller.LastSteeringTarget);
            Assert.Greater(bot.Input.Move.x, 0.99f);
            Assert.AreEqual(0f, bot.Input.Move.y, DirectionTolerance);
            Assert.IsTrue(bot.Input.SprintHeld);
        }
        finally
        {
            bot.Destroy();
            Object.DestroyImmediate(pointObject);
        }
    }

    [Test]
    public void DirectFallbackNormalizesDiagonalDestinationBeforeInjectingInput()
    {
        var bot = CreateAgent("Diagonal Fallback Bot", TeamId.Blue, MovementState.Neutral, Vector3.zero);
        var pointObject = CreateCapturePoint("Diagonal Fallback Target", new Vector3(10f, 0f, 10f));

        try
        {
            var point = pointObject.GetComponent<CapturePoint>();
            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(new[] { point }, new[] { bot.Team }, new[] { bot.Agent }, null);

            controller.Tick(0f);

            Assert.AreEqual(point, controller.CurrentPointTarget);
            Assert.AreEqual(LocalBotMoveMode.DirectFallback, controller.LastMoveMode);
            AssertVectorApproximately(point.transform.position, controller.LastSteeringTarget);
            Assert.AreEqual(1f, bot.Input.Move.magnitude, DirectionTolerance);
            Assert.Greater(bot.Input.Move.x, 0.7f);
            Assert.Greater(bot.Input.Move.y, 0.7f);
            Assert.IsTrue(bot.Input.SprintHeld);
        }
        finally
        {
            bot.Destroy();
            Object.DestroyImmediate(pointObject);
        }
    }

    [Test]
    public void DirectFallbackStopsAtArrivalDistanceBoundary()
    {
        var bot = CreateAgent("Arrival Boundary Bot", TeamId.Blue, MovementState.Neutral, Vector3.zero);
        var pointObject = CreateCapturePoint("Arrival Boundary Target", new Vector3(0f, 0f, 1.1f));

        try
        {
            var point = pointObject.GetComponent<CapturePoint>();
            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(new[] { point }, new[] { bot.Team }, new[] { bot.Agent }, null);

            controller.Tick(0.1f);

            Assert.AreEqual(point, controller.CurrentPointTarget);
            Assert.AreEqual(LocalBotMoveMode.None, controller.LastMoveMode);
            AssertVectorApproximately(bot.GameObject.transform.position, controller.LastSteeringTarget);
            Assert.AreEqual(Vector2.zero, bot.Input.Move);
            Assert.IsFalse(bot.Input.SprintHeld);
        }
        finally
        {
            bot.Destroy();
            Object.DestroyImmediate(pointObject);
        }
    }

    [Test]
    public void DirectFallbackMovesJustOutsideArrivalDistanceBoundary()
    {
        var bot = CreateAgent("Arrival Outside Bot", TeamId.Blue, MovementState.Neutral, Vector3.zero);
        var pointObject = CreateCapturePoint("Arrival Outside Target", new Vector3(0f, 0f, 1.11f));

        try
        {
            var point = pointObject.GetComponent<CapturePoint>();
            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(new[] { point }, new[] { bot.Team }, new[] { bot.Agent }, null);

            controller.Tick(0f);

            Assert.AreEqual(point, controller.CurrentPointTarget);
            Assert.AreEqual(LocalBotMoveMode.DirectFallback, controller.LastMoveMode);
            AssertVectorApproximately(point.transform.position, controller.LastSteeringTarget);
            Assert.AreEqual(0f, bot.Input.Move.x, DirectionTolerance);
            Assert.Greater(bot.Input.Move.y, 0.99f);
            Assert.IsTrue(bot.Input.SprintHeld);
        }
        finally
        {
            bot.Destroy();
            Object.DestroyImmediate(pointObject);
        }
    }

    [Test]
    public void NavMeshPathUsesFirstCornerWhenCompletePathExists()
    {
        var navMesh = CreateTemporaryNavMeshWithWall();
        var bot = CreateAgent("NavMesh Path Bot", TeamId.Blue, MovementState.Neutral, new Vector3(-3f, 0f, -3f));
        var pointObject = CreateCapturePoint("NavMesh Target", new Vector3(3f, 0f, 3f));

        try
        {
            var point = pointObject.GetComponent<CapturePoint>();
            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(new[] { point }, new[] { bot.Team }, new[] { bot.Agent }, null);

            controller.Tick(0f);

            Assert.AreEqual(point, controller.CurrentPointTarget);
            Assert.AreEqual(LocalBotMoveMode.NavMeshPath, controller.LastMoveMode);
            Assert.Greater(Vector3.Distance(point.transform.position, controller.LastSteeringTarget), 0.25f);
            Assert.Greater(bot.Input.Move.sqrMagnitude, 0.01f);
            Assert.IsTrue(bot.Input.SprintHeld);
        }
        finally
        {
            navMesh.Destroy();
            bot.Destroy();
            Object.DestroyImmediate(pointObject);
        }
    }

    [Test]
    public void TickClearsManualInputWhenNullConfigurationLeavesNoFallbackTarget()
    {
        var bot = CreateAgent("Null Config Bot", TeamId.Blue, MovementState.Neutral, Vector3.zero);

        try
        {
            bot.Input.SetManualInput(Vector2.right, true);
            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(null, null, null, null);

            controller.Tick(-0.25f);

            Assert.IsNull(controller.CurrentPointTarget);
            Assert.IsNull(controller.CurrentAgentTarget);
            Assert.AreEqual(LocalBotMoveMode.None, controller.LastMoveMode);
            AssertVectorApproximately(bot.GameObject.transform.position, controller.LastSteeringTarget);
            Assert.AreEqual(Vector2.zero, bot.Input.Move);
            Assert.IsFalse(bot.Input.SprintHeld);
        }
        finally
        {
            bot.Destroy();
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

    [Test]
    public void KingBotHoldingOwnTargetDoesNotAttemptFinalCapture()
    {
        var bot = CreateAgent("Solo King Bot", TeamId.Blue, MovementState.King, Vector3.zero);
        var target = CreateAgent("Solo King Target", TeamId.Red, MovementState.Neutral, Vector3.forward);
        var captureSystemObject = new GameObject("Solo King Capture System");
        var captureSystem = captureSystemObject.AddComponent<LocalCaptureSystem>();

        try
        {
            var agents = new[] { bot.Agent, target.Agent };
            captureSystem.Configure(bot.Agent, agents);
            Assert.IsTrue(bot.Agent.TryHold(target.Agent));
            bot.Input.SetManualInput(Vector2.up, captureHeld: true);

            var controller = bot.GameObject.AddComponent<LocalBotController>();
            controller.Configure(System.Array.Empty<CapturePoint>(), new[] { bot.Team, target.Team }, agents, captureSystem);

            controller.Tick(CaptureInteractionRules.CaptureHoldSeconds);

            Assert.IsNull(controller.CurrentAgentTarget);
            Assert.AreEqual(CaptureStatus.Holding, bot.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);
            Assert.IsFalse(bot.Input.CaptureHeld);
            Assert.AreEqual(0f, captureSystem.CaptureHoldProgress01);
        }
        finally
        {
            Object.DestroyImmediate(captureSystemObject);
            bot.Destroy();
            target.Destroy();
        }
    }

    private static GameObject CreateCapturePoint(string name, Vector3 position)
    {
        var pointObject = new GameObject(name);
        var point = pointObject.AddComponent<CapturePoint>();
        point.Configure("A", 5f);
        pointObject.transform.position = position;
        return pointObject;
    }

    private static AIHearingSensor AddEnabledHearingSensor(GameObject target)
    {
        return target.AddComponent<AIHearingSensor>();
    }

    private static TemporaryNavMesh CreateTemporaryNavMeshWithWall()
    {
        var sources = new List<NavMeshBuildSource>
        {
            new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one),
                size = new Vector3(8f, 0.2f, 8f),
                area = 0
            },
            new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.ModifierBox,
                transform = Matrix4x4.TRS(new Vector3(0f, 0.6f, 0f), Quaternion.identity, Vector3.one),
                size = new Vector3(1.2f, 2f, 5f),
                area = 1
            }
        };

        var data = NavMeshBuilder.BuildNavMeshData(
            NavMesh.GetSettingsByID(0),
            sources,
            new Bounds(Vector3.zero, new Vector3(10f, 4f, 10f)),
            Vector3.zero,
            Quaternion.identity
        );
        Assert.IsNotNull(data);
        return new TemporaryNavMesh(data, NavMesh.AddNavMeshData(data));
    }

    private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
    {
        Assert.LessOrEqual(Vector3.Distance(expected, actual), DirectionTolerance);
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

    private readonly struct TemporaryNavMesh
    {
        public TemporaryNavMesh(NavMeshData data, NavMeshDataInstance instance)
        {
            Data = data;
            Instance = instance;
        }

        private NavMeshData Data { get; }
        private NavMeshDataInstance Instance { get; }

        public void Destroy()
        {
            Instance.Remove();
            Object.DestroyImmediate(Data);
        }
    }
}
