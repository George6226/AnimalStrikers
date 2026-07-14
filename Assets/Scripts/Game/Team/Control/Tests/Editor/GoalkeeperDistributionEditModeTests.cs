#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class GoalkeeperDistributionEditModeTests
{
    [Test]
    public void ComputeReceivePosition_IsAheadOfGoalkeeperTowardEnemyGoal()
    {
        var teamBB = CreateTeamBlackboard();
        try
        {
            Vector3 gkPos = new Vector3(0f, 0f, -16f);
            Vector3 receive = GoalkeeperDistribution.ComputeReceivePosition(
                selfPosition: new Vector3(2f, 0f, -10f),
                slotIndex: 1,
                teamBB,
                goalkeeperPosition: gkPos,
                otherTeammates: null);

            Assert.That(receive.z, Is.GreaterThan(gkPos.z));
            Assert.That(Vector3.Distance(receive, gkPos), Is.GreaterThan(8f));
        }
        finally
        {
            Object.DestroyImmediate(teamBB.gameObject);
        }
    }

    [Test]
    public void ComputeAdvancePosition_MovesTowardEnemyGoal()
    {
        var teamBB = CreateTeamBlackboard();
        try
        {
            Vector3 selfPos = new Vector3(-3f, 0f, -6f);
            Vector3 advance = GoalkeeperDistribution.ComputeAdvancePosition(
                selfPos,
                slotIndex: 2,
                teamBB,
                otherTeammates: null);

            Assert.That(advance.z, Is.GreaterThan(selfPos.z));
        }
        finally
        {
            Object.DestroyImmediate(teamBB.gameObject);
        }
    }

    [Test]
    public void ComputeTeammateResult_WhenGkHoldsBall_ReturnsSupportMode()
    {
        var teamBB = CreateTeamBlackboard();
        try
        {
            var result = GoalkeeperDistribution.ComputeTeammateResult(
                new Vector3(4f, 0f, -8f),
                formationSlotIndex: 1,
                teamBB,
                goalkeeper: null,
                otherTeammatePositions: null,
                selfFacade: null);

            Assert.That(result.IsValid, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(teamBB.gameObject);
        }
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
