using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 数字テキスト情報
public class NumberTextInfo {

	// 整列
	public enum TEXT_ALIGN
	{
		ALIGN_CENTER = 0,
		ALIGN_LEFT,
		ALIGN_RIGHT,
	}

	// スケール
	private Vector2 _scale = new Vector2(1.0f,1.0f);
	public Vector2 Scale{
		get{ return _scale;}
		set{_scale = value;}
	}

	// フォントサイズ
	private int _fontSize = 15;
	public int FontSize{
		get{ return _fontSize;}
		set{ _fontSize = value;}
	}

	// 文字間
	private float _wordSpace = 0.0f;
	public float WordSpace{
		get{ return _wordSpace;}
		set{ _wordSpace = value;}
	}

	// 整列
	private TEXT_ALIGN _textAlign = TEXT_ALIGN.ALIGN_CENTER;
	public TEXT_ALIGN TextAlign{
		get{ return _textAlign;}
		set{ _textAlign = value;}
	}

	// 色
	private Color _textcolor = Color.white;
	public Color TextColor
    {
        get { return _textcolor; }
        set { _textcolor = value; }
    }
}
