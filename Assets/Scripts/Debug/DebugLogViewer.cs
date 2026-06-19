using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// デバッグログの表示
public class DebugLogViewer : MonoBehaviour
{
	#region Singleton
	// インスタンス
	private static DebugLogViewer _instance;
	public static DebugLogViewer Instance
	{
		get
		{
			// インスタンス
			if (_instance == null)
			{
				_instance = (DebugLogViewer)FindObjectOfType(typeof(DebugLogViewer));

				if (_instance == null)
				{
					Debug.LogError(typeof(DebugLogViewer) + "is nothing");
				}
			}
			return _instance;
		}
	}
	#endregion Singleton

	// スクロール/コンテンツ
	[SerializeField] private GameObject _content;
	[SerializeField] private GameObject _prefab;

	// デバッグログの追加
	public void addDebugLog(string log)
    {
		Debug.Log(log);

		//　プレハブを生成
		GameObject obj = Instantiate(_prefab, _content.transform);
		obj.transform.SetParent(_content.transform);

		//log.Length

		// テキストの変更
		TextMeshProUGUI txt = obj.GetComponent<TextMeshProUGUI>();
		txt.text = log;
    }
}
