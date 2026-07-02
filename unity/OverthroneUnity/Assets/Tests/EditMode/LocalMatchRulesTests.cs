using NUnit.Framework;
using Overthrone;

public sealed class LocalMatchRulesTests
{
    [TestCase(0, false, false, MovementState.Neutral)]
    [TestCase(1, true, false, MovementState.Attacker)]
    [TestCase(1, false, true, MovementState.Attacker)]
    [TestCase(2, false, false, MovementState.King)]
    [TestCase(3, true, true, MovementState.King)]
    public void ResolvePlayerStateFollowsGddPriority(int ownedPointCount, bool rallyActive, bool graceActive, MovementState expected)
    {
        Assert.AreEqual(expected, LocalMatchRules.ResolvePlayerState(ownedPointCount, rallyActive, graceActive));
    }

    [Test]
    public void ResolvePlayerStateKeepsNonCandidateOutOfKingState()
    {
        Assert.AreEqual(
            MovementState.Attacker,
            LocalMatchRules.ResolvePlayerState(2, true, false, false)
        );
        Assert.AreEqual(
            MovementState.Neutral,
            LocalMatchRules.ResolvePlayerState(2, false, false, false)
        );
    }

    [TestCase(2, false)]
    [TestCase(3, true)]
    [TestCase(5, true)]
    public void AttackerRallyRequiresThreePlayersOnPoint(int sameTeamPlayerCount, bool expected)
    {
        Assert.AreEqual(expected, LocalMatchRules.HasAttackerRally(sameTeamPlayerCount));
    }

    [Test]
    public void VictoryCountdownRequiresAllThreePointsAndResetsWhenControlBreaks()
    {
        var nextTeam = LocalMatchRules.ResolveCountdownTeam(3, 0);
        Assert.AreEqual(TeamId.Blue, nextTeam);

        var remaining = LocalMatchRules.TickCountdownRemaining(TeamId.Blue, nextTeam, 30f, 5f);
        Assert.AreEqual(25f, remaining);

        nextTeam = LocalMatchRules.ResolveCountdownTeam(2, 1);
        remaining = LocalMatchRules.TickCountdownRemaining(TeamId.Blue, nextTeam, remaining, 5f);

        Assert.AreEqual(TeamId.None, nextTeam);
        Assert.AreEqual(LocalMatchRules.VictoryCountdownSeconds, remaining);
    }

    [Test]
    public void CountdownWinnerRequiresActiveTeamAtZero()
    {
        Assert.IsTrue(LocalMatchRules.HasCountdownWon(TeamId.Blue, 0f));
        Assert.IsFalse(LocalMatchRules.HasCountdownWon(TeamId.None, 0f));
        Assert.IsFalse(LocalMatchRules.HasCountdownWon(TeamId.Blue, 0.1f));
    }
}
