using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 黒の枠を生成
public class BlackEdgeCreator : MonoBehaviour
{
    // プレハブ
    [SerializeField] private GameObject _blackEdge;

    // Use this for initialization
    void Start()
    {
        // ディスプレイ情報
        Vector2 dispSize = Display.Instance.getDisplaySize();
        Vector2 dispScales = Display.Instance.getDisplayScales();

        // 横に黒枠
        if (dispScales.x > dispScales.y)
        {
            //// 横幅/横位置
            //float bw = Screen.width - dispSize.x * dispScales.y;
            //bw = Mathf.Ceil(bw);
            //float px = dispSize.x / 2.0f + bw / 2.0f;

            //// 黒枠を生成
            //createBlackEdge(px, 0, bw/2, dispSize.y);
            //createBlackEdge(-px, 0, bw/2, dispSize.y);

            //Debug.Log("width:" + Screen.width + " dispSize.x:" + dispSize.x + " scale:" + dispScales.y);
            //Debug.Log("bw:" + bw + " px+" + px);

            // エディタ用?
            float bw = dispSize.y / Screen.height * Screen.width - dispSize.x;
            bw = Mathf.Ceil(bw);
            float px = dispSize.x / 2.0f + bw / 4.0f;

            createBlackEdge(px, 0, bw/2, dispSize.y);
            createBlackEdge(-px, 0, bw/2, dispSize.y);
        }
        // 縦に黒枠
        else if (dispScales.x < dispScales.y)
        {
            // 横幅/横位置
            float bh = Screen.height - dispSize.y * dispScales.x;
            bh = Mathf.Ceil(bh);
            float py = dispSize.y / 2.0f + bh / 2.0f;

            // 黒枠を生成
            //createBlackEdge(0.0f, py, Screen.width, bh);
            //createBlackEdge(0.0f, -py, Screen.width, bh);
            createBlackEdge(0.0f, Mathf.Floor(py), dispSize.x, bh);
            createBlackEdge(0.0f, Mathf.Floor(py)*-1, dispSize.x, bh);
        }
    }

    // 黒枠を生成
    private void createBlackEdge(float x, float y, float w, float h)
    {
        // 生成
        GameObject edge = Instantiate(_blackEdge, this.transform);
        edge.transform.localPosition = new Vector3(x, y, 0.0f);
        edge.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
    }
}
