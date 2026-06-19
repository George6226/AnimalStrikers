//  CacheDataEditor.cs
//  http://kan-kikuchi.hatenablog.com/entry/CacheData
//
//  Created by kan.kikuchi on 2016.05.09.

using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// CacheDataのInspectorを変えるエディター
/// </summary>
[CustomEditor(typeof(CacheData))]
public class CacheDataEditor : Editor {

	//=================================================================================
	//更新
	//=================================================================================

	//Inspectorを更新する
	public override void OnInspectorGUI(){
		//元の表示
		base.OnInspectorGUI ();

		//更新開始
		serializedObject.Update();

		//CacheDataを取得
		CacheData cacheData = target as CacheData;

		//リフレッシュボタンを表示
		if(GUILayout.Button("Refresh")){
			cacheData.Refresh ();
		}

		//更新終わり
		serializedObject.ApplyModifiedProperties();
	}

}