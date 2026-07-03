using System;
using UnityEngine;
using UnityEngine.AI;

namespace Overthrone
{
    public enum LocalBotMoveMode
    {
        None,
        DirectFallback,
        NavMeshPath
    }

    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerCaptureAgent))]
    [RequireComponent(typeof(LocalPlayerTeam))]
    public sealed class LocalBotController : MonoBehaviour
    {
        [SerializeField] private CapturePoint[] capturePoints = Array.Empty<CapturePoint>();
        [SerializeField] private LocalPlayerTeam[] participants = Array.Empty<LocalPlayerTeam>();
        [SerializeField] private PlayerCaptureAgent[] captureAgents = Array.Empty<PlayerCaptureAgent>();
        [SerializeField] private LocalCaptureSystem captureSystem;
        [SerializeField] private float pointArrivalDistance = 1.1f;
        [SerializeField] private float chaseRange = 10f;
        [SerializeField] private float turnDegreesPerSecond = 540f;
        [SerializeField] private float navMeshSampleDistance = 1.5f;
        [SerializeField] private float navMeshCornerArrivalDistance = 0.35f;
        [SerializeField] private float enemyNoiseMemorySeconds = 4f;
        [SerializeField] private float noiseSearchDurationSeconds = 2.5f;
        [SerializeField] private float noiseGuardDurationSeconds = 1.25f;
        [SerializeField] private float noiseSearchRadius = 2f;
        [SerializeField] private float noiseSearchWaypointSeconds = 0.65f;

        private NavMeshPath navMeshPath;
        private PlayerInputReader input;
        private PlayerMotor motor;
        private PlayerCaptureAgent agent;
        private LocalPlayerTeam team;
        private AIHearingSensor hearingSensor;
        private Vector3 rememberedEnemyNoisePosition;
        private float rememberedEnemyNoiseTimer;
        private Vector3 noiseSearchCenter;
        private Vector3 noiseSearchWaypoint;
        private Vector3 consumedEnemyNoisePosition;
        private float noiseSearchTimer;
        private float noiseGuardTimer;
        private float noiseSearchWaypointTimer;
        private int noiseSearchStep;
        private bool hasConsumedEnemyNoise;

        public CapturePoint CurrentPointTarget { get; private set; }
        public PlayerCaptureAgent CurrentAgentTarget { get; private set; }
        public bool IsInvestigatingNoise { get; private set; }
        public bool IsSearchingNoise { get; private set; }
        public bool IsGuardingNoise { get; private set; }
        public Vector3 CurrentNoiseTarget { get; private set; }
        public LocalBotMoveMode LastMoveMode { get; private set; }
        public Vector3 LastSteeringTarget { get; private set; }

        public void Configure(
            CapturePoint[] points,
            LocalPlayerTeam[] matchParticipants,
            PlayerCaptureAgent[] agents,
            LocalCaptureSystem localCaptureSystem)
        {
            capturePoints = points ?? Array.Empty<CapturePoint>();
            participants = matchParticipants ?? Array.Empty<LocalPlayerTeam>();
            captureAgents = agents ?? Array.Empty<PlayerCaptureAgent>();
            captureSystem = localCaptureSystem;
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            ResolveReferences();
            deltaTime = Mathf.Max(0f, deltaTime);
            CurrentPointTarget = null;
            CurrentAgentTarget = null;
            IsInvestigatingNoise = false;
            IsSearchingNoise = false;
            IsGuardingNoise = false;
            CurrentNoiseTarget = transform.position;
            ClearSteeringTelemetry();

            if (input == null || agent == null || team == null || motor == null)
            {
                return;
            }

            UpdateEnemyNoiseMemory(deltaTime);
            agent.TickTackleCooldown(deltaTime, this);
            if (agent.Status == CaptureStatus.Captured || agent.Status == CaptureStatus.Held)
            {
                input.ClearManualInput();
                return;
            }

            captureSystem?.TryRescueNearby(agent);

            if (agent.Status == CaptureStatus.Holding)
            {
                input.ClearManualInput();
                return;
            }

            var heldEnemy = FindHeldEnemyForKing();
            if (heldEnemy != null)
            {
                CurrentAgentTarget = heldEnemy;
                if (CaptureInteractionRules.IsInRange(transform.position, heldEnemy.transform.position, CaptureInteractionRules.CaptureRange))
                {
                    input.SetManualInput(Vector2.zero, captureHeld: true);
                    captureSystem?.TickFinalCapture(agent, deltaTime);
                    return;
                }

                MoveToward(heldEnemy.transform.position, deltaTime, true);
                return;
            }

            var allyToRescue = FindHeldAlly();
            if (allyToRescue != null)
            {
                CurrentAgentTarget = allyToRescue;
                MoveToward(allyToRescue.transform.position, deltaTime, true);
                return;
            }

            var enemyToChase = FindEnemyToChase();
            if (enemyToChase != null)
            {
                CurrentAgentTarget = enemyToChase;
                MoveToward(enemyToChase.transform.position, deltaTime, true);
                if (IsInsideTackleRange(enemyToChase))
                {
                    captureSystem?.TryTackle(agent);
                }
                return;
            }

            if (RunNoiseSearch(deltaTime))
            {
                return;
            }

            if (RunNoiseGuard(deltaTime))
            {
                return;
            }

            if (TryGetEnemyNoisePosition(out var noisePosition))
            {
                IsInvestigatingNoise = true;
                CurrentNoiseTarget = noisePosition;
                if (IsAtPosition(noisePosition, pointArrivalDistance))
                {
                    StartNoiseSearch(noisePosition);
                    RunNoiseSearch(deltaTime);
                    return;
                }

                MoveToward(noisePosition, deltaTime, true);
                return;
            }

            var point = ChooseCapturePoint();
            if (point != null)
            {
                CurrentPointTarget = point;
                MoveToward(point.transform.position, deltaTime, true);
                return;
            }

            input.ClearManualInput();
        }

        private PlayerCaptureAgent FindHeldEnemyForKing()
        {
            if (agent == null || agent.Status != CaptureStatus.Free || agent.CaptureAuthorityState != MovementState.King)
            {
                return null;
            }

            PlayerCaptureAgent best = null;
            var bestDistance = float.MaxValue;
            foreach (var candidate in captureAgents)
            {
                if (candidate == null || candidate == agent || candidate.Team == null || candidate.Status != CaptureStatus.Held)
                {
                    continue;
                }

                if (candidate.Team.Team == TeamId.None || candidate.Team.Team == team.Team)
                {
                    continue;
                }

                var distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private PlayerCaptureAgent FindHeldAlly()
        {
            PlayerCaptureAgent best = null;
            var bestDistance = float.MaxValue;
            foreach (var candidate in captureAgents)
            {
                if (candidate == null || candidate == agent || candidate.Team == null || candidate.Status != CaptureStatus.Held)
                {
                    continue;
                }

                if (candidate.Team.Team != team.Team)
                {
                    continue;
                }

                var distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private PlayerCaptureAgent FindEnemyToChase()
        {
            if (agent == null || !CaptureInteractionRules.CanTackle(agent.PersistentState, agent.Status, motor.currentStamina))
            {
                return null;
            }

            PlayerCaptureAgent best = null;
            var bestDistance = float.MaxValue;
            foreach (var candidate in captureAgents)
            {
                if (candidate == null || candidate == agent || candidate.Team == null)
                {
                    continue;
                }

                if (!CaptureInteractionRules.CanTackleTarget(team.Team, candidate.Team.Team, candidate.Status))
                {
                    continue;
                }

                var distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance <= chaseRange && distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private bool TryGetEnemyNoisePosition(out Vector3 noisePosition)
        {
            noisePosition = transform.position;
            if (rememberedEnemyNoiseTimer <= 0f)
            {
                return false;
            }

            noisePosition = rememberedEnemyNoisePosition;
            return true;
        }

        private void UpdateEnemyNoiseMemory(float deltaTime)
        {
            rememberedEnemyNoiseTimer = Mathf.Max(0f, rememberedEnemyNoiseTimer - deltaTime);
            if (TryReadCurrentEnemyNoise(out var noisePosition))
            {
                var shouldResetSearch = rememberedEnemyNoiseTimer <= 0f
                    || Vector3.Distance(rememberedEnemyNoisePosition, noisePosition) > 0.05f;

                rememberedEnemyNoisePosition = noisePosition;
                rememberedEnemyNoiseTimer = enemyNoiseMemorySeconds;
                hasConsumedEnemyNoise = false;
                if (shouldResetSearch)
                {
                    ResetNoiseSearch(noisePosition);
                }
            }
        }

        private bool TryReadCurrentEnemyNoise(out Vector3 noisePosition)
        {
            noisePosition = transform.position;
            if (hearingSensor == null || !hearingSensor.HasHeardNoise)
            {
                return false;
            }

            var source = hearingSensor.LastHeardSource;
            if (source == null || source == gameObject)
            {
                return false;
            }

            var sourceTeam = source.GetComponent<LocalPlayerTeam>();
            if (sourceTeam == null || sourceTeam.Team == TeamId.None || team.Team == TeamId.None)
            {
                return false;
            }

            if (sourceTeam.Team == team.Team)
            {
                return false;
            }

            noisePosition = hearingSensor.LastHeardPosition;
            if (hasConsumedEnemyNoise && Vector3.Distance(consumedEnemyNoisePosition, noisePosition) <= 0.05f)
            {
                return false;
            }

            return true;
        }

        private bool RunNoiseSearch(float deltaTime)
        {
            if (noiseSearchTimer <= 0f)
            {
                return false;
            }

            noiseSearchTimer = Mathf.Max(0f, noiseSearchTimer - deltaTime);
            if (noiseSearchTimer <= 0f)
            {
                noiseGuardTimer = noiseGuardDurationSeconds;
                return RunNoiseGuard(deltaTime);
            }

            IsSearchingNoise = true;
            CurrentNoiseTarget = noiseSearchWaypoint;
            noiseSearchWaypointTimer = Mathf.Max(0f, noiseSearchWaypointTimer - deltaTime);
            if (noiseSearchWaypointTimer <= 0f || IsAtPosition(noiseSearchWaypoint, pointArrivalDistance))
            {
                ChooseNextNoiseSearchWaypoint();
                CurrentNoiseTarget = noiseSearchWaypoint;
            }

            MoveToward(noiseSearchWaypoint, deltaTime, false);
            return true;
        }

        private bool RunNoiseGuard(float deltaTime)
        {
            if (noiseGuardTimer <= 0f)
            {
                return false;
            }

            IsGuardingNoise = true;
            CurrentNoiseTarget = noiseSearchCenter;
            noiseGuardTimer = Mathf.Max(0f, noiseGuardTimer - deltaTime);
            input.SetManualInput(Vector2.zero);

            if (noiseGuardTimer <= 0f)
            {
                ClearNoiseMemory();
            }

            return true;
        }

        private void StartNoiseSearch(Vector3 center)
        {
            if (noiseSearchTimer > 0f || noiseGuardTimer > 0f)
            {
                return;
            }

            noiseSearchCenter = center;
            noiseSearchTimer = noiseSearchDurationSeconds;
            ChooseNextNoiseSearchWaypoint();
        }

        private void ResetNoiseSearch(Vector3 center)
        {
            noiseSearchCenter = center;
            noiseSearchTimer = 0f;
            noiseGuardTimer = 0f;
            noiseSearchWaypointTimer = 0f;
            noiseSearchStep = 0;
            noiseSearchWaypoint = center;
        }

        private void ClearNoiseMemory()
        {
            consumedEnemyNoisePosition = rememberedEnemyNoisePosition;
            hasConsumedEnemyNoise = true;
            rememberedEnemyNoiseTimer = 0f;
            noiseSearchTimer = 0f;
            noiseGuardTimer = 0f;
            noiseSearchWaypointTimer = 0f;
            noiseSearchStep = 0;
        }

        private void ChooseNextNoiseSearchWaypoint()
        {
            noiseSearchWaypointTimer = noiseSearchWaypointSeconds;
            noiseSearchStep++;
            var phase = noiseSearchStep % 4;
            var offset = phase switch
            {
                0 => Vector3.forward,
                1 => Vector3.right,
                2 => Vector3.back,
                _ => Vector3.left
            };
            noiseSearchWaypoint = noiseSearchCenter + offset * noiseSearchRadius;
        }

        private CapturePoint ChooseCapturePoint()
        {
            CapturePoint best = null;
            var bestScore = float.MaxValue;
            foreach (var point in capturePoints)
            {
                if (point == null)
                {
                    continue;
                }

                var distance = Vector3.Distance(transform.position, point.transform.position);
                var ownerPenalty = point.Owner == team.Team ? 15f : 0f;
                var score = distance + ownerPenalty;
                if (score < bestScore)
                {
                    best = point;
                    bestScore = score;
                }
            }

            return best;
        }

        private bool IsInsideTackleRange(PlayerCaptureAgent target)
        {
            if (target == null)
            {
                return false;
            }

            var range = agent.PersistentState == MovementState.King
                ? CaptureInteractionRules.KingTackleRange
                : CaptureInteractionRules.TackleRange;
            return CaptureInteractionRules.IsInRange(transform.position, target.transform.position, range)
                && CaptureInteractionRules.IsInsideForwardCone(transform, target.transform.position, CaptureInteractionRules.TackleAngleDegrees);
        }

        private bool IsAtPosition(Vector3 worldPosition, float distance)
        {
            var offset = worldPosition - transform.position;
            offset.y = 0f;
            return offset.magnitude <= distance;
        }

        private void MoveToward(Vector3 worldPosition, float deltaTime, bool sprint)
        {
            var finalOffset = worldPosition - transform.position;
            finalOffset.y = 0f;
            if (finalOffset.magnitude <= pointArrivalDistance)
            {
                input.SetManualInput(Vector2.zero);
                return;
            }

            var steeringTarget = ResolveSteeringTarget(worldPosition);
            var direction = steeringTarget - transform.position;
            direction.y = 0f;
            if (direction.magnitude <= navMeshCornerArrivalDistance)
            {
                input.SetManualInput(Vector2.zero);
                return;
            }

            var normalized = direction.normalized;
            var targetRotation = Quaternion.LookRotation(normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnDegreesPerSecond * deltaTime);
            var local = transform.InverseTransformDirection(normalized);
            input.SetManualInput(new Vector2(local.x, local.z), sprint);
        }

        private Vector3 ResolveSteeringTarget(Vector3 worldPosition)
        {
            if (TryResolveNavMeshSteeringTarget(worldPosition, out var steeringTarget))
            {
                LastMoveMode = LocalBotMoveMode.NavMeshPath;
                LastSteeringTarget = steeringTarget;
                return steeringTarget;
            }

            LastMoveMode = LocalBotMoveMode.DirectFallback;
            LastSteeringTarget = worldPosition;
            return worldPosition;
        }

        private bool TryResolveNavMeshSteeringTarget(Vector3 worldPosition, out Vector3 steeringTarget)
        {
            steeringTarget = worldPosition;
            if (!NavMesh.SamplePosition(transform.position, out var startHit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                return false;
            }

            if (!NavMesh.SamplePosition(worldPosition, out var targetHit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                return false;
            }

            navMeshPath ??= new NavMeshPath();
            if (!NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, navMeshPath))
            {
                return false;
            }

            if (navMeshPath.status != NavMeshPathStatus.PathComplete || navMeshPath.corners.Length < 2)
            {
                return false;
            }

            for (var index = 1; index < navMeshPath.corners.Length; index++)
            {
                var cornerOffset = navMeshPath.corners[index] - startHit.position;
                cornerOffset.y = 0f;
                if (cornerOffset.magnitude > navMeshCornerArrivalDistance)
                {
                    steeringTarget = navMeshPath.corners[index];
                    return true;
                }
            }

            steeringTarget = targetHit.position;
            return true;
        }

        private void ClearSteeringTelemetry()
        {
            LastMoveMode = LocalBotMoveMode.None;
            LastSteeringTarget = transform.position;
        }

        private void ResolveReferences()
        {
            input ??= GetComponent<PlayerInputReader>();
            motor ??= GetComponent<PlayerMotor>();
            agent ??= GetComponent<PlayerCaptureAgent>();
            team ??= GetComponent<LocalPlayerTeam>();
            hearingSensor ??= GetComponent<AIHearingSensor>();
        }

        private void OnValidate()
        {
            pointArrivalDistance = Mathf.Max(0.1f, pointArrivalDistance);
            chaseRange = Mathf.Max(0f, chaseRange);
            turnDegreesPerSecond = Mathf.Max(0f, turnDegreesPerSecond);
            navMeshSampleDistance = Mathf.Max(0.1f, navMeshSampleDistance);
            navMeshCornerArrivalDistance = Mathf.Max(0.05f, navMeshCornerArrivalDistance);
            enemyNoiseMemorySeconds = Mathf.Max(0f, enemyNoiseMemorySeconds);
            noiseSearchDurationSeconds = Mathf.Max(0f, noiseSearchDurationSeconds);
            noiseGuardDurationSeconds = Mathf.Max(0f, noiseGuardDurationSeconds);
            noiseSearchRadius = Mathf.Max(0f, noiseSearchRadius);
            noiseSearchWaypointSeconds = Mathf.Max(0.05f, noiseSearchWaypointSeconds);
        }
    }
}
