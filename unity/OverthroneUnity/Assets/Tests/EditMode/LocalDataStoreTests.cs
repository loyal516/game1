using System;
using System.IO;
using NUnit.Framework;
using Overthrone;

public sealed class LocalDataStoreTests
{
    [Test]
    public void LocalDataStoreExportsSupabaseNamedCsvTables()
    {
        var store = CreateFilledStore();

        StringAssert.StartsWith(
            "id,display_name,mmr,rank_tier,created_at,last_seen_at",
            store.ToCsv(LocalDataTable.Profiles)
        );
        StringAssert.StartsWith(
            "id,mode,map,started_at,ended_at,winning_team,duration_sec",
            store.ToCsv(LocalDataTable.Matches)
        );
        StringAssert.StartsWith(
            "match_id,profile_id,team,captures,rescues,captured_count,point_contribution,mmr_change,was_mvp",
            store.ToCsv(LocalDataTable.MatchPlayers)
        );
        StringAssert.StartsWith(
            "event_id,match_id,profile_id,event_name,occurred_at,payload_json",
            store.ToCsv(LocalDataTable.TelemetryEvents)
        );

        StringAssert.Contains("player-001,Suchan,1250,Bronze", store.ToCsv(LocalDataTable.Profiles));
        StringAssert.Contains("match-001,standard,Prototype Garden", store.ToCsv(LocalDataTable.Matches));
        StringAssert.Contains("match-001,player-001,1,2,1,0,0.875,16,true", store.ToCsv(LocalDataTable.MatchPlayers));
        StringAssert.Contains("event-001,match-001,player-001,match_end", store.ToCsv(LocalDataTable.TelemetryEvents));
    }

    [Test]
    public void LocalDataStoreEscapesCsvPayloads()
    {
        var store = new LocalDataStore();
        store.UpsertProfile(new LocalPlayerProfileRecord(
            "player-quoted",
            "Su, \"Chan\"",
            1400,
            "Silver",
            "2026-07-03T00:00:00Z",
            "2026-07-03T00:01:00Z"
        ));
        store.AddTelemetryEvent(new LocalTelemetryEventRecord(
            "event-quoted",
            "match-quoted",
            "player-quoted",
            "state_change",
            "2026-07-03T00:00:30Z",
            "{\"winner\":1,\"note\":\"comma,ok\"}"
        ));

        var profilesCsv = store.ToCsv(LocalDataTable.Profiles);
        var telemetryCsv = store.ToCsv(LocalDataTable.TelemetryEvents);

        StringAssert.Contains("\"Su, \"\"Chan\"\"\"", profilesCsv);
        StringAssert.Contains("\"{\"\"winner\"\":1,\"\"note\"\":\"\"comma,ok\"\"}\"", telemetryCsv);
    }

    [Test]
    public void LocalDataStoreWritesAllLocalCsvFiles()
    {
        var store = CreateFilledStore();
        var directory = Path.Combine(Path.GetTempPath(), $"OverthroneLocalData_{Guid.NewGuid():N}");

        try
        {
            store.SaveCsvDirectory(directory);

            Assert.IsTrue(File.Exists(Path.Combine(directory, LocalDataStore.ProfilesFileName)));
            Assert.IsTrue(File.Exists(Path.Combine(directory, LocalDataStore.MatchesFileName)));
            Assert.IsTrue(File.Exists(Path.Combine(directory, LocalDataStore.MatchPlayersFileName)));
            Assert.IsTrue(File.Exists(Path.Combine(directory, LocalDataStore.TelemetryEventsFileName)));
            StringAssert.Contains(
                "event-001",
                File.ReadAllText(Path.Combine(directory, LocalDataStore.TelemetryEventsFileName))
            );
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static LocalDataStore CreateFilledStore()
    {
        var store = new LocalDataStore();
        store.UpsertProfile(new LocalPlayerProfileRecord(
            "player-001",
            "Suchan",
            1250,
            "Bronze",
            "2026-07-03T00:00:00Z",
            "2026-07-03T00:08:00Z"
        ));
        store.AddMatch(new LocalMatchRecord(
            "match-001",
            "standard",
            "Prototype Garden",
            "2026-07-03T00:00:00Z",
            "2026-07-03T00:08:00Z",
            TeamId.Blue,
            480
        ));
        store.AddMatchPlayer(new LocalMatchPlayerRecord(
            "match-001",
            "player-001",
            TeamId.Blue,
            2,
            1,
            0,
            0.875f,
            16,
            true
        ));
        store.AddTelemetryEvent(new LocalTelemetryEventRecord(
            "event-001",
            "match-001",
            "player-001",
            "match_end",
            "2026-07-03T00:08:00Z",
            "{\"duration\":480,\"winner\":1,\"switches\":4}"
        ));
        return store;
    }
}
