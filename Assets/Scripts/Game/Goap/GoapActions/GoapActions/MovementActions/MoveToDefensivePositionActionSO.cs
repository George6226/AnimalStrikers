using UnityEngine;
using Game.Goap;

/// <summary>
/// 相手ボール保持中に、保持者へ適切な距離でプレッシャーをかけられる守備位置へ移動する最小アクション。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Movement/MoveToDefensivePosition", fileName = "MoveToDefensivePositionActionSO")]
public class MoveToDefensivePositionActionSO : GoapActionSO
{
    [Header("Move Settings")]
    [SerializeField] private float _maxMoveDuration = 8f;
    [SerializeField] private float _arriveDistance = 1.5f;
    [SerializeField] private float _moveIntensity = 1f;
    [Tooltip("相手保持者の移動に合わせて守備目標位置を再計算する間隔（秒）")]
    [SerializeField] private float _retargetInterval = 0.35f;

    public float MaxMoveDuration => _maxMoveDuration;
    public float ArriveDistance => _arriveDistance;
    public float MoveIntensity => _moveIntensity;
    public float RetargetInterval => _retargetInterval;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "MoveToDefensivePosition";
        if (Mathf.Approximately(_baseCost, 1f))
        {
            _baseCost = 1.0f;
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
        return new MoveToDefensivePositionActionRuntime(this, debugName);
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        return TeammateNpcDefensePlanning.ComputeDynamicCost(
            this, bb, _baseCost, CalculateSituationalAdjustment(bb));
    }

    protected override float CalculateSituationalAdjustment(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || !teamBB.BallInfo.EnemyHasBall)
        {
            return 0f;
        }

        float fieldLen = teamBB.FieldInfo.FieldLength;
        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        float distToOwner = Vector3.Distance(bb.PhysicalState.Position, ownerPos);
        float optimal = fieldLen * 0.12f;
        float pressureScore = Mathf.Clamp01(1f - Mathf.Abs(distToOwner - optimal) / Mathf.Max(optimal, 0.01f));

        int nearbyPressurers = 0;
        float pressureThreshold = fieldLen * 0.15f;
        Vector3 selfPos = bb.PhysicalState.Position;
        foreach (var ally in teamBB.BasicInfo.TeammatePositions)
        {
            if (Vector3.Distance(ally, selfPos) < 0.1f)
            {
                continue;
            }

            if (Vector3.Distance(ally, ownerPos) <= pressureThreshold)
            {
                nearbyPressurers++;
            }
        }

        float ownerDistToGoal = Vector3.Distance(ownerPos, teamBB.FieldInfo.OwnGoalPosition);
        float shotDangerScore = 1f - Mathf.Clamp01(ownerDistToGoal / fieldLen);

        float adjustment = -pressureScore * 1.45f;
        if (nearbyPressurers >= 2)
        {
            adjustment += 0.45f;
        }

        if (TeammateNpcDefensePlanning.ComputePassLaneBlockUrgency(bb) >= 0.75f
            && shotDangerScore < 0.45f)
        {
            adjustment += TeammateNpcDefensePlanning.ComputePassLaneDelegationPenalty(bb);
        }

        if (shotDangerScore >= 0.45f)
        {
            adjustment -= 0.7f;
        }

        adjustment += TeammateNpcDefensePlanning.ComputeOverextendedDefensePenalty(bb);

        return adjustment;
    }
}
