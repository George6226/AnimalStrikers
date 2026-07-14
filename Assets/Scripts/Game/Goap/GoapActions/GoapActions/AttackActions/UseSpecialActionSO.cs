using UnityEngine;

/// <summary>
/// F5: 必殺技（AnimalAction_Special）。攻撃・守備ゴール双方の候補。
/// ゲージ満タンかつキャラ別 CanExecuteSpecial が true のときだけ低コスト。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Attack/UseSpecial", fileName = "UseSpecialActionSO")]
public class UseSpecialActionSO : GoapActionSO
{
    [SerializeField] private float _executionTimeoutSeconds = 5f;

    public float ExecutionTimeoutSeconds => _executionTimeoutSeconds;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "UseSpecial";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost <= 0.01f)
        {
            _baseCost = 0.72f;
        }

        // キャラによって保持/非保持が分かれるため、ボール前提は弱い条件のみ。
        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
        });

        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Basic.IS_MOVING, true),
        });
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new UseSpecialActionRuntime(this, debugName);
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        if (!MainNpcAttackPlanning.CanUseSpecial(bb))
        {
            return 99f;
        }

        float adjustment = MainNpcAttackPlanning.ComputeSpecialCostAdjustment(bb);
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (TeammateNpcDefensePlanning.IsEnemyBallDefenseContext(teamBB, bb))
        {
            return TeammateNpcDefensePlanning.ComputeDynamicCost(this, bb, _baseCost, adjustment);
        }

        return TeammateNpcSupportPlanning.ComputeDynamicCost(this, bb, _baseCost, adjustment);
    }
}
