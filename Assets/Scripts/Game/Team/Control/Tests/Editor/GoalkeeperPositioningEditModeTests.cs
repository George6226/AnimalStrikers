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
  public void Compute_LooseBallInGoalArea_RushesTowardBallOnZ()
  {
    var teamBB = CreateTeamBlackboard();
    try
    {
      var result = GoalkeeperPositioning.Compute(
        teamBB,
        mirrored: false,
        ballPosition: new Vector3(2f, 0f, -18f),
        BallManager_State.BALL_STATE.FREE,
        enemyHasBall: false,
        teamHasBall: false,
        rushLooseBallDistance: 10f,
        goalAreaDepth: 6f,
        rushForwardDepth: 2.5f);

      Assert.That(result.Mode, Is.EqualTo(GoalkeeperPositioning.Mode.RushLooseBall));
      Assert.That(result.TargetPosition.x, Is.EqualTo(2f).Within(0.01f));
      Assert.That(result.TargetPosition.z, Is.EqualTo(-18f).Within(0.01f));
    }
    finally
    {
      Object.DestroyImmediate(teamBB.gameObject);
    }
  }

  [Test]
  public void Compute_ShootFarFromGoal_TracksLaterallyOnly()
  {
    var teamBB = CreateTeamBlackboard();
    try
    {
      var result = GoalkeeperPositioning.Compute(
        teamBB,
        mirrored: false,
        ballPosition: new Vector3(4f, 0.25f, -8f),
        BallManager_State.BALL_STATE.SHOOT,
        enemyHasBall: false,
        teamHasBall: false);

      Assert.That(result.Mode, Is.EqualTo(GoalkeeperPositioning.Mode.TrackBall));
      Assert.That(result.TargetPosition.z, Is.EqualTo(-16.5f).Within(0.01f));
    }
    finally
    {
      Object.DestroyImmediate(teamBB.gameObject);
    }
  }

  [Test]
  public void Compute_ShootNearGoalLine_RushesWithinDepth()
  {
    var teamBB = CreateTeamBlackboard();
    try
    {
      var result = GoalkeeperPositioning.Compute(
        teamBB,
        mirrored: false,
        ballPosition: new Vector3(2f, 0.4f, -17f),
        BallManager_State.BALL_STATE.SHOOT,
        enemyHasBall: false,
        teamHasBall: false);

      Assert.That(result.Mode, Is.EqualTo(GoalkeeperPositioning.Mode.RushLooseBall));
      Assert.That(result.TargetPosition.z, Is.GreaterThan(-17.5f));
    }
    finally
    {
      Object.DestroyImmediate(teamBB.gameObject);
    }
  }

  [Test]
  public void IsBallInGoalArea_BallInsidePenaltyBox_ReturnsTrue()
  {
    Assert.That(
      GoalkeeperPositioning.IsBallInGoalArea(
        new Vector3(2f, 0f, -17f),
        new Vector3(0f, 0f, -20f),
        Vector3.zero,
        3.5f,
        6f),
      Is.True);
    Assert.That(
      GoalkeeperPositioning.IsBallInGoalArea(
        new Vector3(0f, 0f, -10f),
        new Vector3(0f, 0f, -20f),
        Vector3.zero,
        3.5f,
        6f),
      Is.False);
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

  [Test]
  public void ResolveHomeLineDepth_MatchesKickoffSpawnDepths()
  {
    Assert.That(
      GoalkeeperPositioning.ResolveHomeLineDepth(mirrored: false),
      Is.EqualTo(ConstData.GK_SPAWN_DEPTH_ALLY).Within(0.001f));
    Assert.That(
      GoalkeeperPositioning.ResolveHomeLineDepth(mirrored: true),
      Is.EqualTo(ConstData.GK_SPAWN_DEPTH_ENEMY).Within(0.001f));
  }

  [Test]
  public void Compute_MirroredEnemyGk_HoldsKickoffHomeZ()
  {
    var teamBB = CreateTeamBlackboard();
    try
    {
      float enemyLineDepth = GoalkeeperPositioning.ResolveHomeLineDepth(mirrored: true);
      var result = GoalkeeperPositioning.Compute(
        teamBB,
        mirrored: true,
        ballPosition: new Vector3(0f, 0f, -5f),
        BallManager_State.BALL_STATE.HOLD,
        enemyHasBall: false,
        teamHasBall: true,
        lineDepth: enemyLineDepth);

      // Enemy goal +20, depth 2.0 → home Z = +18（キックオフと同じ）
      Assert.That(result.Mode, Is.EqualTo(GoalkeeperPositioning.Mode.HoldLine));
      Assert.That(result.TargetPosition.z, Is.EqualTo(18f).Within(0.01f));
    }
    finally
    {
      Object.DestroyImmediate(teamBB.gameObject);
    }
  }

  [Test]
  public void NeedsHomeDepthCorrection_DetectsForwardCreep()
  {
    Assert.That(
      GoalkeeperPositioning.NeedsHomeDepthCorrection(currentZ: 16.5f, targetZ: 18f, threshold: 0.25f),
      Is.True,
      "敵 GK がホームより前に出たら復帰が必要");
    Assert.That(
      GoalkeeperPositioning.NeedsHomeDepthCorrection(currentZ: 18.05f, targetZ: 18f, threshold: 0.25f),
      Is.False,
      "閾値以内ならラテラル維持");
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
