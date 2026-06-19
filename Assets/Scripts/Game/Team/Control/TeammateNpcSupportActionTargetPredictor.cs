using System.Collections.Generic;
using Game.Goap;
using UnityEngine;

/// <summary>
/// 味方ボール時サポート各 GOAP アクションの移動先予測（重なりコスト用。Runtime と同系の簡略版）。
/// </summary>
public static class TeammateNpcSupportActionTargetPredictor
{
    public static bool TryPredictSupportTarget(GoapActionSO action, PlayerBlackboard bb, out Vector3 target)
    {
        target = default;
        if (action == null || bb == null)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || !TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB))
        {
            return false;
        }

        Vector3 selfPos = ResolveSelfPosition(bb);
        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        var others = TeammateNpcGoapRoleDifferentiation.CollectOtherTeammateFieldPositions(bb);

        switch (action)
        {
            case MoveToSupportPositionActionSO:
                target = PredictMoveToSupport(selfPos, slot, teamBB, others, bb);
                return true;
            case GetOpenActionSO getOpen:
                target = PredictGetOpen(selfPos, slot, teamBB, bb, getOpen);
                return true;
            case CreateSupportAngleActionSO supportAngle:
                target = PredictCreateSupportAngle(selfPos, slot, teamBB, bb, supportAngle);
                return true;
            case MakeRunBehindActionSO runBehind:
                target = PredictMakeRunBehind(selfPos, slot, teamBB, bb, runBehind);
                return true;
            default:
                return false;
        }
    }

    private static Vector3 PredictMoveToSupport(
        Vector3 selfPos,
        int slot,
        TeamBlackboard teamBB,
        List<Vector3> others,
        PlayerBlackboard bb)
    {
        if (TeammateNpcSupportPlanning.ShouldUseWidthLayoutSupportPosition(bb))
        {
            return CreateSupportAnglePositioning.SelectBestPosition(
                selfPos,
                slot,
                teamBB,
                CreateSupportAnglePositioning.CreateDefaultSettings());
        }

        var facade = bb.BasicData?.Self != null
            ? bb.BasicData.Self.GetComponentInParent<AnimalFacade>()
                ?? bb.BasicData.Self.GetComponent<AnimalFacade>()
            : null;
        var result = TeammateNpcTacticalPositionCalculator.Calculate(selfPos, slot, teamBB, others, facade);
        if (result.IsValid && result.Mode == TeammateNpcTacticalMode.Support)
        {
            return result.TargetPosition;
        }

        return TeammateNpcGoapRoleDifferentiation.PredictTacticalTarget(bb, TeammateNpcTacticalMode.Support);
    }

    private static Vector3 PredictGetOpen(
        Vector3 selfPos,
        int slot,
        TeamBlackboard teamBB,
        PlayerBlackboard bb,
        GetOpenActionSO so)
    {
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float fieldWidth = teamBB.FieldInfo.FieldWidth;
        Vector3 ownerPos = ResolveBallOwnerPosition(teamBB);
        Vector3 toGoal = (teamBB.FieldInfo.EnemyGoalPosition - ownerPos).normalized;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        float cellSizeX = fieldWidth / 3f;
        float cellSizeZ = fieldLength / 6f;

        Vector3 bestPosition = selfPos;
        float bestScore = float.MinValue;

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -3; z <= 2; z++)
            {
                Vector3 cellPosition = ownerPos + right * (x * cellSizeX) + toGoal * (z * cellSizeZ * 0.45f);
                float score = EvaluateGetOpenCell(cellPosition, selfPos, ownerPos, teamBB, so.OptimalDistanceRatio);
                if (slot == 1)
                {
                    score += Vector3.Dot(cellPosition - ownerPos, right) * 0.02f;
                }
                else if (slot == 2)
                {
                    score -= Vector3.Dot(cellPosition - ownerPos, right) * 0.02f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = cellPosition;
                }
            }
        }

        return bestPosition;
    }

    private static Vector3 PredictCreateSupportAngle(
        Vector3 selfPos,
        int slot,
        TeamBlackboard teamBB,
        PlayerBlackboard bb,
        CreateSupportAngleActionSO so)
    {
        return CreateSupportAnglePositioning.SelectBestPosition(
            selfPos,
            slot,
            teamBB,
            new CreateSupportAnglePositioning.Settings
            {
                ForwardLeadRatio = so.ForwardLeadRatio,
                WingLaneRatio = so.WingLaneRatio,
                OptimalDistanceRatio = so.OptimalDistanceRatio,
                MinDistanceRatio = so.MinDistanceRatio,
                MaxDistanceRatio = so.MaxDistanceRatio,
                AngleTolerance = so.AngleTolerance,
            });
    }

    private static Vector3 PredictMakeRunBehind(
        Vector3 selfPos,
        int slot,
        TeamBlackboard teamBB,
        PlayerBlackboard bb,
        MakeRunBehindActionSO so)
    {
        Vector3 ownerPos = ResolveBallOwnerPosition(teamBB);
        Vector3 enemyGoal = teamBB.FieldInfo.EnemyGoalPosition;
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float fieldWidth = teamBB.FieldInfo.FieldWidth;

        Vector3 toGoal = (enemyGoal - ownerPos).normalized;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = (enemyGoal - selfPos).normalized;
        }

        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        float defenseLineToGoal = float.MaxValue;
        foreach (Vector3 enemyPos in teamBB.BasicInfo.EnemyPositions)
        {
            float d = Vector3.Distance(enemyPos, enemyGoal);
            if (d < defenseLineToGoal)
            {
                defenseLineToGoal = d;
            }
        }

        if (defenseLineToGoal < float.MaxValue)
        {
            float behindMargin = fieldLength * 0.06f;
            Vector3 linePoint = enemyGoal - toGoal * defenseLineToGoal;
            float lat = slot switch
            {
                1 => 0.18f,
                2 => -0.18f,
                0 => 0.04f,
                _ => 0f,
            };
            return linePoint - toGoal * behindMargin + right * (fieldWidth * lat);
        }

        float depth = fieldLength * Mathf.Clamp(so.MaxRunDistanceRatio * 0.55f, 0.22f, 0.38f);
        Vector3 lateral = slot switch
        {
            1 => right * (fieldWidth * 0.22f),
            2 => -right * (fieldWidth * 0.22f),
            _ => right * (fieldWidth * 0.06f),
        };
        return selfPos + toGoal * depth + lateral;
    }

    private static float EvaluateGetOpenCell(
        Vector3 position,
        Vector3 selfPos,
        Vector3 ownerPos,
        TeamBlackboard teamBB,
        float optimalDistanceRatio)
    {
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float checkRadius = fieldLength * 0.15f;

        float proximity = 1f - Mathf.Clamp01(Vector3.Distance(position, selfPos) / fieldLength);
        float score = proximity * 3f;

        int nearbyEnemies = 0;
        foreach (Vector3 enemyPos in teamBB.BasicInfo.EnemyPositions)
        {
            if (Vector3.Distance(position, enemyPos) < checkRadius)
            {
                nearbyEnemies++;
            }
        }

        score += (1f - nearbyEnemies / 4f) * 2f;

        int nearbyTeammates = 0;
        foreach (Vector3 teammatePos in teamBB.BasicInfo.TeammatePositions)
        {
            if ((teammatePos - selfPos).sqrMagnitude < 0.01f)
            {
                continue;
            }

            if (Vector3.Distance(position, teammatePos) < checkRadius)
            {
                nearbyTeammates++;
            }
        }

        score += (1f - nearbyTeammates / 3f) * 1.5f;

        float optimalDistance = fieldLength * optimalDistanceRatio;
        float ballDistance = Vector3.Distance(position, ownerPos);
        score += (1f - Mathf.Clamp01(Mathf.Abs(ballDistance - optimalDistance) / Mathf.Max(optimalDistance, 0.1f)));

        return score;
    }

    private static Vector3 ResolveSelfPosition(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self != null)
        {
            return bb.BasicData.Self.transform.position;
        }

        return bb.PhysicalState.Position;
    }

    private static Vector3 ResolveBallOwnerPosition(TeamBlackboard teamBB)
    {
        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        if (ownerPos.sqrMagnitude < 0.01f)
        {
            ownerPos = teamBB.BallInfo.BallPosition;
        }

        return ownerPos;
    }
}
