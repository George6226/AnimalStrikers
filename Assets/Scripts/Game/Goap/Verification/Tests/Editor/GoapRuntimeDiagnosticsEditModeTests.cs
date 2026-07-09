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
    public void ShouldIncludeInSummaryLog_KeepsPlanOutcome()
    {
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("PlanSuccess(goal=Attack, actions=2, path=Pass>Shoot, attempt=1)"), Is.True);
        Assert.That(GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog("PlanFailure(goal=Attack, reason=NoPlan, category=NoPlan, details=-, attempt=1)"), Is.True);
    }
}
