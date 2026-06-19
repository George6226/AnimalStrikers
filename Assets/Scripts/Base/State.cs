using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 状態(単体)
public class State : MonoBehaviour {

	// 状態の種類
	[SerializeField] private StateManager.STATE_KIND _stateKind;
	public StateManager.STATE_KIND StateKind
	{
		get{ return _stateKind;}
	}

	// オブジェクトの表示変更
	[SerializeField]
	private VisibleObject _visible;

	// オブジェクトの変更
	public void changeObject()
	{
		if(_visible == null){
			return;
        }
		_visible.changeVisibleObject ();
	}
}
