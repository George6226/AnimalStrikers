using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 動物の情報リスト(パラメーター用)
public class ParamList_AnimalInfo : ScriptableObject
{
    // 動物の情報リスト
    [SerializeField] private List<AnimalInfoStruct> _animalInfoList;

    // 動物の情報構造体
    [System.Serializable]
    public struct AnimalInfoStruct
    {
        // 動物の名前
        [SerializeField] private string _animalName;
        // Param_AnimalInfo を取得
        [SerializeField] private Param_AnimalInfo _paramAnimalInfo;
        public Param_AnimalInfo ParamAnimalInfo => _paramAnimalInfo;
    }

    // 動物の情報を取得
    public Param_AnimalInfo GetAnimalInfo(Param_AnimalInfo.AnimalType animalType)
    {
        return _animalInfoList.Find(info => info.ParamAnimalInfo.InfoParam.AnimalType == animalType).ParamAnimalInfo;
    }
}
