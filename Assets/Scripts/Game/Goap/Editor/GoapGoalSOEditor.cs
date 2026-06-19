using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// GoapGoalSOのカスタムエディター
/// RequiredFactsをInspectorで見やすく表示
/// </summary>
[CustomEditor(typeof(GoapGoalSO), true)]
public class GoapGoalSOEditor : Editor
{
    private bool _showRequiredFacts = true;
    private bool _showGoalInfo = true;
    private bool _showPriorityInfo = true;
    
    public override void OnInspectorGUI()
    {
        var goalSO = target as GoapGoalSO;
        if (goalSO == null) return;
        
        // 基本情報の表示
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("基本情報", EditorStyles.boldLabel);
        
        // ゴール名
        EditorGUI.BeginChangeCheck();
        string goalName = EditorGUILayout.TextField("ゴール名", goalSO.GoalName);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(goalSO, "Change Goal Name");
            // ゴール名の変更は直接できないので、コメントで説明
            EditorGUILayout.HelpBox("ゴール名はクラス名から自動生成されます", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        
        // ゴール情報の表示
        _showGoalInfo = EditorGUILayout.Foldout(_showGoalInfo, "ゴール情報", true);
        if (_showGoalInfo)
        {
            EditorGUI.indentLevel++;
            DisplayGoalInfo(goalSO);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // 優先度情報の表示
        _showPriorityInfo = EditorGUILayout.Foldout(_showPriorityInfo, "優先度情報", true);
        if (_showPriorityInfo)
        {
            EditorGUI.indentLevel++;
            DisplayPriorityInfo(goalSO);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // RequiredFactsの表示
        _showRequiredFacts = EditorGUILayout.Foldout(_showRequiredFacts, "Required Facts", true);
        if (_showRequiredFacts)
        {
            EditorGUI.indentLevel++;
            DisplayRequiredFacts(goalSO);
            EditorGUI.indentLevel--;
        }
        
        // 変更を適用
        if (GUI.changed)
        {
            EditorUtility.SetDirty(goalSO);
        }
    }
    
    /// <summary>
    /// ゴール情報を表示
    /// </summary>
    private void DisplayGoalInfo(GoapGoalSO goalSO)
    {
        string goalType = goalSO.GetType().Name;
        string category = GetGoalCategory(goalType);
        
        EditorGUILayout.LabelField($"ゴールタイプ: {goalType}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"カテゴリ: {category}", EditorStyles.miniLabel);
        
        // ゴールの説明を表示
        string description = GetGoalDescription(goalType);
        if (!string.IsNullOrEmpty(description))
        {
            EditorGUILayout.HelpBox(description, MessageType.Info);
        }
        
        // ゴールの特徴を表示
        DisplayGoalFeatures(goalType);
    }
    
    /// <summary>
    /// 優先度情報を表示
    /// </summary>
    private void DisplayPriorityInfo(GoapGoalSO goalSO)
    {
        // リフレクションを使用して_priorityフィールドを取得
        var priorityField = goalSO.GetType().GetField("_priority", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (priorityField != null)
        {
            float priority = (float)priorityField.GetValue(goalSO);
            EditorGUILayout.LabelField($"基本優先度: {priority}", EditorStyles.boldLabel);
            
            // 優先度の説明
            string priorityDescription = GetPriorityDescription(priority);
            if (!string.IsNullOrEmpty(priorityDescription))
            {
                EditorGUILayout.HelpBox(priorityDescription, MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("優先度フィールドが見つかりません", MessageType.Warning);
        }
        
        // 優先度評価メソッドの説明
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("優先度評価", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("EvaluatePriority()メソッドにより、実行時に動的に優先度が計算されます。", MessageType.Info);
        EditorGUILayout.HelpBox("チーム状態、プレイヤー状態、ゲーム状況に応じて優先度が調整されます。", MessageType.Info);
    }
    
    /// <summary>
    /// RequiredFactsを表示
    /// </summary>
    private void DisplayRequiredFacts(GoapGoalSO goalSO)
    {
        var requiredFacts = goalSO.RequiredFacts;
        
        if (requiredFacts == null || requiredFacts.Count == 0)
        {
            EditorGUILayout.HelpBox("RequiredFactsが設定されていません", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.LabelField($"RequiredFacts ({requiredFacts.Count}個)", EditorStyles.boldLabel);
        
        for (int i = 0; i < requiredFacts.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Fact名と期待値の表示
            string factName = requiredFacts[i].Tag;
            bool expectedValue = requiredFacts[i].ExpectedValue;
            bool isValid = SymbolTag.IsValidFactName(factName);
            
            // 有効性に応じて色を変更
            Color originalColor = GUI.color;
            if (!isValid)
            {
                GUI.color = Color.red;
            }
            
            EditorGUILayout.LabelField($"{i + 1}. {factName} = {expectedValue}", EditorStyles.label);
            
            GUI.color = originalColor;
            
            EditorGUILayout.EndHorizontal();
            
            // 無効なFactの場合の警告
            if (!isValid)
            {
                EditorGUILayout.HelpBox($"無効なFact名: {factName}", MessageType.Error);
            }
            
            // Factの説明を表示
            string factDescription = GetFactDescription(factName);
            if (!string.IsNullOrEmpty(factDescription))
            {
                EditorGUILayout.HelpBox(factDescription, MessageType.None);
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("これらのFactが全て満たされた時、ゴールが達成されます。", MessageType.Info);
    }
    
    /// <summary>
    /// ゴールカテゴリを取得
    /// </summary>
    private string GetGoalCategory(string goalType)
    {
        if (goalType.Contains("Attack")) return "攻撃";
        if (goalType.Contains("Defense")) return "守備";
        if (goalType.Contains("Tactical")) return "戦術";
        if (goalType.Contains("Situational")) return "状況";
        return "その他";
    }
    
    /// <summary>
    /// ゴールの説明を取得
    /// </summary>
    private string GetGoalDescription(string goalType)
    {
        switch (goalType)
        {
            // 実際に存在するゴール
            case "ReceivePassGoalSO":
                return "パスを受ける位置に移動して、ボールを受け取る準備をする。GetOpen、CreateSupportAngle、WallPass、ProtectPassSupportMove、MakeRunBehindなどの攻撃アクションと連携して使用される。";
            case "DefensivePositioningGoalSO":
                return "守備時の適切な位置取りを行う。マーク、プレッシャー、パスコース遮断を実現しやすい位置を目指す。BlockPassLane、BlockShotLane、PressureBallOwner、MarkOpponent、RetreatToDefensiveLineなどの守備アクションと連携して使用される。";
            case "TestGoalSO":
                return "テスト用のゴール。開発・デバッグ時に使用される。";
            
            // 将来実装される可能性のあるゴール（参考用）
            case "ScoreGoalSO":
                return "得点を狙う最終的なゴール。ゴールに近づいてシュートを決めることを目指します。";
            case "PreventGoalGoalSO":
                return "失点を防ぐ重要なゴール。自陣ゴールが危険な状況で守備を強化します。";
            case "AdvanceBallGoalSO":
                return "ボールを前進させるゴール。攻撃の流れを作り、ゴールに近づくことを目指します。";
            case "ApproachGoalGoalSO":
                return "ゴールに接近する中間目標。シュートの準備としてゴールに近づくことを目指します。";
            case "CreateChanceGoalSO":
                return "チャンスを作るゴール。味方との連携で攻撃の機会を作り出すことを目指します。";
            case "ComplexAttackGoalSO":
                return "複合攻撃を実行するゴール。複数のアクションを連鎖させて効果的な攻撃を目指します。";
            case "RecoverBallGoalSO":
                return "ボールを奪取するゴール。敵からボールを奪い、攻撃の機会を作ります。";
            case "ApproachEnemyGoalSO":
                return "敵に接近する中間目標。守備の準備として敵に近づくことを目指します。";
            case "ComplexDefenseGoalSO":
                return "複合守備を実行するゴール。複数のアクションを連鎖させて効果的な守備を目指します。";
            case "MarkEnemyGoalSO":
                return "敵のマークを行うゴール。特定の敵プレイヤーをマークして守備を強化します。";
            case "MaintainFormationGoalSO":
                return "フォーメーションを維持するゴール。チームの戦術的な位置取りを保ちます。";
            case "ApproachTeammateGoalSO":
                return "味方に接近する中間目標。パスや連携の準備として味方に近づくことを目指します。";
            case "SupportTeammateGoalSO":
                return "味方をサポートするゴール。味方の攻撃や守備を支援することを目指します。";
            case "RegainStaminaGoalSO":
                return "スタミナを回復するゴール。疲労状態を改善し、次のアクションに備えます。";
            case "ChaseLooseBallGoalSO":
                return "フリーボールを追跡するゴール。誰も持っていないボールを獲得することを目指します。";
            default:
                return "このゴールの詳細な説明は実装されていません。";
        }
    }
    
    /// <summary>
    /// ゴールの特徴を表示
    /// </summary>
    private void DisplayGoalFeatures(string goalType)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("特徴", EditorStyles.boldLabel);
        
        switch (goalType)
        {
            // 実際に存在するゴール
            case "ReceivePassGoalSO":
                EditorGUILayout.LabelField("• 攻撃時の基本的なゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• チームがボール保持時に優先度が上昇", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 最適距離に近いほど優先度が上昇", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 連携アクション: GetOpen, CreateSupportAngle, WallPass, ProtectPassSupportMove, MakeRunBehind", EditorStyles.miniLabel);
                break;
            case "DefensivePositioningGoalSO":
                EditorGUILayout.LabelField("• 守備時の基本的なゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 敵がボール保持時に優先度が上昇", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• プレッシャー、マーク、パス遮断の位置取りを評価", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 連携アクション: BlockPassLane, BlockShotLane, PressureBallOwner, MarkOpponent, RetreatToDefensiveLine", EditorStyles.miniLabel);
                break;
            case "TestGoalSO":
                EditorGUILayout.LabelField("• テスト・デバッグ用のゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 開発時に使用される", EditorStyles.miniLabel);
                break;
            
            // 将来実装される可能性のあるゴール（参考用）
            case "ScoreGoalSO":
                EditorGUILayout.LabelField("• 最高優先度のゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 得点差に応じて優先度が調整される", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 攻撃モード時に優先度が上昇", EditorStyles.miniLabel);
                break;
            case "PreventGoalGoalSO":
                EditorGUILayout.LabelField("• 緊急時の守備ゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 自陣ゴールの危険度に応じて優先度が上昇", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 守備モード時に優先度が上昇", EditorStyles.miniLabel);
                break;
            case "AdvanceBallGoalSO":
                EditorGUILayout.LabelField("• 攻撃の流れを作るゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 敵の圧力が低い時に優先度が上昇", EditorStyles.miniLabel);
                break;
            case "ApproachGoalGoalSO":
                EditorGUILayout.LabelField("• シュート準備の中間目標", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• ボール保持時に優先度が上昇", EditorStyles.miniLabel);
                break;
            case "CreateChanceGoalSO":
                EditorGUILayout.LabelField("• 連携を重視するゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 味方との協力でチャンスを作る", EditorStyles.miniLabel);
                break;
            case "ComplexAttackGoalSO":
                EditorGUILayout.LabelField("• 複数アクションの連鎖", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 高優先度の攻撃ゴール", EditorStyles.miniLabel);
                break;
            case "RecoverBallGoalSO":
                EditorGUILayout.LabelField("• ボール奪取を目指すゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 守備モード時に優先度が上昇", EditorStyles.miniLabel);
                break;
            case "ApproachEnemyGoalSO":
                EditorGUILayout.LabelField("• 守備準備の中間目標", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 敵ボール保持時に優先度が上昇", EditorStyles.miniLabel);
                break;
            case "ComplexDefenseGoalSO":
                EditorGUILayout.LabelField("• 複数アクションの連鎖", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 高優先度の守備ゴール", EditorStyles.miniLabel);
                break;
            case "MarkEnemyGoalSO":
                EditorGUILayout.LabelField("• 特定敵のマーク", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 守備モード時に優先度が上昇", EditorStyles.miniLabel);
                break;
            case "MaintainFormationGoalSO":
                EditorGUILayout.LabelField("• フォーメーション維持を目指すゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 低優先度だが重要な戦術的ゴール", EditorStyles.miniLabel);
                break;
            case "ApproachTeammateGoalSO":
                EditorGUILayout.LabelField("• 連携準備の中間目標", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 攻撃モード時に優先度が上昇", EditorStyles.miniLabel);
                break;
            case "SupportTeammateGoalSO":
                EditorGUILayout.LabelField("• 味方支援を目指すゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• チームプレイを重視", EditorStyles.miniLabel);
                break;
            case "RegainStaminaGoalSO":
                EditorGUILayout.LabelField("• スタミナ回復を目指すゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• チームが優勢な時に優先度が上昇", EditorStyles.miniLabel);
                break;
            case "ChaseLooseBallGoalSO":
                EditorGUILayout.LabelField("• フリーボール獲得を目指すゴール", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• 状況に応じた優先度調整", EditorStyles.miniLabel);
                break;
        }
    }
    
    /// <summary>
    /// 優先度の説明を取得
    /// </summary>
    private string GetPriorityDescription(float priority)
    {
        if (priority >= 9f) return "最高優先度 - 緊急時や重要なゴール";
        if (priority >= 7f) return "高優先度 - 重要な戦術的ゴール";
        if (priority >= 5f) return "中優先度 - 一般的なゴール";
        if (priority >= 3f) return "低優先度 - 補助的なゴール";
        return "最低優先度 - 緊急時以外は実行されない";
    }
    
    /// <summary>
    /// Factの説明を取得
    /// </summary>
    private string GetFactDescription(string factName)
    {
        switch (factName)
        {
            case "hasBall":
                return "プレイヤーがボールを持っている状態";
            case "isReady":
                return "プレイヤーがアクション実行の準備ができている状態";
            case "actionCompleted":
                return "アクションが完了した状態";
            case "nearGoal":
                return "ゴールに近い位置にいる状態";
            case "teamHasBall":
                return "チームがボールを保持している状態";
            case "enemyHasBall":
                return "敵がボールを保持している状態";
            case "inGoodPosition":
                return "戦術的に良い位置にいる状態";
            case "atTarget":
                return "目標位置に到達した状態";
            case "hasStamina":
                return "スタミナが十分にある状態";
            case "canDefend":
                return "守備が可能な状態";
            case "nearEnemy":
                return "敵に近い位置にいる状態";
            case "inDanger":
                return "危険な状況にある状態";
            default:
                return "";
        }
    }
} 