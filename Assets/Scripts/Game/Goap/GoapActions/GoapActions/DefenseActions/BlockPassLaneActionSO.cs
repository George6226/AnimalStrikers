using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

/// <summary>
/// 保持者→危険な受け手のパスコースを塞ぐための移動アクション。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Defense/BlockPassLane", fileName = "BlockPassLaneActionSO")]
public class BlockPassLaneActionSO : GoapActionSO
{
    [Header("基本設定")]
    [SerializeField] private float _executionTime = 2.5f;
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField, Range(0.02f, 0.12f)] private float _laneWidthRatio = 0.04f; // 遮断帯幅（フィールド長比）

    public float ExecutionTime => _executionTime;
    public float MoveSpeed => _moveSpeed;
    public float LaneWidthRatio => _laneWidthRatio;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "BlockPassLane";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost >= 1.8f)
        {
            _baseCost = 1.05f;
        }

        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, false),
            new GoapCondition(SymbolTag.Basic.HAS_BALL, false),
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
        });

        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Basic.IS_MOVING, true),
            new GoapCondition(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, true),
        });
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new BlockPassLaneActionRuntime(this, debugName);
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        return TeammateNpcDefensePlanning.ComputeDynamicCost(
            this, bb, _baseCost, CalculateSituationalAdjustment(bb));
    }

    protected override float CalculateSituationalAdjustment(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        Vector3 ownGoal = teamBB.FieldInfo.OwnGoalPosition;
        Vector3 playerPos = bb.PhysicalState.Position;
        float fieldLen = teamBB.FieldInfo.FieldLength;

        float ownerDistToGoal = Vector3.Distance(ownerPos, ownGoal);
        float shotDangerScore = 1f - Mathf.Clamp01(ownerDistToGoal / fieldLen);

        float totalAdjustment = 0f;
        if (shotDangerScore >= 0.52f)
        {
            totalAdjustment += 0.95f;
        }

        float distPlayerToOwner = Vector3.Distance(playerPos, ownerPos);
        if (distPlayerToOwner > fieldLen * 0.24f)
        {
            totalAdjustment += 1.15f;
        }

        float pressureThreshold = fieldLen * 0.15f;
        int pressureCount = 0;
        foreach (Vector3 allyPos in teamBB.BasicInfo.TeammatePositions)
        {
            if (Vector3.Distance(allyPos, playerPos) < 0.1f)
            {
                continue;
            }

            if (Vector3.Distance(allyPos, ownerPos) <= pressureThreshold)
            {
                pressureCount++;
            }
        }

        if (pressureCount >= 1)
        {
            totalAdjustment -= Mathf.Clamp01(pressureCount / 3f) * 0.85f;
        }

        float markThreshold = fieldLen * 0.15f;
        Vector3 passTarget = default;
        float passTargetDist = float.MaxValue;
        foreach (Vector3 enemyPos in teamBB.BasicInfo.EnemyPositions)
        {
            if (Vector3.Distance(enemyPos, ownerPos) <= 0.1f)
            {
                continue;
            }

            bool isMarked = false;
            foreach (Vector3 allyPos in teamBB.BasicInfo.TeammatePositions)
            {
                if (Vector3.Distance(allyPos, playerPos) < 0.1f)
                {
                    continue;
                }

                if (Vector3.Distance(allyPos, enemyPos) <= markThreshold)
                {
                    isMarked = true;
                    break;
                }
            }

            if (isMarked)
            {
                continue;
            }

            float distFromOwner = Vector3.Distance(enemyPos, ownerPos);
            if (distFromOwner < passTargetDist)
            {
                passTargetDist = distFromOwner;
                passTarget = enemyPos;
            }
        }

        if (passTargetDist >= float.MaxValue * 0.5f)
        {
            return totalAdjustment;
        }

        Vector3 passDir = passTarget - ownerPos;
        passDir.y = 0f;
        if (passDir.sqrMagnitude < 0.01f)
        {
            return totalAdjustment;
        }

        passDir.Normalize();
        Vector3 ownerToPlayer = playerPos - ownerPos;
        ownerToPlayer.y = 0f;
        float laneAlign = ownerToPlayer.sqrMagnitude < 0.01f
            ? 0f
            : Vector3.Dot(ownerToPlayer.normalized, passDir);
        float alongLane = Vector3.Dot(ownerToPlayer, passDir);

        if (alongLane > 0f && alongLane <= fieldLen * 0.24f && laneAlign > 0.32f)
        {
            totalAdjustment -= 1.3f;
        }
        else if (distPlayerToOwner <= fieldLen * 0.22f && laneAlign < 0.25f)
        {
            totalAdjustment += 1.35f;
        }
        else if (distPlayerToOwner <= fieldLen * 0.22f && laneAlign >= 0.25f)
        {
            float distPlayerToPassTarget = Vector3.Distance(playerPos, passTarget);
            if (distPlayerToOwner < distPlayerToPassTarget * 0.8f)
            {
                totalAdjustment -= 1.15f;
            }
        }

        float blockUrgency = TeammateNpcDefensePlanning.ComputePassLaneBlockUrgency(bb);
        if (blockUrgency < 0.75f)
        {
            totalAdjustment += 1.25f;
        }
        else if (!TeammateNpcDefensePlanning.IsPrimaryPassLaneBlocker(bb))
        {
            totalAdjustment += 1.75f;
        }

        return totalAdjustment;
    }
}


