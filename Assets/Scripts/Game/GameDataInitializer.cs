using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム開始時の各種データを初期化するクラス。
/// Photonのマッチング処理とは分離し、アタッチされたオブジェクトのOnEnableで実行される。
/// </summary>
public class GameDataInitializer : MonoBehaviour
{
    private void OnEnable()
    {
        // デフォルト値を保存
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.INT_REMAINING_GAME_TIME, ConstData.TIME_GAME);
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.INT_TEAM_SCORE, 0);
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.INT_ENEMY_SCORE, 0);
        
        // PLAYER側のデータを初期化
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.LIST_VECTOR3_CHARACTER_POSITION_PLAYER, new List<Vector3>());
        
        // NPC側のデータを初期化
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.LIST_VECTOR3_CHARACTER_POSITION_NPC, new List<Vector3>());
        
        // PLAYER側のチーム編成データが存在しない場合、デフォルト値で初期化
        string playerFormationKey = DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_PLAYER;
        if (!ES3.KeyExists(playerFormationKey))
        {
            List<Param_AnimalInfo.AnimalType> defaultPlayerFormation = new List<Param_AnimalInfo.AnimalType>
            {
                Param_AnimalInfo.AnimalType.Lion,
                Param_AnimalInfo.AnimalType.Gorilla,
                Param_AnimalInfo.AnimalType.Boar,
                Param_AnimalInfo.AnimalType.Bear,
            };
            ES3.Save(playerFormationKey, defaultPlayerFormation);
        }

        Debug.Log($"[GameDataInitializer] 保存した PLAYER側のチーム編成データ: {ES3.Load<List<Param_AnimalInfo.AnimalType>>(playerFormationKey)}");
        
        // NPC側のチーム編成データが存在しない場合、デフォルト値で初期化
        string npcFormationKey = DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_NPC;
        List<Param_AnimalInfo.AnimalType> defaultNPCFormation = new List<Param_AnimalInfo.AnimalType>
        {
        };
        ES3.Save(npcFormationKey, defaultNPCFormation);

        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.VECTOR3_BALL_POSITION, Vector3.zero);
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.INT_BALL_OWNER, -1);
    }
}

