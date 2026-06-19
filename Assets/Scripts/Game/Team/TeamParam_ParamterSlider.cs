using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeamParam_ParamterSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;                // UI のスライダー
    [SerializeField] private Param_SpritData.ParameterType parameterType;  // このスライダーが表すパラメータ種別
    [SerializeField] private float maxValue = 100f;        // 正規化用の最大値
    [Header("Number Text Creator")]
    [SerializeField] private Transform numberParent;       // 数字画像を並べる親
    [SerializeField] private string numberFileName = "number"; // NumberTextCreator 用のファイル名
    private NumberTextInfo numberTextInfo;    // 表示設定（任意）
    private GameObject currentNumberObject;                // 生成した数字オブジェクト

    public Param_SpritData.ParameterType Type => parameterType;

    private void Start()
    {
        // NumberTextInfo が未設定なら生成し、右寄せに設定
        if (numberTextInfo == null)
        {
            numberTextInfo = new NumberTextInfo();
        }
        numberTextInfo.TextAlign = NumberTextInfo.TEXT_ALIGN.ALIGN_RIGHT;
    }

    public void SetValue(float value)
    {
        // 値に10を加算し、スライダーを10～110の範囲で表示
        float adjusted = value + 10f;
        if (adjusted < 10f) adjusted = 10f;
        if (adjusted > 110f) adjusted = 110f;

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 110f;
            slider.value = adjusted;
        }

        // 数字表示（NumberTextCreator を利用）
        if (NumberTextCreator.Instance != null)
        {
            // 既存をクリア
            if (currentNumberObject != null)
            {
                Destroy(currentNumberObject);
                currentNumberObject = null;
            }

            var parent = numberParent != null ? numberParent.gameObject : this.gameObject;
            currentNumberObject = NumberTextCreator.Instance.createNumberImageObjectList(
                numberFileName,
                value.ToString(),   // 調整前の値をそのまま表示
                parent,
                numberTextInfo
            );
        }
    }
}
