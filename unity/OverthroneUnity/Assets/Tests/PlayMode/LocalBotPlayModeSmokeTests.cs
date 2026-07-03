using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Overthrone;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

public sealed class LocalBotPlayModeSmokeTests
{
    private const int FrameBudget = 90;
    private const int SoakFrameBudget = 240;
    private const float MinimumMovementDistance = 0.25f;
    private const float MinimumSoakMovementDistance = 1.25f;
    private const float MinimumAvoidanceOffsetMagnitude = 0.05f;
    private const float MinimumLateralAvoidanceDistance = 0.08f;
    private const float MinimumBlockerClearance = 0.25f;
    private const float PositionTolerance = 0.0001f;
    private const float ChokeHalfWidth = 0.9f;

    private readonly List<Object> createdObjects = new List<Object>();
    private float previousCaptureDeltaTime;

    [SetUp]
    public void SetUp()
    {
        previousCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 30f;
    }

    [UnityTest]
    public IEnumerator SightSuspicionAndDynamicAvoidanceMoveCharacterController()
    {
        CreateGroundCollider("PlayMode Sight Ground");
        var bot = CreateAgent("PlayMode Sight Bot", TeamId.Blue, MovementState.Neutral, Vector3.zero);
        var enemy = CreateAgent("PlayMode Visible Enemy", TeamId.Red, MovementState.Neutral, new Vector3(0f, 0f, 7f));
        var blocker = CreateAgent("PlayMode Dynamic Blocker", TeamId.Blue, MovementState.Neutral, new Vector3(0.55f, 0f, 1.25f));
        var participants = new[] { bot.Team, enemy.Team, blocker.Team };
        var agents = new[] { bot.Agent, enemy.Agent, blocker.Agent };

        bot.Controller.Configure(System.Array.Empty<CapturePoint>(), participants, agents, null);
        Physics.SyncTransforms();

        var startPosition = bot.GameObject.transform.position;
        var blockerPosition = blocker.GameObject.transform.position;
        var observedSightSuspicion = false;
        var observedDynamicAvoidance = false;
        var largestAvoidanceOffset = 0f;
        var largestLateralDisplacement = 0f;
        var largestBlockerLineClearance = 0f;
        var passedBlockerWithLateralClearance = false;

        for (var frame = 0; frame < FrameBudget; frame += 1)
        {
            yield return null;
            var botPosition = bot.GameObject.transform.position;
            var lateralDisplacement = Mathf.Abs(botPosition.x - startPosition.x);
            var blockerDistance = HorizontalDistance(botPosition, blockerPosition);

            observedSightSuspicion |= bot.Controller.HasSightSuspicion && bot.Controller.IsInvestigatingSight;
            observedDynamicAvoidance |= bot.Controller.IsAvoidingDynamicObstacle;
            largestAvoidanceOffset = Mathf.Max(largestAvoidanceOffset, bot.Controller.LastAvoidanceOffset.magnitude);
            largestLateralDisplacement = Mathf.Max(largestLateralDisplacement, lateralDisplacement);
            if (Mathf.Abs(botPosition.z - blockerPosition.z) <= 0.45f)
            {
                largestBlockerLineClearance = Mathf.Max(largestBlockerLineClearance, blockerDistance);
            }

            passedBlockerWithLateralClearance |= botPosition.z > blockerPosition.z + MinimumMovementDistance
                && lateralDisplacement >= MinimumLateralAvoidanceDistance
                && blockerDistance >= MinimumBlockerClearance;
            AssertFinitePosition(botPosition);

            if (observedSightSuspicion
                && observedDynamicAvoidance
                && largestAvoidanceOffset >= MinimumAvoidanceOffsetMagnitude
                && passedBlockerWithLateralClearance)
            {
                break;
            }
        }

        Assert.IsTrue(observedSightSuspicion, "Bot should keep sight suspicion while the blocker is offset from the sight ray.");
        Assert.IsTrue(observedDynamicAvoidance, "Bot should flag dynamic obstacle avoidance while steering around the blocker.");
        Assert.GreaterOrEqual(largestAvoidanceOffset, MinimumAvoidanceOffsetMagnitude);
        Assert.GreaterOrEqual(largestLateralDisplacement, MinimumLateralAvoidanceDistance);
        Assert.GreaterOrEqual(largestBlockerLineClearance, MinimumBlockerClearance);
        Assert.IsTrue(passedBlockerWithLateralClearance, "Bot should pass the blocker with lateral clearance instead of holding the blocker center line.");
        Assert.GreaterOrEqual(HorizontalDistance(startPosition, bot.GameObject.transform.position), MinimumMovementDistance);
    }

    [UnityTest]
    public IEnumerator HeardEnemyNoiseEntersSearchOrGuardDuringFrameLoop()
    {
        CreateGroundCollider("PlayMode Hearing Ground");
        var bot = CreateAgent("PlayMode Hearing Bot", TeamId.Blue, MovementState.Neutral, Vector3.zero);
        var enemy = CreateAgent("PlayMode Noise Enemy", TeamId.Red, MovementState.Attacker, new Vector3(4f, 0f, 0f));
        var hearingSensor = AddCreatedComponent<AIHearingSensor>(bot.GameObject);
        var participants = new[] { bot.Team, enemy.Team };
        var agents = new[] { bot.Agent, enemy.Agent };

        bot.Controller.Configure(System.Array.Empty<CapturePoint>(), participants, agents, null);
        Physics.SyncTransforms();

        var noisePosition = enemy.GameObject.transform.position;
        var startDistanceToNoise = HorizontalDistance(bot.GameObject.transform.position, noisePosition);
        Assert.IsFalse(hearingSensor.HasHeardNoise);
        NoiseSystem.Emit(new NoiseEvent(enemy.GameObject, noisePosition, 25f, MovementState.Attacker));
        Assert.IsTrue(hearingSensor.HasHeardNoise);
        AssertVectorApproximately(noisePosition, hearingSensor.LastHeardPosition);

        var observedNoiseTarget = false;
        var observedMovementTowardNoise = false;
        var observedSearchOrGuard = false;
        for (var frame = 0; frame < FrameBudget; frame += 1)
        {
            yield return null;
            var botPosition = bot.GameObject.transform.position;
            observedNoiseTarget |= bot.Controller.IsInvestigatingNoise
                && Approximately(bot.Controller.CurrentNoiseTarget, noisePosition);
            observedMovementTowardNoise |= HorizontalDistance(botPosition, noisePosition)
                <= startDistanceToNoise - MinimumMovementDistance;
            observedSearchOrGuard |= bot.Controller.IsSearchingNoise || bot.Controller.IsGuardingNoise;
            AssertFinitePosition(botPosition);

            if (observedNoiseTarget && observedMovementTowardNoise && observedSearchOrGuard)
            {
                break;
            }
        }

        Assert.IsTrue(observedNoiseTarget);
        Assert.IsTrue(observedMovementTowardNoise);
        Assert.IsTrue(observedSearchOrGuard);
    }

    [UnityTest]
    public IEnumerator LocalSixBotChokePointDirectFallbackSoakMaintainsMatchAndSteeringInvariants()
    {
        NavMesh.RemoveAllNavMeshData();
        CreateGroundCollider("PlayMode Six Bot Soak Ground");
        CreateWall("PlayMode Choke Left Wall", new Vector3(-ChokeHalfWidth, 1f, -1.6f), new Vector3(0.22f, 2f, 8.2f));
        CreateWall("PlayMode Choke Right Wall", new Vector3(ChokeHalfWidth, 1f, -1.6f), new Vector3(0.22f, 2f, 8.2f));

        var capturePoints = new[]
        {
            CreateCapturePoint("A", new Vector3(0f, 0f, 1.25f), 1.25f),
            CreateCapturePoint("B", new Vector3(-5.5f, 0f, 0.5f), 1.25f),
            CreateCapturePoint("C", new Vector3(5.5f, 0f, 0.5f), 1.25f)
        };
        var bots = new[]
        {
            CreateAgent("PlayMode Soak Blue Choke Runner", TeamId.Blue, MovementState.Neutral, new Vector3(0f, 0f, -7f)),
            CreateAgent("PlayMode Soak Blue Blocker", TeamId.Blue, MovementState.Neutral, new Vector3(0.18f, 0f, -4.8f)),
            CreateAgent("PlayMode Soak Blue Wide", TeamId.Blue, MovementState.Neutral, new Vector3(-2.2f, 0f, -7.2f)),
            CreateAgent("PlayMode Soak Red Choke Runner", TeamId.Red, MovementState.Neutral, new Vector3(0f, 0f, 7f)),
            CreateAgent("PlayMode Soak Red Left", TeamId.Red, MovementState.Neutral, new Vector3(-2.2f, 0f, 7.2f)),
            CreateAgent("PlayMode Soak Red Right", TeamId.Red, MovementState.Neutral, new Vector3(2.2f, 0f, 7.2f))
        };
        var participants = new LocalPlayerTeam[bots.Length];
        var agents = new PlayerCaptureAgent[bots.Length];
        var startPositions = new Vector3[bots.Length];
        for (var index = 0; index < bots.Length; index += 1)
        {
            participants[index] = bots[index].Team;
            agents[index] = bots[index].Agent;
            startPositions[index] = bots[index].GameObject.transform.position;
            AssertFinitePosition(startPositions[index]);
        }

        var systems = CreateMatchSystems("PlayMode Six Bot Soak Systems", bots[0].Agent, agents, capturePoints, participants);
        systems.MatchManager.enabled = false;
        foreach (var bot in bots)
        {
            bot.Controller.Configure(capturePoints, participants, agents, systems.CaptureSystem);
        }

        Physics.SyncTransforms();

        var observedDirectFallback = false;
        var observedPointTarget = false;
        var observedSightTarget = false;
        var observedDynamicAvoidance = false;
        var observedChokeTraversal = false;
        var largestAvoidanceOffset = 0f;
        var largestLateralAvoidance = 0f;
        var closestBlockerApproach = float.MaxValue;

        for (var frame = 0; frame < SoakFrameBudget; frame += 1)
        {
            yield return null;
            systems.MatchManager.ApplyMatchRules(Time.captureDeltaTime);

            AssertMatchInvariant(systems.MatchManager);

            var blockerPosition = bots[1].GameObject.transform.position;
            for (var index = 0; index < bots.Length; index += 1)
            {
                var bot = bots[index];
                var position = bot.GameObject.transform.position;
                AssertFinitePosition(position);

                observedDirectFallback |= bot.Controller.LastMoveMode == LocalBotMoveMode.DirectFallback;
                observedPointTarget |= bot.Controller.CurrentPointTarget != null;
                observedSightTarget |= bot.Controller.HasSightSuspicion
                    || bot.Controller.IsInvestigatingSight
                    || bot.Controller.CurrentSightAgent != null;
                observedDynamicAvoidance |= bot.Controller.IsAvoidingDynamicObstacle;
                largestAvoidanceOffset = Mathf.Max(largestAvoidanceOffset, bot.Controller.LastAvoidanceOffset.magnitude);

                if (index == 0)
                {
                    largestLateralAvoidance = Mathf.Max(largestLateralAvoidance, Mathf.Abs(position.x - startPositions[index].x));
                    closestBlockerApproach = Mathf.Min(closestBlockerApproach, HorizontalDistance(position, blockerPosition));
                }

                var enteredChokeFromOutside = startPositions[index].z < -6f
                    ? position.z > -4.7f
                    : startPositions[index].z > 6f && position.z < 2.5f;
                if (enteredChokeFromOutside
                    && Mathf.Abs(position.x) < ChokeHalfWidth
                    && position.z > -4.7f
                    && position.z < 1.5f)
                {
                    observedChokeTraversal = true;
                }
            }
        }

        var movedBotCount = 0;
        for (var index = 0; index < bots.Length; index += 1)
        {
            if (HorizontalDistance(startPositions[index], bots[index].GameObject.transform.position) >= MinimumSoakMovementDistance)
            {
                movedBotCount += 1;
            }
        }

        Assert.GreaterOrEqual(movedBotCount, 2, "At least two of the six local bots should move meaningful distance during the soak.");
        Assert.IsTrue(observedDirectFallback, "No baked NavMesh is present, so bots should exercise DirectFallback steering.");
        Assert.IsTrue(observedPointTarget || observedSightTarget, "At least one bot should acquire an objective target during the soak.");
        Assert.IsTrue(observedDynamicAvoidance, "The choke blocker should trigger dynamic obstacle avoidance.");
        Assert.GreaterOrEqual(largestAvoidanceOffset, MinimumAvoidanceOffsetMagnitude);
        Assert.GreaterOrEqual(largestLateralAvoidance, MinimumLateralAvoidanceDistance);
        Assert.Less(closestBlockerApproach, 2.5f, "The central bot should actually encounter the blocker instead of avoiding the choke scenario entirely.");
        Assert.IsTrue(observedChokeTraversal, "At least one bot should traverse the corridor between the two wall colliders.");
        Assert.AreEqual(LocalMatchPhase.Playing, systems.MatchManager.Phase);
        Assert.AreEqual(TeamId.None, systems.MatchManager.Winner);
    }

    [TearDown]
    public void TearDown()
    {
        Time.captureDeltaTime = previousCaptureDeltaTime;

        for (var index = createdObjects.Count - 1; index >= 0; index -= 1)
        {
            if (createdObjects[index] != null)
            {
                Object.DestroyImmediate(createdObjects[index]);
            }
        }

        createdObjects.Clear();
    }

    private AgentFixture CreateAgent(string name, TeamId team, MovementState state, Vector3 position)
    {
        var gameObject = CreateGameObject(name);
        gameObject.transform.position = position;

        var controller = gameObject.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.3f;
        controller.center = Vector3.up;
        controller.skinWidth = 0.03f;

        gameObject.AddComponent<PlayerInputReader>();
        var motor = gameObject.AddComponent<PlayerMotor>();
        motor.movementProfiles = CreateMovementProfileSet();
        motor.lockCursorOnStart = false;
        motor.lockCursorOnClick = false;
        motor.currentStamina = 100f;

        var localTeam = gameObject.AddComponent<LocalPlayerTeam>();
        localTeam.Configure(team);

        var stateController = gameObject.AddComponent<PlayerStateController>();
        stateController.SetPersistentState(state);

        var captureAgent = gameObject.AddComponent<PlayerCaptureAgent>();
        captureAgent.Configure(stateController);

        var botController = gameObject.AddComponent<LocalBotController>();
        return new AgentFixture(gameObject, localTeam, captureAgent, botController);
    }

    private MovementProfileSet CreateMovementProfileSet()
    {
        var profileSet = ScriptableObject.CreateInstance<MovementProfileSet>();
        createdObjects.Add(profileSet);
        profileSet.SetProfiles(new[]
        {
            new MovementProfile
            {
                state = MovementState.Neutral,
                canMove = true,
                canSprint = true,
                walkSpeed = 4.5f,
                runSpeed = 7.2f,
                acceleration = 18f
            },
            new MovementProfile
            {
                state = MovementState.Attacker,
                canMove = true,
                canSprint = true,
                walkSpeed = 4.5f,
                runSpeed = 7.2f,
                acceleration = 18f
            },
            new MovementProfile
            {
                state = MovementState.King,
                canMove = true,
                canSprint = true,
                walkSpeed = 4.5f,
                runSpeed = 7.2f,
                acceleration = 18f
            }
        });
        return profileSet;
    }

    private GameObject CreateGameObject(string name)
    {
        var gameObject = new GameObject(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private void CreateGroundCollider(string name)
    {
        var ground = CreateGameObject(name);
        var boxCollider = ground.AddComponent<BoxCollider>();
        ground.transform.position = new Vector3(0f, -0.05f, 0f);
        boxCollider.size = new Vector3(40f, 0.1f, 40f);
    }

    private void CreateWall(string name, Vector3 position, Vector3 size)
    {
        var wall = CreateGameObject(name);
        wall.transform.position = position;
        var boxCollider = wall.AddComponent<BoxCollider>();
        boxCollider.size = size;
    }

    private CapturePoint CreateCapturePoint(string pointId, Vector3 position, float radius)
    {
        var gameObject = CreateGameObject($"PlayMode Capture Point {pointId}");
        gameObject.transform.position = position;
        var capturePoint = gameObject.AddComponent<CapturePoint>();
        capturePoint.Configure(pointId, radius);
        return capturePoint;
    }

    private MatchSystemsFixture CreateMatchSystems(
        string name,
        PlayerCaptureAgent localPlayer,
        PlayerCaptureAgent[] agents,
        CapturePoint[] capturePoints,
        LocalPlayerTeam[] participants)
    {
        var gameObject = CreateGameObject(name);
        var captureSystem = gameObject.AddComponent<LocalCaptureSystem>();
        captureSystem.Configure(localPlayer, agents);
        var matchManager = gameObject.AddComponent<LocalMatchManager>();
        matchManager.Configure(capturePoints, participants, 120f);
        return new MatchSystemsFixture(captureSystem, matchManager);
    }

    private T AddCreatedComponent<T>(GameObject gameObject) where T : Component
    {
        return gameObject.AddComponent<T>();
    }

    private static float HorizontalDistance(Vector3 from, Vector3 to)
    {
        from.y = 0f;
        to.y = 0f;
        return Vector3.Distance(from, to);
    }

    private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
    {
        Assert.IsTrue(Approximately(expected, actual), $"Expected {actual} to be approximately {expected}.");
    }

    private static bool Approximately(Vector3 expected, Vector3 actual)
    {
        return HorizontalDistance(expected, actual) <= PositionTolerance
            && Mathf.Abs(expected.y - actual.y) <= PositionTolerance;
    }

    private static void AssertFinitePosition(Vector3 position)
    {
        Assert.IsFalse(float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z));
        Assert.IsFalse(float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z));
        Assert.GreaterOrEqual(position.y, -PositionTolerance);
    }

    private static void AssertMatchInvariant(LocalMatchManager matchManager)
    {
        Assert.IsNotNull(matchManager);
        Assert.Greater(matchManager.MatchTimeRemaining, 0f);
        Assert.AreNotEqual(LocalMatchPhase.Result, matchManager.Phase);
        Assert.AreEqual(TeamId.None, matchManager.Winner);
        Assert.IsTrue(matchManager.VictoryCountdownTeam == TeamId.None
            || matchManager.VictoryCountdownTeam == TeamId.Blue
            || matchManager.VictoryCountdownTeam == TeamId.Red);
        Assert.GreaterOrEqual(matchManager.VictoryTimeRemaining, 0f);
        Assert.GreaterOrEqual(matchManager.BlueOwnedCount, 0);
        Assert.GreaterOrEqual(matchManager.RedOwnedCount, 0);
        Assert.LessOrEqual(matchManager.BlueOwnedCount, matchManager.CapturePoints.Length);
        Assert.LessOrEqual(matchManager.RedOwnedCount, matchManager.CapturePoints.Length);
    }

    private readonly struct MatchSystemsFixture
    {
        public MatchSystemsFixture(LocalCaptureSystem captureSystem, LocalMatchManager matchManager)
        {
            CaptureSystem = captureSystem;
            MatchManager = matchManager;
        }

        public LocalCaptureSystem CaptureSystem { get; }
        public LocalMatchManager MatchManager { get; }
    }

    private readonly struct AgentFixture
    {
        public AgentFixture(GameObject gameObject, LocalPlayerTeam team, PlayerCaptureAgent agent, LocalBotController controller)
        {
            GameObject = gameObject;
            Team = team;
            Agent = agent;
            Controller = controller;
        }

        public GameObject GameObject { get; }
        public LocalPlayerTeam Team { get; }
        public PlayerCaptureAgent Agent { get; }
        public LocalBotController Controller { get; }
    }
}
