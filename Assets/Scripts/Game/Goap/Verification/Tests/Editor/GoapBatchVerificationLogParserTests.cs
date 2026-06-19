#if UNITY_EDITOR
using NUnit.Framework;

public sealed class GoapBatchVerificationLogParserTests
{
    [Test]
    public void Evaluate_PassesWhenSelectionTotalMatches()
    {
        const string diag =
            "========== SELECTION_TOTAL 11/11 ==========\n" +
            "========== BATCH_COMPLETE ==========\n";

        GoapBatchVerificationLogParser.Result result = GoapBatchVerificationLogParser.Evaluate(diag);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(11, result.PassCount);
        Assert.AreEqual(11, result.EvalCount);
    }

    [Test]
    public void Evaluate_FailsOnBatchAbort()
    {
        const string diag = "========== BATCH_ABORT GAME_TIMEOUT ==========\n";

        GoapBatchVerificationLogParser.Result result = GoapBatchVerificationLogParser.Evaluate(diag);

        Assert.IsFalse(result.Succeeded);
        Assert.That(result.Summary, Does.Contain("BATCH_ABORT"));
    }

    [Test]
    public void Evaluate_FailsWhenSelectionTotalMismatch()
    {
        const string diag =
            "========== SELECTION_FAIL 3/11 ==========\n" +
            "========== SELECTION_TOTAL 10/11 ==========\n" +
            "========== BATCH_COMPLETE ==========\n";

        GoapBatchVerificationLogParser.Result result = GoapBatchVerificationLogParser.Evaluate(diag);

        Assert.IsFalse(result.Succeeded);
    }

    [Test]
    public void Evaluate_PassesRuntimeTotal()
    {
        const string diag =
            "========== RUNTIME_TOTAL 2/2 ==========\n" +
            "========== BATCH_COMPLETE ==========\n";

        GoapBatchVerificationLogParser.Result result = GoapBatchVerificationLogParser.Evaluate(diag);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(2, result.PassCount);
        Assert.AreEqual(2, result.EvalCount);
    }
}
#endif
