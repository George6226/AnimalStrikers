using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour {

	/// </summary>

	#region Singleton
	private static BGMManager _instanceBGMManager;
	public static BGMManager InstanceBGMManager {
		get {
			if (_instanceBGMManager == null) {
				_instanceBGMManager = (BGMManager)FindObjectOfType (typeof(BGMManager));
				if (_instanceBGMManager == null) {
					Debug.LogError (typeof(BGMManager) + "is nothing");
				}
			}
			return _instanceBGMManager;
		}
	}
	#endregion Singleton

	private bool _load = false;
	public bool Load
    {
        get { return _load; }
    }

	public void Awake ()
	{
		//シングルトンチェック
		if (this != InstanceBGMManager) {
			Destroy (this.gameObject);
			return;
		}
		//DontDestroyOnLoad (this.gameObject);

		_myAudioSources = GetComponents<AudioSource> ();
		_load = true;

	}

	private AudioSource[] _myAudioSources;
	private bool _isAudio1Playing = false; ///intで見た時再生中のAudioSource配列番号が帰る
	private float[] _audioLastTime = {0,0};	//音楽の途切れた時間。
	public UnityEngine.Audio.AudioMixer _myMixer;	//オーディオミキサー

	void Start(){
	}

	//マスターボリュームを調整。デシベルなので-80(無音)~0(デフォ)~20(爆音)の範囲。
	public void SetMasterVolume(float Volume){
		_myMixer.SetFloat ("MasterVolume", Volume);
	}

	public void PauseBGM(bool isPause){
		//再生中の音源番号
		int PlayingAudioNum = _isAudio1Playing ? 1 : 0;
		if (isPause == true) {
			_myAudioSources [PlayingAudioNum].Pause();
		} else {
			_myAudioSources [PlayingAudioNum].UnPause();
		}
	}

	//再生する音楽、クロスフェードにかかる時間、音楽を頭から再生するか
	public void PlayBGM(AudioClip Clip, float FadeTime,bool isReset){
		StartCoroutine (PlayBGMCoroutine (Clip, FadeTime,isReset));
	}
	IEnumerator PlayBGMCoroutine(AudioClip Clip, float FadeTime, bool isReset){
		//再生中の音源番号
		int PlayingAudioNum = _isAudio1Playing ? 1 : 0;
		//再生先の音源番号
		int DesiredAudioNum = !_isAudio1Playing ? 1 : 0;
		_myAudioSources [DesiredAudioNum].loop = true;
		//クリップ登録。クリップ内容が残っているものと同じ＆頭から再生モードではない場合、前回の終了時間を入力してから再生開始
		if (_myAudioSources [DesiredAudioNum].clip != null && _myAudioSources [DesiredAudioNum].clip.name == Clip.name) {
			if (isReset == false) {
				_myAudioSources [DesiredAudioNum].time = _audioLastTime [DesiredAudioNum];
			} else {
				_myAudioSources [DesiredAudioNum].time = 0f;
			}
		} else {
			_myAudioSources [DesiredAudioNum].time = 0f;
			_myAudioSources [DesiredAudioNum].clip = Clip;
		}
		_myAudioSources [DesiredAudioNum].Play ();
		//クロスフェード実行
		for (float f = 0; f < FadeTime; f += Time.deltaTime) {
			_myAudioSources [DesiredAudioNum].volume = f / FadeTime;
			_myAudioSources [PlayingAudioNum].volume = 1 - f / FadeTime;
			yield return null;
		}
		_myAudioSources [DesiredAudioNum].volume = 1.0f;
		_myAudioSources [PlayingAudioNum].volume = 0;
		_audioLastTime [PlayingAudioNum] = _myAudioSources [PlayingAudioNum].time;
		_myAudioSources [PlayingAudioNum].Stop ();
		//再生中の音源を交代
		_isAudio1Playing = !_isAudio1Playing;
		yield return null;
	}

	public void StopBGM(float fadeTime)
    {
		StartCoroutine(StopBGMCoroutine(fadeTime));
	}
	IEnumerator StopBGMCoroutine(float fadeTime)
    {
		//再生中の音源番号
		int PlayingAudioNum = _isAudio1Playing ? 1 : 0;
		// 音量
		float volume = _myAudioSources[PlayingAudioNum].volume;

		float sec = 0.0f;

		do
		{
			sec += Time.deltaTime;
			sec = Mathf.Clamp(sec, 0.0f, fadeTime);
			volume = 1.0f - sec / fadeTime;

			_myAudioSources[PlayingAudioNum].volume = volume;

			yield return null;

		} while (sec < fadeTime);

		_myAudioSources[PlayingAudioNum].Stop();
	}

	//イントロありのBGMを流す
	public void StartBGMWithIntro(AudioClip IntroClip, AudioClip MainClip){
		int PlayingNum = _isAudio1Playing ? 1 : 0;
		_audioLastTime [PlayingNum] = _myAudioSources [PlayingNum].time;
		_myAudioSources [PlayingNum].volume = 0f;
		_myAudioSources [PlayingNum].Stop ();
		int DesiredAudioNum = !_isAudio1Playing ? 1 : 0;
		_myAudioSources [2].clip = IntroClip;
		_myAudioSources [2].loop = false;
		_myAudioSources [2].Play ();
		_myAudioSources [DesiredAudioNum].clip = MainClip;
		_myAudioSources [DesiredAudioNum].loop = true;
		_myAudioSources [DesiredAudioNum].volume = 1.0f;
 		_myAudioSources [DesiredAudioNum].PlayScheduled (AudioSettings.dspTime + IntroClip.length);
		_isAudio1Playing = !_isAudio1Playing;
	}

	public IEnumerator PlayJingle(AudioClip Jingle){
		_myAudioSources [0].Stop ();
		_myAudioSources [1].Stop ();
		_myAudioSources [2].clip = Jingle;
		float WaitSecond = Jingle.length;
		_myAudioSources [2].Play ();
		yield return new WaitForSeconds (WaitSecond);
	}
}
