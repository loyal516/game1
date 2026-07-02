using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Overthrone
{
    public sealed class LocalDataStore
    {
        public const string ProfilesFileName = "profiles.csv";
        public const string MatchesFileName = "matches.csv";
        public const string MatchPlayersFileName = "match_players.csv";
        public const string TelemetryEventsFileName = "telemetry_events.csv";

        private readonly List<LocalPlayerProfileRecord> profiles = new List<LocalPlayerProfileRecord>();
        private readonly List<LocalMatchRecord> matches = new List<LocalMatchRecord>();
        private readonly List<LocalMatchPlayerRecord> matchPlayers = new List<LocalMatchPlayerRecord>();
        private readonly List<LocalTelemetryEventRecord> telemetryEvents = new List<LocalTelemetryEventRecord>();

        public IReadOnlyList<LocalPlayerProfileRecord> Profiles => profiles;
        public IReadOnlyList<LocalMatchRecord> Matches => matches;
        public IReadOnlyList<LocalMatchPlayerRecord> MatchPlayers => matchPlayers;
        public IReadOnlyList<LocalTelemetryEventRecord> TelemetryEvents => telemetryEvents;

        public void UpsertProfile(LocalPlayerProfileRecord profile)
        {
            profiles.RemoveAll(existing => string.Equals(existing.Id, profile.Id, StringComparison.Ordinal));
            profiles.Add(profile);
        }

        public void AddMatch(LocalMatchRecord match)
        {
            matches.Add(match);
        }

        public void AddMatchPlayer(LocalMatchPlayerRecord matchPlayer)
        {
            matchPlayers.Add(matchPlayer);
        }

        public void AddTelemetryEvent(LocalTelemetryEventRecord telemetryEvent)
        {
            telemetryEvents.Add(telemetryEvent);
        }

        public string ToCsv(LocalDataTable table)
        {
            return table switch
            {
                LocalDataTable.Profiles => BuildProfilesCsv(),
                LocalDataTable.Matches => BuildMatchesCsv(),
                LocalDataTable.MatchPlayers => BuildMatchPlayersCsv(),
                LocalDataTable.TelemetryEvents => BuildTelemetryEventsCsv(),
                _ => string.Empty
            };
        }

        public void SaveCsvDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("A local data output directory is required.", nameof(directoryPath));
            }

            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(Path.Combine(directoryPath, ProfilesFileName), ToCsv(LocalDataTable.Profiles), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directoryPath, MatchesFileName), ToCsv(LocalDataTable.Matches), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directoryPath, MatchPlayersFileName), ToCsv(LocalDataTable.MatchPlayers), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directoryPath, TelemetryEventsFileName), ToCsv(LocalDataTable.TelemetryEvents), Encoding.UTF8);
        }

        public static string FileNameFor(LocalDataTable table)
        {
            return table switch
            {
                LocalDataTable.Profiles => ProfilesFileName,
                LocalDataTable.Matches => MatchesFileName,
                LocalDataTable.MatchPlayers => MatchPlayersFileName,
                LocalDataTable.TelemetryEvents => TelemetryEventsFileName,
                _ => string.Empty
            };
        }

        private string BuildProfilesCsv()
        {
            var builder = CreateBuilder("id,display_name,mmr,rank_tier,created_at,last_seen_at");
            foreach (var profile in profiles)
            {
                AppendRow(
                    builder,
                    profile.Id,
                    profile.DisplayName,
                    profile.Mmr.ToString(CultureInfo.InvariantCulture),
                    profile.RankTier,
                    profile.CreatedAt,
                    profile.LastSeenAt
                );
            }

            return builder.ToString();
        }

        private string BuildMatchesCsv()
        {
            var builder = CreateBuilder("id,mode,map,started_at,ended_at,winning_team,duration_sec");
            foreach (var match in matches)
            {
                AppendRow(
                    builder,
                    match.Id,
                    match.Mode,
                    match.Map,
                    match.StartedAt,
                    match.EndedAt,
                    TeamValue(match.WinningTeam),
                    match.DurationSec.ToString(CultureInfo.InvariantCulture)
                );
            }

            return builder.ToString();
        }

        private string BuildMatchPlayersCsv()
        {
            var builder = CreateBuilder("match_id,profile_id,team,captures,rescues,captured_count,point_contribution,mmr_change,was_mvp");
            foreach (var player in matchPlayers)
            {
                AppendRow(
                    builder,
                    player.MatchId,
                    player.ProfileId,
                    TeamValue(player.Team),
                    player.Captures.ToString(CultureInfo.InvariantCulture),
                    player.Rescues.ToString(CultureInfo.InvariantCulture),
                    player.CapturedCount.ToString(CultureInfo.InvariantCulture),
                    player.PointContribution.ToString("0.###", CultureInfo.InvariantCulture),
                    player.MmrChange.ToString(CultureInfo.InvariantCulture),
                    player.WasMvp ? "true" : "false"
                );
            }

            return builder.ToString();
        }

        private string BuildTelemetryEventsCsv()
        {
            var builder = CreateBuilder("event_id,match_id,profile_id,event_name,occurred_at,payload_json");
            foreach (var telemetryEvent in telemetryEvents)
            {
                AppendRow(
                    builder,
                    telemetryEvent.EventId,
                    telemetryEvent.MatchId,
                    telemetryEvent.ProfileId,
                    telemetryEvent.EventName,
                    telemetryEvent.OccurredAt,
                    telemetryEvent.PayloadJson
                );
            }

            return builder.ToString();
        }

        private static StringBuilder CreateBuilder(string header)
        {
            return new StringBuilder(header).AppendLine();
        }

        private static void AppendRow(StringBuilder builder, params string[] values)
        {
            for (var index = 0; index < values.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append(Escape(values[index]));
            }

            builder.AppendLine();
        }

        private static string Escape(string value)
        {
            value ??= string.Empty;
            var mustQuote = value.Contains(",")
                || value.Contains("\"")
                || value.Contains("\n")
                || value.Contains("\r");
            if (!mustQuote)
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static string TeamValue(TeamId team)
        {
            return ((int)team).ToString(CultureInfo.InvariantCulture);
        }
    }
}
