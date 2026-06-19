using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ボールを追跡する(途中アクション)
[CreateAssetMenu(menuName = "GOAP/Action/Movement/ChaseBall")]
public class ChaseBallActionSO : GoapActionSO
{
    [Header("ボール追跡設定")]
    [SerializeField] private float _maxChaseDistance = 15f;
    
    // プロパティをpublicにしてランタイムクラスからアクセス可能にする
    public float MaxChaseDistance => _maxChaseDistance;
    
    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new ChaseBallActionRuntime(this, debugName);
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        // プログラム上でPreconditionとEffectを設定
        SetupPreconditionsAndEffects();
    }
    
    /// <summary>
    /// 前提条件と効果を設定
    /// </summary>
    private void SetupPreconditionsAndEffects()
    {
        // 前提条件の設定
        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Position.NEAR_BALL, true),
            new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, false),
            new GoapCondition(SymbolTag.Tactical.ENEMY_HAS_BALL, false),
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
            new GoapCondition(SymbolTag.Basic.HAS_BALL, false), // ボールを持っていない
        });
        
        // 効果の設定（次のアクションの前提条件としても機能）
        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Basic.IS_MOVING, true),
            new GoapCondition(SymbolTag.Basic.HAS_BALL, true), // ボールを獲得
            new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, true),
        });
    }
    
} 