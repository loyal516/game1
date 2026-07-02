using UnityEngine;

namespace Overthrone
{
    public sealed class LocalPlayerTeam : MonoBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Blue;
        [SerializeField] private int finalCaptureCount;
        [SerializeField] private float pointContribution;
        [SerializeField] private int kingTieBreaker;

        public TeamId Team => team;
        public int FinalCaptureCount => finalCaptureCount;
        public float PointContribution => pointContribution;
        public int KingTieBreaker => kingTieBreaker;

        public void Configure(TeamId nextTeam)
        {
            team = nextTeam;
        }

        public void ConfigureKingPriority(int captures, float contribution, int tieBreaker)
        {
            finalCaptureCount = Mathf.Max(0, captures);
            pointContribution = Mathf.Max(0f, contribution);
            kingTieBreaker = tieBreaker;
        }

        public void RegisterFinalCapture()
        {
            finalCaptureCount++;
        }

        public void AddPointContribution(float contribution)
        {
            pointContribution = Mathf.Max(0f, pointContribution + Mathf.Max(0f, contribution));
        }

        private void Awake()
        {
            if (kingTieBreaker == 0)
            {
                kingTieBreaker = Random.Range(1, int.MaxValue);
            }
        }
    }
}
