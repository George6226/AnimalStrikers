#if UNITY_EDITOR
using NUnit.Framework;

/// <summary>6-H P2: GK バッチ純判定。</summary>
public sealed class GoalkeeperBatchVerifyRulesEditModeTests
{
    [Test]
    public void GetExpectedMode_EnemyThreat_IsTrackBall()
    {
        Assert.That(
            GoalkeeperBatchVerifyRules.GetExpectedMode(GoalkeeperBatchScenarioId.EnemyThreatTrackBall),
            Is.EqualTo(GoalkeeperPositioning.Mode.TrackBall));
    }

    [Test]
    public void IsBatchSucceeded_PassesOnSelectionTotalMatch()
    {
        const string log =
            "========== SELECTION_TOTAL 1/1 ==========\n" +
            "========== BATCH_COMPLETE ==========\n";

        Assert.That(GoalkeeperBatchVerifyRules.IsBatchSucceeded(log), Is.True);
    }

    [Test]
    public void IsBatchSucceeded_FailsOnAbortOrMismatch()
    {
        Assert.That(
            GoalkeeperBatchVerifyRules.IsBatchSucceeded("========== BATCH_ABORT GAME_STATE_TIMEOUT ==========\n"),
            Is.False);
        Assert.That(
            GoalkeeperBatchVerifyRules.IsBatchSucceeded(
                "========== SELECTION_TOTAL 0/1 ==========\n========== BATCH_COMPLETE ==========\n"),
            Is.False);
    }

    [Test]
    public void TryParseSelectionTotal_ReadsLastBanner()
    {
        const string log =
            "noise\n" +
            "========== SELECTION_TOTAL 1/1 ==========\n";

        Assert.That(GoalkeeperBatchVerifyRules.TryParseSelectionTotal(log, out int pass, out int eval), Is.True);
        Assert.That(pass, Is.EqualTo(1));
        Assert.That(eval, Is.EqualTo(1));
    }
}
#endif
