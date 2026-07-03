using System;
using UnityEngine;

namespace Overthrone
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class LocalMatchManager : MonoBehaviour
    {
        [SerializeField] private CapturePoint[] capturePoints = Array.Empty<CapturePoint>();
        [SerializeField] private LocalPlayerTeam[] participants = Array.Empty<LocalPlayerTeam>();
        [SerializeField] private float attackerGraceSeconds = LocalMatchRules.AttackerGraceSeconds;
        [SerializeField] private float victoryCountdownSeconds = LocalMatchRules.VictoryCountdownSeconds;
        [SerializeField] private float matchDurationSeconds = LocalMatchRules.MatchDurationSeconds;

        private readonly System.Collections.Generic.Dictionary<LocalPlayerTeam, float> attackerGraceRemaining = new System.Collections.Generic.Dictionary<LocalPlayerTeam, float>();
        private TeamId victoryCountdownTeam = TeamId.None;
        private float victoryTimeRemaining = LocalMatchRules.VictoryCountdownSeconds;
        private float matchTimeRemaining = LocalMatchRules.MatchDurationSeconds;
        private TeamId defenderTeam = TeamId.None;
        private float defenderReentryTimeRemaining;

        public CapturePoint[] CapturePoints => capturePoints;
        public LocalPlayerTeam[] Participants => participants;
        public int BlueOwnedCount => GetOwnedCount(TeamId.Blue);
        public int RedOwnedCount => GetOwnedCount(TeamId.Red);
        public TeamId VictoryCountdownTeam => victoryCountdownTeam;
        public float VictoryTimeRemaining => victoryTimeRemaining;
        public float MatchDurationSeconds => matchDurationSeconds;
        public float MatchTimeRemaining => matchTimeRemaining;
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
            Configure(points, matchParticipants, matchDurationSeconds);
        }

        public void Configure(CapturePoint[] points, LocalPlayerTeam[] matchParticipants, float durationSeconds)
        {
            capturePoints = points ?? Array.Empty<CapturePoint>();
            participants = matchParticipants ?? Array.Empty<LocalPlayerTeam>();
            matchDurationSeconds = Mathf.Max(0f, durationSeconds);
            victoryCountdownTeam = TeamId.None;
            victoryTimeRemaining = victoryCountdownSeconds;
            matchTimeRemaining = matchDurationSeconds;
            defenderTeam = TeamId.None;
            defenderReentryTimeRemaining = 0f;
            Winner = TeamId.None;
            Phase = LocalMatchPhase.Playing;
            LastFlowEvent = default;
            attackerGraceRemaining.Clear();
        }

        private void Awake()
        {
            matchDurationSeconds = Mathf.Max(0f, matchDurationSeconds);
            matchTimeRemaining = matchDurationSeconds;

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
            if (Phase == LocalMatchPhase.Result)
            {
                return;
            }

            ApplyAllCapturedVictory();
            if (Phase == LocalMatchPhase.Result)
            {
                return;
            }

            ApplyForfeitVictory();
            if (Phase == LocalMatchPhase.Result)
            {
                return;
            }

            if (ShouldResolveTimeoutBeforeStartingCountdown(deltaTime))
            {
                ApplyMatchTimeout(deltaTime);
                if (Phase == LocalMatchPhase.Result)
                {
                    return;
                }
            }

            ApplyVictoryCountdown(deltaTime);

            if (Phase == LocalMatchPhase.Result)
            {
                return;
            }

            ApplyMatchTimeout(deltaTime);
            if (Phase == LocalMatchPhase.Result)
            {
                return;
            }

            var blueOwnedCount = GetOwnedCount(TeamId.Blue);
            var redOwnedCount = GetOwnedCount(TeamId.Red);
            var blueRallyActive = HasAttackerRally(TeamId.Blue);
            var redRallyActive = HasAttackerRally(TeamId.Red);
            var blueKingCandidate = ResolveKingCandidate(TeamId.Blue, blueOwnedCount);
            var redKingCandidate = ResolveKingCandidate(TeamId.Red, redOwnedCount);

            foreach (var participant in participants)
            {
                var ownedCount = participant != null && participant.Team == TeamId.Blue ? blueOwnedCount : redOwnedCount;
                var rallyActive = participant != null && participant.Team == TeamId.Blue ? blueRallyActive : redRallyActive;
                var kingCandidate = participant != null && participant.Team == TeamId.Blue ? blueKingCandidate : redKingCandidate;
                ApplyParticipantState(participant, deltaTime, ownedCount, rallyActive, kingCandidate);
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

        private void ApplyAllCapturedVictory()
        {
            if (Winner != TeamId.None)
            {
                return;
            }

            var captureWinner = ResolveAllCapturedWinner();
            if (captureWinner == TeamId.None)
            {
                return;
            }

            CompleteRound(captureWinner);
        }

        private void ApplyForfeitVictory()
        {
            if (Winner != TeamId.None)
            {
                return;
            }

            var forfeitWinner = ResolveForfeitWinner();
            if (forfeitWinner == TeamId.None)
            {
                return;
            }

            CompleteRound(forfeitWinner);
        }

        private void ApplyMatchTimeout(float deltaTime)
        {
            matchTimeRemaining = LocalMatchRules.TickMatchTimeRemaining(matchTimeRemaining, deltaTime);
            if (!LocalMatchRules.HasMatchTimedOut(matchTimeRemaining))
            {
                return;
            }

            CompleteRound(ResolveTimeoutWinner());
        }

        private bool ShouldResolveTimeoutBeforeStartingCountdown(float deltaTime)
        {
            return victoryCountdownTeam == TeamId.None
                && LocalMatchRules.HasMatchTimedOut(LocalMatchRules.TickMatchTimeRemaining(matchTimeRemaining, deltaTime));
        }

        private TeamId ResolveAllCapturedWinner()
        {
            var hasBlue = false;
            var hasRed = false;
            var blueAllCaptured = true;
            var redAllCaptured = true;

            foreach (var participant in participants)
            {
                if (participant == null || !participant.isActiveAndEnabled || participant.Team == TeamId.None)
                {
                    continue;
                }

                var captureAgent = participant.GetComponent<PlayerCaptureAgent>();
                var isCaptured = captureAgent != null && captureAgent.Status == CaptureStatus.Captured;
                if (participant.Team == TeamId.Blue)
                {
                    hasBlue = true;
                    blueAllCaptured &= isCaptured;
                }
                else if (participant.Team == TeamId.Red)
                {
                    hasRed = true;
                    redAllCaptured &= isCaptured;
                }
            }

            if (hasBlue && hasRed && blueAllCaptured)
            {
                return TeamId.Red;
            }

            return hasBlue && hasRed && redAllCaptured ? TeamId.Blue : TeamId.None;
        }

        private TeamId ResolveForfeitWinner()
        {
            var hasBlueRoster = false;
            var hasRedRoster = false;
            var activeBlueCount = 0;
            var activeRedCount = 0;

            foreach (var participant in participants)
            {
                if (participant == null || participant.Team == TeamId.None)
                {
                    continue;
                }

                if (participant.Team == TeamId.Blue)
                {
                    hasBlueRoster = true;
                    activeBlueCount += participant.isActiveAndEnabled ? 1 : 0;
                }
                else if (participant.Team == TeamId.Red)
                {
                    hasRedRoster = true;
                    activeRedCount += participant.isActiveAndEnabled ? 1 : 0;
                }
            }

            return LocalMatchRules.ResolveForfeitWinner(hasBlueRoster, hasRedRoster, activeBlueCount, activeRedCount);
        }

        private TeamId ResolveTimeoutWinner()
        {
            var blueSurvivorCount = 0;
            var redSurvivorCount = 0;

            foreach (var participant in participants)
            {
                if (participant == null || !participant.isActiveAndEnabled)
                {
                    continue;
                }

                var captureAgent = participant.GetComponent<PlayerCaptureAgent>();
                if (captureAgent != null && captureAgent.Status == CaptureStatus.Captured)
                {
                    continue;
                }

                if (participant.Team == TeamId.Blue)
                {
                    blueSurvivorCount++;
                }
                else if (participant.Team == TeamId.Red)
                {
                    redSurvivorCount++;
                }
            }

            return LocalMatchRules.ResolveTimeoutWinner(
                blueSurvivorCount,
                redSurvivorCount,
                BlueOwnedCount,
                RedOwnedCount
            );
        }

        private void ApplyParticipantState(
            LocalPlayerTeam participant,
            float deltaTime,
            int ownedPointCount,
            bool rallyActive,
            LocalPlayerTeam kingCandidate)
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

            var graceActive = UpdateAttackerGrace(participant, rallyActive, deltaTime);
            var desiredState = LocalMatchRules.ResolvePlayerState(
                ownedPointCount,
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

        private LocalPlayerTeam ResolveKingCandidate(TeamId team, int ownedPointCount)
        {
            if (team == TeamId.None || ownedPointCount < LocalMatchRules.KingOwnedPointThreshold)
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
            matchDurationSeconds = Mathf.Max(0f, matchDurationSeconds);
            matchTimeRemaining = Mathf.Max(0f, matchTimeRemaining);
            defenderReentryTimeRemaining = Mathf.Max(0f, defenderReentryTimeRemaining);
        }

        private static TeamId OpposingTeam(TeamId team)
        {
            return team == TeamId.Blue ? TeamId.Red : team == TeamId.Red ? TeamId.Blue : TeamId.None;
        }

        private void CompleteRound(TeamId winner)
        {
            Winner = winner;
            Phase = LocalMatchPhase.Result;
            victoryCountdownTeam = TeamId.None;
            victoryTimeRemaining = victoryCountdownSeconds;
            defenderTeam = TeamId.None;
            defenderReentryTimeRemaining = 0f;
            EmitFlowEvent(new LocalMatchFlowEvent(
                LocalMatchFlowEventType.RoundEnded,
                Winner,
                TeamId.None,
                0f
            ));
        }

        private void EmitFlowEvent(LocalMatchFlowEvent flowEvent)
        {
            LastFlowEvent = flowEvent;
            FlowChanged?.Invoke(flowEvent);
        }
    }
}
