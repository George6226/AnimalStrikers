using UnityEngine;

/// <summary>
/// Phase M1: メイン NPC が味方へパスする GOAP アクション（AnimalAction_Pass へ委譲）。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Attack/PassToTeammate", fileName = "PassToTeammateActionSO")]
public class PassToTeammateActionSO : GoapActionSO
{
    [SerializeField] private float _executionTimeoutSeconds = 3f;

    public float ExecutionTimeoutSeconds => _executionTimeoutSeconds;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "PassToTeammate";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost <= 0.01f)
        {
            _baseCost = 1.12f;
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
        return new PassToTeammateActionRuntime(this, debugName);
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        return TeammateNpcSupportPlanning.ComputeDynamicCost(
            this,
            bb,
            _baseCost,
            MainNpcAttackPlanning.ComputePassCostAdjustment(bb));
    }
}
