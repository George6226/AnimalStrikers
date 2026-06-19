using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundPlayer : MonoBehaviour {

	// 動作の種類
	public enum MOVE_KIND
	{
		CLICK = 0,
		POINT_DOWN,
	}

	// サウンド情報
	[System.Serializable]
	public class SoundPlayInfo
	{
		// 動作の種類
		[SerializeField] private MOVE_KIND _moveKind;
		public MOVE_KIND MoveKind{
			get{ return _moveKind;}
		}

		// サウンド名
		[SerializeField] private string _soundName;
		public string SoundName{
			get{ return _soundName;}
		}
	}

	// サウンド情報リスト
	[SerializeField] private List<SoundPlayInfo> _soundInfoList = new List<SoundPlayInfo>();
	// トグルボタン
	private Toggle _toggle;
	// ボタン
	private Button _button;
	// 初期化
	private bool _init = false;

	// 初期化
	void Awake()
	{
		_toggle = GetComponent<Toggle> ();
		_button = GetComponent <Button> ();
	}

	// 初期化
	IEnumerator Start()
	{
		// 1フレーム開ける
		yield return null;
		_init = true;
	}

	// ポインタが置かれた時のサウンド
	public void pointDownPlaySound()
	{
//		Debug.Log ("bInterpoint:" + _button.interactable);
		// 有効でなければ
		if (!_button.interactable) {
			return;
		}
		SoundPlayInfo info = getSameMoveKind (MOVE_KIND.POINT_DOWN);
		if (info == null) {
			return;
		}
		SoundManager.Instance.ManagerSE.playSoundEffect (info.SoundName);
	}

	// クリックされた時のサウンド
	public void clickPlaySound()
	{
//		Debug.Log ("bInterclick:" + _button.interactable);
		// 有効でなければ
		//if (!_button.interactable) {
		//	return;
		//}
		SoundPlayInfo info = getSameMoveKind (MOVE_KIND.CLICK);
		if (info == null) {
			return;
		}
		SoundManager.Instance.ManagerSE.playSoundEffect (info.SoundName);
	}

	// サウンドをならす
	public void clickPlaySound(int index)
	{
		// 有効でなければ
		//if (!_button.interactable)
		//{
		//	return;
		//}
		SoundPlayInfo info = _soundInfoList[index];
		if (info == null){
			return;
		}
		SoundManager.Instance.ManagerSE.playSoundEffect(info.SoundName);
	}

	// クリックされた時のサウンド(トグル)
	public void clickPlaySoundOnToggle(bool value)
	{
    	    // 初期化していない
    	    if (!_init) {
            return;
    	    }
        // OFFになったら
        if (!_toggle.isOn) {
			return;
            }
        SoundPlayInfo info = getSameMoveKind (MOVE_KIND.CLICK);
		if (info == null) {
			return;
		}
        SoundManager.Instance.ManagerSE.playSoundEffect (info.SoundName);
	}

	// 同じ種類の動作の音情報を取得する
	private SoundPlayInfo getSameMoveKind(MOVE_KIND kind)
	{
		// 検索
		for (int i = 0; i < _soundInfoList.Count; i++) {

			// 同じ種類ならば
			if (_soundInfoList [i].MoveKind == kind) {
				return _soundInfoList [i];
			}
		}

		return null;
	}
}
