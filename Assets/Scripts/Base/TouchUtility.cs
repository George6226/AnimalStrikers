using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class TouchUtility
{
	private static Vector3 TouchPosition = Vector3.zero;

	// タッチ情報を取得
	public static TouchInfo GetTouch(int id)
	{
		// エディターの場合
		if (Application.isEditor || Application.platform == RuntimePlatform.OSXPlayer)
		{
			if (Input.GetMouseButtonDown(0)) { return TouchInfo.Began; }
			if (Input.GetMouseButton(0)) { return TouchInfo.Moved; }
			if (Input.GetMouseButtonUp(0)) { return TouchInfo.Ended; }
		}
		// スマホの場合
		else
		{
			if (Input.touchCount > id)
			{
				return (TouchInfo)((int)Input.GetTouch(id).phase);
			}
		}
		return TouchInfo.None;
	}

	// タッチの位置を取得
	public static Vector3 GetTouchPosition(int id)
	{
		// エディタ用
		if (Application.isEditor || Application.platform == RuntimePlatform.OSXPlayer)
		{
			TouchInfo touch = TouchUtility.GetTouch(id);
			if (touch != TouchInfo.None) { return Input.mousePosition; }
		}
		// スマホ用
		else
		{
			if (Input.touchCount > id)
			{
				Touch touch = Input.GetTouch(id);
				TouchPosition.x = touch.position.x;
				TouchPosition.y = touch.position.y;
				return TouchPosition;
			}
		}
		return Vector3.zero;
	}

	// ワールドポジションに変換
	public static Vector3 GetTouchWorldPosition(Camera camera, int id)
	{
		return camera.ScreenToWorldPoint(GetTouchPosition(id));
	}

	// UIをタッチしているか?
	public static bool isTouchUI(int id)
	{
		// イベントがない場合
		if (!EventSystem.current) {
			return false;
		}

		// エディタ用
		if (Application.isEditor || Application.platform == RuntimePlatform.OSXPlayer)
		{
			if (EventSystem.current.IsPointerOverGameObject ()) {
				Debug.Log("UIタッチ中?");
				return true;
			}
		}
		// スマホ用
		else
		{
			return getTouchUIs(id).Count > 0;
		}

		return false;
	}

	// タッチしているUIを取得
	public static List<RaycastResult> getTouchUIs(int id)
	{
		var results = new List<RaycastResult>();
		var eventSystem = EventSystem.current;
		if (eventSystem == null || !eventSystem.isActiveAndEnabled)
		{
			return results;
		}

		var eventDataCurrentPosition = new PointerEventData(eventSystem);
		eventDataCurrentPosition.position = GetTouchPosition(id);
		eventSystem.RaycastAll(eventDataCurrentPosition, results);

		int uiLayer = LayerMask.NameToLayer("UI");
		for (int i = results.Count - 1; i >= 0; i--)
		{
			var go = results[i].gameObject;
			if (go == null || go.layer != uiLayer)
			{
				results.RemoveAt(i);
			}
		}
		return results;
	}

    // 範囲外をタッチしているか?
    public static bool isTouchOutside(int id)
    {
        // タッチ情報
        TouchInfo info = GetTouch(id);
        // なし時
        if(info == TouchInfo.None){
            return false;
        }
        // 黒枠のサイズ
        Vector2 blackSize = Display.Instance.getBlackSize();
        // タッチ位置
        Vector3 touchPos = GetTouchPosition(id);

        // 横側に黒がある
        if(blackSize.x > 0.0f){
            // 横側に黒枠に触れている
            if(Screen.width - blackSize.x < touchPos.x || blackSize.x > touchPos.x)
            {
                //Debug.Log("よこ blackSize:"+blackSize.x+" tPos:"+touchPos.x);
                return true;
            }
        }
        // たて側に黒がある
        if(blackSize.y > 0.0f)
        {
            // たて側に黒枠に触れている
            if (Screen.height - blackSize.y < touchPos.y || blackSize.y > touchPos.y)
            {
                //Debug.Log("たて blackSize:" + blackSize.y + " tPos:" + touchPos.y);
                return true;
            }
        }

        return false;
    }
}

// タッチ情報
public enum TouchInfo
{
	// なし
	None = 99,
	// 開始
	Began = 0,
	// 移動
	Moved = 1,
	// 静止
	Stationary = 2,
	// 終了
	Ended = 3,
	// キャンセル
	Canceled = 4,
}
