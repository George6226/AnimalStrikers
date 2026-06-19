using System.Collections.Generic;
using UnityEngine;

// 基本情報
[System.Serializable]
public class PlayerBasicData
{
    // 自分自身
    public GameObject Self { get; private set; }
    // プレイヤーID
    public int PlayerID { get; private set; }
    // プレイヤー名
    public string PlayerName { get; private set; }
    // アクティブ状態か?
    public bool IsActive { get; private set; }

    // 初期化
    public void init(GameObject self)
    {
        // GOAP子オブジェクト配下でも正しい本体を参照できるように、親方向も探索する
        var photonView = self != null ? self.GetComponentInParent<Photon.Pun.PhotonView>() : null;
        Self = photonView != null ? photonView.gameObject : self;
        if (photonView != null)
        {
            // プレイヤーID
            PlayerID = photonView.ViewID;
        }
        else
        {
            PlayerID = -1;
        }
        PlayerName = "";
        IsActive = true;
    }
}

// 物理状態
[System.Serializable]
public class PlayerPhysicalState
{
    // 位置
    public Vector3 Position { get; private set; }
    // 速度
    public Vector3 Velocity { get; private set; }
    // 移動スピード
    public float MoveSpeed { get; private set; }
    // 移動中か?
    public bool IsMoving { get; private set; }

    // 初期化
    public void init(Vector3 pos)
    {
        Position = pos;
        Velocity = Vector3.zero;
        MoveSpeed = 0f;
        IsMoving = false;
    }

    // 物理情報の更新
    public void updatePhysicalInfo(Vector3 pos, Vector3 v)
    {
        Position = pos;
        Velocity = v;
        MoveSpeed = Velocity.magnitude;
        IsMoving = MoveSpeed > 0.1f;
    }
}

// ボールの状態
[System.Serializable]
public class PlayerBallState
{
    // ボールを所持しているか
    public bool HasBall { get; private set; }
    // ボールまでの距離
    public float BallDistance { get; private set; }
    // 自分から見たボールの方向
    public Vector3 BallDirection { get; private set; }

    // 初期化
    public void init()
    {
        HasBall = false;
        BallDistance = 0f;
        BallDirection = Vector3.zero;
    }

    // ボール情報の更新
    public void updateBallInfo(bool hasBall, float distance, Vector3 direction)
    {
        HasBall = hasBall;
        BallDistance = distance;
        BallDirection = direction;
    }
}

[System.Serializable]
public class PlayerSkillState
{
    // public float Stamina { get; private set; }                    // 残りスタミナ
    // public float MaxStamina { get; private set; }                 // 最大スタミナ
    // public float StaminaRecoveryRate { get; private set; }        // スタミナ回復率
    // public float PassAccuracy { get; private set; }               // パス精度
    // public float ShootPower { get; private set; }                 // シュート力
    // public float DribbleSpeed { get; private set; }               // ドリブル速度
    // public float TackleRange { get; private set; }                // タックル範囲

    // // 初期化
    // public void init()
    // {
    //     Stamina = 100f;
    //     MaxStamina = 100f;
    //     StaminaRecoveryRate = 5f;
    //     PassAccuracy = 0.8f;
    //     ShootPower = 0.7f;
    //     DribbleSpeed = 5f;
    //     TackleRange = 2f;
    // }
}

// 戦術
[System.Serializable]
public class PlayerTacticalState
{
    // // マークしているキャラクター
    // public GameObject MarkTarget { get; private set; }
    // // 最適な位置
    // public Vector3 OptimalPosition { get; private set; }
    // // フォーメーション内にいるか?
    // public bool IsInFormation { get; private set; }

    // public Vector3 OpenSpacePosition { get; private set; }        // 空いているスペースの位置
    // public float FormationDeviation { get; private set; }         // フォーメーションからの逸脱度
    // public bool IsMarking { get; private set; }                   // マークしているか
    // public bool IsSupporting { get; private set; }                // サポートしているか

    // 初期化
    public void init()
    {
        // MarkTarget = null;
        // OptimalPosition = Vector3.zero;
        // IsInFormation = false;
        // OpenSpacePosition = Vector3.zero;
        // FormationDeviation = 0f;
        // IsMarking = false;
        // IsSupporting = false;
    }
}

// 周辺環境
[System.Serializable]
public class PlayerSurroundingInfo
{
    //// 近くの味方
    //public List<GameObject> NearbyTeammates { get; private set; } = new();
    //// 近くの敵
    //public List<GameObject> NearbyEnemies { get; private set; } = new();
    //// プレッシャーレベル
    //public float PressureLevel { get; private set; }

    //public float NearestEnemyDistance { get; private set; }       // 最も近い敵との距離
    //public Vector3 NearestEnemyDirection { get; private set; }    // 最も近い敵への方向
    //public bool IsUnderPressure { get; private set; }             // プレッシャーを受けているか

    //public void init()
    //{
    //    NearbyTeammates.Clear();
    //    NearbyEnemies.Clear();
    //    PressureLevel = 0f;
    //    NearestEnemyDistance = float.MaxValue;
    //    NearestEnemyDirection = Vector3.zero;
    //    IsUnderPressure = false;
    //}
}

// 行動状態
[System.Serializable]
public class PlayerActionState
{
    // アクション実行中か?
    // public bool IsExecutingAction { get; private set; }
    // // 実行中のアクション名
    // public string CurrentAction { get; private set; }
    // アクション実行割合
    public float ActionProgress { get; private set; }
    // スタン状態か?
    public bool IsStunned { get; private set; }
    // // スタンの時間
    // public float StunTime { get; private set; }

    // 初期化
    public void init()
    {
        // IsExecutingAction = false;
        // CurrentAction = "";
        ActionProgress = 0f;
        IsStunned = false;
        // StunTime = 0f;
    }

    // アクション進行割合の設定
    public void SetActionProgress(float progress)
    {
        ActionProgress = progress;
    }
}

// 戦術判断
[System.Serializable]
public class PlayerDerivedMetrics
{
    //// パスの成功確率
    //public float PassSuccessRate { get; private set; }
    //// シュートの成功確率
    //public float ShootSuccessRate { get; private set; }
    //// タックル成功確率
    //public float TackleSuccessRate { get; private set; }

    //public Vector3 BestPassTarget { get; private set; }           // 最適なパス先
    //public Vector3 BestShootPosition { get; private set; }        // 最適なシュート位置
    //public float GoalDistance { get; private set; }               // ゴールまでの距離
    //public float OwnGoalDistance { get; private set; }            // 自陣ゴールまでの距離

    //// 初期化
    //public void init()
    //{
    //    PassSuccessRate = 0f;
    //    ShootSuccessRate = 0f;
    //    TackleSuccessRate = 0f;
    //    BestPassTarget = Vector3.zero;
    //    BestShootPosition = Vector3.zero;
    //    GoalDistance = 0f;
    //    OwnGoalDistance = 0f;
    //}
}

[System.Serializable]
public class PlayerTimeControlInfo
{
    //public float LastActionTime { get; private set; }             // 最後のアクション時間
    //public float LastPassTime { get; private set; }               // 最後のパス時間
    //public float LastShootTime { get; private set; }              // 最後のシュート時間
    //public float PossessionTime { get; private set; }             // ボール保持時間

    //// 初期化
    //public void init()
    //{
    //    LastActionTime = 0f;
    //    LastPassTime = 0f;
    //    LastShootTime = 0f;
    //    PossessionTime = 0f;
    //}
}
