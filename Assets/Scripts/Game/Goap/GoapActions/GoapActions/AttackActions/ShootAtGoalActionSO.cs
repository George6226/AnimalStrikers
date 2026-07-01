using UnityEngine;

/// <summary>
/// Phase M1: メイン NPC がゴール方向へシュートする GOAP アクション（AnimalAction_Shoot へ委譲）。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Attack/ShootAtGoal", fileName = "ShootAtGoalActionSO")]
public class ShootAtGoalActionSO : GoapActionSO
{
    [SerializeField] private float _executionTimeoutSeconds = 3f;

    public float ExecutionTimeoutSeconds => _executionTimeoutSeconds;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "ShootAtGoal";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost <= 0.01f)
        {
            _baseCost = 1.05f;
        }

        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, true),
            new GoapCondition(SymbolTag.Basic.HAS_BALL, true),
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
        });

        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Basic.HAS_BALL, false),
        });
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new ShootAtGoalActionRuntime(this, debugName);
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        return TeammateNpcSupportPlanning.ComputeDynamicCost(
            this,
            bb,
            _baseCost,
            MainNpcAttackPlanning.ComputeShootCostAdjustment(bb));
    }
}
