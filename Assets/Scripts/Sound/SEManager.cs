using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 効果音の管理
public class SEManager : MonoBehaviour {

	// サウンドリスト
	[SerializeField] private List<AudioClip> _clipList = new List<AudioClip>();
	// オーディオ
	[SerializeField] private AudioSource _audioSource;

	// サウンドエフェクトを鳴らす(公開)
	public void playSoundEffect(string clipName)
	{
		AudioClip clip = getClip (clipName);
		if (clip == null) {
			return;
		}

		//Debug.Log("soundならす:" + clip.name);

        // SEを鳴らす
        _audioSource.PlayOneShot (clip);
	}

    // サウンドを流す
    public void playSound(string clipName, bool loop = true)
    {
        AudioClip clip = getClip(clipName);
        if(clip == null){
            return;
        }

        // 設定/ループ/再生
        _audioSource.clip = clip;
        _audioSource.loop = loop;
        _audioSource.Play();
    }

    // サウンドを止める
    public void stopSound()
    {
        _audioSource.Stop();
    }

    // クリップを取得する
    private AudioClip getClip(string name)
	{
		for (int i = 0; i < _clipList.Count; i++) {
			if (_clipList [i].name == name) {
				return _clipList [i];
			}
		}

		return null;
	}
}
