using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

/// <summary>
/// 危険な相手（保持者から見て前方の敵）をマークするための移動アクション。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Defense/MarkOpponent", fileName = "MarkOpponentActionSO")]
public class MarkOpponentActionSO : GoapActionSO
{
    [Header("基本設定")]
    [SerializeField] private float _executionTime = 2.5f;
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField, Range(0.02f, 0.2f)] private float _markDistanceRatio = 0.08f;

    public float ExecutionTime => _executionTime;
    public float MoveSpeed => _moveSpeed;
    public float MarkDistanceRatio => _markDistanceRatio;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "MarkOpponent";
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
        return new MarkOpponentActionRuntime(this, debugName);
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        return TeammateNpcDefensePlanning.ComputeDynamicCost(
            this, bb, _baseCost, CalculateSituationalAdjustment(bb));
    }

    protected override float CalculateSituationalAdjustment(PlayerBlackboard bb)
    {
        // フリー状態の敵が近いほどコスト減（TeamFacade 経由で TeamBlackboard を参照）
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        float overextensionPenalty = TeammateNpcDefensePlanning.ComputeOverextendedDefensePenalty(bb);
        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        Vector3 playerPos = bb.PhysicalState.Position;
        float fieldLen = teamBB.FieldInfo.FieldLength;
        
        // ボールを持っていない & 味方がマークしていない敵（フリー状態）をフィルタリング
        List<Vector3> freeEnemies = new List<Vector3>();
        float markThreshold = fieldLen * 0.15f; // マーク判定の閾値
        foreach (var e in teamBB.BasicInfo.EnemyPositions)
        {
            // ボール保持者の位置と異なる敵（ボールを持っていない敵）
            if (Vector3.Distance(e, ownerPos) > 0.1f)
            {
                bool isMarked = false;
                foreach (var allyPos in teamBB.BasicInfo.TeammatePositions)
                {
                    // 自分自身は除外
                    if (Vector3.Distance(allyPos, playerPos) < 0.1f) continue;
                    
                    float distance = Vector3.Distance(allyPos, e);
                    if (distance <= markThreshold)
                    {
                        isMarked = true;
                        break;
                    }
                }
                
                if (!isMarked)
                {
                    freeEnemies.Add(e);
                }
            }
        }
        
        // フリー状態の敵がいない場合はデフォルトコスト
        if (freeEnemies.Count == 0)
        {
            return overextensionPenalty;
        }
        
        // 最も近いフリー状態の敵を探す
        float minDistance = float.MaxValue;
        foreach (var e in freeEnemies)
        {
            float distance = Vector3.Distance(playerPos, e);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }
        
        float distToOwner = Vector3.Distance(playerPos, ownerPos);
        float idealDistance = fieldLen * _markDistanceRatio;
        Vector3 ownGoal = teamBB.FieldInfo.OwnGoalPosition;
        float ownerDistToGoal = Vector3.Distance(ownerPos, ownGoal);
        float shotDangerScore = 1f - Mathf.Clamp01(ownerDistToGoal / fieldLen);
        if (shotDangerScore >= 0.45f && minDistance > idealDistance * 0.85f)
        {
            return 0.75f + overextensionPenalty;
        }

        if (distToOwner <= fieldLen * 0.22f && minDistance > distToOwner * 1.1f)
        {
            return 1.15f + overextensionPenalty;
        }

        // 距離が近いほどコストを下げる
        if (minDistance <= idealDistance * 0.55f)
        {
            return -1.85f + overextensionPenalty;
        }

        float score = 1f - Mathf.Clamp01(minDistance / Mathf.Max(idealDistance * 2f, 0.01f));
        return -score * 1.55f + overextensionPenalty;
    }
}


