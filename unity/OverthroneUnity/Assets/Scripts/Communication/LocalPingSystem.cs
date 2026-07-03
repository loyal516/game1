using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Overthrone
{
    public readonly struct LocalPingResponse
    {
        public LocalPingResponse(string responderName, TeamId team, LocalPingType type, string label)
        {
            ResponderName = string.IsNullOrWhiteSpace(responderName) ? "Unknown" : responderName;
            Team = team;
            Type = type;
            Label = string.IsNullOrWhiteSpace(label) ? type.ToString() : label;
        }

        public string ResponderName { get; }
        public TeamId Team { get; }
        public LocalPingType Type { get; }
        public string Label { get; }
    }

    [DisallowMultipleComponent]
    public sealed class LocalPingSystem : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerCaptureAgent localPlayer;
        [SerializeField] private CapturePoint[] capturePoints = Array.Empty<CapturePoint>();
        [SerializeField] private LocalPlayerTeam[] participants = Array.Empty<LocalPlayerTeam>();
        [SerializeField] private float pingDurationSeconds = 6f;
        [SerializeField] private float contextSearchRadius = 14f;
        [SerializeField] private float fallbackForwardDistance = 8f;
        [SerializeField] private float wheelHoldThresholdSeconds = 0.28f;

        private float activeTimeRemaining;
        private bool pingPressActive;
        private bool wheelOpen;
        private float pingHoldElapsed;
        private Vector2 wheelSelection;
        private readonly List<LocalPingResponse> responses = new List<LocalPingResponse>();

        public bool HasActivePing => activeTimeRemaining > 0f;
        public LocalPingEvent CurrentPing { get; private set; }
        public float ActiveTimeRemaining => Mathf.Max(0f, activeTimeRemaining);
        public bool IsWheelOpen => wheelOpen;
        public string CurrentWheelSelectionLabel => LabelForWheelType(CurrentWheelSelectionType);
        public LocalPingType CurrentWheelSelectionType => ResolveWheelType(wheelSelection);
        public int ResponseCount => responses.Count;
        public LocalPingResponse LatestResponse => responses.Count > 0 ? responses[responses.Count - 1] : default;

        public void Configure(
            PlayerInputReader input,
            PlayerCaptureAgent localAgent,
            CapturePoint[] objectives,
            LocalPlayerTeam[] matchParticipants)
        {
            inputReader = input;
            localPlayer = localAgent;
            capturePoints = objectives ?? Array.Empty<CapturePoint>();
            participants = matchParticipants ?? Array.Empty<LocalPlayerTeam>();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            Tick(deltaTime);

            if (inputReader != null)
            {
                TickPingInput(
                    deltaTime,
                    inputReader.PingPressed,
                    inputReader.PingHeld,
                    inputReader.PingReleased,
                    inputReader.Move
                );
            }
        }

        public void Tick(float deltaTime)
        {
            if (activeTimeRemaining <= 0f)
            {
                activeTimeRemaining = 0f;
                responses.Clear();
                return;
            }

            activeTimeRemaining = Mathf.Max(0f, activeTimeRemaining - Mathf.Max(0f, deltaTime));
            if (activeTimeRemaining <= 0f)
            {
                responses.Clear();
            }
        }

        public void TickPingInput(float deltaTime, bool pressed, bool held, bool released, Vector2 selection)
        {
            if (pressed)
            {
                pingPressActive = true;
                wheelOpen = false;
                pingHoldElapsed = 0f;
                wheelSelection = selection;
            }

            if (pingPressActive && held)
            {
                pingHoldElapsed += Mathf.Max(0f, deltaTime);
                if (selection.sqrMagnitude > 0.04f)
                {
                    wheelSelection = selection;
                }

                if (pingHoldElapsed >= Mathf.Max(0f, wheelHoldThresholdSeconds))
                {
                    wheelOpen = true;
                }
            }

            if (!released)
            {
                return;
            }

            if (!pingPressActive)
            {
                ResetWheelInput();
                return;
            }

            if (wheelOpen)
            {
                SubmitWheelPing(wheelSelection);
            }
            else
            {
                SubmitContextPing();
            }

            ResetWheelInput();
        }

        public LocalPingEvent SubmitContextPing()
        {
            var ping = ResolveContextPing();
            SubmitPing(ping);
            return ping;
        }

        public void SubmitPing(LocalPingEvent ping)
        {
            CurrentPing = ping;
            activeTimeRemaining = ping.Duration > 0f ? ping.Duration : Mathf.Max(0f, pingDurationSeconds);
            responses.Clear();
        }

        public bool SubmitResponse(string responderName, LocalPingType type, TeamId team = TeamId.None)
        {
            if (!HasActivePing)
            {
                return false;
            }

            responses.Add(new LocalPingResponse(responderName, team, type, LabelForWheelType(type)));
            return true;
        }

        public string BuildVisibleLog()
        {
            if (!HasActivePing)
            {
                return string.Empty;
            }

            var builder = new StringBuilder($"PING  {CurrentPing.Label}  {Mathf.CeilToInt(ActiveTimeRemaining)}s");
            if (responses.Count == 0)
            {
                return builder.ToString();
            }

            builder.AppendLine();
            builder.Append("RESPONSES  ");
            for (var i = 0; i < responses.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                AppendResponseLabel(builder, responses[i]);
            }

            builder.AppendLine();
            builder.Append("LATEST  ");
            AppendResponseLabel(builder, LatestResponse);
            return builder.ToString();
        }

        public string BuildWheelText()
        {
            if (!IsWheelOpen)
            {
                return string.Empty;
            }

            return $"PING WHEEL  {CurrentWheelSelectionLabel}\nW GOING   A DEFEND   D CAPTURE   S HELP";
        }

        public LocalPingEvent SubmitWheelPing(Vector2 selection)
        {
            var type = ResolveWheelType(selection);
            var localPosition = localPlayer != null ? localPlayer.transform.position : transform.position;
            var localTeam = ResolveLocalTeam();
            var forward = localPlayer != null ? localPlayer.transform.forward : transform.forward;
            var position = localPosition + forward.normalized * Mathf.Max(1f, fallbackForwardDistance);
            if (type == LocalPingType.Objective)
            {
                var point = FindNearestCapturePoint(localPosition);
                if (point != null)
                {
                    position = point.transform.position;
                }
            }

            var ping = new LocalPingEvent(
                type,
                localTeam,
                position,
                LabelForWheelType(type),
                pingDurationSeconds
            );
            SubmitPing(ping);
            return ping;
        }

        private LocalPingEvent ResolveContextPing()
        {
            var localPosition = localPlayer != null ? localPlayer.transform.position : transform.position;
            var localTeam = ResolveLocalTeam();
            var enemy = FindNearestEnemyThreat(localPosition, localTeam);
            if (enemy != null)
            {
                var state = enemy.GetComponent<PlayerStateController>();
                var labelState = state != null ? state.PersistentState.ToString() : "Enemy";
                return new LocalPingEvent(
                    LocalPingType.Enemy,
                    localTeam,
                    enemy.transform.position,
                    $"Enemy {labelState}",
                    pingDurationSeconds
                );
            }

            var point = FindNearestCapturePoint(localPosition);
            if (point != null)
            {
                return new LocalPingEvent(
                    LocalPingType.Objective,
                    localTeam,
                    point.transform.position,
                    $"Point {point.PointId}",
                    pingDurationSeconds
                );
            }

            var forward = localPlayer != null ? localPlayer.transform.forward : transform.forward;
            return new LocalPingEvent(
                LocalPingType.Attention,
                localTeam,
                localPosition + forward.normalized * Mathf.Max(1f, fallbackForwardDistance),
                "Attention",
                pingDurationSeconds
            );
        }

        private LocalPlayerTeam FindNearestEnemyThreat(Vector3 origin, TeamId localTeam)
        {
            if (localTeam == TeamId.None)
            {
                return null;
            }

            var bestDistance = Mathf.Max(0f, contextSearchRadius);
            LocalPlayerTeam best = null;
            foreach (var participant in participants)
            {
                if (participant == null || participant.Team == TeamId.None || participant.Team == localTeam)
                {
                    continue;
                }

                var state = participant.GetComponent<PlayerStateController>();
                if (state == null || !IsEnemyThreatState(state.PersistentState))
                {
                    continue;
                }

                var distance = Vector3.Distance(origin, participant.transform.position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = participant;
                }
            }

            return best;
        }

        private CapturePoint FindNearestCapturePoint(Vector3 origin)
        {
            var bestDistance = Mathf.Max(0f, contextSearchRadius);
            CapturePoint best = null;
            foreach (var point in capturePoints)
            {
                if (point == null)
                {
                    continue;
                }

                var distance = Vector3.Distance(origin, point.transform.position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = point;
                }
            }

            return best;
        }

        private TeamId ResolveLocalTeam()
        {
            return localPlayer != null && localPlayer.Team != null ? localPlayer.Team.Team : TeamId.None;
        }

        private static bool IsEnemyThreatState(MovementState state)
        {
            return state == MovementState.Attacker || state == MovementState.King;
        }

        private void ResetWheelInput()
        {
            pingPressActive = false;
            wheelOpen = false;
            pingHoldElapsed = 0f;
            wheelSelection = Vector2.zero;
        }

        private static LocalPingType ResolveWheelType(Vector2 selection)
        {
            if (selection.sqrMagnitude <= 0.04f)
            {
                return LocalPingType.Objective;
            }

            if (Mathf.Abs(selection.x) > Mathf.Abs(selection.y))
            {
                return selection.x < 0f ? LocalPingType.Defend : LocalPingType.Objective;
            }

            return selection.y < 0f ? LocalPingType.Help : LocalPingType.Attention;
        }

        private static string LabelForWheelType(LocalPingType type)
        {
            return type switch
            {
                LocalPingType.Attention => "Going",
                LocalPingType.Defend => "Defend",
                LocalPingType.Help => "Help",
                LocalPingType.Objective => "Capture",
                LocalPingType.Enemy => "Enemy",
                _ => type.ToString()
            };
        }

        private static void AppendResponseLabel(StringBuilder builder, LocalPingResponse response)
        {
            builder.Append(response.ResponderName);
            builder.Append(": ");
            builder.Append(response.Label);
        }
    }
}
