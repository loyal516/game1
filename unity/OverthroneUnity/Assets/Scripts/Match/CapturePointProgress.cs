namespace Overthrone
{
    public sealed class CapturePointProgress
    {
        public const float OnePlayerCaptureRatePerSecond = 0.05f;
        public const float TwoPlayerCaptureRatePerSecond = 0.08f;
        public const float ThreePlusPlayerCaptureRatePerSecond = 0.12f;
        public const float EmptyDecayRatePerSecond = 0.03f;
        public const float OnePlayerCaptureRate = OnePlayerCaptureRatePerSecond;
        public const float TwoPlayerCaptureRate = TwoPlayerCaptureRatePerSecond;
        public const float ThreePlusPlayerCaptureRate = ThreePlusPlayerCaptureRatePerSecond;
        public const float EmptyDecayRate = EmptyDecayRatePerSecond;

        public TeamId Owner { get; private set; }
        public TeamId ActiveCapturingTeam { get; private set; }
        public float Progress01 { get; private set; }
        public bool IsContested { get; private set; }

        public void Tick(int blueCount, int redCount, float deltaTime)
        {
            blueCount = Max(0, blueCount);
            redCount = Max(0, redCount);
            deltaTime = Max(0f, deltaTime);

            if (blueCount > 0 && redCount > 0)
            {
                IsContested = true;
                ActiveCapturingTeam = TeamId.None;
                return;
            }

            IsContested = false;

            if (blueCount <= 0 && redCount <= 0)
            {
                TickEmpty(deltaTime);
                return;
            }

            var team = blueCount > 0 ? TeamId.Blue : TeamId.Red;
            var count = blueCount > 0 ? blueCount : redCount;
            TickTeamPresent(team, count, deltaTime);
        }

        public void Reset()
        {
            Owner = TeamId.None;
            ActiveCapturingTeam = TeamId.None;
            Progress01 = 0f;
            IsContested = false;
        }

        private void TickEmpty(float deltaTime)
        {
            ActiveCapturingTeam = TeamId.None;

            if (Owner != TeamId.None)
            {
                Progress01 = 1f;
                return;
            }

            Progress01 = Max(0f, Progress01 - EmptyDecayRatePerSecond * deltaTime);
        }

        private void TickTeamPresent(TeamId team, int count, float deltaTime)
        {
            ActiveCapturingTeam = team;

            if (Owner == team)
            {
                Progress01 = 1f;
                return;
            }

            var delta = CaptureRateFor(count) * deltaTime;
            if (Owner == TeamId.None)
            {
                Progress01 = Clamp01(Progress01 + delta);
                if (Progress01 >= 1f)
                {
                    Owner = team;
                    Progress01 = 1f;
                }
                return;
            }

            Progress01 = Max(0f, Progress01 - delta);
            if (Progress01 <= 0f)
            {
                Owner = TeamId.None;
                Progress01 = 0f;
            }
        }

        private static float CaptureRateFor(int count)
        {
            if (count >= 3)
            {
                return ThreePlusPlayerCaptureRatePerSecond;
            }

            if (count == 2)
            {
                return TwoPlayerCaptureRatePerSecond;
            }

            return count > 0 ? OnePlayerCaptureRatePerSecond : 0f;
        }

        private static int Max(int left, int right)
        {
            return left > right ? left : right;
        }

        private static float Max(float left, float right)
        {
            return left > right ? left : right;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }
    }
}
