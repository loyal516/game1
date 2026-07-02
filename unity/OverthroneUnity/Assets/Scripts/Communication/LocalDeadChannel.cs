using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Overthrone
{
    public readonly struct LocalDeadChannelMessage
    {
        public LocalDeadChannelMessage(int sequence, TeamId team, string authorName, string body, float timeSeconds, bool isSystem)
        {
            Sequence = sequence;
            Team = team;
            AuthorName = authorName;
            Body = body;
            TimeSeconds = timeSeconds;
            IsSystem = isSystem;
        }

        public int Sequence { get; }
        public TeamId Team { get; }
        public string AuthorName { get; }
        public string Body { get; }
        public float TimeSeconds { get; }
        public bool IsSystem { get; }
    }

    public sealed class LocalDeadChannel : MonoBehaviour
    {
        private const int MaxBodyLength = 96;

        [SerializeField] private int maxStoredMessages = 48;

        private readonly List<LocalDeadChannelMessage> messages = new();
        private int nextSequence = 1;

        public int MessageCount => messages.Count;

        public bool TryPost(PlayerCaptureAgent author, string body)
        {
            if (author == null || author.Status != CaptureStatus.Captured || author.Team == null)
            {
                return false;
            }

            return TryAppend(author.Team.Team, author.name, body, Time.time, false);
        }

        public bool PostSystemMessage(TeamId team, string body)
        {
            return TryAppend(team, "System", body, Time.time, true);
        }

        public int CountVisibleMessages(PlayerCaptureAgent viewer)
        {
            if (!CanRead(viewer))
            {
                return 0;
            }

            var team = viewer.Team.Team;
            var count = 0;
            foreach (var message in messages)
            {
                if (message.Team == team)
                {
                    count++;
                }
            }

            return count;
        }

        public string BuildVisibleLog(PlayerCaptureAgent viewer, int maxLines = 4)
        {
            if (!CanRead(viewer))
            {
                return string.Empty;
            }

            maxLines = Mathf.Max(1, maxLines);
            var viewerTeam = viewer.Team.Team;
            var visible = new List<LocalDeadChannelMessage>(maxLines);
            for (var i = messages.Count - 1; i >= 0 && visible.Count < maxLines; i--)
            {
                var message = messages[i];
                if (message.Team == viewerTeam)
                {
                    visible.Add(message);
                }
            }

            var builder = new StringBuilder();
            builder.Append("DEAD CHANNEL  ");
            builder.Append(viewerTeam);
            builder.AppendLine(" team-only");

            if (visible.Count == 0)
            {
                builder.Append("No dead comms yet");
                return builder.ToString();
            }

            for (var i = visible.Count - 1; i >= 0; i--)
            {
                var message = visible[i];
                builder.Append(message.IsSystem ? "*" : "-");
                builder.Append(" ");
                builder.Append(message.AuthorName);
                builder.Append(": ");
                builder.Append(message.Body);
                if (i > 0)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private bool TryAppend(TeamId team, string authorName, string body, float timeSeconds, bool isSystem)
        {
            if (team == TeamId.None || string.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            var normalizedBody = NormalizeBody(body);
            var normalizedAuthor = string.IsNullOrWhiteSpace(authorName) ? "Unknown" : authorName.Trim();
            messages.Add(new LocalDeadChannelMessage(
                nextSequence++,
                team,
                normalizedAuthor,
                normalizedBody,
                Mathf.Max(0f, timeSeconds),
                isSystem
            ));

            TrimOldMessages();
            return true;
        }

        private void TrimOldMessages()
        {
            maxStoredMessages = Mathf.Max(1, maxStoredMessages);
            while (messages.Count > maxStoredMessages)
            {
                messages.RemoveAt(0);
            }
        }

        private static bool CanRead(PlayerCaptureAgent viewer)
        {
            return viewer != null
                && viewer.Status == CaptureStatus.Captured
                && viewer.Team != null
                && viewer.Team.Team != TeamId.None;
        }

        private static string NormalizeBody(string body)
        {
            var normalized = body.Trim().Replace("\r", " ").Replace("\n", " ");
            return normalized.Length <= MaxBodyLength ? normalized : normalized.Substring(0, MaxBodyLength);
        }
    }
}
