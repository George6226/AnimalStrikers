using Photon.Realtime;
using ExitGames.Client.Photon;

// Photonの拡張プレイヤープロパティ
public static class PhotonPlayerPropertiesExtensions
{
    // バトルモード
    private const string BATTLE_MODE = "BattleMode";
    // シーン読み込み完了したか?
    private const string IS_SCENE_LOADED = "IsSceneLoaded";
    // キャラクタ位生成完了したか?
    private const string IS_CHARACTER_SPAWNED = "IsCharacterSpawned";

    // バトルモードを取得する
    public static ConstData.BATTLE_MODE getBattleMode(this Player player)
    {
        // カスタムプロパティがない
        if (player.CustomProperties == null) return ConstData.BATTLE_MODE.NONE;
        // カスタムプロパティから値を取得
        if (player.CustomProperties.TryGetValue(BATTLE_MODE, out var value) && value is int mode)
        {
            return (ConstData.BATTLE_MODE)mode;
        }
        return ConstData.BATTLE_MODE.NONE;
    }

    // バトルモードを設定する
    public static void setBattleMode(this Player player, ConstData.BATTLE_MODE mode)
    {
        // カスタムプロパティに値を設定
        var propsToSet = new Hashtable { { BATTLE_MODE, (int)mode } };
        player.SetCustomProperties(propsToSet);
    }

    // シーン読み込み完了を取得する
    public static bool getIsSceneLoaded(this Player player)
    {
        if (player.CustomProperties == null) return false;
        if (player.CustomProperties.TryGetValue(IS_SCENE_LOADED, out var value) && value is bool isLoaded)
        {
            return isLoaded;
        }
        return false;
    }

    // シーン読み込み完了を設定する
    public static void setIsSceneLoaded(this Player player, bool isLoaded)
    {
        var propsToSet = new Hashtable { { IS_SCENE_LOADED, isLoaded } };
        player.SetCustomProperties(propsToSet);
    }

    // キャラクター生成完了を取得する
    public static bool getIsCharacterSpawned(this Player player)
    {
        if (player.CustomProperties == null) return false;
        if (player.CustomProperties.TryGetValue(IS_CHARACTER_SPAWNED, out var value) && value is bool isSpawned)
        {
            return isSpawned;
        }
        return false;
    }

    // キャラクター生成完了を設定する
    public static void setIsCharacterSpawned(this Player player, bool isSpawned)
    {
        var propsToSet = new Hashtable { { IS_CHARACTER_SPAWNED, isSpawned } };
        player.SetCustomProperties(propsToSet);
    }

    // 再マッチ用に前戦の同期フラグをクリアする
    public static void resetMatchProperties(this Player player)
    {
        var propsToSet = new Hashtable
        {
            { BATTLE_MODE, (int)ConstData.BATTLE_MODE.NONE },
            { IS_SCENE_LOADED, false },
            { IS_CHARACTER_SPAWNED, false }
        };
        player.SetCustomProperties(propsToSet);
    }
}