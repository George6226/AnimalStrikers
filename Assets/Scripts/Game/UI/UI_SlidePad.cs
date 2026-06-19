using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// スライドパッド
public class UI_SlidePad : MonoBehaviour
{
    // エリア
    [SerializeField] private Image _area;
    // コントローラー
    [SerializeField] private Image _controll;

    // パッドの中心の位置/スクリーンのサイズ
    private Vector2 _center;
    private Vector2 _screenSize;

    // 距離倍率
    private float _slideScale;
    public float SlideScale
    {
        get { return _slideScale; }
    }
    // 角度(ラジアン)
    private float _rad;
    public float Radian
    {
        get { return _rad; }
    }
    // スライド中か?
    private bool _slideNow = false;
    public bool SlideNow
    {
        get { return _slideNow; }
    }

    // Start is called before the first frame update
    void Start()
    {
        _screenSize = Display.Instance.getDisplaySize();
    }

    // Update is called once per frame
    void Update()
    {
        // ゲーム中以外か?
        if (StateManager.Instance == null || !StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME))
        {
            return;
        }

        // 切断リザルト表示中や EventSystem 無効時は入力を受け付けない
        if (EventSystem.current == null || !EventSystem.current.isActiveAndEnabled)
        {
            resetSlide();
            return;
        }

        TouchInfo info = TouchUtility.GetTouch(0);
        // 始まり
        if(info == TouchInfo.Began)
        {
            // 他のUIをタッチしていた場合
            if (isTouchUI()){
                return;
            }
            // 中心
            _center = changeTouchToUIPos(TouchUtility.GetTouchPosition(0));
            this.transform.localPosition = _center;
            _slideNow = true;
            //Debug.Log("中心:" + _center);
            DebugLogViewer.Instance.addDebugLog("中心:" + _center);
        }

        // 移動
        if(info == TouchInfo.Moved)
        {
            // スライドしていない
            if (!_slideNow){
                return;
            }
            Vector2 uiPos = changeTouchToUIPos(TouchUtility.GetTouchPosition(0));
            // 差分/距離/ラジアン
            Vector2 dP = uiPos - _center;
            float d = Mathf.Sqrt(dP.x * dP.x + dP.y * dP.y);
            float rad = Mathf.Atan2(dP.y, dP.x);
            _rad = (dP.y < 0.0f) ? rad + Mathf.PI * 2.0f : rad;

            // エリアXの半分
            float halfAreaX = _area.rectTransform.sizeDelta.x / 2.0f;

            // 位置を反映/倍率計算
            _controll.transform.localPosition = calcLimitControllerImage(dP, d, halfAreaX, rad);
            _slideScale = calcSlideScale(d, halfAreaX);

            //Debug.Log("スライド:" + _slideScale + " 角度:" + _rad);
        }

        if (info == TouchInfo.Ended)
        {
            resetSlide();
        }
    }

    private void resetSlide()
    {
        if (_controll != null)
        {
            _controll.transform.localPosition = Vector2.zero;
        }
        _slideScale = 0.0f;
        _rad = 0.0f;
        _slideNow = false;
    }

    // 他のUIをタッチしているか?
    private bool isTouchUI()
    {
        List<RaycastResult> list = TouchUtility.getTouchUIs(0);

        foreach (RaycastResult rr in list)
        {
            if (rr.gameObject == null || _area == null || _controll == null)
            {
                continue;
            }

            // スライドパッドなら
            if (rr.gameObject.Equals(_area.gameObject) || rr.gameObject.Equals(_controll.gameObject))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    // タッチの位置からUIの位置に変換する
    private Vector2 changeTouchToUIPos(Vector2 tPos)
    {
        // 黒枠のサイズ計算
        Vector2 black = Display.Instance.getBlackSize();
        float x = (tPos.x - black.x) * _screenSize.x / (Screen.width - black.x*2.0f) - _screenSize.x/2.0f;
        float y = (tPos.y - black.y) * _screenSize.y / (Screen.height - black.y*2.0f) - _screenSize.y/2.0f;
        // UIの位置に変換
        return new Vector2(x,y);
    }

    // コントローラー画像の限界を計算する
    private Vector2 calcLimitControllerImage(Vector2 pos, float distance, float radius, float rad)
    {
        // 半径を超えていたら
        if(distance >= radius)
        {
            pos.x = Mathf.Cos(rad) * radius;
            pos.y = Mathf.Sin(rad) * radius;
        }

        return pos;
    }

    // 移動倍率の計算
    private float calcSlideScale(float distance, float radius)
    {
        // 倍率
        float per = (distance / radius);
        per = Mathf.Clamp(per, 0.0f, 1.0f);

        return per;
    }
}
