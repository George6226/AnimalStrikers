using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundOnOffButtonHandler : MonoBehaviour
{
	// OFF/Onボタン
	[SerializeField] private Toggle _offButton;
	[SerializeField] private Toggle _onButton;

	// トグル/ボリューム
	private float _volume;
	// 初期化
	private bool _init = false;

	// 初期化
	void Awake()
	{
		_volume = ES3.Load<float>(DataKey.DATAKEY_GAME_INFO + DataKey.FLOAT_SOUND);
		Debug.Log("_volume:" + _volume);
		// ボリュームがない場合
		if (_volume >= 0)
		{
			
			_offButton.isOn = false;
			_onButton.isOn = true;
		}
		else
		{
			_onButton.isOn = false;
			_offButton.isOn = true;
		}

		Debug.Log("_offButton:" + _offButton.isOn + " _onButton]"+ _onButton.isOn);

		_init = true;
	}

	// サウンドボタンが押される
	public void clickSoundButton(bool change)
	{
        if (!_init){
			return;
        }

		_volume = (_offButton.isOn) ? -80 : -0;

		// Toggle(true) = OFF画像(false)
		//b.isOn = true;
		
		ES3.Save<float>(DataKey.DATAKEY_GAME_INFO + DataKey.FLOAT_SOUND,_volume);
		// 全体のボリューム設定
		//SoundManager.Instance.SetMasterVolume(_volume);
	}
}
