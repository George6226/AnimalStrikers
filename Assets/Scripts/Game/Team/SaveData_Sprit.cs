using System;
using UnityEngine;

// 魂（Sprit）のセーブデータ
[Serializable]
public class SaveData_Sprit
{
    // プラス魂の種類/値
    public int plusSpritType;
    public int plusSpritValue;
    // マイナス魂の種類/値
    public int minusSpritType;
    public int minusSpritValue;
}