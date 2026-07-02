using System;
using UnityEngine;

namespace Overthrone
{
    public sealed class LocalSpectatorCamera : MonoBehaviour
    {
        [SerializeField] private PlayerCaptureAgent localPlayer;
        [SerializeField] private PlayerCaptureAgent[] candidates = Array.Empty<PlayerCaptureAgent>();
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 3.2f, -5.6f);
        [SerializeField] private float lookHeight = 1.35f;
        [SerializeField] private float followSharpness = 18f;

        private Transform defaultParent;
        private Vector3 defaultLocalPosition;
        private Quaternion defaultLocalRotation;
        private bool defaultTransformCaptured;

        public bool IsSpectating { get; private set; }
        public PlayerCaptureAgent SpectatorTarget { get; private set; }
        public Vector3 FollowOffset => followOffset;

        public void Configure(
            PlayerCaptureAgent player,
            PlayerCaptureAgent[] spectatorCandidates,
            PlayerInputReader spectatorInputReader = null)
        {
            CaptureDefaultTransform();
            localPlayer = player;
            candidates = spectatorCandidates ?? Array.Empty<PlayerCaptureAgent>();
            inputReader = spectatorInputReader;
            RefreshSpectatorState(0f);
        }

        public void SetInputReader(PlayerInputReader spectatorInputReader)
        {
            inputReader = spectatorInputReader;
        }

        public void SetCandidates(PlayerCaptureAgent[] spectatorCandidates)
        {
            candidates = spectatorCandidates ?? Array.Empty<PlayerCaptureAgent>();
            if (IsSpectating && !IsValidSpectatorTarget(SpectatorTarget))
            {
                SpectatorTarget = FindPreferredTarget();
            }
        }

        public PlayerCaptureAgent CycleTarget(int direction)
        {
            if (!IsSpectating || localPlayer == null || localPlayer.Status != CaptureStatus.Captured)
            {
                return null;
            }

            var available = GetLiveAllyTargets();
            if (available.Length == 0)
            {
                SpectatorTarget = localPlayer;
                return SpectatorTarget;
            }

            var currentIndex = Array.IndexOf(available, SpectatorTarget);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }
            else
            {
                var step = direction < 0 ? -1 : 1;
                currentIndex = (currentIndex + step + available.Length) % available.Length;
            }

            SpectatorTarget = available[currentIndex];
            ApplySpectatorTransform(0f);
            return SpectatorTarget;
        }

        public PlayerCaptureAgent HandleSpectatorInput(bool previousPressed, bool nextPressed)
        {
            if (!IsSpectating || localPlayer == null || localPlayer.Status != CaptureStatus.Captured)
            {
                return null;
            }

            if (previousPressed)
            {
                return CycleTarget(-1);
            }

            if (nextPressed)
            {
                return CycleTarget(1);
            }

            return SpectatorTarget;
        }

        public bool RefreshSpectatorState(float deltaTime)
        {
            if (localPlayer == null || localPlayer.Status != CaptureStatus.Captured)
            {
                RestoreDefaultTransform();
                return false;
            }

            if (!IsSpectating)
            {
                CaptureDefaultTransform();
                transform.SetParent(null, true);
                IsSpectating = true;
            }

            if (!IsValidSpectatorTarget(SpectatorTarget))
            {
                SpectatorTarget = FindPreferredTarget();
            }

            SpectatorTarget ??= localPlayer;
            ApplySpectatorTransform(deltaTime);
            return true;
        }

        private void Awake()
        {
            CaptureDefaultTransform();
        }

        private void LateUpdate()
        {
            if (RefreshSpectatorState(Time.deltaTime))
            {
                HandleSpectatorInput(
                    inputReader != null && inputReader.SpectatePreviousPressed,
                    inputReader != null && inputReader.SpectateNextPressed
                );
            }
        }

        private void CaptureDefaultTransform()
        {
            if (defaultTransformCaptured)
            {
                return;
            }

            defaultParent = transform.parent;
            defaultLocalPosition = transform.localPosition;
            defaultLocalRotation = transform.localRotation;
            defaultTransformCaptured = true;
        }

        private void RestoreDefaultTransform()
        {
            if (!IsSpectating)
            {
                return;
            }

            if (defaultTransformCaptured)
            {
                transform.SetParent(defaultParent, false);
                transform.localPosition = defaultLocalPosition;
                transform.localRotation = defaultLocalRotation;
            }

            IsSpectating = false;
            SpectatorTarget = null;
        }

        private void ApplySpectatorTransform(float deltaTime)
        {
            if (SpectatorTarget == null)
            {
                return;
            }

            var targetTransform = SpectatorTarget.transform;
            var desiredPosition = targetTransform.TransformPoint(followOffset);
            if (deltaTime <= 0f)
            {
                transform.position = desiredPosition;
            }
            else
            {
                var t = 1f - Mathf.Exp(-Mathf.Max(0f, followSharpness) * deltaTime);
                transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
            }

            transform.LookAt(targetTransform.position + Vector3.up * lookHeight);
        }

        private PlayerCaptureAgent FindPreferredTarget()
        {
            var available = GetLiveAllyTargets();
            return available.Length > 0 ? available[0] : null;
        }

        private PlayerCaptureAgent[] GetLiveAllyTargets()
        {
            if (localPlayer == null || localPlayer.Team == null || localPlayer.Team.Team == TeamId.None)
            {
                return Array.Empty<PlayerCaptureAgent>();
            }

            var liveAllies = new System.Collections.Generic.List<PlayerCaptureAgent>();
            foreach (var candidate in candidates)
            {
                if (IsValidSpectatorTarget(candidate))
                {
                    liveAllies.Add(candidate);
                }
            }

            return liveAllies.ToArray();
        }

        private bool IsValidSpectatorTarget(PlayerCaptureAgent candidate)
        {
            return candidate != null
                && candidate != localPlayer
                && candidate.isActiveAndEnabled
                && candidate.Team != null
                && localPlayer != null
                && localPlayer.Team != null
                && candidate.Team.Team == localPlayer.Team.Team
                && candidate.Team.Team != TeamId.None
                && candidate.Status != CaptureStatus.Captured;
        }
    }
}
