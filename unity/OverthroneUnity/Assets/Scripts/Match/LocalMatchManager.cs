using System;
using UnityEngine;

namespace Overthrone
{
    [DisallowMultipleComponent]
    public sealed class LocalMatchManager : MonoBehaviour
    {
        [SerializeField] private CapturePoint[] capturePoints = Array.Empty<CapturePoint>();
        [SerializeField] private LocalPlayerTeam[] participants = Array.Empty<LocalPlayerTeam>();
        [SerializeField] private float attackerGraceSeconds = LocalMatchRules.AttackerGraceSeconds;
        [SerializeField] private float victoryCountdownSeconds = LocalMatchRules.VictoryCountdownSeconds;

        private readonly System.Collections.Generic.Dictionary<LocalPlayerTeam, float> attackerGraceRemaining = new System.Collections.Generic.Dictionary<LocalPlayerTeam, float>();
        private TeamId victoryCountdownTeam = TeamId.None;
        private float victoryTimeRemaining = LocalMatchRules.VictoryCountdownSeconds;
        private TeamId defenderTeam = TeamId.None;
        private float defenderReentryTimeRemaining;

        public CapturePoint[] CapturePoints => capturePoints;
        public LocalPlayerTeam[] Participants => participants;
        public int BlueOwnedCount => GetOwnedCount(TeamId.Blue);
        public int RedOwnedCount => GetOwnedCount(TeamId.Red);
        public TeamId VictoryCountdownTeam => victoryCountdownTeam;
        public float VictoryTimeRemaining => victoryTimeRemaining;
        public LocalMatchPhase Phase { get; private set; } = LocalMatchPhase.Playing;
        public TeamId DefenderTeam => defenderTeam;
        public float DefenderReentryTimeRemaining => defenderReentryTimeRemaining;
        public bool IsDefenderReentryWindowActive => Phase == LocalMatchPhase.VictoryCountdown
            && defenderTeam != TeamId.None
            && Winner == TeamId.None;
        public LocalMatchFlowEvent LastFlowEvent { get; private set; }
        public TeamId Winner { get; private set; }
        public bool IsVictoryCountdownActive => victoryCountdownTeam != TeamId.None && Winner == TeamId.None;
        public event Action<LocalMatchFlowEvent> FlowChanged;

        public void Configure(CapturePoint[] points, LocalPlayerTeam[] matchParticipants = null)
        {
            capturePoints = points ?? Array.Empty<CapturePoint>();
            participants = matchParticipants ?? Array.Empty<LocalPlayerTeam>();
            victoryCountdownTeam = TeamId.None;
            victoryTimeRemaining = victoryCountdownSeconds;
            defenderTeam = TeamId.None;
            defenderReentryTimeRemaining = 0f;
            Winner = TeamId.None;
            Phase = LocalMatchPhase.Playing;
            LastFlowEvent = default;
            attackerGraceRemaining.Clear();
        }

        private void Awake()
        {
            if (participants == null || participants.Length == 0)
            {
                participants = FindObjectsByType<LocalPlayerTeam>(FindObjectsSortMode.None);
            }
        }

        private void Update()
        {
            ApplyMatchRules(Time.deltaTime);
        }

        public void ApplyMatchRules(float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);
            ApplyVictoryCountdown(deltaTime);

            if (Phase == LocalMatchPhase.Result)
            {
                return;
            }

            foreach (var participant in participants)
            {
                ApplyParticipantState(participant, deltaTime);
            }
        }

        public CapturePoint GetPoint(string pointId)
        {
            if (string.IsNullOrWhiteSpace(pointId))
            {
                return null;
            }

            foreach (var capturePoint in capturePoints)
            {
                if (capturePoint != null && string.Equals(capturePoint.PointId, pointId, StringComparison.OrdinalIgnoreCase))
                {
                    return capturePoint;
                }
            }

            return null;
        }

        public int GetOwnedCount(TeamId team)
        {
            var count = 0;
            foreach (var capturePoint in capturePoints)
            {
                if (capturePoint != null && capturePoint.Owner == team)
                {
                    count++;
                }
            }

            return count;
        }

        public bool HasAttackerRally(TeamId team)
        {
            foreach (var capturePoint in capturePoints)
            {
                if (capturePoint == null)
                {
                    continue;
                }

                var sameTeamCount = team == TeamId.Blue ? capturePoint.BlueCount : capturePoint.RedCount;
                if (LocalMatchRules.HasAttackerRally(sameTeamCount))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyVictoryCountdown(float deltaTime)
        {
            if (Winner != TeamId.None)
            {
                Phase = LocalMatchPhase.Result;
                return;
            }

            var previousCountdownTeam = victoryCountdownTeam;
            var previousPhase = Phase;
            var previousRemaining = victoryTimeRemaining;
            var nextCountdownTeam = LocalMatchRules.ResolveCountdownTeam(BlueOwnedCount, RedOwnedCount);
            victoryTimeRemaining = LocalMatchRules.TickCountdownRemaining(
                victoryCountdownTeam,
                nextCountdownTeam,
                victoryTimeRemaining,
                deltaTime,
                victoryCountdownSeconds
            );
            victoryCountdownTeam = nextCountdownTeam;

            if (LocalMatchRules.HasCountdownWon(victoryCountdownTeam, victoryTimeRemaining))
            {
                Winner = victoryCountdownTeam;
                Phase = LocalMatchPhase.Result;
                defenderReentryTimeRemaining = 0f;
                defenderTeam = TeamId.None;
                EmitFlowEvent(new LocalMatchFlowEvent(
                    LocalMatchFlowEventType.RoundEnded,
                    Winner,
                    TeamId.None,
                    0f
                ));
                return;
            }

            if (victoryCountdownTeam != TeamId.None)
            {
                Phase = LocalMatchPhase.VictoryCountdown;
                defenderTeam = OpposingTeam(victoryCountdownTeam);
                defenderReentryTimeRemaining = victoryTimeRemaining;

                if (previousPhase != LocalMatchPhase.VictoryCountdown || previousCountdownTeam != victoryCountdownTeam)
                {
                    EmitFlowEvent(new LocalMatchFlowEvent(
                        LocalMatchFlowEventType.VictoryCountdownStarted,
                        victoryCountdownTeam,
                        defenderTeam,
                        victoryTimeRemaining
                    ));
                }
                return;
            }

            Phase = LocalMatchPhase.Playing;
            defenderTeam = TeamId.None;
            defenderReentryTimeRemaining = 0f;

            if (previousPhase == LocalMatchPhase.VictoryCountdown || previousCountdownTeam != TeamId.None)
            {
                EmitFlowEvent(new LocalMatchFlowEvent(
                    LocalMatchFlowEventType.VictoryCountdownInterrupted,
                    previousCountdownTeam,
                    TeamId.None,
                    previousRemaining
                ));
            }
        }

        private void ApplyParticipantState(LocalPlayerTeam participant, float deltaTime)
        {
            if (participant == null || !participant.isActiveAndEnabled || participant.Team == TeamId.None)
            {
                return;
            }

            var stateController = participant.GetComponent<PlayerStateController>();
            if (stateController == null || !LocalMatchRules.IsMatchManagedState(stateController.PersistentState))
            {
                return;
            }

            var rallyActive = HasAttackerRally(participant.Team);
            var graceActive = UpdateAttackerGrace(participant, rallyActive, deltaTime);
            var kingCandidate = ResolveKingCandidate(participant.Team);
            var desiredState = LocalMatchRules.ResolvePlayerState(
                GetOwnedCount(participant.Team),
                rallyActive,
                graceActive,
                kingCandidate == participant
            );

            if (stateController.PersistentState != desiredState)
            {
                stateController.SetPersistentState(desiredState);
            }
        }

        private bool UpdateAttackerGrace(LocalPlayerTeam participant, bool rallyActive, float deltaTime)
        {
            if (rallyActive)
            {
                attackerGraceRemaining[participant] = Mathf.Max(0f, attackerGraceSeconds);
                return true;
            }

            if (!attackerGraceRemaining.TryGetValue(participant, out var remaining))
            {
                return false;
            }

            remaining -= deltaTime;
            if (remaining <= 0f)
            {
                attackerGraceRemaining.Remove(participant);
                return false;
            }

            attackerGraceRemaining[participant] = remaining;
            return true;
        }

        private LocalPlayerTeam ResolveKingCandidate(TeamId team)
        {
            if (team == TeamId.None || GetOwnedCount(team) < LocalMatchRules.KingOwnedPointThreshold)
            {
                return null;
            }

            LocalPlayerTeam bestCandidate = null;
            foreach (var participant in participants)
            {
                if (!IsEligibleKingCandidate(participant, team))
                {
                    continue;
                }

                if (bestCandidate == null || IsBetterKingCandidate(participant, bestCandidate))
                {
                    bestCandidate = participant;
                }
            }

            return bestCandidate;
        }

        private static bool IsEligibleKingCandidate(LocalPlayerTeam participant, TeamId team)
        {
            if (participant == null || !participant.isActiveAndEnabled || participant.Team != team)
            {
                return false;
            }

            var stateController = participant.GetComponent<PlayerStateController>();
            return stateController != null && LocalMatchRules.IsMatchManagedState(stateController.PersistentState);
        }

        private static bool IsBetterKingCandidate(LocalPlayerTeam candidate, LocalPlayerTeam current)
        {
            if (candidate.FinalCaptureCount != current.FinalCaptureCount)
            {
                return candidate.FinalCaptureCount > current.FinalCaptureCount;
            }

            if (!Mathf.Approximately(candidate.PointContribution, current.PointContribution))
            {
                return candidate.PointContribution > current.PointContribution;
            }

            return candidate.KingTieBreaker > current.KingTieBreaker;
        }

        private void OnValidate()
        {
            attackerGraceSeconds = Mathf.Max(0f, attackerGraceSeconds);
            victoryCountdownSeconds = Mathf.Max(0f, victoryCountdownSeconds);
            victoryTimeRemaining = Mathf.Max(0f, victoryTimeRemaining);
            defenderReentryTimeRemaining = Mathf.Max(0f, defenderReentryTimeRemaining);
        }

        private static TeamId OpposingTeam(TeamId team)
        {
            return team == TeamId.Blue ? TeamId.Red : team == TeamId.Red ? TeamId.Blue : TeamId.None;
        }

        private void EmitFlowEvent(LocalMatchFlowEvent flowEvent)
        {
            LastFlowEvent = flowEvent;
            FlowChanged?.Invoke(flowEvent);
        }
    }
}
