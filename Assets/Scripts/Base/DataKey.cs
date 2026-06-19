using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DataKey {

    // PlayerPrefs
    // ゲームのバージョン
    public static string DATAKEY_INT_GAME_VERSION = "BaibaiDatakeyIntGameVersion";
    public static string TREASURE_INIT_VERSION = "?tag=treasureInitVersion";
    public static string ALIENS_INIT_VERSION = "?tag=aliensInitVersion";

    /******************* 共通データキー *********************/

    // サウンド(ボリューム) [サウンドのON/OFFに使用]
    public static string FLOAT_SOUND = "?tag=soundVolume";
    public static string FLOAT_BGM = "?tag=bgmVolume";

    // ゲーム情報
    public static string DATAKEY_GAME_INFO = "BaibaiDatakeyGameInfo";
    public static string DATAKEY_GAME_INFO_SPRIT = "BaibaiDatakeyGameInfoSprit";

    // 残り試合時間
    public static string INT_REMAINING_GAME_TIME = "?tag=remainingGameTime";
    // 味方スコア
    public static string INT_TEAM_SCORE = "?tag=teamScore";
    // 敵スコア
    public static string INT_ENEMY_SCORE = "?tag=enemyScore";
    // 所持コイン
    public static string INT_COIN = "?tag=coin";
    // キャラクタAnimalTypeリスト（統合済み：ARRAY_INT_TEAM_FORMATION_PLAYER/NPCを使用）
    // public static string LIST_ANIMALTYPE_CHARACTER_NAME = "?tag=characterNameList";
    // ボールの位置
    public static string VECTOR3_BALL_POSITION = "?tag=ballPosition";
    // ボールの所持者
    public static string INT_BALL_OWNER = "?tag=ballOwner";

    //魂セッティング
    public static string LIST_ARRAY_SD_SPRIT_SETTING = "?tag=spritSetting";
    
    // PLAYER側のキャラクタ配置リスト
    public static string LIST_VECTOR3_CHARACTER_POSITION_PLAYER = "?tag=characterPositionListPlayer";
    // PLAYER側のチーム編成
    public static string ARRAY_INT_TEAM_FORMATION_PLAYER = "?tag=teamFormationPlayer";
    
    // NPC側のキャラクタ配置リスト
    public static string LIST_VECTOR3_CHARACTER_POSITION_NPC = "?tag=characterPositionListNPC";
    // NPC側のチーム編成
    public static string ARRAY_INT_TEAM_FORMATION_NPC = "?tag=teamFormationNPC";
}
