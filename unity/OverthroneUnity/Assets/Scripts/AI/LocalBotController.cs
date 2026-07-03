using System;
using UnityEngine;

namespace Overthrone
{
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

        private PlayerInputReader input;
        private PlayerMotor motor;
        private PlayerCaptureAgent agent;
        private LocalPlayerTeam team;

        public CapturePoint CurrentPointTarget { get; private set; }
        public PlayerCaptureAgent CurrentAgentTarget { get; private set; }

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

            if (input == null || agent == null || team == null || motor == null)
            {
                return;
            }

            agent.TickTackleCooldown(deltaTime, this);
            if (agent.Status == CaptureStatus.Captured || agent.Status == CaptureStatus.Held)
            {
                input.ClearManualInput();
                return;
            }

            captureSystem?.TryRescueNearby(agent);

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

            if (agent.Status == CaptureStatus.Holding)
            {
                input.ClearManualInput();
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
            if (agent == null || agent.CaptureAuthorityState != MovementState.King)
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

        private void MoveToward(Vector3 worldPosition, float deltaTime, bool sprint)
        {
            var direction = worldPosition - transform.position;
            direction.y = 0f;
            if (direction.magnitude <= pointArrivalDistance)
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

        private void ResolveReferences()
        {
            input ??= GetComponent<PlayerInputReader>();
            motor ??= GetComponent<PlayerMotor>();
            agent ??= GetComponent<PlayerCaptureAgent>();
            team ??= GetComponent<LocalPlayerTeam>();
        }

        private void OnValidate()
        {
            pointArrivalDistance = Mathf.Max(0.1f, pointArrivalDistance);
            chaseRange = Mathf.Max(0f, chaseRange);
            turnDegreesPerSecond = Mathf.Max(0f, turnDegreesPerSecond);
        }
    }
}
