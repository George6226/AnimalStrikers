#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class GoalkeeperPositioningEditModeTests
{
  [Test]
  public void Compute_NoThreat_HoldsGoalCenter()
  {
    var teamBB = CreateTeamBlackboard();
    try
    {
      var result = GoalkeeperPositioning.Compute(
        teamBB,
        mirrored: false,
        ballPosition: new Vector3(4f, 0f, 12f),
        BallManager_State.BALL_STATE.HOLD,
        enemyHasBall: false,
        teamHasBall: true);

      Assert.That(result.IsValid, Is.True);
      Assert.That(result.Mode, Is.EqualTo(GoalkeeperPositioning.Mode.HoldLine));
      Assert.That(result.TargetPosition.x, Is.EqualTo(0f).Within(0.01f));
      Assert.That(result.TargetPosition.z, Is.EqualTo(-16.5f).Within(0.01f));
    }
    finally
    {
      Object.DestroyImmediate(teamBB.gameObject);
    }
  }

  [Test]
  public void Compute_EnemyThreatInDefensiveZone_TracksBallX()
  {
    var teamBB = CreateTeamBlackboard();
    try
    {
      var result = GoalkeeperPositioning.Compute(
        teamBB,
        mirrored: false,
        ballPosition: new Vector3(3f, 0f, -8f),
        BallManager_State.BALL_STATE.HOLD,
        enemyHasBall: true,
        teamHasBall: false);

      Assert.That(result.Mode, Is.EqualTo(GoalkeeperPositioning.Mode.TrackBall));
      Assert.That(result.TargetPosition.x, Is.EqualTo(3f).Within(0.01f));
      Assert.That(result.TargetPosition.z, Is.EqualTo(-16.5f).Within(0.01f));
      Assert.That(result.IsUnderThreat, Is.True);
    }
    finally
    {
      Object.DestroyImmediate(teamBB.gameObject);
    }
  }

  [Test]
  public void Compute_ShootThreat_TracksBall()
  {
    var teamBB = CreateTeamBlackboard();
    try
    {
      var result = GoalkeeperPositioning.Compute(
        teamBB,
        mirrored: false,
        ballPosition: new Vector3(-2f, 0f, -5f),
        BallManager_State.BALL_STATE.SHOOT,
        enemyHasBall: false,
        teamHasBall: false);

      Assert.That(result.Mode, Is.EqualTo(GoalkeeperPositioning.Mode.TrackBall));
      Assert.That(result.TargetPosition.x, Is.EqualTo(-2f).Within(0.01f));
    }
    finally
    {
      Object.DestroyImmediate(teamBB.gameObject);
    }
  }

  [Test]
  public void IsInDefensiveZone_BallBetweenGoalAndCenter_ReturnsTrue()
  {
    Assert.That(
      GoalkeeperPositioning.IsInDefensiveZone(
        new Vector3(0f, 0f, -5f),
        new Vector3(0f, 0f, -20f),
        Vector3.zero),
      Is.True);
    Assert.That(
      GoalkeeperPositioning.IsInDefensiveZone(
        new Vector3(0f, 0f, 10f),
        new Vector3(0f, 0f, -20f),
        Vector3.zero),
      Is.False);
  }

  private static TeamBlackboard CreateTeamBlackboard()
  {
    var teamGo = new GameObject("teamBB");
    var teamBB = teamGo.AddComponent<TeamBlackboard>();
    teamBB.FieldInfo.Initialize(ConstData.FIELD_SIZE_Z, ConstData.FIELD_SIZE_X);
    teamBB.BallInfo.setExistBall();
    return teamBB;
  }
}
#endif
