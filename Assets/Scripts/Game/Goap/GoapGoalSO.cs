using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// GOAP用ゴール
[CreateAssetMenu(menuName = "GOAP/Goal")]
public class GoapGoalSO : ScriptableObject
{
    // ゴール名
    [SerializeField] protected string _goalName;
    public string GoalName
    {
        get { return _goalName; }
    }

    // 必要な効力リスト
    [SerializeField] protected List<GoapCondition> _requiredFacts;
    public List<GoapCondition> RequiredFacts
    {
        get { return _requiredFacts; }
    }

    /// <summary>
    /// プランナーが到達判定に使う Fact。通常は RequiredFacts と同じ。
    /// </summary>
    public virtual List<GoapCondition> GetPlanningRequiredFacts(PlayerBlackboard bb)
    {
        return _requiredFacts;
    }
    
    // === プログラム上での設定メソッド ===
    
    /// <summary>
    /// RequiredFactsを設定
    /// </summary>
    /// <param name="requiredFacts">RequiredFactsのリスト</param>
    public void SetRequiredFacts(List<GoapCondition> requiredFacts)
    {
        _requiredFacts.Clear();
        if (requiredFacts != null)
        {
            _requiredFacts.AddRange(requiredFacts);
        }
    }
    
    /// <summary>
    /// RequiredFactsを設定（可変長引数）
    /// </summary>
    /// <param name="requiredFacts">RequiredFacts</param>
    public void SetRequiredFacts(params GoapCondition[] requiredFacts)
    {
        _requiredFacts.Clear();
        if (requiredFacts != null)
        {
            _requiredFacts.AddRange(requiredFacts);
        }
    }
    
    /// <summary>
    /// RequiredFactsを追加
    /// </summary>
    /// <param name="requiredFact">追加するRequiredFact</param>
    public void AddRequiredFact(GoapCondition requiredFact)
    {
        if (requiredFact != null && !_requiredFacts.Contains(requiredFact))
        {
            _requiredFacts.Add(requiredFact);
        }
    }
    
    /// <summary>
    /// RequiredFactsを削除
    /// </summary>
    /// <param name="requiredFact">削除するRequiredFact</param>
    public void RemoveRequiredFact(GoapCondition requiredFact)
    {
        _requiredFacts.Remove(requiredFact);
    }
    
    /// <summary>
    /// 全てのRequiredFactsをクリア
    /// </summary>
    public void ClearRequiredFacts()
    {
        _requiredFacts.Clear();
    }
    
    /// <summary>
    /// ゴールの優先度を評価（継承クラスでオーバーライド）
    /// </summary>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <param name="tb">チームブラックボード</param>
    /// <returns>優先度スコア</returns>
    public virtual float EvaluatePriority(PlayerBlackboard bb, TeamBlackboard tb)
    {
        // デフォルトの優先度（継承クラスでオーバーライド）
        return 1f;
    }
    
    /// <summary>
    /// ゴールが達成可能かチェック
    /// </summary>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <returns>達成可能かどうか</returns>
    public virtual bool IsAchievable(PlayerBlackboard bb)
    {
        // 基本的なチェック：RequiredFactsが設定されているか
        if (_requiredFacts == null || _requiredFacts.Count == 0)
        {
            return false;
        }
        
        // より詳細なチェックは継承クラスで実装
        return true;
    }
    
    /// <summary>
    /// ゴールの説明を取得
    /// </summary>
    /// <returns>ゴールの説明</returns>
    public virtual string GetGoalDescription()
    {
        return $"ゴール: {_goalName} (RequiredFacts: {_requiredFacts?.Count ?? 0}個)";
    }
    
    /// <summary>
    /// ゴールのカテゴリを取得
    /// </summary>
    /// <returns>ゴールのカテゴリ</returns>
    public virtual string GetGoalCategory()
    {
        string typeName = GetType().Name;
        if (typeName.Contains("Attack")) return "攻撃";
        if (typeName.Contains("Defense")) return "守備";
        if (typeName.Contains("Tactical")) return "戦術";
        if (typeName.Contains("Situational")) return "状況";
        return "その他";
    }
    
    // === 初期化メソッド ===
    
    /// <summary>
    /// ScriptableObject作成時の初期化
    /// </summary>
    protected virtual void OnEnable()
    {
        // リストの初期化
        if (_requiredFacts == null) _requiredFacts = new List<GoapCondition>();
        
        // ゴール名が設定されていない場合は自動設定
        if (string.IsNullOrEmpty(_goalName))
        {
            _goalName = GetType().Name.Replace("GoalSO", "");
        }
    }
}
