using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;//追加　シーンマネージャ用
using UnityEngine.EventSystems;

/// <summary>
/// シーン遷移時のフェードイン・アウトを制御するためのクラス .
/// </summary>
public class FadeManager : MonoBehaviour
{

	#region Singleton

	private static FadeManager instance;

	public static FadeManager Instance {
		get {
			if (instance == null) {
				instance = (FadeManager)FindObjectOfType (typeof(FadeManager));
				
				if (instance == null) {
					Debug.LogError (typeof(FadeManager) + "is nothing");
				}
			}
			
			return instance;
		}
	}

	#endregion Singleton

	/// <summary>
	/// デバッグモード .
	/// </summary>
	public bool DebugMode = true;
	/// <summary>フェード中の透明度</summary>
	private float fadeAlpha = 0;
	/// <summary>フェード中かどうか</summary>
	public bool isFading = false;
	// フェードの頂点にきた
	public bool isFadeMax = false;
	/// <summary>フェード色</summary>
	public Color fadeColor = Color.black;

	public void OnGUI ()
	{
	
		// Fade .
		if (this.isFading) {
			//色と透明度を更新して白テクスチャを描画 .
			this.fadeColor.a = this.fadeAlpha;
			GUI.color = this.fadeColor;
			GUI.depth = 0;
			GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); 
		}		

	}

	/// <summary>
	/// 画面遷移 .
	/// </summary>
	/// <param name='scene'>シーン名</param>
	/// <param name='interval'>暗転にかかる時間(秒)</param>
	public void LoadLevel (string scene, float ExitInterval, float EnterInterval)
	{
		StartCoroutine (TransScene (scene, ExitInterval, EnterInterval));
	}

	/// <summary>
	/// シーン遷移用コルーチン .
	/// </summary>
	/// <param name='scene'>シーン名</param>
	/// <param name='interval'>暗転にかかる時間(秒)</param>
	private IEnumerator TransScene (string scene, float ExitInterval , float EnterInterval)
	{
		//入力無効
		EventSystem.current.enabled = false;
		//だんだん暗く
		this.isFading = true;
		this.isFadeMax = false;

		float time = 0;
		if (ExitInterval > 0.0f) {
			while (this.fadeAlpha < 1f) {
				this.fadeAlpha = Mathf.Lerp (0f, 1f, time / ExitInterval);      
				time += Time.deltaTime;
				yield return 0;
			}
		} else {
			fadeAlpha = 0.0f;
		}
		Resources.UnloadUnusedAssets ();
		//遷移先シーン非同期ロード
		AsyncOperation async = SceneManager.LoadSceneAsync(scene);
		//ロード完了まで遷移禁止
		async.allowSceneActivation = false;
		yield return async.progress >= 0.9f;
		//遷移再開
		async.allowSceneActivation = true;
		yield return null;
		yield return async;
		this.isFadeMax = true;
		yield return null;

		// 秒数がある場合
		if (EnterInterval > 0) {
			//フェードイン
			if (this.fadeAlpha >= 1f) {
				//だんだん明るく .
				time = 0;
				while (time <= EnterInterval) {
					this.fadeAlpha = Mathf.Lerp (1f, 0f, time / EnterInterval);
					time += Time.deltaTime;
					yield return 0;
				}
		
				this.isFading = false;
				this.isFadeMax = false;
			}
		} else {
			this.isFading = false;
			this.isFadeMax = false;
			this.fadeAlpha = 0.0f;
		}
		if (EventSystem.current != null) {
			EventSystem.current.enabled = true;
		}
	}
}

