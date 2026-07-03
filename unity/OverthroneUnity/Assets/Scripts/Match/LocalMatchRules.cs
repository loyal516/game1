namespace Overthrone
{
    public static class LocalMatchRules
    {
        public const int AttackerRallyPlayerCount = 3;
        public const int KingOwnedPointThreshold = 2;
        public const int VictoryOwnedPointThreshold = 3;
        public const float AttackerGraceSeconds = 5f;
        public const float VictoryCountdownSeconds = 30f;
        public const float MatchDurationSeconds = 600f;

        public static MovementState ResolvePlayerState(int ownedPointCount, bool attackerRallyActive, bool attackerGraceActive)
        {
            return ResolvePlayerState(ownedPointCount, attackerRallyActive, attackerGraceActive, true);
        }

        public static MovementState ResolvePlayerState(
            int ownedPointCount,
            bool attackerRallyActive,
            bool attackerGraceActive,
            bool isKingCandidate)
        {
            if (ownedPointCount >= KingOwnedPointThreshold)
            {
                return isKingCandidate ? MovementState.King : ResolveNonKingState(attackerRallyActive, attackerGraceActive);
            }

            return ResolveNonKingState(attackerRallyActive, attackerGraceActive);
        }

        public static bool HasAttackerRally(int sameTeamPlayerCount)
        {
            return sameTeamPlayerCount >= AttackerRallyPlayerCount;
        }

        public static bool IsMatchManagedState(MovementState state)
        {
            return state == MovementState.Neutral || state == MovementState.Attacker || state == MovementState.King;
        }

        public static TeamId ResolveCountdownTeam(int blueOwnedCount, int redOwnedCount)
        {
            if (blueOwnedCount >= VictoryOwnedPointThreshold)
            {
                return TeamId.Blue;
            }

            return redOwnedCount >= VictoryOwnedPointThreshold ? TeamId.Red : TeamId.None;
        }

        public static float TickCountdownRemaining(
            TeamId previousCountdownTeam,
            TeamId nextCountdownTeam,
            float previousRemaining,
            float deltaTime,
            float countdownSeconds = VictoryCountdownSeconds)
        {
            countdownSeconds = Max(0f, countdownSeconds);
            if (nextCountdownTeam == TeamId.None)
            {
                return countdownSeconds;
            }

            if (previousCountdownTeam != nextCountdownTeam)
            {
                return countdownSeconds;
            }

            return Max(0f, previousRemaining - Max(0f, deltaTime));
        }

        public static bool HasCountdownWon(TeamId countdownTeam, float remainingSeconds)
        {
            return countdownTeam != TeamId.None && remainingSeconds <= 0f;
        }

        public static float TickMatchTimeRemaining(float previousRemaining, float deltaTime)
        {
            return Max(0f, previousRemaining - Max(0f, deltaTime));
        }

        public static bool HasMatchTimedOut(float remainingSeconds)
        {
            return remainingSeconds <= 0f;
        }

        public static TeamId ResolveForfeitWinner(
            bool hasBlueRoster,
            bool hasRedRoster,
            int activeBlueCount,
            int activeRedCount)
        {
            if (!hasBlueRoster || !hasRedRoster)
            {
                return TeamId.None;
            }

            if (activeBlueCount <= 0 && activeRedCount > 0)
            {
                return TeamId.Red;
            }

            return activeRedCount <= 0 && activeBlueCount > 0 ? TeamId.Blue : TeamId.None;
        }

        public static TeamId ResolveTimeoutWinner(
            int blueSurvivorCount,
            int redSurvivorCount,
            int blueOwnedCount,
            int redOwnedCount)
        {
            if (blueSurvivorCount != redSurvivorCount)
            {
                return blueSurvivorCount > redSurvivorCount ? TeamId.Blue : TeamId.Red;
            }

            if (blueOwnedCount != redOwnedCount)
            {
                return blueOwnedCount > redOwnedCount ? TeamId.Blue : TeamId.Red;
            }

            return TeamId.None;
        }

        private static float Max(float left, float right)
        {
            return left > right ? left : right;
        }

        private static MovementState ResolveNonKingState(bool attackerRallyActive, bool attackerGraceActive)
        {
            return attackerRallyActive || attackerGraceActive ? MovementState.Attacker : MovementState.Neutral;
        }
    }
}
