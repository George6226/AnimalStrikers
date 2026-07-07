using UnityEngine;

/// <summary>
/// Phase M1: ボール保持中に敵ゴール方向へドリブルする GOAP アクション（HAS_BALL は維持）。
/// プランナーは Pass/Shoot と同様に強制プラン経路で選出する。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Attack/DribbleTowardGoal", fileName = "DribbleTowardGoalActionSO")]
public class DribbleTowardGoalActionSO : GoapActionSO
{
    [Header("Dribble Settings")]
    [SerializeField] private float _burstDurationSeconds = 1.75f;
    [SerializeField] private float _moveIntensity = 1f;

    public float BurstDurationSeconds => _burstDurationSeconds;
    public float MoveIntensity => _moveIntensity;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "DribbleTowardGoal";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost <= 0.01f)
        {
            _baseCost = MainNpcAttackPlanning.DefaultDribbleBaseCost;
        }

        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, true),
            new GoapCondition(SymbolTag.Basic.HAS_BALL, true),
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
        });

        // ボールは保持したまま前進する（BallPossessionAttack の HAS_BALL=false ゴールには後方連鎖で寄与しない）
        _effects.Clear();
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new DribbleTowardGoalActionRuntime(this, debugName);
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        return TeammateNpcSupportPlanning.ComputeDynamicCost(
            this,
            bb,
            _baseCost,
            MainNpcAttackPlanning.ComputeDribbleCostAdjustment(bb));
    }
}
