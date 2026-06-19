using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// BGMを流す
public class BGMPlayer : MonoBehaviour {

	[SerializeField] private BGMManager _bgmManager;
	// BGM情報リスト
	[SerializeField] private List<StructBGMInfo> _bgmList;
	// 音源名を保存
	private string _keepClipName = "";

	[System.Serializable]
	public struct StructBGMInfo{
		// BGM
		[SerializeField] private AudioClip _bgm;
		public AudioClip BGM{
			get{ return _bgm;}
		}
		// シーン名のリスト
		[SerializeField] private List<string> _sceneNameList;
		public List<string> SceneNameList{
			get{ return _sceneNameList;}
		}
		// リセットを行うか?
		[SerializeField] private bool _isReset;
		public bool IsReset{
            get { return _isReset; }
        }
	}

	void Awake()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;

        //Debug.Log("BGMPlayerのAwake");
    }

	// シーンの読み込み時
	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		// BGM管理が読み込まれていない
        if (!_bgmManager.Load){
			return;
        }

		AudioClip clip = searchAudio (scene.name);

        // クリップなしならば
        if(clip == null){
            return;
        }

		Debug.Log("OnSceneLoaded:" + clip.name);
		// リセットを行うか?
		bool isReset = findIsReset(scene.name);

		// 違うBGMならば更新
		if (!_keepClipName.Equals (clip.name) || isReset)
		{
			Debug.Log("曲変更:" + clip.name + " isReset:" + isReset);
			_keepClipName = clip.name;
			SoundManager.Instance.ManagerBGM.PlayBGM (searchAudio (scene.name), 0.5f, isReset);
		}
	}

    private void OnDestroy()
    {
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

    // 音源を探す
    private AudioClip searchAudio(string sceneName)
	{
		for (int i = 0; i < _bgmList.Count; i++) {
			List<string> nameList = _bgmList [i].SceneNameList;

			// シーン名分探す
			for (int j = 0; j < nameList.Count; j++) 
			{
				if (nameList [j].Equals (sceneName)) {
					return _bgmList [i].BGM;
				}
			}
		}

		return null;
	}

	// リセットを行うか?
	private bool findIsReset(string sceneName)
    {
		for (int i = 0; i < _bgmList.Count; i++)
		{
			List<string> nameList = _bgmList[i].SceneNameList;

			// シーン名分探す
			for (int j = 0; j < nameList.Count; j++)
			{
				if (nameList[j].Equals(sceneName))
				{
					return _bgmList[i].IsReset;
				}
			}
		}

		return false;
	}
}
