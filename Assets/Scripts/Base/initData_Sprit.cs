using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 魂（Sprit）用の初期化データ
/// </summary>
public class initData_Sprit : MonoBehaviour
{
    /// <summary>
    /// 魂データの初期化処理
    /// </summary>
    public static void InitSpritData(int version)
    {
        // バージョンに応じた初期化処理
        if (version < 100)
        {
            InitSpritData_100();
        }
    }

    /// <summary>
    /// バージョン1.00での魂データ初期化
    /// </summary>
    private static void InitSpritData_100()
    {
          // 初期データが存在しない場合のみ作成
          if (ES3.KeyExists(DataKey.DATAKEY_GAME_INFO_SPRIT + DataKey.LIST_ARRAY_SD_SPRIT_SETTING))
          {
              Debug.Log("魂データが存在します");
              return;
          }

          // Param_AnimalCharacter の AnimalType の種類数を取得する
          int animalTypeCount = EnumUtility.GetTypeNum<Param_AnimalInfo.AnimalType>();
          int levelMax = 3;
          // 一つの動物ごとに3つのレベルでデータを作成
          // Noneは除く（AnimalType.Noneをスキップ）
          // 動物ごとに配列を作成し、レベルごとにSaveData_Spritの配列を作成し、全体をES3に保存する
          List<SaveData_Sprit[]> spritList = new List<SaveData_Sprit[]>();
          for (int i = 0; i < animalTypeCount; i++)
          {
              if ((Param_AnimalInfo.AnimalType)i == Param_AnimalInfo.AnimalType.None)
                  continue;

              // レベルごとのSaveData_SpritのArrayを用意
              SaveData_Sprit[] spritLevelArray = new SaveData_Sprit[levelMax];
              for (int level = 1; level <= levelMax; level++)
              {
                  SaveData_Sprit defaultData = new SaveData_Sprit();
                  defaultData.plusSpritType = (int)Param_SpritData.ParameterType.None;
                  defaultData.plusSpritValue = 0;
                  defaultData.minusSpritType = (int)Param_SpritData.ParameterType.None;
                  defaultData.minusSpritValue = 0;
                  // Arrayのインデックスは0始まりなのでlevel-1に格納
                  spritLevelArray[level - 1] = defaultData;
              }
              // 各動物の配列をListに追加
              spritList.Add(spritLevelArray);
          }
          // 魂データのListを保存
          ES3.Save(DataKey.DATAKEY_GAME_INFO_SPRIT + DataKey.LIST_ARRAY_SD_SPRIT_SETTING, spritList);
          Debug.Log("魂データを初期化しました");
    }
}

