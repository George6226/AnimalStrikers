using UnityEngine;
using System.Collections;

public class SceneFader : MonoBehaviour
{
	// 遷移先の名前
	[SerializeField] private string _sceneName;
	public string SceneName{
		get{ return _sceneName;}
	}
	// 終了時間
	[SerializeField] private float _exitTime;
	// 開始時間
	[SerializeField] private float _enterTime;

	// シーンを変更する
	public void changeScene ()
	{
		//フェード中に何度も実行されないように
		if (!FadeManager.Instance.isFading)
		{
			FadeManager.Instance.LoadLevel (_sceneName, _exitTime, _enterTime);
		}
	}

	// シーンを変更する
	public void changeScene(string sceneName)
	{
		//フェード中に何度も実行されないように
		if (!FadeManager.Instance.isFading)
		{
			FadeManager.Instance.LoadLevel (sceneName, _exitTime, _enterTime);
		}
	}
}
