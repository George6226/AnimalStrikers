using UnityEngine;
using System.Collections;
using UnityEngine.UI;

// サウンドのスライダー
public class Sound_Slider : MonoBehaviour
{
    // BGMスライダー
    [SerializeField] private Slider _bgmSlider;
    // SEスライダー
    [SerializeField] private Slider _seSlider;
    // 初期化
    private bool _init = false;
    // 最小に聞こえるボリューム
    private float MIN_LISTEN_VOLUME = -20.0f;

    // 表示時
    private void OnEnable()
    {
        // 初めのサウンドを鳴らす
        float seVolume = PlayerPrefs.GetFloat(DataKey.FLOAT_SOUND);
        float bgmVolume = PlayerPrefs.GetFloat(DataKey.FLOAT_BGM);

        float perSE = 1.0f - seVolume / MIN_LISTEN_VOLUME;
        float perBGM = 1.0f - bgmVolume / MIN_LISTEN_VOLUME;

        if (seVolume <= -80.0f)
        {
            perSE = 0.0f;
        }
        if (bgmVolume <= -80.0f)
        {
            perBGM = 0.0f;
        }

        _seSlider.value = perSE;
        _bgmSlider.value = perBGM;

        _init = true;
    }

    // SEのボリュームスライダーを変更する
    public void changeSEValue(bool change)
    {
        if (!_init)
        {
            return;
        }

        if (change)
        {
            float value = (1.0f - _seSlider.value) * MIN_LISTEN_VOLUME;
            if (_seSlider.value <= 0.0f)
            {
                value = -80.0f;
            }

            PlayerPrefs.SetFloat(DataKey.FLOAT_SOUND, value);
        }
    }
    // SEのボリュームを変更終了
    public void endSEValueChange()
    {
        float value = (1.0f - _seSlider.value) * MIN_LISTEN_VOLUME;
        if (_seSlider.value <= 0.0f)
        {
            value = -80.0f;
        }

        SoundManager.Instance.setSEVolume(value);

        // 弾を打つサウンドを鳴らす
        SoundManager.Instance.ManagerSE.playSoundEffect("sound_05_rupture");
    }

    // SEのボリュームスライダーを変更する
    public void changeBGMValue(bool change)
    {
        if (!_init)
        {
            return;
        }

        if (change)
        {
            float value = (1.0f - _bgmSlider.value) * MIN_LISTEN_VOLUME;
            if (_bgmSlider.value <= 0.0f)
            {
                value = -80.0f;
            }

            PlayerPrefs.SetFloat(DataKey.FLOAT_BGM, value);

            //Debug.Log("changeBGM:" + value+ " _bgmSlider.value"+ _bgmSlider.value);

            SoundManager.Instance.SetBGMVolume(value);
        }
    }

    // SEのボリュームを変更終了
    public void endBGMValueChange()
    {
        float value = (1.0f - _bgmSlider.value) * MIN_LISTEN_VOLUME;
        if (_bgmSlider.value <= 0.0f)
        {
            value = -80.0f;
        }
    }
}
