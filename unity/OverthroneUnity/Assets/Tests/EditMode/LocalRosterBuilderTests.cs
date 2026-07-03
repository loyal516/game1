using NUnit.Framework;
using Overthrone;
using UnityEngine;

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

    [Test]
    public void DefaultRosterSpawnsOutsideInitialCaptureRadii()
    {
        var slots = LocalRosterBuilder.CreateDefaultThreeVsThree();
        var capturePointPositions = new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(-8f, 0f, 7f),
            new Vector3(8f, 0f, 7f)
        };

        foreach (var slot in slots)
        {
            foreach (var pointPosition in capturePointPositions)
            {
                var slotPosition = new Vector2(slot.SpawnPosition.x, slot.SpawnPosition.z);
                var point = new Vector2(pointPosition.x, pointPosition.z);
                Assert.Greater(
                    Vector2.Distance(slotPosition, point),
                    5f,
                    $"{slot.DisplayName} must not spawn inside a capture point radius."
                );
            }
        }
    }
}
