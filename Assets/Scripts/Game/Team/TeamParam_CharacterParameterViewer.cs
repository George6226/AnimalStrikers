using UnityEngine;
using System.Collections.Generic;

// キャラクタのパラメーターの表示を行う
public class TeamParam_CharacterParameterViewer : MonoBehaviour
{
    [SerializeField] private TeamParam_ParamterSlider[] parameterSliders;    // パラメーター表示用のスライダー一覧
    [SerializeField] private ParamList_AnimalInfo paramAnimalInfoList;       // 動物キャラのパラメータ定義リスト
    [SerializeField] private TeamParam_CharacterSpritSettingManager spritsSetting; // 魂設定（プラス／マイナス補正）コンテナ

    /// <summary>
    /// 指定された動物タイプのパラメーターを取得して、スライダーに反映する
    /// </summary>
    public void ShowParameters(Param_AnimalInfo.AnimalType animalType)
    {
        if (paramAnimalInfoList == null)
        {
            Debug.LogWarning("[TeamParam_CharacterParameterViewer] paramAnimalInfoList が設定されていません。");
            return;
        }

        if (parameterSliders == null || parameterSliders.Length == 0)
        {
            Debug.LogWarning("[TeamParam_CharacterParameterViewer] parameterSliders が設定されていません。");
            return;
        }

        Param_AnimalInfo animalInfo;
        try
        {
            animalInfo = paramAnimalInfoList.GetAnimalInfo(animalType);
        }
        catch
        {
            Debug.LogWarning($"[TeamParam_CharacterParameterViewer] animalType={animalType} の Param_AnimalInfo が見つかりません。");
            return;
        }

        var param = animalInfo.InfoParam;
        var spritModifiers = CalculateSpritModifiers();

        // 各スライダーが持っているパラメータタイプにもとづいて値を設定する
        foreach (var slider in parameterSliders)
        {
            if (slider == null) continue;

            switch (slider.Type)
            {
                case Param_SpritData.ParameterType.Stamina:
                    slider.SetValue(param.Stamina + GetModifier(spritModifiers, Param_SpritData.ParameterType.Stamina));
                    break;
                case Param_SpritData.ParameterType.Speed:
                    slider.SetValue(param.Speed + GetModifier(spritModifiers, Param_SpritData.ParameterType.Speed));
                    break;
                case Param_SpritData.ParameterType.Shoot:
                    slider.SetValue(param.Shoot + GetModifier(spritModifiers, Param_SpritData.ParameterType.Shoot));
                    break;
                case Param_SpritData.ParameterType.Pass:
                    slider.SetValue(param.Pass + GetModifier(spritModifiers, Param_SpritData.ParameterType.Pass));
                    break;
                case Param_SpritData.ParameterType.Attack:
                    slider.SetValue(param.Attack + GetModifier(spritModifiers, Param_SpritData.ParameterType.Attack));
                    break;
                case Param_SpritData.ParameterType.Defense:
                    slider.SetValue(param.Defense + GetModifier(spritModifiers, Param_SpritData.ParameterType.Defense));
                    break;
            }
        }
    }

    /// <summary>
    /// spritSettings に設定されているプラス／マイナス値を種類ごとに合算する
    /// </summary>
    private Dictionary<Param_SpritData.ParameterType, int> CalculateSpritModifiers()
    {
        var result = new Dictionary<Param_SpritData.ParameterType, int>();

        if (spritsSetting == null || spritsSetting.SpritSettings == null)
        {
            return result;
        }

        foreach (var setting in spritsSetting.SpritSettings)
        {
            if (setting == null) continue;

            int value = setting.SpritValue;

            // プラス分
            var plusType = setting.CurrentPlusType;
            if (plusType != Param_SpritData.ParameterType.None)
            {
                if (!result.ContainsKey(plusType)) result[plusType] = 0;
                result[plusType] += value;
            }

            // マイナス分
            var minusType = setting.CurrentMinusType;
            if (minusType != Param_SpritData.ParameterType.None)
            {
                if (!result.ContainsKey(minusType)) result[minusType] = 0;
                result[minusType] -= value;
            }
        }

        return result;
    }

    /// <summary>
    /// 指定したパラメータータイプに対応する魂補正値を取得する
    /// </summary>
    private int GetModifier(Dictionary<Param_SpritData.ParameterType, int> dict, Param_SpritData.ParameterType type)
    {
        if (dict == null) return 0;
        if (dict.TryGetValue(type, out int value))
        {
            return value;
        }
        return 0;
    }
}
