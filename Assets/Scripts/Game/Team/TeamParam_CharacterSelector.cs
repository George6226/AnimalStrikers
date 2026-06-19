using UnityEngine;
using UnityEngine.UI;

// チームパラメーターシーンのキャラクタ選択
// このクラスを Toggle にアタッチして使用する
public class TeamParam_CharacterSelector : MonoBehaviour
{
    [SerializeField] private Toggle toggle;                                   // このキャラ用のトグル
    [SerializeField] private TeamParam_SpritCharacterViewer viewer;                // モデル表示用ビューア
    [SerializeField] private TeamParam_CharacterParameterViewer paramViewer;  // パラメーター表示用ビューア
    [SerializeField] private TeamParam_CharacterSpritSettingManager spritsSetting; // 複数魂設定コンテナ
    [SerializeField] private Param_AnimalInfo.AnimalType animalType;     // このトグルが表す動物タイプ
    [SerializeField] private TeamUI_CharacterSelectToggle characterSelectToggle; // キャラクタ選択トグルUI
    [SerializeField] private TeamUI_CharacterDecideButton decideButton;         // 決定ボタン

    // AnimalType を外部から取得するためのプロパティ
    public Param_AnimalInfo.AnimalType AnimalType => animalType;

    private void Awake()
    {
        // Inspector で未設定なら、同じ GameObject 上の Toggle を自動取得
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }
    }

    private void OnEnable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);

            // 有効化時点で既に ON なら、初期状態を反映する
            if (toggle.isOn)
            {
                OnToggleValueChanged(true);
            }
        }
    }

    private void OnDisable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }

    /// <summary>
    /// Toggle の ON / OFF が変わったときに呼ばれる
    /// </summary>
    /// <param name="isOn">トグルが ON になったかどうか</param>
    private void OnToggleValueChanged(bool isOn)
    {
        // キャラクタ選択トグルUIの更新
        if (characterSelectToggle != null)
        {
            Debug.Log("キャラクタ選択トグルUIの更新: " + isOn + " " + animalType.ToString());
            characterSelectToggle.changeToggle(isOn);
        }

        // ON のときだけ処理する
        if (!isOn) return;

        // キャラクタモデルの表示切り替え
        if (viewer != null)
        {
            viewer.ChangeCharacterDisplay(animalType);
        }

        // 魂スロット更新
        if (spritsSetting != null)
        {
            spritsSetting.ApplySprit(animalType);
        }

        // パラメーター表示の更新
        if (paramViewer != null)
        {
            paramViewer.ShowParameters(animalType);
        }

        // 決定ボタンに選択された動物タイプを保持
        if (decideButton != null)
        {
            decideButton.SetSelectedAnimalType(animalType);
        }
    }
}
