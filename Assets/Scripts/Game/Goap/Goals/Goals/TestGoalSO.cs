using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// テスト用ゴール
[CreateAssetMenu(menuName = "GOAP/Goal/Test/TestGoal")]
public class TestGoalSO : GoapGoalSO
{
    [Header("テストゴール設定")]
    [SerializeField] private float _priority = 5f;              // 基本優先度
    [SerializeField] private float _testDuration = 3f;           // テスト実行時間
    [SerializeField] private string _testDescription = "テストゴールの説明"; // テスト説明
    
    protected override void OnEnable()
    {
        base.OnEnable();
        _goalName = "TestGoal";
        
        // プログラム上でRequiredFactsを設定
        SetRequiredFacts(
            new GoapCondition(SymbolTag.Test.TEST_COMPLETE, true),
            new GoapCondition(SymbolTag.Test.TEST0_MODE, true)
        );
    }
    
    /// <summary>
    /// ゴールの優先度を評価
    /// </summary>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <param name="tb">チームブラックボード</param>
    /// <returns>優先度スコア</returns>
    public override float EvaluatePriority(PlayerBlackboard bb, TeamBlackboard tb)
    {
        float priority = _priority;
        
//         // テストモードが有効でない場合の優先度調整
//         if (!bb.GetFact(SymbolTag.Test.TEST1_MODE))
//         {
//             priority += 10f; // テストモードが無効の場合は高優先度
//         }
        
//         // テストが完了していない場合の優先度調整
//         if (!bb.GetFact(SymbolTag.Test.TEST_COMPLETE))
//         {
//             priority += 5f; // テスト未完了の場合は中優先度
//         }
        
//         // プレイヤーの状態による調整
//         if (bb.ActionState.CanMove)
//         {
//             priority += 2f; // 移動可能な場合は少し優先度を上げる
//         }
        
        return priority;
    }
    
    /// <summary>
    /// ゴールの説明を取得
    /// </summary>
    /// <returns>ゴールの説明</returns>
    public override string GetGoalDescription()
    {
        return _testDescription;
    }
    
    /// <summary>
    /// ゴールが達成可能かチェック
    /// </summary>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <returns>達成可能かどうか</returns>
    public override bool IsAchievable(PlayerBlackboard bb)
    {
        // 基本的なチェック
        if (!base.IsAchievable(bb)) return false;
        
//         // テストモードが無効であることを確認（テストを開始するため）
//         if (bb.GetFact(SymbolTag.Test.TEST1_MODE))
//         {
//             return false; // 既にテストモードが有効な場合は達成済み
//         }
        
//         // 移動可能かチェック
//         if (!bb.ActionState.CanMove)
//         {
//             return false; // 移動できない場合はテストを実行できない
//         }
        
        return true;
    }
    
    /// <summary>
    /// ゴールのカテゴリを取得
    /// </summary>
    /// <returns>ゴールのカテゴリ</returns>
    public override string GetGoalCategory()
    {
        return "テスト";
    }
}
