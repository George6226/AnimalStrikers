using NUnit.Framework;

public class GoapRuntimeDiagnosticsEditModeTests
{
    [Test]
    public void ShouldIncludeInSummaryLog_KeepsActionLifecycle()
    {
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("ActionStart(action=Pass, goal=Attack)"), Is.True);
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("ActionComplete(action=Pass, goal=Attack)"), Is.True);
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("GoalChanged(goal=DefensivePositioning)"), Is.True);
    }

    [Test]
    public void ShouldIncludeInSummaryLog_SkipsHighVolumeNoise()
    {
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("PlanCosts(goal=Attack, tier=Main, slot=0, ...)"), Is.False);
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("PlanningStart(reason=interval, attempt=3)"), Is.False);
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("NoGoalIdle(wait=0.6s, reason=interval, attempt=3)"), Is.False);
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("ReplanCooldown(seconds=1.20, streak=2, category=NoPlan)"), Is.False);
    }

    [Test]
    public void ShouldIncludeInSummaryLog_KeepsPassReceiveLifecycle()
    {
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("PassReceiveComplete(received=true, reason=received)"), Is.True);
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("PassIssued(passer_released_ball)"), Is.True);
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("ReceivePassOutcome(finishReason=timeout, received=false, hasBallFact=false)"), Is.True);
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("ReceivePassTransition(received=true, finishReason=received, goal=BallPossessionAttack, transition=attack)"), Is.True);
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("MatchPlayStarted(state=GAME)"), Is.True);
    }

    [Test]
    public void ShouldIncludeInSummaryLog_KeepsPlanOutcome()
    {
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("PlanSuccess(goal=Attack, actions=2, path=Pass>Shoot, attempt=1)"), Is.True);
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("PlanFailure(goal=Attack, reason=NoPlan, category=NoPlan, details=-, attempt=1)"), Is.True);
    }
}
