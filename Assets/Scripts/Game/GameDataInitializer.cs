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
        EnsureMatchSessionData();
    }

    /// <summary>
    /// MainMenu を経由しない GameScene 直 Play（GOAP バッチ CI 等）向けの初期化。
    /// </summary>
    public static void EnsureMatchSessionData(bool forceDefaultFormation = false)
    {
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.INT_REMAINING_GAME_TIME, ConstData.TIME_GAME);
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.INT_TEAM_SCORE, 0);
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.INT_ENEMY_SCORE, 0);
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.LIST_VECTOR3_CHARACTER_POSITION_PLAYER, new List<Vector3>());
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.LIST_VECTOR3_CHARACTER_POSITION_NPC, new List<Vector3>());

        string playerFormationKey = DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_PLAYER;
        if (forceDefaultFormation || !ES3.KeyExists(playerFormationKey))
        {
            ES3.Save(playerFormationKey, CreateDefaultPlayerFormation());
        }

        string npcFormationKey = DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_NPC;
        if (forceDefaultFormation || !ES3.KeyExists(npcFormationKey))
        {
            ES3.Save(npcFormationKey, CreateDefaultNpcFormation());
        }

        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.VECTOR3_BALL_POSITION, Vector3.zero);
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.INT_BALL_OWNER, -1);

        Debug.Log(
            $"[GameDataInitializer] match session ready formation={string.Join(", ", ES3.Load<List<Param_AnimalInfo.AnimalType>>(playerFormationKey))}");
    }

    private static List<Param_AnimalInfo.AnimalType> CreateDefaultPlayerFormation() =>
        new()
        {
            Param_AnimalInfo.AnimalType.Lion,
            Param_AnimalInfo.AnimalType.Gorilla,
            Param_AnimalInfo.AnimalType.Boar,
            Param_AnimalInfo.AnimalType.Bear,
        };

    private static List<Param_AnimalInfo.AnimalType> CreateDefaultNpcFormation() =>
        new()
        {
            Param_AnimalInfo.AnimalType.Crocodile,
            Param_AnimalInfo.AnimalType.Shark,
            Param_AnimalInfo.AnimalType.Elephant,
            Param_AnimalInfo.AnimalType.Tiger,
        };
}
