//  CacheData.cs
//  http://kan-kikuchi.hatenablog.com/entry/CacheData
//
//  Created by kan.kikuchi on 2016.05.09.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CacheData")]
/// <summary>
/// キャッシュ用のデータ
/// </summary>
public class CacheData : ScriptableObject {

	//データの実体
	private static CacheData _entity;
	public  static CacheData  Entity{
		get{
			if(_entity == null){
				_entity = Resources.Load<CacheData>("CacheData");

				if(_entity == null){
					Debug.LogError("CacheData is null");
				}

				//実機で実行する時は初期化する
				#if !UNITY_EDITOR
				_entity.Refresh ();
				#endif
			}
			return _entity;
		}
	}
	public int _testScore = 0; //シーン越しに受け継ぐ数値（仮）

	//=================================================================================
	//初期化
	//=================================================================================

	/// <summary>
	/// キャッシュを全て初期化
	/// </summary>
	public void Refresh()
	{
		_testScore = 0;
	}
}