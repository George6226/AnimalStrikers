using UnityEngine;
using Game.Goap;

/// <summary>
/// シュートコース（保持者→自陣ゴール）を塞ぐための移動アクション。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Defense/BlockShotLane", fileName = "BlockShotLaneActionSO")]
public class BlockShotLaneActionSO : GoapActionSO
{
    [Header("基本設定")]
    [SerializeField] private float _executionTime = 2.5f;
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField, Range(0.02f, 0.12f)] private float _laneWidthRatio = 0.05f; // 遮断帯幅（フィールド長比）

    public float ExecutionTime => _executionTime;
    public float MoveSpeed => _moveSpeed;
    public float LaneWidthRatio => _laneWidthRatio;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "BlockShotLane";
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
        return new BlockShotLaneActionRuntime(this, debugName);
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

        // 1. ゴールが危険なほどコスト減（ボール保持者がゴールに近いほど危険）
        float distanceToGoal = Vector3.Distance(ownerPos, ownGoal);
        float normalizedDistance = Mathf.Clamp01(distanceToGoal / fieldLen);
        float dangerScore = 1f - normalizedDistance;
        const float minDangerForShotBlock = 0.52f;
        if (dangerScore < minDangerForShotBlock)
        {
            return 0.9f;
        }

        float totalAdjustment = 0f;
        totalAdjustment -= dangerScore * 1.5f;
        if (dangerScore >= 0.58f)
        {
            totalAdjustment -= 0.45f;
        }
        
        // 2. 敵のボール保持者にプレッシャーがかかっていなければコスト減
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
        
        // プレッシャーが弱い（1人以下）場合はコストを下げる
        if (pressureCount <= 1)
        {
            float pressureScore = 1f - (pressureCount / 2f); // 0人で1.0、1人で0.5
            totalAdjustment -= pressureScore * 1.0f; // 最大-1.0のコスト減
        }
        
        // 3. 自分がゴールに近いほどコストを少し下げる
        float playerDistanceToGoal = Vector3.Distance(playerPos, ownGoal);
        float normalizedPlayerDistance = Mathf.Clamp01(playerDistanceToGoal / fieldLen);
        float proximityScore = 1f - normalizedPlayerDistance; // ゴール前で1.0、フィールド端で0.0
        totalAdjustment -= proximityScore * 0.5f; // 最大-0.5のコスト減（少し）
        
        return totalAdjustment;
    }
}


