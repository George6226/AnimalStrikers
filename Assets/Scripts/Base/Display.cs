using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Display : MonoBehaviour {
	
	// カメラ
	[SerializeField]
	private Camera _mainCamera;

    [SerializeField] private Canvas _uiCanvas;

	#region Singleton
	// インスタンス
	private static Display _instance;
	public static Display Instance {
		get {
			// インスタンス
			if (_instance == null) {
				_instance = (Display)FindObjectOfType (typeof(Display));

				if (_instance == null) {
					Debug.LogError (typeof(Display) + "is nothing");
				}
			}
			return _instance;
		}
	}
	#endregion Singleton

	// 画面の右上を取得
	public Vector3 getScreenTopRightInWorld()
	{
		Vector3 baseWorld =  _mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0.0f));

		Vector3 topRight = new Vector3(0.0f,0.0f,0.0f);
		Vector2 scales = getDisplayScales();
		Vector3 dSize = getDisplaySize();
		// 横に黒が入る場合
		if(scales.x > scales.y)
        {
			topRight.x = baseWorld.y * dSize.x / dSize.y;
			topRight.y = baseWorld.y;
        }
        else
        {
			topRight.x = baseWorld.y * Screen.width / Screen.height;
			float h = dSize.y * Screen.width / dSize.x;
			topRight.y = baseWorld.y * h / Screen.height;
        }
		//Debug.Log("左上:" + topRight+" scales:"+scales+" dSize:"+dSize);
		return topRight;
	}

	// 画面の左下を取得する
	public Vector3 getScreenBottomLeftInWorld()
	{
		Vector3 bottomLeft = getScreenTopRightInWorld();
		bottomLeft.Scale(new Vector3(-1.0f, -1.0f, 1.0f));
		return bottomLeft;
	}

	// ワールドのサイズ
	public Vector2 getWorldSize()
	{
		Vector2 TR = getScreenTopRightInWorld();
		Vector2 BL = getScreenBottomLeftInWorld();

		return TR - BL;
	}

	// ディスプレイサイズを取得する
	public Vector2 getDisplaySize()
	{
		// 設定したアスペクト比を取得する
        Vector2 aspect = _uiCanvas.GetComponent<CanvasScaler>().referenceResolution;
        return new Vector2(aspect.x,aspect.y);
	}

	// ディスプレイの倍率を取得する
	public float getDisplayScale()
	{
		// 画面解像度の割合を計算
		Vector2 display = getDisplaySize ();
		float wPer = Screen.width / display.x;
		float hPer = Screen.height / display.y;

		// より低い方に合わせる
		if (hPer > wPer) {
			return wPer;
		}

		return hPer;
	}

	// ディスプレイの倍率を取得する
	public Vector2 getDisplayScales()
	{
		// 画面解像度の割合を計算
		Vector2 display = getDisplaySize();
		float wPer = Screen.width / display.x;
		float hPer = Screen.height / display.y;

		//Debug.Log("display:" + display + " screenw:" + Screen.width + " h:" + Screen.height + " wper:" + wPer + " hper:" + hPer);

		return new Vector2(wPer,hPer);
	}

	// ローカル座標からスクリーン座標に変換する
	public Vector2 changeLocalToScreenPosition(Transform first)
	{
		// 初めのTransform
		Transform trans = first;
		// 初めの位置
		Vector3 pos = trans.localPosition;

		while (true)
		{
			// 親を取得
			Transform parent = trans.parent;
			if (parent == null)
			{
				break;
			}
			// 親の分を追加する
			pos += parent.localPosition;

			// Rootと同じ名前
			if (parent.name == first.transform.root.name)
			{
				break;
			}
			// 1階層上へ
			trans = parent;
		}

		// ディスプレイサイズを半分足す
		Vector2 displaySize = getDisplaySize();
		pos.x += displaySize.x / 2.0f;
		pos.y += displaySize.y / 2.0f;

		return pos;
	}

	// ワールドポジションに変換
	public Vector3 changeScreenToWorldPosition(Vector3 screenPos)
	{
		return _mainCamera.ScreenToWorldPoint(screenPos);
	}

	// Canvasに対する倍率
	public float getCanvasScale()
    {
		float scale = 1.0f;
		// 縦の方が比率が高い場合
		Vector2 scales = Display.Instance.getDisplayScales();
		if (scales.y > scales.x)
		{
			// (1242,2208)
			Vector2 size = Display.Instance.getDisplaySize();
			// スクリーン
			float sw = Screen.width;
			float sh = Screen.height;

			float canvasY = sh / sw * size.x;
			scale = size.y / canvasY;

			Debug.Log("canvasY:" + canvasY + " scale:" + scale);
		}

		return scale;
	}

    // 黒枠のサイズを取得する
    public Vector2 getBlackSize()
    {
        // 基本のサイズ/スケール
        Vector2 size = getDisplaySize();
        float scale = getDisplayScale();

        // 黒枠のサイズ
        float width = Screen.width - size.x * scale;
        float height = Screen.height - size.y * scale;

        return new Vector2(width/2.0f, height/2.0f);
    }
}
