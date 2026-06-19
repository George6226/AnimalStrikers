using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// アニマルのスペシャルゲージ UI を制御するクラス。
/// AnimalSelector_Player から選択中アニマルの変更通知を受け取り、
/// 対象アニマルの SpecialValue を AnimalAction_Gauge に反映する。
/// </summary>
public class UI_SpecialGauge : MonoBehaviour
{
    [SerializeField] private Image _gaugeImage;          // ゲージ画像

    // ゲージを更新する
    public void UpdateGauge(float value)
    {
        _gaugeImage.fillAmount = value;
    }
}

