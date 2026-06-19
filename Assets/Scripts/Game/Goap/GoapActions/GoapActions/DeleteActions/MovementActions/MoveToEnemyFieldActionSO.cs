using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "GOAP/Action/Movement/MoveToEnemyFieldAction")]
public class MoveToEnemyFieldActionSO : GoapActionSO
{
    [Header("敵陣地移動設定")]
    [SerializeField] private float _goodPositionRadius = 0.5f;
    public float GoodPositionRadius
    {
        get { return _goodPositionRadius; }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "MoveToEnemyFieldAction";
        SetupPreconditionsAndEffects();
    }

    private void SetupPreconditionsAndEffects()
    {
        // 前提条件：移動可能、攻撃中、味方がボールを保持
        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true), // 移動可能
            new GoapCondition(SymbolTag.Position.MY_FIELD_NOW, true), // 自分の陣地にいる
        });
        // 効果：敵陣地にいる
        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Position.MY_FIELD_NOW, false), // 敵陣地にいる
        });
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new MoveToEnemyFieldActionRuntime(this, debugName);
    }
} 