#if UNITY_EDITOR
using NUnit.Framework;

public sealed class MainNpcPostPassPlanningEditModeTests
{
    [Test]
    public void VerifyMainNpcPostPassSupportStarted_DetectsGoalChangeAfterPass()
    {
        const string summary =
            "bootstrap complete\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] ActionStart(action=PassToTeammate, goal=BallPossessionAttack)\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] GoalChanged(goal=TeamBallSupport)\n";

        Assert.That(MainNpcPostPassPlanning.VerifyMainNpcPostPassSupportStarted(summary), Is.True);
    }

    [Test]
    public void VerifyMainNpcPostPassSupportStarted_DetectsSupportActionAfterPass()
    {
        const string summary =
            "bootstrap complete\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] ActionStart(action=PassToTeammate, goal=BallPossessionAttack)\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] ActionStart(action=MoveToSupportPosition, goal=TeamBallSupport)\n";

        Assert.That(MainNpcPostPassPlanning.VerifyMainNpcPostPassSupportStarted(summary), Is.True);
    }

    [Test]
    public void VerifyMainNpcPostPassSupportStarted_IgnoresSubNpcSupportOnly()
    {
        const string summary =
            "bootstrap complete\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] ActionStart(action=PassToTeammate, goal=BallPossessionAttack)\n" +
            "[GOAP_SUMMARY] [Goap#2|owner=Gorilla(Clone),playerId=1002] GoalChanged(goal=TeamBallSupport)\n";

        Assert.That(MainNpcPostPassPlanning.VerifyMainNpcPostPassSupportStarted(summary), Is.False);
    }

    [Test]
    public void VerifyMainNpcPostPassSupportStarted_RequiresPassFirst()
    {
        const string summary =
            "bootstrap complete\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] GoalChanged(goal=TeamBallSupport)\n";

        Assert.That(MainNpcPostPassPlanning.VerifyMainNpcPostPassSupportStarted(summary), Is.False);
    }
}
#endif
