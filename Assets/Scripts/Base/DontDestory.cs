using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestory : MonoBehaviour {

	#region Singleton
	// インスタンス
	private static DontDestory _instance;
	public static DontDestory Instance {
		get {
			// インスタンス
			if (_instance == null) {
				_instance = (DontDestory)FindObjectOfType (typeof(DontDestory));

				if (_instance == null) {
					Debug.LogError (typeof(DontDestory) + "is nothing");
				}
			}
			return _instance;
		}
	}
	#endregion Singleton

	// Use this for initialization
	void Awake () {

		// すでに作成している場合
		if (this != Instance) {
			Debug.Log("DontDestory:Awake");
			Destroy (this.gameObject);
			return;
		}

		// このゲームオブジェクトを破棄しない
		DontDestroyOnLoad (this.gameObject);
	}
}
