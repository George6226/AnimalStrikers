using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// サウンドボタンの操作を行う
public class SoundButtonHandler : MonoBehaviour {

	// OFF画像
	[SerializeField] private Image _offImage;

	// トグル/ボリューム
	private Toggle _toggle;
	private float _volume;

	// 初期化
	void Awake()
	{
		_toggle = GetComponent<Toggle> ();
		_volume = ES3.Load<float> (DataKey.DATAKEY_GAME_INFO + DataKey.FLOAT_SOUND, 0.0f);

		// ボリュームがない場合
		if (_volume >= 0) {
			_offImage.enabled = false;
			_toggle.isOn = true;
		} else {
			_offImage.enabled = true;
			_toggle.isOn = false;
		}
	}

	// サウンドボタンが押される
	public void clickSoundButton(bool change)
	{
		// Toggle(true) = OFF画像(false)
		_offImage.enabled = !_toggle.isOn;
		_volume = (_toggle.isOn) ? 0 : -80;
		ES3.Save<float> (DataKey.DATAKEY_GAME_INFO + DataKey.FLOAT_SOUND,_volume);
		// 全体のボリューム設定
		//SoundManager.Instance.SetMasterVolume (_volume);
	}
}
