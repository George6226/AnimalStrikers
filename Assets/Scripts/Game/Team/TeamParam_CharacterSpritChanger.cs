using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeamParam_CharacterSpritChanger : MonoBehaviour
{
    // 現在の魂設定を保持する変数
    private TeamParam_CharacterSpritSetting spritSetting = null;
    public TeamParam_CharacterSpritSetting SpritSetting{
        set{
            spritSetting = value;
            // 設定が変更されたら画像を更新
            UpdateSpritImage();
            // ボタンの表示も更新
            UpdateCoinButtonText();
            Debug.Log("sprit設定:"+spritSetting);
        }
    }

    // 魂画像表示用のImage
    [SerializeField] private Image spritImage;
    // 魂タイプ情報（ScriptableObject）
    [SerializeField] private Param_SpritData spritData;
    // セーブ反映用のコンテナ
    [SerializeField] private TeamParam_CharacterSpritSettingManager spritsSetting;
    // パラメーター表示ビューア
    [SerializeField] private TeamParam_CharacterParameterViewer parameterViewer;
    // コイン表示用ボタンテキスト
    [SerializeField] private TextMeshProUGUI coinButtonText;
    // コインボタン本体
    [SerializeField] private Button coinButton;

    private void OnEnable()
    {
//         UpdateSpritImage();
    }

    /// <summary>
    /// spritSetting のプラスタイプから画像を取得して表示を更新
    /// </summary>
    private void UpdateSpritImage()
    {
        if (spritSetting == null || spritImage == null || spritData == null)
            return;

        var plusType = spritSetting.CurrentPlusType;
        // Param_SpritData から画像を取得
        Sprite sprite = spritData.GetParameterTypeSprite(plusType);
        if (sprite != null)
        {
            spritImage.sprite = sprite;
        }
    }

    /// <summary>
    /// コインボタンのテキストと色を更新する
    /// </summary>
    private void UpdateCoinButtonText()
    {
        if (coinButtonText == null || spritSetting == null)
            return;

        int cost = spritSetting.Cost;
        coinButtonText.text = cost.ToString();

        // コイン所持数に応じて色を変更
        if (CoinManager.Instance == null)
        {
            // CoinManager がなければデフォルト（白）
            coinButtonText.color = Color.white;
            return;
        }

        int currentCoin = CoinManager.Instance.CurrentCoin;
        bool canAfford = currentCoin >= cost;
        coinButtonText.color = canAfford ? Color.white : Color.red;

        // 足りないときはボタンを押せないようにする
        if (coinButton != null)
        {
            coinButton.interactable = canAfford;
        }
    }

    /// <summary>
    /// ボタンで呼ばれる：現在設定されている魂を外す（リセット）
    /// </summary>
    public void RemoveSprit()
    {
        if (spritSetting != null)
        {
            // None を設定することで魂を外す
            spritSetting.ChangeSprit(
                Param_SpritData.ParameterType.None,
                Param_SpritData.ParameterType.None
            );
            UpdateSpritImage();
            Debug.Log("魂を外しました");

            // セーブデータ更新
            int levelIndex = Mathf.Max(0, spritSetting.Level - 1);
            spritsSetting.SaveCurrentAnimalSprit(levelIndex);

            // パラメーター再描画
            if (parameterViewer != null)
            {
                parameterViewer.ShowParameters(spritsSetting.CurrentAnimalType);
            }
        }
        else
        {
            Debug.LogWarning("RemoveSprit: spritSetting が設定されていません");
        }
    }

    /// <summary>
    /// ボタンで呼ばれる：魂を変更する（選択画面を開くなど）
    /// </summary>
    public void ChangeSprit()
    {
        if (spritSetting != null)
        {
            // ParameterType の enum 値の配列を取得
            var allTypes = System.Enum.GetValues(typeof(Param_SpritData.ParameterType));
            
            // None 以外のタイプをリストに追加
            var availableTypes = new System.Collections.Generic.List<Param_SpritData.ParameterType>();
            foreach (Param_SpritData.ParameterType type in allTypes)
            {
                if (type != Param_SpritData.ParameterType.None)
                {
                    availableTypes.Add(type);
                }
            }
            
            // ランダムに2つ選ぶ（重複なし）
            if (availableTypes.Count >= 2)
            {
                int index1 = Random.Range(0, availableTypes.Count);
                int index2 = Random.Range(0, availableTypes.Count);
                
                // 同じインデックスが選ばれた場合は再抽選
                while (index2 == index1)
                {
                    index2 = Random.Range(0, availableTypes.Count);
                }
                
                var plusType = availableTypes[index1];
                var minusType = availableTypes[index2];
                
                // 魂を変更
                spritSetting.ChangeSprit(plusType, minusType);
                Debug.Log($"魂を変更しました: PlusType={plusType}, MinusType={minusType}");

                // コインを消費
                if (CoinManager.Instance != null)
                {
                    CoinManager.Instance.UseCoin(spritSetting.Cost);
                }
                // コイン表示を更新
                UpdateCoinButtonText();
                // 画像を更新
                UpdateSpritImage();

                // セーブデータ更新
                int levelIndex = Mathf.Max(0, spritSetting.Level - 1);
                spritsSetting.SaveCurrentAnimalSprit(levelIndex);

                // パラメーター再描画
                if (parameterViewer != null)
                {
                    parameterViewer.ShowParameters(spritsSetting.CurrentAnimalType);
                }
            }
            else
            {
                Debug.LogWarning("ChangeSprit: 利用可能なパラメータタイプが2つ未満です");
            }
        }
        else
        {
            Debug.LogWarning("ChangeSprit: spritSetting が設定されていません");
        }
    }
}

