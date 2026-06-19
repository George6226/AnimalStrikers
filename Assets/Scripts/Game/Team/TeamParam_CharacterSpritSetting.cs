using UnityEngine;
using UnityEngine.UI;

// 魂設定によるパラメーターの変更
public class TeamParam_CharacterSpritSetting : MonoBehaviour
{
    // この魂のレベル
    [SerializeField] private int level;
    // コスト
    [SerializeField] private int cost;
    // 魂の画像
    [SerializeField] private Image spritImage;
    // プラス魂とマイナス魂のタイプ用のテキスト変数を追加
    [SerializeField] private TMPro.TextMeshProUGUI plusTypeText;
    [SerializeField] private TMPro.TextMeshProUGUI minusTypeText;
    // コスト表示用のText
    [SerializeField] private TMPro.TextMeshProUGUI costText;

    // コスト切り替え用のGameObject
    [SerializeField] private GameObject costSwitchObject;
    // 魂パラメーター切り替え用のGameObject
    [SerializeField] private GameObject spritParamSwitchObject;
    // 魂タイプ情報（ScriptableObject）
    [SerializeField] private Param_SpritData spritData;
    // 現在選択されているプラス／マイナス魂タイプ
    [SerializeField] private Param_SpritData.ParameterType currentPlusType;
    [SerializeField] private Param_SpritData.ParameterType currentMinusType;

    // プロパティで外部から取得可能にする
    public Param_SpritData.ParameterType CurrentPlusType => currentPlusType;
    public Param_SpritData.ParameterType CurrentMinusType => currentMinusType;
    public int Level => level;
    public int Cost => cost;
    // このスロットが持つ補正値（+/- どちらも同じ値で、符号は使用側で制御）
    public int SpritValue => level * 5;
    [SerializeField] private TeamParam_CharacterSpritSettingManager characterSprits;
    // 魂変更用のボタン
    [SerializeField] private Button changeButton;

    void Start()
    {
        costText.text = "コスト: " + cost.ToString();
        UpdateCostDisplay();

        Debug.Log("コスト:" + cost.ToString());

        // ボタンのイベントを設定
        if (changeButton != null)
        {
            changeButton.onClick.AddListener(OnButtonClicked);
            Debug.Log("ボタンのイベントを設定しました :level=" + level.ToString());
        }
    }

    void OnDestroy()
    {
        // ボタンのイベントを解除
        if (changeButton != null)
        {
            changeButton.onClick.RemoveListener(OnButtonClicked);
        }
    }

    /// <summary>
    /// ボタンが押されたときに呼ばれる関数
    /// </summary>
    public void OnButtonClicked()
    {
        if (characterSprits != null)
        {
            characterSprits.ChangeSpritByLevel(level);
        }
    }

    /// <summary>
    /// 画像の有無でコスト表示と魂パラメーター表示を切り替える
    /// </summary>
    private void UpdateCostDisplay()
    {
        bool showCost = (currentPlusType == Param_SpritData.ParameterType.None);
        if (costSwitchObject != null) costSwitchObject.SetActive(showCost);
        if (spritParamSwitchObject != null) spritParamSwitchObject.SetActive(!showCost);
    }

    /// <summary>
    /// 魂（Sprit）を変更する
    /// </summary>
    /// <param name="newSpritSprite">新しい魂画像（Sprite）</param>
    /// <param name="plusType">プラス魂のタイプ</param>
    /// <param name="minusType">マイナス魂のタイプ</param>
    public void ChangeSprit(Param_SpritData.ParameterType plusType, Param_SpritData.ParameterType minusType)
    {
        currentPlusType = plusType;
        currentMinusType = minusType;

        if (spritImage != null && spritData != null)
        {
            
            spritImage.sprite = spritData.GetParameterTypeSprite(currentPlusType);
            Debug.Log($"ChangeSprit: CurrentPlusType={currentPlusType} sprite={spritImage.sprite.name} level={level}");
        }

        // 画像（Sprite）変更時にコスト表示の切り替えを行う
        UpdateCostDisplay();

        // plusTypeおよびminusTypeに関するテキストを更新
        UpdateSpritTypeTexts(plusType, minusType);

        // plusTypeおよびminusTypeに関する関連処理（例: デバッグ表記）
        Debug.Log($"ChangeSprit: PlusType={plusType}, MinusType={minusType}");
    }

    /// <summary>
    /// プラス・マイナス魂タイプが変更された際にテキストを変更する
    /// </summary>
    private void UpdateSpritTypeTexts(Param_SpritData.ParameterType plusType, Param_SpritData.ParameterType minusType)
    {
        int value = level * 5;

        if (plusTypeText != null)
        {
            string plusName = spritData != null ? spritData.GetParameterTypeName(plusType) : plusType.ToString();
            plusTypeText.text = $"{plusName}+{value}";
        }
        if (minusTypeText != null)
        {
            string minusName = spritData != null ? spritData.GetParameterTypeName(minusType) : minusType.ToString();
            minusTypeText.text = $"{minusName}-{value}";
        }
    }
}
