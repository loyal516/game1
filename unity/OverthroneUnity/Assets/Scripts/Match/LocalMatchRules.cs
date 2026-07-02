namespace Overthrone
{
    public static class LocalMatchRules
    {
        public const int AttackerRallyPlayerCount = 3;
        public const int KingOwnedPointThreshold = 2;
        public const int VictoryOwnedPointThreshold = 3;
        public const float AttackerGraceSeconds = 5f;
        public const float VictoryCountdownSeconds = 30f;

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
