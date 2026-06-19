using UnityEngine;
using Game.Goap;

/// <summary>
/// 味方ボール保持中に、保持者から見てパスを受けやすいサポート位置へ移動する最小アクション。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Movement/MoveToSupportPosition", fileName = "MoveToSupportPositionActionSO")]
public class MoveToSupportPositionActionSO : GoapActionSO
{
    [Header("Move Settings")]
    [SerializeField] private float _maxMoveDuration = 8f;
    [SerializeField] private float _arriveDistance = 1.5f;
    [SerializeField] private float _moveIntensity = 1f;
    [Tooltip("ボール保持者の移動に合わせてサポート位置を再計算する間隔（秒）")]
    [SerializeField] private float _retargetInterval = 0.35f;

    public float MaxMoveDuration => _maxMoveDuration;
    public float ArriveDistance => _arriveDistance;
    public float MoveIntensity => _moveIntensity;
    public float RetargetInterval => _retargetInterval;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "MoveToSupportPosition";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost < 0.5f)
        {
            _baseCost = 1.15f;
        }

        RefreshPlanningFacts();
    }

    protected override void RefreshPlanningFacts()
    {
        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
        });

        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Basic.IS_MOVING, true),
            new GoapCondition(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, true),
            new GoapCondition(SymbolTag.Action.IS_MAINTAINING_SUPPORT_RELATIONSHIP, true),
        });
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new MoveToSupportPositionActionRuntime(this, debugName);
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        return TeammateNpcSupportPlanning.ComputeDynamicCost(
            this, bb, _baseCost, CalculateSituationalAdjustment(bb));
    }

    protected override float CalculateSituationalAdjustment(PlayerBlackboard bb)
    {
        if (!TeammateNpcSupportPlanning.ShouldUseWidthLayoutSupportPosition(bb))
        {
            return 0f;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return 0f;
        }

        int ownerSlot = CreateSupportAnglePositioning.ResolveBallOwnerFormationSlot(teamBB);
        if (ownerSlot == 1 || ownerSlot == 2)
        {
            // 翼保持時は slot0=中央レーン追従（MTS）を最優先
            return -0.35f;
        }

        return -0.12f;
    }
}
