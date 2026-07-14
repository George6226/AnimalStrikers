using UnityEngine;

/// <summary>
/// F4: 相手ボール保持者への近接スライディング（AnimalAction_Sliding へ委譲）。
/// DefensivePositioning / EnemyBallDefense の候補。遠距離では高コストで選ばれない。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Defense/SlideTackle", fileName = "SlideTackleActionSO")]
public class SlideTackleActionSO : GoapActionSO
{
    [SerializeField] private float _executionTimeoutSeconds = 0.85f;

    public float ExecutionTimeoutSeconds => _executionTimeoutSeconds;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "SlideTackle";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost <= 0.01f)
        {
            _baseCost = 0.88f;
        }

        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, false),
            new GoapCondition(SymbolTag.Basic.HAS_BALL, false),
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
            new GoapCondition(SymbolTag.Position.NEAR_ENEMY_HAS_BALL, true),
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
        return new SlideTackleActionRuntime(this, debugName);
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        return TeammateNpcDefensePlanning.ComputeDynamicCost(
            this,
            bb,
            _baseCost,
            TeammateNpcDefensePlanning.ComputeSlideTackleCostAdjustment(bb));
    }
}
