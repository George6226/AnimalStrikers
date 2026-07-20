#if UNITY_EDITOR
using NUnit.Framework;

/// <summary>終盤停止 P0': PASS/FREE 中立遷移では ContextSwitcher Abort を抑止。</summary>
public sealed class PossessionContextSwitchRulesEditModeTests
{
    [Test]
    public void ShouldAbortOnPossessionChange_FalseWhenTeamHoldToPassNeutral()
    {
        Assert.That(
            PossessionContextSwitchRules.ShouldAbortOnPossessionChange(
                lastTeamHasBall: true,
                lastEnemyHasBall: false,
                nowTeamHasBall: false,
                nowEnemyHasBall: false),
            Is.False);
    }

    [Test]
    public void ShouldAbortOnPossessionChange_FalseWhenEnemyHoldToPassNeutral()
    {
        Assert.That(
            PossessionContextSwitchRules.ShouldAbortOnPossessionChange(
                lastTeamHasBall: false,
                lastEnemyHasBall: true,
                nowTeamHasBall: false,
                nowEnemyHasBall: false),
            Is.False);
    }

    [Test]
    public void ShouldAbortOnPossessionChange_FalseWhenNeutralToTeamHold()
    {
        Assert.That(
            PossessionContextSwitchRules.ShouldAbortOnPossessionChange(
                lastTeamHasBall: false,
                lastEnemyHasBall: false,
                nowTeamHasBall: true,
                nowEnemyHasBall: false),
            Is.False);
    }

    [Test]
    public void ShouldAbortOnPossessionChange_FalseWhenNeutralToEnemyHold()
    {
        Assert.That(
            PossessionContextSwitchRules.ShouldAbortOnPossessionChange(
                lastTeamHasBall: false,
                lastEnemyHasBall: false,
                nowTeamHasBall: false,
                nowEnemyHasBall: true),
            Is.False);
    }

    [Test]
    public void ShouldAbortOnPossessionChange_TrueWhenTeamHoldFlipsToEnemyHold()
    {
        Assert.That(
            PossessionContextSwitchRules.ShouldAbortOnPossessionChange(
                lastTeamHasBall: true,
                lastEnemyHasBall: false,
                nowTeamHasBall: false,
                nowEnemyHasBall: true),
            Is.True);
    }

    [Test]
    public void ShouldAbortOnPossessionChange_TrueWhenEnemyHoldFlipsToTeamHold()
    {
        Assert.That(
            PossessionContextSwitchRules.ShouldAbortOnPossessionChange(
                lastTeamHasBall: false,
                lastEnemyHasBall: true,
                nowTeamHasBall: true,
                nowEnemyHasBall: false),
            Is.True);
    }
}
#endif
