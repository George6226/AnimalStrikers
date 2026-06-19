using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スペシャルゲージなど、アクション用ゲージの管理を担当するクラス。
/// </summary>
public class AnimalAction_Gauge : MonoBehaviour
{
    // 現在のゲージ値（0.0f〜1.0f）
    [Range(0.0f, 1.0f)]
    private float _gaugeValue = 0.0f;
    public float GaugeValue => _gaugeValue;

    // ゲージの更新があるかどうか(初期値false)
    private bool _updateGauge = false;
    public bool UpdateGauge => _updateGauge;


    /// <summary>
    /// ゲージ値を設定し、UI に反映する。
    /// 0.0f〜1.0f にクランプされる。
    /// </summary>
    /// <param name="value">加算するゲージ値（0.0f〜1.0f）</param>
    public void AddGaugeValue(float value)
    {
        _gaugeValue += value;
        _gaugeValue = Mathf.Clamp01(_gaugeValue);
        _updateGauge = true;
    }

    /// <summary>
    /// ゲージを 0 にリセットする。
    /// </summary>
    public void ResetGauge()
    {
        _gaugeValue = 0.0f;
        _updateGauge = true;
    }

    // ゲージの更新完了
    public void UpdateGaugeComplete()
    {
        _updateGauge = false;
    }
}

