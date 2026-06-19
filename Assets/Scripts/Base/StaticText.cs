using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 固定テキストの反映
public class StaticText : MonoBehaviour {

	// リストの番号
	[SerializeField] private StaticTextKey.TEXTKEY _key;
	// テキスト
	[SerializeField] private Text _text;

	// Use this for initialization
	void Start () 
	{
		// テキスト更新
		string text = StaticTextManager.Instance.getText (_key);
		_text.text = text;
	}
}
