using NUnit.Framework;
using Overthrone;

public sealed class LocalRosterBuilderTests
{
    [Test]
    public void DefaultRosterCreatesThreeVsThreeWithOneLocalPlayer()
    {
        var slots = LocalRosterBuilder.CreateDefaultThreeVsThree();

        Assert.AreEqual(LocalRosterBuilder.TotalPlayers, slots.Length);
        Assert.AreEqual(LocalRosterBuilder.TeamSize, LocalRosterBuilder.CountTeam(slots, TeamId.Blue));
        Assert.AreEqual(LocalRosterBuilder.TeamSize, LocalRosterBuilder.CountTeam(slots, TeamId.Red));

        var localCount = 0;
        foreach (var slot in slots)
        {
            if (slot.IsLocalPlayer)
            {
                localCount++;
                Assert.AreEqual(TeamId.Blue, slot.Team);
            }
        }

        Assert.AreEqual(1, localCount);
    }

    [Test]
    public void DefaultRosterProvidesUniqueSpawnPositions()
    {
        var slots = LocalRosterBuilder.CreateDefaultThreeVsThree();

        for (var left = 0; left < slots.Length; left++)
        {
            for (var right = left + 1; right < slots.Length; right++)
            {
                Assert.AreNotEqual(slots[left].SpawnPosition, slots[right].SpawnPosition);
            }
        }
    }
}
