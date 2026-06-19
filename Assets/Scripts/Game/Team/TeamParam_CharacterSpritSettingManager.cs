using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 複数の TeamParam_CharacterSpritSetting をまとめて制御するコンテナ。
/// TeamParam_CharacterSelector.OnToggleValueChanged から呼び出して使用することを想定。
/// </summary>
public class TeamParam_CharacterSpritSettingManager : MonoBehaviour
{
    [SerializeField] private List<TeamParam_CharacterSpritSetting> spritSettings = new();
    [SerializeField] private TeamParam_SpritCharacterViewer spritCharacterViewer;
    [SerializeField] private TeamParam_AnimalSpritWritter animalSpritWritter;
    [SerializeField] private TeamParam_CharacterSpritChanger spritChanger; // 魂変更処理を行うコンポーネント
    // 現在選択されている動物タイプ
    private Param_AnimalInfo.AnimalType currentAnimalType = Param_AnimalInfo.AnimalType.None;

    /// <summary>
    /// 外部（パラメーター表示など）から魂スロット一覧を参照するためのプロパティ
    /// </summary>
    public List<TeamParam_CharacterSpritSetting> SpritSettings => spritSettings;

    /// <summary>
    /// 現在選択されている動物タイプを外部から参照するためのプロパティ
    /// </summary>
    public Param_AnimalInfo.AnimalType CurrentAnimalType => currentAnimalType;

    /// <summary>
    /// 指定された動物タイプに応じた魂データをすべてのスロットに適用する。
    /// </summary>
    public void ApplySprit(Param_AnimalInfo.AnimalType animalType)
    {
        currentAnimalType = animalType;

        int levelMax = 3; // initData_Sprit で作成したレベル数
        if (spritCharacterViewer == null)
        {
            Debug.LogWarning("ApplySprit: spritCharacterViewer が設定されていません");
            return;
        }

        AnimalSpritInfo spritInfo = spritCharacterViewer.GetSpritInfo(animalType);
        if (spritInfo == null)
        {
            Debug.LogWarning($"ApplySprit: AnimalType={animalType} の AnimalSpritInfo が見つかりません");
            return;
        }

        // 常に最新データを反映するため読み直してから参照
        spritInfo.LoadSpritFromES3();
        SaveData_Sprit[] spritArray = spritInfo.SpritLevels;
        if (spritArray == null || spritArray.Length < levelMax)
            return;

        // 各スロット（レベル）にデータを適用
        for (int i = 0; i < spritSettings.Count && i < spritArray.Length; i++)
        {
            // saveData（spritArray[i]）からプラス・マイナスタイプを取得
            SaveData_Sprit saveData = spritArray[i];
            Param_SpritData.ParameterType plusType = (Param_SpritData.ParameterType)saveData.plusSpritType;
            Param_SpritData.ParameterType minusType = (Param_SpritData.ParameterType)saveData.minusSpritType;
            spritSettings[i].ChangeSprit(plusType, minusType);
        }
    }

    /// <summary>
    /// 現在選択中の動物タイプに紐づく魂セーブデータを更新する。
    /// </summary>
    public void SaveCurrentAnimalSprit(int index)
    {
        if (currentAnimalType == Param_AnimalInfo.AnimalType.None)
        {
            Debug.LogWarning("SaveCurrentAnimalSprit: 動物タイプが選択されていません");
            return;
        }
        if (animalSpritWritter == null)
        {
            Debug.LogWarning("SaveCurrentAnimalSprit: animalSpritWritter が設定されていません");
            return;
        }

        // 既存 API（単体保存）を使って指定インデックス相当のスロットのみ保存
        for (int i = 0; i < spritSettings.Count; i++)
        {
            TeamParam_CharacterSpritSetting setting = spritSettings[i];
            if (setting == null)
            {
                continue;
            }
            int levelIndex = Mathf.Max(0, setting.Level - 1);
            if (levelIndex != index)
            {
                continue;
            }
            animalSpritWritter.SaveFromSettings(currentAnimalType, setting);
            break;
        }

        // 保存後に再適用して表示中の情報を最新化
        ApplySprit(currentAnimalType);
        Debug.Log($"SaveCurrentAnimalSprit: Animal={currentAnimalType} を保存しました");
    }

    /// <summary>
    /// 指定されたレベルに対応する魂を変更する（現在選択されている動物タイプを使用）
    /// </summary>
    /// <param name="level">変更する魂のレベル（1-3）</param>
    public void ChangeSpritByLevel(int level)
    {
        if (currentAnimalType == Param_AnimalInfo.AnimalType.None)
        {
            Debug.LogWarning("ChangeSpritByLevel: 動物タイプが選択されていません");
            return;
        }
        spritChanger.SpritSetting = spritSettings[level - 1];
    }
}
