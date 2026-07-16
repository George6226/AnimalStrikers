using UnityEngine;

/// <summary>
/// 6-C P0: 待機してスタミナを回復する（AnimalHandler.stand 経路）。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Utility/StandRecoverStamina", fileName = "StandRecoverStaminaActionSO")]
public class StandRecoverStaminaActionSO : GoapActionSO
{
    [SerializeField] private float _executionTimeoutSeconds = 8f;

    public float ExecutionTimeoutSeconds => _executionTimeoutSeconds;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "StandRecoverStamina";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost <= 0.01f)
        {
            _baseCost = 0.55f;
        }

        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
            new GoapCondition(SymbolTag.Basic.HAS_BALL, false),
        });

        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Basic.HAS_STAMINA, true),
            new GoapCondition(SymbolTag.Basic.IS_MOVING, false),
        });
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new StandRecoverStaminaActionRuntime(this, debugName);
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        if (bb != null && bb.GetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "true")) == true)
        {
            return 99f;
        }

        if (bb != null && bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return 99f;
        }

        return _baseCost;
    }
}
