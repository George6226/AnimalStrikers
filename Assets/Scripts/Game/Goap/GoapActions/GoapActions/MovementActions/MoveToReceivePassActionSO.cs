using Game.Goap;
using UnityEngine;

/// <summary>
/// パス飛行中、指定された受け手がボール（または受け位置）へ移動する。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Movement/MoveToReceivePass", fileName = "MoveToReceivePassActionSO")]
public class MoveToReceivePassActionSO : GoapActionSO
{
    [Header("Move Settings")]
    [SerializeField] private float _maxChaseDuration = 3f;
    [SerializeField] private float _nearBallDistance = 1.1f;
    [SerializeField] private float _moveIntensity = 1f;

    public float MaxChaseDuration => _maxChaseDuration;
    public float NearBallDistance => _nearBallDistance;
    public float MoveIntensity => _moveIntensity;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "MoveToReceivePass";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost <= 0.01f)
        {
            _baseCost = 0.35f;
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
        return new MoveToReceivePassActionRuntime(this, debugName);
    }
}
