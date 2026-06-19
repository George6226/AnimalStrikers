using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 固定テキストの管理
public class StaticTextManager : MonoBehaviour 
{
	// テキストリスト/判定リスト/場所リスト
	[SerializeField] private List<StructStaticText> _textList;
	[SerializeField] private List<StructContainText> _containList;
	[SerializeField] private List<StructTutorialText> _tutoList;

	// 固定テキスト
	[System.Serializable]
	public struct StructStaticText
	{
		// テキストキー
		[SerializeField] private StaticTextKey.TEXTKEY _textKey;
		public StaticTextKey.TEXTKEY TextKey{
			get{ return _textKey;}
		}
		// 日本語
		[SerializeField][MultilineAttribute] private string _japanText;
		public string JapanText{
			get{ return _japanText;}
		}
		// 英語
		[SerializeField][MultilineAttribute] private string _englishText;
		public string EnglishText{
			get{ return _englishText;}
		}
	}

	// 文字列判定テキスト
	[System.Serializable]
	public struct StructContainText
	{
		// 判定キー
		[SerializeField] private StaticTextKey.CONTAIN_KEY _containKey;
		public StaticTextKey.CONTAIN_KEY ContainKey{
			get{ return _containKey;}
		}
		// 日本語
		[SerializeField] private string _japanText;
		public string JapanText{
			get{ return _japanText;}
		}
		// 英語
		[SerializeField] private string _englishText;
		public string EnglishText{
			get{ return _englishText;}
		}
	}

	// チュートリアルテキスト
	[System.Serializable]
	public struct StructTutorialText
	{
		// 判定キー
		[SerializeField] private StaticTextKey.TUTORIAL_KEY _tutoKey;
		public StaticTextKey.TUTORIAL_KEY TutorialKey{
			get{ return _tutoKey;}
		}
		// 日本語
		[MultilineAttribute][SerializeField] private string _japanText;
		public string JapanText{
			get{ return _japanText;}
		}
		// 英語
		[MultilineAttribute][SerializeField] private string _englishText;
		public string EnglishText{
			get{ return _englishText;}
		}
	}

	#region Singleton
	// インスタンス
	private static StaticTextManager _instance;
	public static StaticTextManager Instance {
		get {
			// インスタンス
			if (_instance == null) {
				_instance = (StaticTextManager)FindObjectOfType (typeof(StaticTextManager));

				if (_instance == null) {
					Debug.LogError (typeof(StaticTextManager) + "is nothing");
				}
			}
			return _instance;
		}
	}
	#endregion Singleton

	// テキストを取得する
	public string getText(StaticTextKey.TEXTKEY key)
	{
		// Indexを検索/範囲外判定
		int index = searchIndex (key);
		if (index < 0 || index >= _textList.Count) {
			return "";
		}

		// TODO:日本語以外の場合
		return _textList [index].JapanText;
	}

	// テキストを取得する
	public string getText(StaticTextKey.CONTAIN_KEY key)
	{
		// Indexを検索/範囲外判定
		int index = searchIndex (key);
		if (index < 0 || index >= _containList.Count) {
			return "";
		}

		// TODO:日本語以外の場合
		return _containList [index].JapanText;
	}

	// テキストを取得する
	public string getText(StaticTextKey.TUTORIAL_KEY key)
	{
		// Indexを検索/範囲外判定
		int index = searchIndex (key);
		if (index < 0 || index >= _tutoList.Count) {
			return "";
		}

		// TODO:日本語以外の場合
		return _tutoList [index].JapanText;
	}

	// Indexを検索する
	private int searchIndex(StaticTextKey.TEXTKEY key)
	{
		for (int i = 0; i < _textList.Count; i++) 
		{
			// 同じキーならば
			if (_textList [i].TextKey == key) {
				return i;
			}
		}

		return -1;
	}

	// Indexを検索する
	private int searchIndex(StaticTextKey.CONTAIN_KEY key)
	{
		for (int i = 0; i < _containList.Count; i++) 
		{
			// 同じキーならば
			if (_containList [i].ContainKey == key) {
				return i;
			}
		}

		return -1;
	}

	// Indexを検索する
	private int searchIndex(StaticTextKey.TUTORIAL_KEY key)
	{
		for (int i = 0; i < _tutoList.Count; i++) 
		{
			// 同じキーならば
			if (_tutoList [i].TutorialKey == key) {
				return i;
			}
		}

		return -1;
	}
}
