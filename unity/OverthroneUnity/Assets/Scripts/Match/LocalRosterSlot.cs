using UnityEngine;

namespace Overthrone
{
    public readonly struct LocalRosterSlot
    {
        public LocalRosterSlot(string displayName, TeamId team, int teamIndex, Vector3 spawnPosition, bool isLocalPlayer)
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName;
            Team = team;
            TeamIndex = Mathf.Max(1, teamIndex);
            SpawnPosition = spawnPosition;
            IsLocalPlayer = isLocalPlayer;
        }

        public string DisplayName { get; }
        public TeamId Team { get; }
        public int TeamIndex { get; }
        public Vector3 SpawnPosition { get; }
        public bool IsLocalPlayer { get; }
    }

    public static class LocalRosterBuilder
    {
        public const int TeamSize = 3;
        public const int TotalPlayers = TeamSize * 2;

        public static LocalRosterSlot[] CreateDefaultThreeVsThree()
        {
            return new[]
            {
                new LocalRosterSlot("Blue Local Player", TeamId.Blue, 1, new Vector3(-2.8f, 1f, -8f), true),
                new LocalRosterSlot("Blue Ally 2", TeamId.Blue, 2, new Vector3(-5.1f, 1f, -6.4f), false),
                new LocalRosterSlot("Blue Ally 3", TeamId.Blue, 3, new Vector3(-0.6f, 1f, -6.4f), false),
                new LocalRosterSlot("Red Rival 1", TeamId.Red, 1, new Vector3(2.8f, 1f, 13.5f), false),
                new LocalRosterSlot("Red Rival 2", TeamId.Red, 2, new Vector3(5.1f, 1f, 15.2f), false),
                new LocalRosterSlot("Red Rival 3", TeamId.Red, 3, new Vector3(0.6f, 1f, 15.2f), false)
            };
        }

        public static int CountTeam(LocalRosterSlot[] slots, TeamId team)
        {
            if (slots == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var slot in slots)
            {
                if (slot.Team == team)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
