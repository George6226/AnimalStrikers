using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 定数
public static class ConstData
{ 
    // 広告サイズ
    public static float AREA_ADVERTISE = 100.0f;

    // デバッグモードか?
    public enum TYPE_DEBUG
    {
        DEFAULT = 0,
        DEBUG,
    }

    // 方向の種類
    public enum DIR_KIND
    {
        NONE = 0,
        UP = 1,
        DOWN = 2,
        RIGHT = 4,
        LEFT = 8,
    }

    // バトルモード
    public enum BATTLE_MODE
    {
        NONE = 0,
        NORMAL,
        NPC,
    }

    // ピンチの最大量
    public static float MAX_PINCH_VALUE = 10.0f;

    // キャラのタグの定数
    public const string PLAYER_TAG  = "PlayerAgent";
    public const string ENEMY_TAG   = "EnemyAgent";
    public const string BALL_TAG    = "Ball";
    public const string NPC_TAG     = "NPC";
    public const string WALL_TAG    = "Wall";

    /// <summary>壁オブジェクト用 Unity レイヤー名（Layer 設定と一致させる）</summary>
    public const string WALL_LAYER_NAME = "Wall";

    // 試合時間(180秒)
    public static int TIME_GAME = 180;

    // スペシャルゲージの増える量(0.0f ~ 1.0f)。シュート成功・被ダメ時。
    // 0.25 → おおよそ 4 回で満タン（試合終了前に F5 目視しやすい）。
    public static float SPECIAL_GAUGE_VALUE = 0.25f;

    /// <summary>パス成功時のスペシャルゲージ増分（シュートより控えめ）。</summary>
    public static float SPECIAL_GAUGE_VALUE_ON_PASS = 0.12f;

    // ダッシュの倍率
    public static float DASH_MULTIPLIER = 1.5f;

    // シュート精度ブレ角（Shoot=0 のときの最大角度）
    public static float MAX_SHOOT_SPREAD_ANGLE = 32.0f;

    /// <summary>ゴール口半幅（GK 位置取り・シュート狙いと共通）。</summary>
    public const float GOAL_MOUTH_HALF_WIDTH = 3.5f;

    /// <summary>自ゴール前の GK 積極拾いエリア深さ（ゴールラインからフィールド方向）。</summary>
    public const float GK_GOAL_AREA_DEPTH = 6f;

    /// <summary>GK がホームラインから前に出られる最大距離。</summary>
    public const float GK_RUSH_FORWARD_DEPTH = 2.5f;

    /// <summary>シュートをロフト（上方向）にする確率（製品プレイ時）。</summary>
    public const float SHOOT_LOFT_CHANCE = 0.45f;

    /// <summary>シュート狙いをゴール口端（GK がいない側のポスト）寄りにする比率。</summary>
    public const float SHOOT_OPEN_POST_RATIO = 0.92f;

    /// <summary>ロフトシュートの最大到達高度。</summary>
    public const float SHOOT_LOFT_MAX_HEIGHT = 4.0f;

    /// <summary>GK がジャンプパリーアニメを選ぶボール高さ（GK 基準）。</summary>
    public const float GK_SAVE_HIGH_BALL_HEIGHT = 1.15f;

    /// <summary>GK 配球の最短待機（秒）。</summary>
    public const float GK_DISTRIBUTION_MIN_DELAY = 0.75f;

    /// <summary>GK 配球の最長待機（秒）。超えたら条件未達でもパス。</summary>
    public const float GK_DISTRIBUTION_MAX_DELAY = 2.0f;

    /// <summary>守備 GK として扱う、攻撃ゴールからの最大距離。</summary>
    public const float GK_DEFEND_GOAL_MAX_DISTANCE = 12f;

    /// <summary>パスの最大距離（それ以上は選定・キック双方で抑制）。</summary>
    public const float MAX_PASS_DISTANCE = 14f;

    /// <summary>移動中受け手にはこの距離以下なら地上パスを優先。</summary>
    public const float PASS_GROUND_PREFER_MAX_DISTANCE = 11f;

    /// <summary>GK シュート時の前出しはゴール前この深度まで（FREE より浅い）。</summary>
    public const float GK_SHOOT_RUSH_MAX_DEPTH = 3.5f;

    /// <summary>味方 GK スポーン位置（自ゴールライン Z=-20 からフィールド側へ）。</summary>
    public const float GK_SPAWN_DEPTH_ALLY = 3.5f;

    /// <summary>敵 GK スポーン位置（敵ゴールライン Z=+20 からフィールド側へ。小さいほどゴール寄り）。</summary>
    public const float GK_SPAWN_DEPTH_ENEMY = 2.0f;

    // パス精度ブレ角（Pass=0 のときの最大角度）
    public static float MAX_PASS_SPREAD_ANGLE = 20.0f;

    // 待機時のスタミナ回復量（毎秒）
    public static float STAND_HEAL_PER_SECOND = 20.0f;

    /// <summary>通常移動時のスタミナ回復量（毎秒）。</summary>
    public static float STAMINA_NORMAL_MOVE_HEAL_PER_SECOND = 20.0f;

    /// <summary>ダッシュ移動時のスタミナ消費量（毎秒）。</summary>
    public static float STAMINA_DASH_DRAIN_PER_SECOND = 20.0f;

    /// <summary>スタミナ残量がこの比率以下で移動速度が低下し始める（F1）。</summary>
    public static float STAMINA_LOW_MOVE_RATIO_THRESHOLD = 0.25f;

    /// <summary>スタミナ枯渇時（比率 0）の移動速度倍率（F1）。</summary>
    public static float STAMINA_EXHAUSTED_MOVE_SPEED_MULTIPLIER = 0.55f;

    // HPのデフォルト値
    public static float DEFAULT_HP = 100.0f;

    // 通常攻撃の基本ダメージ
    public static float BASE_ATTACK_DAMAGE = 50.0f;

    // 通常攻撃ダメージの下限
    public static float MIN_ATTACK_DAMAGE = 10.0f;

    // スペシャル中の固定攻撃ダメージ
    public static float SPECIAL_ATTACK_DAMAGE = 99999.0f;

    /// <summary>バトルフィールドサイズ（Z軸・ゴール間の長さ）。<see cref="TeamFieldInfo.Initialize"/> の第1引数と一致。</summary>
    public const float FIELD_SIZE_Z = 40f;

    /// <summary>バトルフィールドサイズ（X軸・幅）。<see cref="TeamFieldInfo.Initialize"/> の第2引数と一致。</summary>
    public const float FIELD_SIZE_X = 14f;
}
