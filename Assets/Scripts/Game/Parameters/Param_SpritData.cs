using UnityEngine;
using System.Collections.Generic;

public class Param_SpritData : ScriptableObject
{
    public enum ParameterType
    {
        None,
        Stamina,
        Speed,
        Shoot,
        Pass,
        Attack,
        Defense
    }

    [System.Serializable]
    public struct SpritTypeInfo
    {
        [SerializeField] private string name;
        [SerializeField] private ParameterType type;
        [SerializeField] private Sprite sprite;
        [SerializeField] private float baseParameterValue;
        [SerializeField] private float increaseParameterValue;

        public string Name => name;
        public ParameterType Type => type;
        public Sprite Sprite => sprite;
        public float BaseParameterValue => baseParameterValue;
        public float IncreaseParameterValue => increaseParameterValue;
    }

    // 名前・タイプ・画像をまとめた定義リスト
    [SerializeField] private List<SpritTypeInfo> spritTypeInfoList = new();

    /// <summary>
    /// ParameterTypeに応じた画像を取得する
    /// </summary>
    /// <param name="type">パラメーターの種類</param>
    /// <returns>対応するSprite(存在しない場合はnull)</returns>
    public Sprite GetParameterTypeSprite(ParameterType type)
    {
        // まず構造体リストから検索
        for (int i = 0; i < spritTypeInfoList.Count; i++)
        {
            if (spritTypeInfoList[i].Type == type)
            {
                return spritTypeInfoList[i].Sprite;
            }
        }

        return null;
    }

    
    /// <summary>
    /// パラメーターの名前を取得
    /// </summary>
    /// <param name="type">パラメーターの種類</param>
    /// <returns>パラメーターの名前</returns>
    public string GetParameterTypeName(ParameterType type)
    {
        // まず構造体リストから検索
        for (int i = 0; i < spritTypeInfoList.Count; i++)
        {
            if (spritTypeInfoList[i].Type == type && !string.IsNullOrEmpty(spritTypeInfoList[i].Name))
            {
                return spritTypeInfoList[i].Name;
            }
        }

        return "不明";
    }

    /// <summary>
    /// パラメーターの基礎値を取得
    /// </summary>
    /// <param name="type">パラメーターの種類</param>
    /// <returns>基礎値（未定義の場合は0）</returns>
    public float GetBaseParameterValue(ParameterType type)
    {
        for (int i = 0; i < spritTypeInfoList.Count; i++)
        {
            if (spritTypeInfoList[i].Type == type)
            {
                return spritTypeInfoList[i].BaseParameterValue;
            }
        }

        return 0f;
    }

    /// <summary>
    /// パラメーターの増加値を取得
    /// </summary>
    /// <param name="type">パラメーターの種類</param>
    /// <returns>増加値（未定義の場合は0）</returns>
    public float GetIncreaseParameterValue(ParameterType type)
    {
        for (int i = 0; i < spritTypeInfoList.Count; i++)
        {
            if (spritTypeInfoList[i].Type == type)
            {
                return spritTypeInfoList[i].IncreaseParameterValue;
            }
        }

        return 0f;
    }
}
