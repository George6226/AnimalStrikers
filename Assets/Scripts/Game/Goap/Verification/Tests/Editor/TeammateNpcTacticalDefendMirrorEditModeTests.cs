#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class TeammateNpcTacticalDefendMirrorEditModeTests
{
    [Test]
    public void CalculateDefend_NonMirrored_TargetsTowardOwnGoal()
    {
        var teamBB = CreateTeamBlackboard(out Vector3 ownerPos);
        try
        {
            var result = TeammateNpcTacticalPositionCalculator.CalculateDefend(
                selfPosition: ownerPos + Vector3.right * 2f,
                formationSlotIndex: 0,
                teamBB,
                otherTeammatePositions: new List<Vector3>(),
                mirrored: false);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Mode, Is.EqualTo(TeammateNpcTacticalMode.Defend));
            // OwnGoal z=-20 → 守備マークは所有者より負方向寄り
            Assert.That(result.TargetPosition.z, Is.LessThan(ownerPos.z));
        }
        finally
        {
            Object.DestroyImmediate(teamBB.gameObject);
        }
    }

    [Test]
    public void CalculateDefend_Mirrored_TargetsTowardEnemyGoalAsHome()
    {
        var teamBB = CreateTeamBlackboard(out Vector3 ownerPos);
        try
        {
            var result = TeammateNpcTacticalPositionCalculator.CalculateDefend(
                selfPosition: ownerPos + Vector3.left * 2f,
                formationSlotIndex: 0,
                teamBB,
                otherTeammatePositions: new List<Vector3>(),
                mirrored: true);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Mode, Is.EqualTo(TeammateNpcTacticalMode.Defend));
            // 敵視点の自ゴールは z=+20 → 守備マークは所有者より正方向寄り
            Assert.That(result.TargetPosition.z, Is.GreaterThan(ownerPos.z));
        }
        finally
        {
            Object.DestroyImmediate(teamBB.gameObject);
        }
    }

    [Test]
    public void CalculateDefend_MirroredAndAlly_OpposingHomeSides()
    {
        var teamBB = CreateTeamBlackboard(out Vector3 ownerPos);
        try
        {
            var ally = TeammateNpcTacticalPositionCalculator.CalculateDefend(
                ownerPos,
                0,
                teamBB,
                new List<Vector3>(),
                mirrored: false);
            var enemy = TeammateNpcTacticalPositionCalculator.CalculateDefend(
                ownerPos,
                0,
                teamBB,
                new List<Vector3>(),
                mirrored: true);

            Assert.That(ally.TargetPosition.z, Is.LessThan(enemy.TargetPosition.z));
            Assert.That(
                Mathf.Sign(ally.TargetPosition.z - ownerPos.z),
                Is.EqualTo(-Mathf.Sign(enemy.TargetPosition.z - ownerPos.z)));
        }
        finally
        {
            Object.DestroyImmediate(teamBB.gameObject);
        }
    }

    private static TeamBlackboard CreateTeamBlackboard(out Vector3 ownerPos)
    {
        var teamGo = new GameObject("teamBB_defend_mirror");
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        teamBB.FieldInfo.Initialize(ConstData.FIELD_SIZE_Z, ConstData.FIELD_SIZE_X);
        teamBB.BallInfo.setExistBall();
        ownerPos = new Vector3(0f, 0f, 0f);
        teamBB.BallInfo.updateBallID(1, BallManager_State.BELONG_TEAM.PLAYER, ownerPos);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);
        teamBB.BallInfo.updateBallOwnerPosition(ownerPos);
        return teamBB;
    }
}
#endif
