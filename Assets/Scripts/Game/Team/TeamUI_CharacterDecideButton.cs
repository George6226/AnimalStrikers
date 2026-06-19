using UnityEngine;
using UnityEngine.UI;

public class TeamUI_CharacterDecideButton : MonoBehaviour
{
    // 選択された動物の種類
    private Param_AnimalInfo.AnimalType selectedAnimalType;
    // コールバック対象の選択ボタン
    private Team_CharacterSelectButton callbackTarget;
    // ボタンコンポーネント
    private Button button;

    // 選択された動物の種類を取得
    public Param_AnimalInfo.AnimalType SelectedAnimalType => selectedAnimalType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupButton();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// ボタンのクリックイベントを設定
    /// </summary>
    private void SetupButton()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogWarning("[TeamUI_CharacterDecideButton] Button コンポーネントが見つかりません。");
        }
    }

    /// <summary>
    /// ボタンがクリックされた時の処理
    /// </summary>
    private void OnButtonClicked()
    {
        // コールバック対象に選択された動物タイプを更新
        if (callbackTarget != null)
        {
            callbackTarget.UpdateAnimalType(selectedAnimalType);
            Debug.Log($"[TeamUI_CharacterDecideButton] 決定ボタンが押されました。動物タイプを更新: {selectedAnimalType}");
        }
        else
        {
            Debug.LogWarning("[TeamUI_CharacterDecideButton] callbackTarget が設定されていません。");
        }
    }

    /// <summary>
    /// 選択された動物の種類を設定する
    /// </summary>
    /// <param name="animalType">選択された動物の種類</param>
    public void SetSelectedAnimalType(Param_AnimalInfo.AnimalType animalType)
    {
        selectedAnimalType = animalType;
        Debug.Log($"[TeamUI_CharacterDecideButton] 選択された動物タイプを設定しました: {animalType}");
    }

    /// <summary>
    /// コールバック対象の選択ボタンを設定する
    /// </summary>
    /// <param name="target">コールバック対象の選択ボタン</param>
    public void SetCallbackTarget(Team_CharacterSelectButton target)
    {
        callbackTarget = target;
    }
}
