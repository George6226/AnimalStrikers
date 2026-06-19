using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// GOAP用の途中アクション(継承前)
[CreateAssetMenu(menuName = "GOAP/Action")]
public class GoapActionSO : ScriptableObject
{
    // アクション名
    [SerializeField] protected string _actionName;
    public string ActionName {
        get { return _actionName; }
    }
    
    // アクションの説明
    public virtual string Description => "このGOAPアクションの説明は未設定です。";
    
    // 基本コスト（固定値）
    [SerializeField] protected float _baseCost = 1f;
    public float BaseCost {
        get { return _baseCost; }
    }
    
    // 前提条件リスト
    [SerializeField] protected List<GoapCondition> _preconditions = new List<GoapCondition>();
    public List<GoapCondition> Preconditions
    {
        get { return _preconditions; }
    }
    
    // 効力リスト
    [SerializeField] protected List<GoapCondition> _effects = new List<GoapCondition>();
    public List<GoapCondition> Effects
    {
        get { return _effects; }
    }


    // ランタイムを生成
    public virtual GoapActionRuntime CreateRuntime(string debugName) { return null; }

    // === コスト計算メソッド ===
    
    /// <summary>
    /// 動的コストを計算する（基本コスト + 状況に応じた調整値）
    /// </summary>
    /// <param name="bb">プレイヤーのブラックボード</param>
    /// <returns>計算された動的コスト</returns>
    public virtual float CalculateDynamicCost(PlayerBlackboard bb)
    {
        float dynamicCost = _baseCost;
        
        // 基本コストに状況に応じた調整を加える
        // 継承クラスでオーバーライドして具体的な調整ロジックを実装
        dynamicCost += CalculateSituationalAdjustment(bb);

        return dynamicCost;
    }
    
    /// <summary>
    /// 状況に応じたコスト調整値を計算する（継承クラスでオーバーライド）
    /// </summary>
    /// <param name="bb">プレイヤーのブラックボード</param>
    /// <returns>調整値（正の値でコスト増加、負の値でコスト減少）</returns>
    protected virtual float CalculateSituationalAdjustment(PlayerBlackboard bb)
    {
        // デフォルトでは調整なし
        return 0f;
    }

    // === 初期化メソッド ===
    
    /// <summary>
    /// ScriptableObject作成時の初期化
    /// </summary>
    protected virtual void OnEnable()
    {
        // リストの初期化
        if (_preconditions == null) _preconditions = new List<GoapCondition>();
        if (_effects == null) _effects = new List<GoapCondition>();
        
        // アクション名が設定されていない場合は自動設定
        if (string.IsNullOrEmpty(_actionName))
        {
            _actionName = GetType().Name.Replace("ActionSO", "");
        }
    }

    /// <summary>プランナー用の前提・効果を最新コード定義へ同期（SO アセットの OnEnable 取りこぼし対策）。</summary>
    public void EnsurePlanningFactsConfigured()
    {
        RefreshPlanningFacts();
    }

    protected virtual void RefreshPlanningFacts()
    {
    }
}
