using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// チームの基本情報を管理するクラス
/// ボールの所有状況、敵味方の位置情報などを保持 
/// </summary>
[System.Serializable]
public class TeamBasicInfo
{
    // 敵プレイヤーの位置情報リスト
    public List<Vector3> EnemyPositions { get; private set; } = new();
    // 味方プレイヤーの位置情報リスト
    public List<Vector3> TeammatePositions { get; private set; } = new();
    
    /// <summary>
    /// 基本情報を初期化する
    /// </summary>
    public void Initialize()
    {
       EnemyPositions.Clear();
       TeammatePositions.Clear();
    }
    
    /// <summary>
    /// 基本情報を更新する
    /// </summary>
    /// <param name="hasBall">チームがボールを保持しているか</param>
    /// <param name="owner">ボール保持者の名前</param>
    /// <param name="enemies">敵プレイヤーの位置リスト</param>
    /// <param name="teammates">味方プレイヤーの位置リスト</param>
    public void Update(List<Vector3> enemies, List<Vector3> teammates)
    {
       EnemyPositions.Clear();
       EnemyPositions.AddRange(enemies);
       TeammatePositions.Clear();
       TeammatePositions.AddRange(teammates);
    }
}

/// <summary>
/// ボール情報を管理するクラス
/// ボールの位置、状態、速度、所属チーム情報を統合して保持
/// </summary>
[System.Serializable]
public class TeamBallInfo
{
    // ボールの現在位置
    public Vector3 BallPosition { get; private set; }
    // ボールの現在の状態（フリー、キープ中など）
    public BallManager_State.BALL_STATE BallState { get; private set; }
    // ボールの所属チーム（プレイヤー、敵など）
    public BallManager_State.BELONG_TEAM BallBelongTeam { get; private set; }
    // ボールの速度ベクトル
    public Vector3 BallVelocity { get; private set; }
    // ボールを保持しているプレイヤーのID
    public int BallOwnerID { get; private set; }
    // ボールを保持しているプレイヤーの位置
    public Vector3 BallOwnerPosition { get; private set; }
    // チームがボールを保持しているかどうか
    public bool TeamHasBall { get; private set; }
    // 敵がボールを保持しているかどうか
    public bool EnemyHasBall { get; private set; }
    // ボールが存在しているか?
    public bool IsExistBall { get; private set; }
    // ボールがフリー状態
    public bool BallFree{ get; private set; }

    // 直近のポゼッション切替時刻
    public float LastPossessionSwitchTime { get; private set; }
    // 直近の切替後の所属チーム
    public BallManager_State.BELONG_TEAM LastPossessionBelongTeam { get; private set; }
    // ボール保持者に近接している敵の人数（チームメイトがボールを保持している場合のみ有効）
    public int IsBallOwnerUnderPressure { get; private set; }

    /// <summary>
    /// ボール情報を初期化する
    /// </summary>
    public void Initialize()
    {
        BallOwnerID = -1;
        TeamHasBall = false;
        EnemyHasBall = false;
        BallFree = true;
        BallPosition = Vector3.zero;
        BallState = BallManager_State.BALL_STATE.FREE;
        BallBelongTeam = BallManager_State.BELONG_TEAM.FREE;
        BallVelocity = Vector3.zero;
        IsExistBall = false;
        BallOwnerPosition = Vector3.zero;

        LastPossessionSwitchTime = -9999f;
        LastPossessionBelongTeam = BallManager_State.BELONG_TEAM.FREE;
        IsBallOwnerUnderPressure = 0;
    }

    // ボールを存在させる
    public void setExistBall()
    {
        IsExistBall = true;
    }
    // ボールの所持IDを更新
    public void updateBallID(int ownerId, BallManager_State.BELONG_TEAM belongTeam, Vector3 ownerPosition)
    {
        // 所属チームが変更されたかを検出
        bool changed = (BallBelongTeam != belongTeam);

        BallOwnerID = ownerId;
        BallBelongTeam = belongTeam;
        TeamHasBall = (belongTeam == BallManager_State.BELONG_TEAM.PLAYER);
        EnemyHasBall = (belongTeam == BallManager_State.BELONG_TEAM.ENEMY);
        BallFree = (belongTeam == BallManager_State.BELONG_TEAM.FREE);
        BallOwnerPosition = ownerPosition;

        if (changed)
        {
            LastPossessionSwitchTime = Time.time;
            LastPossessionBelongTeam = belongTeam;
        }
    }

    /// <summary>保持者のワールド座標のみ更新（所有権変更なし・毎フレーム同期用）。</summary>
    public void updateBallOwnerPosition(Vector3 ownerPosition)
    {
        BallOwnerPosition = ownerPosition;
    }

    // ボールの状態を更新
    public void updateBallState(BallManager_State.BALL_STATE state)
    {
        BallState = state;
    }
    // ボールの物理状態を更新
    public void updateBallPhysics(Vector3 pos, Vector3 v)
    {
        BallPosition = pos;
        BallVelocity = v;
    }
    
    /// <summary>
    /// ボール保持者のプレッシャー状態を更新する
    /// </summary>
    /// <param name="enemyPositions">敵の位置リスト</param>
    /// <param name="fieldLength">フィールドの長さ</param>
    public void UpdateBallOwnerPressure(List<Vector3> enemyPositions, float fieldLength)
    {
        // チームメイトがボールを保持している場合のみ更新
        if (!TeamHasBall || BallOwnerID < 0)
        {
            IsBallOwnerUnderPressure = 0;
            return;
        }
        
        // 敵が存在しない場合はプレッシャーなし
        if (enemyPositions == null || enemyPositions.Count == 0)
        {
            IsBallOwnerUnderPressure = 0;
            return;
        }
        
        // プレッシャーの閾値（フィールド長の15%）
        float pressureThreshold = fieldLength * 0.15f;
        
        // 閾値以内にいる敵の人数をカウント
        int pressureCount = 0;
        foreach (Vector3 enemyPos in enemyPositions)
        {
            float distance = Vector3.Distance(BallOwnerPosition, enemyPos);
            if (distance <= pressureThreshold)
            {
                pressureCount++;
            }
        }
        
        // 近接している敵の人数を設定
        IsBallOwnerUnderPressure = pressureCount;
    }
}

/// <summary>
/// チーム戦術情報を管理するクラス
/// 攻撃・守備モード、フォーメーション、プレッシャー度、戦略指標を統合して保持
/// </summary>
[System.Serializable]
public class TeamTacticalInfo
{
    // 攻撃モードかどうか
    public bool IsOffensiveMode { get; private set; }
    // 守備モードかどうか
    public bool IsDefensiveMode { get; private set; }
    
    /// <summary>
    /// 戦術情報を初期化する
    /// </summary>
    public void Initialize()
    {
       IsOffensiveMode = false;
       IsDefensiveMode = false;
    }
    
    /// <summary>
    /// 戦術情報を更新する
    /// </summary>
    /// <param name="offensive">攻撃モードかどうか</param>
    /// <param name="defensive">守備モードかどうか</param>
    public void Update(bool offensive, bool defensive)
    {
       IsOffensiveMode = offensive;
       IsDefensiveMode = defensive;
    }
}

/// <summary>
/// フィールド情報を管理するクラス
/// 自陣ゴール位置、フィールドサイズ、中心位置を統合して保持
/// </summary>
[System.Serializable]
public class TeamFieldInfo
{
    // 自陣ゴールの位置
    public Vector3 OwnGoalPosition { get; private set; }
    // 敵ゴールの位置
    public Vector3 EnemyGoalPosition { get; private set; }
    // フィールドの長さ
    public float FieldLength { get; private set; }
    // フィールドの中心位置
    public Vector3 FieldCenter { get; private set; }
    // フィールドの幅
    public float FieldWidth { get; private set; }
    
    /// <summary>
    /// フィールド情報を初期化する
    /// </summary>
    public void Initialize(float length, float width)
    {
        OwnGoalPosition = new Vector3(0.0f, 0.0f, -length/2.0f);
        EnemyGoalPosition = new Vector3(0.0f, 0.0f, length/2.0f);
        FieldLength = length;
        FieldCenter = Vector3.zero;
        FieldWidth = width;
    }
}