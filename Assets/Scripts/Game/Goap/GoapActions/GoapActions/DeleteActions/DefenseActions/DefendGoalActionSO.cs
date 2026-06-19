using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ゴールを守るアクション
[CreateAssetMenu(menuName = "GOAP/Action/Defense/DefendGoal")]
public class DefendGoalActionSO : GoapActionSO
{
    [Header("守備設定")]
    [SerializeField] private float _defenseRadius = 8f;           // 守備範囲
    [SerializeField] private float _interceptRange = 3f;          // インターセプト範囲
    [SerializeField] private float _tackleRange = 2f;             // タックル範囲
    [SerializeField] private float _markDistance = 4f;            // マーク距離
    
    // プロパティをpublicにしてランタイムクラスからアクセス可能にする
    public float DefenseRadius => _defenseRadius;
    public float InterceptRange => _interceptRange;
    public float TackleRange => _tackleRange;
    public float MarkDistance => _markDistance;
    
    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new DefendGoalActionRuntime(this, debugName);
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
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
            new GoapCondition(SymbolTag.Basic.HAS_BALL, false), // ボールを持っていない
            new GoapCondition(SymbolTag.Tactical.ENEMY_HAS_BALL, true), // 敵がボールを持っている
            // new GoapCondition(SymbolTag.Tactical.DEFENSIVE_MODE, true), // 守備モード
            // new GoapCondition(SymbolTag.Tactical.DEFEND_ACTION, false), // 守備アクションが成功していない
            new GoapCondition(SymbolTag.Position.MY_FIELD_NOW, true), // 自陣にいる
        });
        
        // // 効果の設定
        // _effects.Clear();
        // _effects.AddRange(new GoapCondition[]
        // {
        //     // new GoapCondition(SymbolTag.Tactical.DEFEND_ACTION, true), 
        // });
    }
    
} 