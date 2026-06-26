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
        if (shotDangerScore >= 0.52f)
        {
            return 0.8f;
        }

        float totalAdjustment = 0f;

        float distPlayerToOwner = Vector3.Distance(playerPos, ownerPos);
        if (distPlayerToOwner <= fieldLen * 0.22f)
        {
            totalAdjustment += 1.05f;
        }

        List<Vector3> enemyPositions = teamBB.BasicInfo.EnemyPositions;
        
        // 1. ボール保持者へのプレッシャーを計算（一定距離以内の味方の数）
        float pressureThreshold = fieldLen * 0.15f; // フィールド長の15%以内
        int pressureCount = 0;
        foreach (var allyPos in teamBB.BasicInfo.TeammatePositions)
        {
            // 自分自身は除外
            if (Vector3.Distance(allyPos, playerPos) < 0.1f) continue;
            
            float distance = Vector3.Distance(allyPos, ownerPos);
            if (distance <= pressureThreshold)
            {
                pressureCount++;
            }
        }
        
        // プレッシャーがかかっている局面ではパスコース遮断を優先
        if (pressureCount >= 1)
        {
            float pressureBonus = Mathf.Clamp01(pressureCount / 3f);
            totalAdjustment -= pressureBonus * 1.15f;
        }
        
        // 2. フリー状態の敵（ボールを持っていない & 味方がマークしていない）が近いほどコストを下げる
        float markThreshold = fieldLen * 0.15f; // マーク判定の閾値（フィールド長の15%以内）
        List<Vector3> freeEnemies = new List<Vector3>();
        foreach (var enemyPos in enemyPositions)
        {
            // ボール保持者の位置と異なる敵（ボールを持っていない敵）
            if (Vector3.Distance(enemyPos, ownerPos) > 0.1f)
            {
                // 味方がマークしているかチェック
                bool isMarked = false;
                foreach (var allyPos in teamBB.BasicInfo.TeammatePositions)
                {
                    // 自分自身は除外
                    if (Vector3.Distance(allyPos, playerPos) < 0.1f) continue;
                    
                    float distance = Vector3.Distance(allyPos, enemyPos);
                    if (distance <= markThreshold)
                    {
                        isMarked = true;
                        break;
                    }
                }
                
                // マークされていない敵のみ追加
                if (!isMarked)
                {
                    freeEnemies.Add(enemyPos);
                }
            }
        }
        
        if (freeEnemies.Count > 0)
        {
            // 最も近いフリー状態の敵を探す
            float minDistance = float.MaxValue;
            foreach (var enemyPos in freeEnemies)
            {
                float distance = Vector3.Distance(playerPos, enemyPos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
            
            // 距離が近いほどコストを下げる
            float idealDistance = fieldLen * 0.2f; // 理想的な距離（フィールド長の20%）
            float proximityScore = 1f - Mathf.Clamp01(minDistance / Mathf.Max(idealDistance, 0.01f));
            totalAdjustment -= proximityScore * 1.0f; // 最大-1.0のコスト減
        }
        
        return totalAdjustment;
    }
}


