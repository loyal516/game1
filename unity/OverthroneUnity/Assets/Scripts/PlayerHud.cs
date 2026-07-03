using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Overthrone
{
    public sealed class PlayerHud : MonoBehaviour
    {
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private Image staminaFill;
        [SerializeField] private Text statusText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Image[] objectivePanelRowBackgrounds = Array.Empty<Image>();
        [SerializeField] private Text[] objectivePanelPointIdTexts = Array.Empty<Text>();
        [SerializeField] private Image[] objectivePanelOwnerIndicators = Array.Empty<Image>();
        [SerializeField] private Image[] objectivePanelProgressFills = Array.Empty<Image>();
        [SerializeField] private Text[] objectivePanelStateTexts = Array.Empty<Text>();
        [SerializeField] private Text[] objectivePanelOccupantCountTexts = Array.Empty<Text>();
        [SerializeField] private Text spectatorText;
        [SerializeField] private Text teamRailText;
        [SerializeField] private Image captureProgressRing;
        [SerializeField] private Image heldStatusRing;
        [SerializeField] private Image rescueOpportunityRing;
        [SerializeField] private LocalMatchManager matchManager;
        [SerializeField] private CapturePoint[] capturePoints = Array.Empty<CapturePoint>();
        [SerializeField] private PlayerStateController stateController;
        [SerializeField] private PlayerCaptureAgent captureAgent;
        [SerializeField] private LocalSpectatorCamera spectatorCamera;
        [SerializeField] private LocalCaptureSystem captureSystem;
        [SerializeField] private LocalDeadChannel deadChannel;
        [SerializeField] private LocalPingSystem pingSystem;
        [SerializeField] private RectTransform minimapRoot;
        [SerializeField] private Image localMinimapMarker;
        [SerializeField] private Image[] participantMinimapMarkers = Array.Empty<Image>();
        [SerializeField] private Image[] capturePointMinimapMarkers = Array.Empty<Image>();
        [SerializeField] private Image pingMinimapMarker;
        [SerializeField] private Text pingLogText;
        [SerializeField] private Text pingWheelText;
        [SerializeField] private InputField deadChannelInputField;
        [SerializeField] private Button deadChannelSendButton;
        [SerializeField] private Text deadChannelInputStatusText;
        [SerializeField] private Text deadChannelInputHintText;
        [SerializeField] private float minimapWorldRadius = 16f;

        private const string DeadChannelInputReadyStatus = "Dead channel ready";
        private const string DeadChannelInputSentStatus = "Sent";
        private const string DeadChannelInputBlockedStatus = "Not sent";
        private const string DeadChannelInputHint = "Dead comms";

        private string deadChannelInputStatus = DeadChannelInputReadyStatus;

        public void Configure(
            PlayerMotor motor,
            Image staminaImage,
            Text stateText,
            Text objectivesText = null,
            CapturePoint[] objectives = null,
            LocalMatchManager localMatchManager = null,
            PlayerCaptureAgent playerCaptureAgent = null,
            LocalSpectatorCamera localSpectatorCamera = null,
            Text spectatorOverlayText = null,
            Text teamStatusRailText = null,
            Image captureHoldProgressRing = null,
            LocalCaptureSystem localCaptureSystem = null,
            LocalDeadChannel localDeadChannel = null,
            RectTransform minimapContainer = null,
            Image localMinimapSelfMarker = null,
            Image[] participantMinimapMarkerImages = null,
            Image[] capturePointMinimapMarkerImages = null,
            float minimapRadius = 16f,
            Image heldRing = null,
            Image rescueRing = null,
            Image[] objectivePanelRowBackgrounds = null,
            Text[] objectivePanelPointIdTexts = null,
            Image[] objectivePanelOwnerIndicators = null,
            Image[] objectivePanelProgressFills = null,
            Text[] objectivePanelStateTexts = null,
            Text[] objectivePanelOccupantCountTexts = null,
            LocalPingSystem localPingSystem = null,
            Image minimapPingMarker = null,
            Text localPingLogText = null,
            Text localPingWheelText = null,
            InputField localDeadChannelInputField = null,
            Button localDeadChannelSendButton = null,
            Text localDeadChannelInputStatusText = null,
            Text localDeadChannelInputHintText = null)
        {
            if (deadChannelSendButton != null)
            {
                deadChannelSendButton.onClick.RemoveListener(SubmitDeadChannelInput);
            }

            playerMotor = motor;
            stateController = playerMotor != null ? playerMotor.GetComponent<PlayerStateController>() : null;
            staminaFill = staminaImage;
            statusText = stateText;
            objectiveText = objectivesText;
            this.objectivePanelRowBackgrounds = objectivePanelRowBackgrounds ?? Array.Empty<Image>();
            this.objectivePanelPointIdTexts = objectivePanelPointIdTexts ?? Array.Empty<Text>();
            this.objectivePanelOwnerIndicators = objectivePanelOwnerIndicators ?? Array.Empty<Image>();
            this.objectivePanelProgressFills = objectivePanelProgressFills ?? Array.Empty<Image>();
            this.objectivePanelStateTexts = objectivePanelStateTexts ?? Array.Empty<Text>();
            this.objectivePanelOccupantCountTexts = objectivePanelOccupantCountTexts ?? Array.Empty<Text>();
            spectatorText = spectatorOverlayText;
            teamRailText = teamStatusRailText;
            captureProgressRing = captureHoldProgressRing;
            heldStatusRing = heldRing;
            rescueOpportunityRing = rescueRing;
            matchManager = localMatchManager;
            captureAgent = playerCaptureAgent;
            spectatorCamera = localSpectatorCamera;
            captureSystem = localCaptureSystem;
            deadChannel = localDeadChannel;
            pingSystem = localPingSystem;
            minimapRoot = minimapContainer;
            localMinimapMarker = localMinimapSelfMarker;
            participantMinimapMarkers = participantMinimapMarkerImages ?? Array.Empty<Image>();
            capturePointMinimapMarkers = capturePointMinimapMarkerImages ?? Array.Empty<Image>();
            pingMinimapMarker = minimapPingMarker;
            pingLogText = localPingLogText;
            pingWheelText = localPingWheelText;
            deadChannelInputField = localDeadChannelInputField;
            deadChannelSendButton = localDeadChannelSendButton;
            deadChannelInputStatusText = localDeadChannelInputStatusText;
            deadChannelInputHintText = localDeadChannelInputHintText;
            minimapWorldRadius = Mathf.Max(1f, minimapRadius);
            deadChannelInputStatus = DeadChannelInputReadyStatus;
            if (deadChannelSendButton != null)
            {
                deadChannelSendButton.onClick.RemoveListener(SubmitDeadChannelInput);
                deadChannelSendButton.onClick.AddListener(SubmitDeadChannelInput);
            }

            SetCapturePoints(objectives);
            RefreshDeadChannelInput();
        }

        public void SetCapturePoints(CapturePoint[] objectives)
        {
            capturePoints = objectives ?? Array.Empty<CapturePoint>();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (playerMotor == null)
            {
                RefreshDeadChannelInput();
                return;
            }

            if (staminaFill != null)
            {
                staminaFill.fillAmount = playerMotor.NormalizedStamina;
            }

            if (statusText != null)
            {
                statusText.text = BuildStatusText();
            }

            if (objectiveText != null)
            {
                objectiveText.text = BuildObjectiveText();
            }

            RefreshObjectivePanel();

            RefreshSpectatorOverlay();

            if (teamRailText != null)
            {
                var hasTeamRail = matchManager != null && matchManager.Participants.Length > 0;
                teamRailText.gameObject.SetActive(hasTeamRail);
                teamRailText.text = hasTeamRail ? BuildTeamRailText() : string.Empty;
            }

            if (captureProgressRing != null)
            {
                var progress = captureSystem != null ? captureSystem.CaptureHoldProgress01 : 0f;
                captureProgressRing.gameObject.SetActive(progress > 0f);
                captureProgressRing.fillAmount = progress;
            }

            RefreshHeldAndRescueRings();

            RefreshPingWheel();
            RefreshPingLog();
            RefreshMinimap();
            RefreshDeadChannelInput();
        }

        public void SubmitDeadChannelInput()
        {
            TrySubmitDeadChannelText(deadChannelInputField != null ? deadChannelInputField.text : string.Empty);
        }

        public bool TrySubmitDeadChannelText(string body)
        {
            if (!CanUseDeadChannelInput() || string.IsNullOrWhiteSpace(body))
            {
                UpdateDeadChannelInputStatus(DeadChannelInputBlockedStatus);
                return false;
            }

            if (!deadChannel.TryPost(captureAgent, body))
            {
                UpdateDeadChannelInputStatus(DeadChannelInputBlockedStatus);
                return false;
            }

            if (deadChannelInputField != null)
            {
                deadChannelInputField.text = string.Empty;
            }

            UpdateDeadChannelInputStatus(DeadChannelInputSentStatus);
            RefreshSpectatorOverlay();
            RefreshDeadChannelInput();
            return true;
        }

        private void RefreshSpectatorOverlay()
        {
            if (spectatorText == null)
            {
                return;
            }

            var isSpectating = spectatorCamera != null && spectatorCamera.IsSpectating;
            spectatorText.gameObject.SetActive(isSpectating);
            spectatorText.text = isSpectating ? BuildSpectatorText() : string.Empty;
        }

        private void RefreshDeadChannelInput()
        {
            var canUse = CanUseDeadChannelInput();
            if (!canUse)
            {
                deadChannelInputStatus = DeadChannelInputReadyStatus;
            }

            if (deadChannelInputField != null)
            {
                deadChannelInputField.gameObject.SetActive(canUse);
                deadChannelInputField.interactable = canUse;
            }

            if (deadChannelInputHintText != null)
            {
                deadChannelInputHintText.gameObject.SetActive(canUse);
                deadChannelInputHintText.text = canUse ? DeadChannelInputHint : string.Empty;
            }

            if (deadChannelSendButton != null)
            {
                deadChannelSendButton.gameObject.SetActive(canUse);
                deadChannelSendButton.interactable = canUse;
            }

            if (deadChannelInputStatusText != null)
            {
                deadChannelInputStatusText.gameObject.SetActive(canUse);
                deadChannelInputStatusText.text = canUse ? deadChannelInputStatus : string.Empty;
            }
        }

        private bool CanUseDeadChannelInput()
        {
            return captureAgent != null
                && deadChannel != null
                && captureAgent.Status == CaptureStatus.Captured
                && captureAgent.Team != null
                && captureAgent.Team.Team != TeamId.None;
        }

        private void UpdateDeadChannelInputStatus(string nextStatus)
        {
            deadChannelInputStatus = nextStatus;
            if (deadChannelInputStatusText != null)
            {
                deadChannelInputStatusText.text = CanUseDeadChannelInput() ? deadChannelInputStatus : string.Empty;
            }
        }

        private string BuildStatusText()
        {
            var movement = GetMovementLabel();
            var grounded = playerMotor.IsGrounded ? "Grounded" : "Airborne";
            var stamina = $"{Mathf.CeilToInt(playerMotor.currentStamina)}/{Mathf.CeilToInt(playerMotor.maxStamina)}";
            var capture = captureAgent != null ? $"  CAP {captureAgent.Status}" : string.Empty;
            var spectator = spectatorCamera != null && spectatorCamera.IsSpectating
                ? $"  SPEC {FormatSpectatorTarget(spectatorCamera.SpectatorTarget)}"
                : string.Empty;
            var dash = BuildDashStatus();
            var slime = BuildSlimeStatus();
            return $"{playerMotor.State}  {movement}  {grounded}  STA {stamina}{dash}{slime}{capture}{spectator}";
        }

        private string BuildDashStatus()
        {
            if (playerMotor == null)
            {
                return string.Empty;
            }

            if (playerMotor.DashCooldownRemaining > 0f)
            {
                return $"  DASH CD {Mathf.CeilToInt(playerMotor.DashCooldownRemaining)}s";
            }

            if (!playerMotor.HasStamina(playerMotor.dashStaminaCost))
            {
                return $"  DASH STA {Mathf.CeilToInt(playerMotor.dashStaminaCost)}";
            }

            return string.Empty;
        }

        private string BuildSlimeStatus()
        {
            if (stateController == null)
            {
                return string.Empty;
            }

            if (stateController.SlimeCooldownRemaining > 0f)
            {
                return $"  SLIME CD {Mathf.CeilToInt(stateController.SlimeCooldownRemaining)}s";
            }

            if (playerMotor != null && !playerMotor.HasStamina(stateController.SlimeStaminaCost))
            {
                return $"  SLIME STA {Mathf.CeilToInt(stateController.SlimeStaminaCost)}";
            }

            return string.Empty;
        }

        private string GetMovementLabel()
        {
            if (playerMotor.IsSprinting)
            {
                return "Sprint";
            }

            if (playerMotor.IsCrouching)
            {
                return "Crouch";
            }

            return playerMotor.IsMoving ? "Move" : "Idle";
        }

        private string BuildObjectiveText()
        {
            if ((capturePoints == null || capturePoints.Length == 0) && matchManager == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder("Objectives");
            AppendMatchSummary(builder);

            if (capturePoints == null || capturePoints.Length == 0)
            {
                return builder.ToString();
            }

            foreach (var point in capturePoints)
            {
                if (point == null)
                {
                    continue;
                }

                builder.AppendLine();
                builder.Append(point.PointId);
                builder.Append("  Owner ");
                builder.Append(FormatTeam(point.Owner));
                builder.Append("  ");
                builder.Append(Mathf.RoundToInt(point.Progress * 100f));
                builder.Append("%  ");
                builder.Append(GetObjectiveState(point));
            }

            return builder.ToString();
        }

        private void RefreshObjectivePanel()
        {
            for (var i = 0; i < ObjectivePanelRowCount(); i++)
            {
                var point = capturePoints != null && i < capturePoints.Length ? capturePoints[i] : null;
                var hasPoint = point != null;
                SetObjectivePanelRowActive(i, hasPoint);
                if (!hasPoint)
                {
                    continue;
                }

                SetObjectivePanelText(objectivePanelPointIdTexts, i, point.PointId);
                SetObjectivePanelText(objectivePanelStateTexts, i, FormatObjectivePanelState(point));
                SetObjectivePanelText(objectivePanelOccupantCountTexts, i, FormatObjectiveOccupants(point));
                SetObjectiveOwnerIndicator(i, point);
                SetObjectiveProgressFill(i, point);
                SetObjectiveRowBackground(i, point);
            }
        }

        private int ObjectivePanelRowCount()
        {
            var rowCount = capturePoints != null ? capturePoints.Length : 0;
            rowCount = Mathf.Max(rowCount, objectivePanelRowBackgrounds.Length);
            rowCount = Mathf.Max(rowCount, objectivePanelPointIdTexts.Length);
            rowCount = Mathf.Max(rowCount, objectivePanelOwnerIndicators.Length);
            rowCount = Mathf.Max(rowCount, objectivePanelProgressFills.Length);
            rowCount = Mathf.Max(rowCount, objectivePanelStateTexts.Length);
            return Mathf.Max(rowCount, objectivePanelOccupantCountTexts.Length);
        }

        private void SetObjectivePanelRowActive(int index, bool active)
        {
            SetImageActive(objectivePanelRowBackgrounds, index, active);
            SetTextActive(objectivePanelPointIdTexts, index, active);
            SetImageActive(objectivePanelOwnerIndicators, index, active);
            SetImageActive(objectivePanelProgressFills, index, active);
            SetTextActive(objectivePanelStateTexts, index, active);
            SetTextActive(objectivePanelOccupantCountTexts, index, active);
        }

        private void SetObjectiveOwnerIndicator(int index, CapturePoint point)
        {
            var indicator = GetPanelElement(objectivePanelOwnerIndicators, index);
            if (indicator != null)
            {
                indicator.color = ColorForTeam(point.Owner, ObjectiveNeutralColor());
            }
        }

        private void SetObjectiveProgressFill(int index, CapturePoint point)
        {
            var fill = GetPanelElement(objectivePanelProgressFills, index);
            if (fill != null)
            {
                fill.fillAmount = Mathf.Clamp01(point.Progress);
                fill.color = ObjectiveProgressColor(point);
            }
        }

        private void SetObjectiveRowBackground(int index, CapturePoint point)
        {
            var background = GetPanelElement(objectivePanelRowBackgrounds, index);
            if (background != null)
            {
                background.color = ObjectiveRowBackgroundColor(point);
            }
        }

        private static void SetObjectivePanelText(Text[] texts, int index, string value)
        {
            var text = GetPanelElement(texts, index);
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetImageActive(Image[] images, int index, bool active)
        {
            var image = GetPanelElement(images, index);
            if (image != null)
            {
                image.gameObject.SetActive(active);
            }
        }

        private static void SetTextActive(Text[] texts, int index, bool active)
        {
            var text = GetPanelElement(texts, index);
            if (text != null)
            {
                text.gameObject.SetActive(active);
            }
        }

        private static T GetPanelElement<T>(T[] elements, int index) where T : class
        {
            return elements != null && index >= 0 && index < elements.Length ? elements[index] : null;
        }

        private string BuildSpectatorText()
        {
            var target = FormatSpectatorTarget(spectatorCamera != null ? spectatorCamera.SpectatorTarget : null);
            var builder = new StringBuilder();
            builder.Append("SPECTATING  ");
            builder.AppendLine(target);
            builder.AppendLine("Q Previous   Tab Next");
            builder.Append(deadChannel != null ? deadChannel.BuildVisibleLog(captureAgent) : "DEAD CHANNEL  Local team-only");
            return builder.ToString();
        }

        private string BuildTeamRailText()
        {
            var builder = new StringBuilder("Team Status");
            foreach (var participant in matchManager.Participants)
            {
                if (participant == null)
                {
                    continue;
                }

                var agent = participant.GetComponent<PlayerCaptureAgent>();
                var state = participant.GetComponent<PlayerStateController>();
                builder.AppendLine();
                builder.Append(FormatTeam(participant.Team));
                builder.Append("  ");
                builder.Append(participant.name);
                builder.Append("  ");
                builder.Append(state != null ? state.PersistentState : MovementState.Neutral);

                if (agent != null)
                {
                    builder.Append("  ");
                    builder.Append(agent.Status);
                }
            }

            return builder.ToString();
        }

        private void AppendMatchSummary(StringBuilder builder)
        {
            if (matchManager == null)
            {
                return;
            }

            builder.AppendLine();
            builder.Append("Owned  Blue ");
            builder.Append(matchManager.BlueOwnedCount);
            builder.Append("  Red ");
            builder.Append(matchManager.RedOwnedCount);
            builder.AppendLine();
            builder.Append("Phase ");
            builder.Append(matchManager.Phase);

            if (matchManager.Winner != TeamId.None)
            {
                builder.AppendLine();
                builder.Append("Winner ");
                builder.Append(FormatTeam(matchManager.Winner));
            }
            else if (matchManager.IsVictoryCountdownActive)
            {
                builder.AppendLine();
                builder.Append("Win ");
                builder.Append(FormatTeam(matchManager.VictoryCountdownTeam));
                builder.Append(" in ");
                builder.Append(Mathf.CeilToInt(matchManager.VictoryTimeRemaining));
                builder.Append("s");
                if (matchManager.IsDefenderReentryWindowActive)
                {
                    builder.AppendLine();
                    builder.Append("Defender ");
                    builder.Append(FormatTeam(matchManager.DefenderTeam));
                    builder.Append(" re-entry ");
                    builder.Append(Mathf.CeilToInt(matchManager.DefenderReentryTimeRemaining));
                    builder.Append("s");
                }
            }
        }

        private void RefreshMinimap()
        {
            if (minimapRoot == null)
            {
                return;
            }

            minimapRoot.gameObject.SetActive(true);
            RefreshCapturePointMarkers();
            RefreshParticipantMarkers();
            RefreshLocalMinimapMarker();
            RefreshPingMarker();
        }

        private void RefreshHeldAndRescueRings()
        {
            if (heldStatusRing != null)
            {
                var isHeld = captureAgent != null && captureAgent.Status == CaptureStatus.Held;
                heldStatusRing.gameObject.SetActive(isHeld);
                heldStatusRing.fillAmount = isHeld ? 1f : 0f;
            }

            if (rescueOpportunityRing != null)
            {
                var canRescue = CanRescueHeldAlly();
                rescueOpportunityRing.gameObject.SetActive(canRescue);
                rescueOpportunityRing.fillAmount = canRescue ? 1f : 0f;
            }
        }

        private bool CanRescueHeldAlly()
        {
            if (captureAgent == null || captureAgent.Team == null || matchManager == null)
            {
                return false;
            }

            foreach (var participant in matchManager.Participants)
            {
                if (participant == null)
                {
                    continue;
                }

                var target = participant.GetComponent<PlayerCaptureAgent>();
                if (target == null || target == captureAgent || target.Team == null)
                {
                    continue;
                }

                if (!CaptureInteractionRules.CanRescue(
                    captureAgent.Team.Team,
                    target.Team.Team,
                    captureAgent.Status,
                    target.Status))
                {
                    continue;
                }

                if (CaptureInteractionRules.IsInRange(
                    captureAgent.transform.position,
                    target.transform.position,
                    CaptureInteractionRules.RescueRange))
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshCapturePointMarkers()
        {
            for (var i = 0; i < capturePointMinimapMarkers.Length; i++)
            {
                var marker = capturePointMinimapMarkers[i];
                var point = capturePoints != null && i < capturePoints.Length ? capturePoints[i] : null;
                if (marker == null)
                {
                    continue;
                }

                marker.gameObject.SetActive(point != null);
                if (point == null)
                {
                    continue;
                }

                marker.rectTransform.anchoredPosition = WorldToMinimap(point.transform.position);
                marker.color = ColorForTeam(point.Owner, new Color(0.22f, 0.92f, 1f, 0.9f));
            }
        }

        private void RefreshParticipantMarkers()
        {
            var participants = matchManager != null ? matchManager.Participants : Array.Empty<LocalPlayerTeam>();
            var localTeam = captureAgent != null && captureAgent.Team != null ? captureAgent.Team.Team : TeamId.None;
            for (var i = 0; i < participantMinimapMarkers.Length; i++)
            {
                var marker = participantMinimapMarkers[i];
                var participant = participants != null && i < participants.Length ? participants[i] : null;
                if (marker == null)
                {
                    continue;
                }

                var participantAgent = participant != null ? participant.GetComponent<PlayerCaptureAgent>() : null;
                var visible = participant != null
                    && participantAgent != captureAgent
                    && ShouldShowParticipantOnMinimap(participant, localTeam);
                marker.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                marker.rectTransform.anchoredPosition = WorldToMinimap(participant.transform.position);
                marker.color = ColorForParticipant(participant, localTeam);
            }
        }

        private void RefreshLocalMinimapMarker()
        {
            if (localMinimapMarker == null)
            {
                return;
            }

            var hasLocal = captureAgent != null;
            localMinimapMarker.gameObject.SetActive(hasLocal);
            if (!hasLocal)
            {
                return;
            }

            localMinimapMarker.rectTransform.anchoredPosition = WorldToMinimap(captureAgent.transform.position);
            localMinimapMarker.color = Color.white;
        }

        private void RefreshPingMarker()
        {
            if (pingMinimapMarker == null)
            {
                return;
            }

            var hasPing = pingSystem != null && pingSystem.HasActivePing;
            pingMinimapMarker.gameObject.SetActive(hasPing);
            if (!hasPing)
            {
                return;
            }

            pingMinimapMarker.rectTransform.anchoredPosition = WorldToMinimap(pingSystem.CurrentPing.Position);
            pingMinimapMarker.color = ColorForPing(pingSystem.CurrentPing.Type);
        }

        private void RefreshPingLog()
        {
            if (pingLogText == null)
            {
                return;
            }

            var hasPing = pingSystem != null && pingSystem.HasActivePing;
            pingLogText.gameObject.SetActive(hasPing);
            pingLogText.text = hasPing ? pingSystem.BuildVisibleLog() : string.Empty;
        }

        private void RefreshPingWheel()
        {
            if (pingWheelText == null)
            {
                return;
            }

            var hasWheel = pingSystem != null && pingSystem.IsWheelOpen;
            pingWheelText.gameObject.SetActive(hasWheel);
            pingWheelText.text = hasWheel ? pingSystem.BuildWheelText() : string.Empty;
        }

        private Vector2 WorldToMinimap(Vector3 worldPosition)
        {
            var halfSize = MinimapHalfSize();
            var normalized = new Vector2(worldPosition.x, worldPosition.z) / minimapWorldRadius;
            normalized = Vector2.ClampMagnitude(normalized, 1f);
            return new Vector2(normalized.x * halfSize.x, normalized.y * halfSize.y);
        }

        private Vector2 MinimapHalfSize()
        {
            var rect = minimapRoot.rect;
            var width = rect.width > 0f ? rect.width : Mathf.Abs(minimapRoot.sizeDelta.x);
            var height = rect.height > 0f ? rect.height : Mathf.Abs(minimapRoot.sizeDelta.y);
            return new Vector2(width * 0.5f, height * 0.5f);
        }

        private static string GetObjectiveState(CapturePoint point)
        {
            if (point.IsContested)
            {
                return "Contested";
            }

            if (point.ActiveCapturingTeam != TeamId.None)
            {
                return $"Capturing {FormatTeam(point.ActiveCapturingTeam)}";
            }

            return point.Owner == TeamId.None ? "Idle" : "Held";
        }

        private static string FormatObjectivePanelState(CapturePoint point)
        {
            return GetObjectiveState(point);
        }

        private static string FormatObjectiveOccupants(CapturePoint point)
        {
            return $"Blue {point.BlueCount}  Red {point.RedCount}";
        }

        private static bool ShouldShowParticipantOnMinimap(LocalPlayerTeam participant, TeamId localTeam)
        {
            if (participant == null || participant.Team == TeamId.None)
            {
                return false;
            }

            if (localTeam != TeamId.None && participant.Team == localTeam)
            {
                return true;
            }

            var stateController = participant.GetComponent<PlayerStateController>();
            return stateController != null
                && (stateController.PersistentState == MovementState.King
                    || stateController.PersistentState == MovementState.Attacker);
        }

        private static Color ColorForParticipant(LocalPlayerTeam participant, TeamId localTeam)
        {
            if (participant != null && participant.Team == localTeam)
            {
                return new Color(0.25f, 0.64f, 1f, 0.95f);
            }

            return new Color(1f, 0.18f, 0.24f, 0.95f);
        }

        private static Color ColorForTeam(TeamId team, Color fallback)
        {
            return team switch
            {
                TeamId.Blue => new Color(0.25f, 0.64f, 1f, 0.95f),
                TeamId.Red => new Color(1f, 0.18f, 0.24f, 0.95f),
                _ => fallback
            };
        }

        private static Color ObjectiveProgressColor(CapturePoint point)
        {
            if (point.IsContested)
            {
                return new Color(1f, 0.84f, 0.18f, 0.95f);
            }

            var team = point.ActiveCapturingTeam != TeamId.None ? point.ActiveCapturingTeam : point.Owner;
            return ColorForTeam(team, ObjectiveNeutralColor());
        }

        private static Color ObjectiveRowBackgroundColor(CapturePoint point)
        {
            var team = point.Owner != TeamId.None ? point.Owner : point.ActiveCapturingTeam;
            var color = ColorForTeam(team, new Color(0.08f, 0.1f, 0.12f, 0.76f));
            color.a = team == TeamId.None ? 0.76f : 0.22f;
            return color;
        }

        private static Color ObjectiveNeutralColor()
        {
            return new Color(0.82f, 0.88f, 0.92f, 0.86f);
        }

        private static Color ColorForPing(LocalPingType type)
        {
            return type switch
            {
                LocalPingType.Enemy => new Color(1f, 0.18f, 0.24f, 0.98f),
                LocalPingType.Objective => new Color(1f, 0.84f, 0.18f, 0.98f),
                LocalPingType.Defend => new Color(0.25f, 0.64f, 1f, 0.98f),
                LocalPingType.Help => new Color(0.18f, 0.95f, 0.46f, 0.98f),
                _ => new Color(0.22f, 0.92f, 1f, 0.98f)
            };
        }

        private static string FormatTeam(TeamId team)
        {
            return team == TeamId.None ? "None" : team.ToString();
        }

        private static string FormatSpectatorTarget(PlayerCaptureAgent target)
        {
            return target != null ? target.name : "FreeCam";
        }
    }
}
