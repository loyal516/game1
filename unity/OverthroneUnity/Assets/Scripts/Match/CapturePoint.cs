using System.Collections.Generic;
using UnityEngine;

namespace Overthrone
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class CapturePoint : MonoBehaviour
    {
        [SerializeField] private string pointId = "A";
        [SerializeField] private float radius = 5f;

        private readonly CapturePointProgress progress = new CapturePointProgress();
        private readonly HashSet<LocalPlayerTeam> occupants = new HashSet<LocalPlayerTeam>();
        private SphereCollider triggerCollider;
        private int blueCount;
        private int redCount;

        public string PointId => pointId;
        public float Radius => radius;
        public float Progress => progress.Progress01;
        public TeamId Owner => progress.Owner;
        public TeamId ActiveCapturingTeam => progress.ActiveCapturingTeam;
        public bool IsContested => progress.IsContested;
        public int BlueCount => blueCount;
        public int RedCount => redCount;

        public void Configure(string id, float captureRadius)
        {
            pointId = string.IsNullOrWhiteSpace(id) ? pointId : id;
            radius = Mathf.Max(0.1f, captureRadius);
            ConfigureTrigger();
        }

        private void Awake()
        {
            ConfigureTrigger();
        }

        private void OnDisable()
        {
            occupants.Clear();
            blueCount = 0;
            redCount = 0;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            RefreshCounts();
            var previousProgress = progress.Progress01;
            var previousOwner = progress.Owner;
            progress.Tick(blueCount, redCount, deltaTime);
            AwardPointContribution(previousOwner, previousProgress);
        }

        private void OnTriggerEnter(Collider other)
        {
            var team = other.GetComponentInParent<LocalPlayerTeam>();
            if (team != null)
            {
                occupants.Add(team);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var team = other.GetComponentInParent<LocalPlayerTeam>();
            if (team != null)
            {
                occupants.Remove(team);
            }
        }

        private void RefreshCounts()
        {
            occupants.RemoveWhere(team => team == null || !team.isActiveAndEnabled);
            blueCount = 0;
            redCount = 0;

            foreach (var team in occupants)
            {
                if (team.Team == TeamId.Blue)
                {
                    blueCount++;
                }
                else if (team.Team == TeamId.Red)
                {
                    redCount++;
                }
            }
        }

        private void AwardPointContribution(TeamId previousOwner, float previousProgress)
        {
            if (progress.IsContested || progress.ActiveCapturingTeam == TeamId.None)
            {
                return;
            }

            if (previousOwner == progress.ActiveCapturingTeam)
            {
                return;
            }

            var progressDelta = Mathf.Abs(progress.Progress01 - previousProgress);
            if (progressDelta <= 0f)
            {
                return;
            }

            var contributorCount = progress.ActiveCapturingTeam == TeamId.Blue ? blueCount : redCount;
            if (contributorCount <= 0)
            {
                return;
            }

            var contributionPerPlayer = progressDelta / contributorCount;
            foreach (var occupant in occupants)
            {
                if (occupant != null && occupant.Team == progress.ActiveCapturingTeam)
                {
                    occupant.AddPointContribution(contributionPerPlayer);
                }
            }
        }

        private void ConfigureTrigger()
        {
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                return;
            }

            triggerCollider.isTrigger = true;
            triggerCollider.radius = Mathf.Max(0.1f, radius);
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0.1f, radius);
            ConfigureTrigger();
        }
    }
}
