using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// サウンドの制御
public class SoundManager : MonoBehaviour {

	// BGM/SEの管理
	[SerializeField] private BGMManager _mBGM;
	public BGMManager ManagerBGM{
		get{ return _mBGM;}
	}
	[SerializeField] private SEManager _mSE;
	public SEManager ManagerSE{
		get{ return _mSE;}
	}

	// 音全体を管理するミキサー
	[SerializeField] private UnityEngine.Audio.AudioMixer _seMixer;
	[SerializeField] private UnityEngine.Audio.AudioMixer _bgmMixer;
	// グローバルサウンド音量
	[SerializeField][Range(0.0f,1.0f)] private float _globalVolume = 0.1f;

    // インスペクターの変更
    private void OnValidate()
    {
#if UNITY_EDITOR
        AudioListener.volume = _globalVolume;
#endif
    }

	private void Start()
	{
		// 初めのサウンドを鳴らす
		float seVolume = PlayerPrefs.GetFloat(DataKey.FLOAT_SOUND);
		float bgmVolume = PlayerPrefs.GetFloat(DataKey.FLOAT_BGM);
		SoundManager.Instance.setSEVolume(seVolume);
		SoundManager.Instance.SetBGMVolume(bgmVolume);
	}

	// インスタンス(シングルトン)
	#region Singleton
	private static SoundManager _instance;
	public static SoundManager Instance {
		get {
			if (_instance == null) {
				_instance = (SoundManager)FindObjectOfType (typeof(SoundManager));
				if (_instance == null) {
					Debug.LogError (typeof(SoundManager) + "is nothing");
				}
			}
			return _instance;
		}
	}
	#endregion Singleton

	public void setSEVolume(float volume)
	{
		// 無音以下ならば
		if (volume <= -80.0f)
		{
			volume = -80.0f;
		}
		// 通常以上ならば * 20までいくと爆音
		else if (volume >= 0.0f)
		{
			volume = 0.0f;
		}

		// ボリューム設定
		_seMixer.SetFloat("MasterVolume", volume);
	}

	// 全体ボリュームの設定
	public void SetBGMVolume(float volume)
	{
		// 無音以下ならば
		if (volume <= -80.0f)
		{
			volume = -80.0f;
		}
		// 通常以上ならば * 20までいくと爆音
		else if (volume >= 0.0f)
		{
			volume = 0.0f;
		}

		//Debug.Log("Master Volume:" + volume);

		// ボリューム設定
		_bgmMixer.SetFloat("MasterVolume", volume);
	}
}
