using UnityEngine;
using Game.Goap;

/// <summary>
/// ボールがFREEの間、ボール近傍まで移動する最小アクション。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Movement/MoveToFreeBall", fileName = "MoveToFreeBallActionSO")]
public class MoveToFreeBallActionSO : GoapActionSO
{
    [Header("Move Settings")]
    [SerializeField] private float _maxChaseDuration = 8f;
    [SerializeField] private float _nearBallDistance = 1.2f;
    [SerializeField] private float _moveIntensity = 1f;

    public float MaxChaseDuration => _maxChaseDuration;
    public float NearBallDistance => _nearBallDistance;
    public float MoveIntensity => _moveIntensity;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "MoveToFreeBall";
        if (Mathf.Approximately(_baseCost, 1f))
        {
            _baseCost = 0.8f;
        }

        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
        });

        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Basic.IS_MOVING, true),
            new GoapCondition(SymbolTag.Position.NEAR_BALL, true),
        });
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new MoveToFreeBallActionRuntime(this, debugName);
    }

    protected override float CalculateSituationalAdjustment(PlayerBlackboard bb)
    {
        float cost = TeammateNpcGoapRoleDifferentiation.AdjustActionCost(
            _baseCost, bb, TeammateNpcTacticalMode.ChaseBall);
        return cost - _baseCost;
    }
}
