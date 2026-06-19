using UnityEngine;
using System.Collections.Generic;

// アニマルの持つSpritに関する情報
public class AnimalSpritInfo : MonoBehaviour
{
    [SerializeField] private Param_AnimalInfo.AnimalType _animalType = Param_AnimalInfo.AnimalType.None;
    [SerializeField] private Param_SpritData _paramSpritData;
    public Param_SpritData ParamSpritData => _paramSpritData;
    private SaveData_Sprit[] _spritLevels;

    public Param_AnimalInfo.AnimalType AnimalType => _animalType;
    public SaveData_Sprit[] SpritLevels => _spritLevels;

    /// <summary>
    /// 指定パラメーターに対する魂補正値を取得する（plus加算 / minus減算）
    /// </summary>
    public int GetSpritModifier(Param_SpritData.ParameterType type)
    {
        SaveData_Sprit[] levels = _spritLevels;
        if (levels == null || levels.Length == 0)
        {
            LoadSpritFromES3();
            levels = _spritLevels;
            if (levels == null || levels.Length == 0)
            {
                return 0;
            }
        }

        int modifier = 0;
        for (int i = 0; i < levels.Length; i++)
        {
            SaveData_Sprit data = levels[i];
            if (data == null)
            {
                continue;
            }

            if ((Param_SpritData.ParameterType)data.plusSpritType == type)
            {
                modifier += data.plusSpritValue;
            }

            if ((Param_SpritData.ParameterType)data.minusSpritType == type)
            {
                modifier -= data.minusSpritValue;
            }
        }

        return modifier;
    }

    /// <summary>
    /// 外部で更新した魂データ配列を、このインスタンスの保持データへ反映する。
    /// </summary>
    public void SetSpritLevels(SaveData_Sprit[] spritLevels)
    {
        _spritLevels = spritLevels;
    }

    private void Start()
    {
        LoadSpritFromES3();
    }

    /// <summary>
    /// 設定された AnimalType に対応する魂データ配列を ES3 から読み込む。
    /// initData_Sprit の構造（None を除いた AnimalType 順）に従う。
    /// </summary>
    public void LoadSpritFromES3()
    {
        if (_animalType == Param_AnimalInfo.AnimalType.None)
        {
            _spritLevels = null;
            return;
        }

        string key = DataKey.DATAKEY_GAME_INFO_SPRIT + DataKey.LIST_ARRAY_SD_SPRIT_SETTING;
        List<SaveData_Sprit[]> spritList = ES3.Load(key, new List<SaveData_Sprit[]>());
        if (spritList == null || spritList.Count == 0)
        {
            _spritLevels = null;
            return;
        }

        int index = (int)_animalType - 1; // None を除いたインデックス
        if (index < 0 || index >= spritList.Count)
        {
            _spritLevels = null;
            return;
        }

        _spritLevels = spritList[index];

        // _spritLevelsの内容をデバッグ表示（クラス名とオブジェクト名も表示）
        string objectName = gameObject != null ? gameObject.name : "(null)";
        string className = nameof(AnimalSpritInfo);
        if (_spritLevels != null)
        {
            for (int i = 0; i < _spritLevels.Length; i++)
            {
                var data = _spritLevels[i];
                Debug.Log($"[{className}:{objectName}] AnimalType={_animalType} LevelIdx={i} PlusType={data.plusSpritType}, MinusType={data.minusSpritType}");
            }
        }
        else
        {
            Debug.Log($"[{className}:{objectName}] AnimalType={_animalType} の _spritLevels がnullです");
        }


    }
}
