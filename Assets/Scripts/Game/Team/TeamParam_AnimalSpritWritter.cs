using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AnimalType ごとの魂（Sprit）データを書き込む専用クラス。
/// ES3 の LIST_ARRAY_SD_SPRIT_SETTING を更新する。
/// </summary>
public class TeamParam_AnimalSpritWritter : MonoBehaviour
{
    [SerializeField] private TeamParam_SpritCharacterViewer spritCharacterViewer;
    [SerializeField] private int levelMax = 3;

    /// <summary>
    /// UI の魂スロット設定（単体）から SaveData_Sprit を作成し、指定動物タイプへ保存する。
    /// </summary>
    public void SaveFromSettings(Param_AnimalInfo.AnimalType animalType, TeamParam_CharacterSpritSetting spritSetting)
    {
        if (!TryValidateInput(animalType, spritSetting, out AnimalSpritInfo spritInfo))
        {
            return;
        }

        // データを更新して保存
        SaveData_Sprit[] buffer = BuildUpdatedSpritArray(spritInfo, spritSetting);
        SaveToEs3(animalType, buffer);
        // Viewer から取得した SpritInfo 側の保持データも同期更新
        spritInfo.SetSpritLevels(buffer);
    }

    // 入力を検証する
    private bool TryValidateInput(Param_AnimalInfo.AnimalType animalType, TeamParam_CharacterSpritSetting spritSetting, out AnimalSpritInfo spritInfo)
    {
        spritInfo = null;
        if (animalType == Param_AnimalInfo.AnimalType.None || spritSetting == null)
        {
            return false;
        }

        if (spritCharacterViewer == null)
        {
            Debug.LogWarning("SaveFromSettings: spritCharacterViewer が設定されていません");
            return false;
        }

        spritInfo = spritCharacterViewer.GetSpritInfo(animalType);
        if (spritInfo == null)
        {
            Debug.LogWarning($"SaveFromSettings: AnimalType={animalType} の SpritInfo が見つかりません");
            return false;
        }
        return true;
    }

    // データを更新して保存
    private SaveData_Sprit[] BuildUpdatedSpritArray(AnimalSpritInfo spritInfo, TeamParam_CharacterSpritSetting spritSetting)
    {
        // 既存データを読み込んでから該当レベルのみ更新
        spritInfo.LoadSpritFromES3();
        SaveData_Sprit[] current = spritInfo.SpritLevels;
        int neededLength = Mathf.Max(levelMax, spritSetting.Level);
        SaveData_Sprit[] buffer = new SaveData_Sprit[neededLength];
        for (int i = 0; i < neededLength; i++)
        {
            if (current != null && i < current.Length && current[i] != null)
            {
                buffer[i] = current[i];
            }
            else
            {
                buffer[i] = new SaveData_Sprit();
            }
        }

        int index = Mathf.Clamp(spritSetting.Level - 1, 0, buffer.Length - 1);
        SaveData_Sprit data = buffer[index];
        data.plusSpritType = (int)spritSetting.CurrentPlusType;
        data.minusSpritType = (int)spritSetting.CurrentMinusType;
        data.plusSpritValue = spritSetting.SpritValue;
        data.minusSpritValue = spritSetting.SpritValue;
        buffer[index] = data;
        return buffer;
    }

    // ES3 に保存
    private void SaveToEs3(Param_AnimalInfo.AnimalType animalType, SaveData_Sprit[] buffer)
    {
        // LIST_ARRAY_SD_SPRIT_SETTING を更新
        string key = DataKey.DATAKEY_GAME_INFO_SPRIT + DataKey.LIST_ARRAY_SD_SPRIT_SETTING;
        List<SaveData_Sprit[]> spritList = ES3.Load(key, new List<SaveData_Sprit[]>());
        int animalIndex = (int)animalType - 1; // None 除外
        while (spritList.Count <= animalIndex)
        {
            spritList.Add(new SaveData_Sprit[levelMax]);
        }
        spritList[animalIndex] = buffer;
        ES3.Save(key, spritList);
    }
}
