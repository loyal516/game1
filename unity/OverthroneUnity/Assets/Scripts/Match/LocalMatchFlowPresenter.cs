using UnityEngine;
using UnityEngine.UI;

namespace Overthrone
{
    public sealed class LocalMatchFlowPresenter : MonoBehaviour
    {
        [SerializeField] private LocalMatchManager matchManager;
        [SerializeField] private Text bannerText;
        [SerializeField] private Image transitionOverlay;
        [SerializeField] private LocalPlayerTeam[] participants = System.Array.Empty<LocalPlayerTeam>();
        [SerializeField] private Transform blueDefenderSpawn;
        [SerializeField] private Transform redDefenderSpawn;
        [SerializeField] private float defenderBuffSeconds = 5f;

        private bool subscribed;

        public int LastReenteredCount { get; private set; }
        public string LastBannerText { get; private set; } = string.Empty;

        public void Configure(
            LocalMatchManager manager,
            Text flowBannerText,
            Image flowOverlay,
            LocalPlayerTeam[] matchParticipants,
            Transform blueSpawn,
            Transform redSpawn,
            float reentryBuffSeconds = 5f)
        {
            Unsubscribe();
            matchManager = manager;
            bannerText = flowBannerText;
            transitionOverlay = flowOverlay;
            participants = matchParticipants ?? System.Array.Empty<LocalPlayerTeam>();
            blueDefenderSpawn = blueSpawn;
            redDefenderSpawn = redSpawn;
            defenderBuffSeconds = Mathf.Max(0f, reentryBuffSeconds);
            SubscribeIfActive();
        }

        public void PlayFlowEvent(LocalMatchFlowEvent flowEvent)
        {
            LastReenteredCount = 0;
            LastBannerText = BuildBannerText(flowEvent);

            if (bannerText != null)
            {
                bannerText.gameObject.SetActive(!string.IsNullOrWhiteSpace(LastBannerText));
                bannerText.text = LastBannerText;
            }

            if (transitionOverlay != null)
            {
                transitionOverlay.gameObject.SetActive(true);
                transitionOverlay.color = OverlayColorFor(flowEvent.Type);
            }

            if (flowEvent.Type == LocalMatchFlowEventType.VictoryCountdownStarted)
            {
                LastReenteredCount = ApplyDefenderReentry(flowEvent.DefenderTeam);
            }
        }

        private void OnEnable()
        {
            SubscribeIfActive();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void SubscribeIfActive()
        {
            if (subscribed || !isActiveAndEnabled || matchManager == null)
            {
                return;
            }

            matchManager.FlowChanged += PlayFlowEvent;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || matchManager == null)
            {
                subscribed = false;
                return;
            }

            matchManager.FlowChanged -= PlayFlowEvent;
            subscribed = false;
        }

        private int ApplyDefenderReentry(TeamId defenderTeam)
        {
            var spawn = SpawnFor(defenderTeam);
            var count = 0;
            foreach (var participant in participants)
            {
                if (!CanReenter(participant, defenderTeam))
                {
                    continue;
                }

                if (spawn != null)
                {
                    Teleport(participant.transform, spawn);
                }

                var motor = participant.GetComponent<PlayerMotor>();
                if (motor != null)
                {
                    motor.currentStamina = motor.maxStamina;
                }

                var stateController = participant.GetComponent<PlayerStateController>();
                if (stateController != null)
                {
                    stateController.RequestTimedState(MovementState.Attacker, defenderBuffSeconds);
                }

                count++;
            }

            return count;
        }

        private Transform SpawnFor(TeamId team)
        {
            return team == TeamId.Blue ? blueDefenderSpawn : team == TeamId.Red ? redDefenderSpawn : null;
        }

        private static bool CanReenter(LocalPlayerTeam participant, TeamId defenderTeam)
        {
            if (participant == null || !participant.isActiveAndEnabled || participant.Team != defenderTeam || defenderTeam == TeamId.None)
            {
                return false;
            }

            var agent = participant.GetComponent<PlayerCaptureAgent>();
            return agent == null || agent.Status == CaptureStatus.Free;
        }

        private static void Teleport(Transform target, Transform spawn)
        {
            var controller = target.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            target.SetPositionAndRotation(spawn.position, spawn.rotation);

            if (controller != null)
            {
                controller.enabled = true;
            }
        }

        private static string BuildBannerText(LocalMatchFlowEvent flowEvent)
        {
            return flowEvent.Type switch
            {
                LocalMatchFlowEventType.VictoryCountdownStarted =>
                    $"OVERTURN\n{FormatTeam(flowEvent.Team)} controls all points\n{FormatTeam(flowEvent.DefenderTeam)} re-entry {Mathf.CeilToInt(flowEvent.RemainingSeconds)}s",
                LocalMatchFlowEventType.VictoryCountdownInterrupted =>
                    $"DEFENDER BREAK\n{FormatTeam(flowEvent.Team)} countdown stopped",
                LocalMatchFlowEventType.RoundEnded =>
                    $"ROUND END\n{FormatTeam(flowEvent.Team)} wins",
                _ => string.Empty
            };
        }

        private static Color OverlayColorFor(LocalMatchFlowEventType type)
        {
            return type switch
            {
                LocalMatchFlowEventType.VictoryCountdownStarted => new Color(1f, 0.78f, 0.15f, 0.22f),
                LocalMatchFlowEventType.VictoryCountdownInterrupted => new Color(0.18f, 0.6f, 1f, 0.2f),
                LocalMatchFlowEventType.RoundEnded => new Color(1f, 1f, 1f, 0.24f),
                _ => new Color(0f, 0f, 0f, 0f)
            };
        }

        private static string FormatTeam(TeamId team)
        {
            return team == TeamId.None ? "None" : team.ToString();
        }
    }
}
