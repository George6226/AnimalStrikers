using System.Collections.Generic;

/// <summary>
/// GOAPシステムで使用するFact名をまとめた静的クラス
/// 複数ルートを実現するため、汎用的で再利用可能なシンボルに統合
/// </summary>
public static class SymbolTag
{
    // === 基本状態（汎用的） ===
    public static class Basic
    {
        public const string HAS_BALL = "hasBall";
        public const string IS_MOVING = "isMoving";
    }
    
    // === アクション状態（汎用的） ===
    public static class Action
    {
        public const string CAN_MOVE = "canMove";
        public const string IS_IN_PASS_RECEIVE_POSITION = "isInPassReceivePosition";
        /// <summary>保持者に対してサポート距離・前方関係を維持しているか（ドリブル追従用）。</summary>
        public const string IS_MAINTAINING_SUPPORT_RELATIONSHIP = "isMaintainingSupportRelationship";
        public const string IS_IN_DEFENSIVE_POSITION = "isInDefensivePosition";
    }

    // === 位置・距離（汎用的） ===
    public static class Position
    {
        public const string NEAR_BALL = "nearBall";
        public const string NEAR_ENEMY_NO_BALL = "nearEnemyNoBall";
        public const string NEAR_ENEMY_HAS_BALL = "nearEnemyHasBall";
        public const string MY_FIELD_NOW = "myFieldNow";
        public const string NEAR_FIELD_SPACE = "nearFieldSpace";
        
    }

    // === 戦術状態（汎用的） ===
    public static class Tactical
    {
        public const string TEAM_HAS_BALL = "teamHasBall";
        public const string ENEMY_HAS_BALL = "enemyHasBall";
        public const string OFFENSIVE_MODE = "offensiveMode";   // 攻撃敵な状態(作戦/時間/スコア)
        public const string DEFENSIVE_MODE = "defensiveMode";   // 守備的な状態(作戦/時間/スコア)
    }

    // === テスト用 ===
    public static class Test
    {
        public const string TEST0_MODE = "test0Mode";                // テスト0モード
        public const string TEST1_MODE = "test1Mode";                // テスト1モード
        public const string TEST2_MODE = "test2Mode";                // テスト2モード
        public const string TEST3_MODE = "test3Mode";                // テスト3モード
        public const string TEST_COMPLETE = "testComplete";        // テスト完了したか
    }
    
    /// <summary>
    /// 全てのFact名を取得
    /// </summary>
    public static List<string> GetAllFactNames()
    {
        var allFacts = new List<string>();
        
        // リフレクションを使用して全ての定数を取得
        var basicType = typeof(Basic);
        var actionType = typeof(Action);
        var positionType = typeof(Position);
        var tacticalType = typeof(Tactical);
        var testType = typeof(Test);
        // var successType = typeof(Success);
        
        AddConstantsFromType(basicType, allFacts);
        AddConstantsFromType(actionType, allFacts);
        AddConstantsFromType(positionType, allFacts);
        AddConstantsFromType(tacticalType, allFacts);
        AddConstantsFromType(testType, allFacts);
        // AddConstantsFromType(successType, allFacts);
        
        return allFacts;
    }
    
    /// <summary>
    /// 指定された型から定数を取得してリストに追加
    /// </summary>
    private static void AddConstantsFromType(System.Type type, List<string> factList)
    {
        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(string))
            {
                var value = field.GetValue(null) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    factList.Add(value);
                }
            }
        }
    }
    
    /// <summary>
    /// Fact名が有効かチェック
    /// </summary>
    public static bool IsValidFactName(string factName)
    {
        var allFacts = GetAllFactNames();
        return allFacts.Contains(factName);
    }
    
    /// <summary>
    /// カテゴリ別のFact名を取得
    /// </summary>
    public static List<string> GetFactNamesByCategory(string category)
    {
        var facts = new List<string>();
        
        switch (category.ToLower())
        {
            case "basic":
                AddConstantsFromType(typeof(Basic), facts);
                break;
            case "action":
                AddConstantsFromType(typeof(Action), facts);
                break;
            case "position":
                AddConstantsFromType(typeof(Position), facts);
                break;
            case "tactical":
                AddConstantsFromType(typeof(Tactical), facts);
                break;
            
            case "test":
                AddConstantsFromType(typeof(Test), facts);
                break;
        }
        
        return facts;
    }
} 