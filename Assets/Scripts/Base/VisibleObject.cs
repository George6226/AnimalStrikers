using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibleObject : MonoBehaviour {

	// 表示/非表示
	[SerializeField] private List<GameObject> _visibleList = new List<GameObject>();
	[SerializeField] private List<GameObject> _disableList = new List<GameObject>();

	// 表示の変更を行う
	public void changeVisibleObject()
	{
		// 表示に
		for (int i = 0; i < _visibleList.Count; i++) {
			_visibleList [i].SetActive (true);
		}
		// 非表示に
		for (int i = 0; i < _disableList.Count; i++) {
			_disableList [i].SetActive (false);
		}
	}

	// 表示の反転を行う
	public void reverseVisibleObject(bool reverse)
	{
		// 表示を反転
		for (int i = 0; i < _visibleList.Count; i++) {
			_visibleList [i].SetActive (!reverse);
		}
		// 非表示に
		for (int i = 0; i < _disableList.Count; i++) {
			_disableList [i].SetActive (reverse);
		}
	}
}
