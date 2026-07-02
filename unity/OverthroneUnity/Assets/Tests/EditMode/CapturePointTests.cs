using NUnit.Framework;
using Overthrone;

public sealed class CapturePointTests
{
    [TestCase(1, CapturePointProgress.OnePlayerCaptureRate)]
    [TestCase(2, CapturePointProgress.TwoPlayerCaptureRate)]
    [TestCase(3, CapturePointProgress.ThreePlusPlayerCaptureRate)]
    [TestCase(5, CapturePointProgress.ThreePlusPlayerCaptureRate)]
    public void CaptureProgressUsesGddPlayerCountRates(int playerCount, float expectedProgress)
    {
        var progress = new CapturePointProgress();

        progress.Tick(playerCount, 0, 1f);

        Assert.AreEqual(TeamId.None, progress.Owner);
        Assert.AreEqual(TeamId.Blue, progress.ActiveCapturingTeam);
        Assert.AreEqual(expectedProgress, progress.Progress01, 0.0001f);
        Assert.IsFalse(progress.IsContested);
    }

    [Test]
    public void ContestedPointDoesNotChangeProgress()
    {
        var progress = new CapturePointProgress();
        progress.Tick(2, 0, 3f);
        var before = progress.Progress01;

        progress.Tick(1, 1, 10f);

        Assert.AreEqual(before, progress.Progress01, 0.0001f);
        Assert.IsTrue(progress.IsContested);
    }

    [Test]
    public void EmptyUnownedPointDecaysWithoutGoingBelowZero()
    {
        var progress = new CapturePointProgress();
        progress.Tick(1, 0, 10f);

        progress.Tick(0, 0, 5f);
        Assert.AreEqual(0.35f, progress.Progress01, 0.0001f);

        progress.Tick(0, 0, 100f);
        Assert.AreEqual(0f, progress.Progress01, 0.0001f);
    }

    [Test]
    public void CaptureCompletionSetsOwnerAndClampsProgress()
    {
        var progress = new CapturePointProgress();

        progress.Tick(3, 0, 10f);

        Assert.AreEqual(TeamId.Blue, progress.Owner);
        Assert.AreEqual(TeamId.Blue, progress.ActiveCapturingTeam);
        Assert.AreEqual(1f, progress.Progress01, 0.0001f);
        Assert.IsFalse(progress.IsContested);
    }
}
