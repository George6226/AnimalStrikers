using System.Collections.Generic;
using UnityEngine;

// チームパラメーターシーンのキャラクタ閲覧
public class TeamParam_SpritCharacterViewer : MonoBehaviour
{
    [SerializeField] private List<AnimalSpritInfo> spritInfos = new();   // キャラ用 SpritInfo の一覧

    /// <summary>
    /// AnimalType に対応する <see cref="AnimalSpritInfo"/> を取得する。
    /// </summary>
    public AnimalSpritInfo GetSpritInfo(Param_AnimalInfo.AnimalType animalType)
    {
        if (spritInfos == null || spritInfos.Count == 0)
        {
            return null;
        }

        foreach (var info in spritInfos)
        {
            if (info == null) continue;
            if (info.AnimalType == animalType)
            {
                return info;
            }
        }

        return null;
    }

    /// <summary>
    /// 指定した AnimalType に応じてキャラクタ表示を切り替える。
    /// </summary>
    public void ChangeCharacterDisplay(Param_AnimalInfo.AnimalType animalType)
    {
        AnimalSpritInfo target = GetSpritInfo(animalType);
        if (spritInfos == null || spritInfos.Count == 0)
        {
            Debug.LogWarning("[TeamParam_SpritCharacterViewer] spritInfos が設定されていません。");
            return;
        }

        foreach (var info in spritInfos)
        {
            if (info == null) continue;
            info.gameObject.SetActive(info == target);
        }

        if (target == null)
        {
            Debug.LogWarning($"[TeamParam_SpritCharacterViewer] AnimalType に一致するキャラクタが見つかりませんでした: {animalType}");
        }
    }
}


