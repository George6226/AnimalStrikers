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

    // スペシャルゲージの増える量(0.0f ~ 1.0f)
    public static float SPECIAL_GAUGE_VALUE = 0.1f;

    // ダッシュの倍率
    public static float DASH_MULTIPLIER = 1.5f;

    // シュート精度ブレ角（Shoot=0 のときの最大角度）
    public static float MAX_SHOOT_SPREAD_ANGLE = 25.0f;

    // パス精度ブレ角（Pass=0 のときの最大角度）
    public static float MAX_PASS_SPREAD_ANGLE = 20.0f;

    // 待機時のHP回復量（毎秒）
    public static float STAND_HEAL_PER_SECOND = 20.0f;

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
