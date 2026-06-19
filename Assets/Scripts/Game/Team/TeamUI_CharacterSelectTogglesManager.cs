using UnityEngine;
using System.Collections.Generic;

public class TeamUI_CharacterSelectTogglesManager : MonoBehaviour
{
    // キャラクタ選択セレクターのリスト
    [SerializeField] private List<TeamParam_CharacterSelector> characterSelectors;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// キャラクタが選択された時に呼ばれる
    /// </summary>
    /// <param name="animalType">選択された動物の種類</param>
    public void OnCharacterSelected(Param_AnimalInfo.AnimalType animalType)
    {
        Debug.Log($"[TeamUI_CharacterSelectTogglesManager] キャラクタが選択されました: {animalType}");
        
        if (characterSelectors == null)
        {
            Debug.LogWarning("[TeamUI_CharacterSelectTogglesManager] characterSelectors が設定されていません。");
            return;
        }

        // リストからAnimalTypeが一致するセレクターを見つけて、ToggleをONにする
        // OnToggleValueChangedが自動的に呼ばれる
        foreach (var selector in characterSelectors)
        {
            if (selector == null) continue;

            if (selector.AnimalType == animalType)
            {
                // 同じAnimalTypeなら、ToggleをONにする
                // これによりOnToggleValueChangedが自動的に呼ばれる
                var toggleComponent = selector.GetComponent<UnityEngine.UI.Toggle>();
                if (toggleComponent != null)
                {
                    toggleComponent.isOn = true;
                    Debug.Log($"[TeamUI_CharacterSelectTogglesManager] {animalType} のトグルをONにしました。");
                }
                break; // 一致するものを見つけたら終了
            }
        }
    }
}
