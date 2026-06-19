using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 数字を画像に変換する
public class NumberToImageChanger : MonoBehaviour
{
    // 数字リスト
    [SerializeField] private List<Sprite> _numbers;
    // プレハブ
    [SerializeField] private GameObject _parentPrefab;
    [SerializeField] private Image _imagePrefab;

    // 整列の位置
    public enum ALIGN_KIND
    {
        RIGHT = 0,
        CENTER,
        LEFT,
    }

    // インスタンス(シングルトン)
    #region Singleton
    private static NumberToImageChanger _instance;
    public static NumberToImageChanger Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = (NumberToImageChanger)FindObjectOfType(typeof(NumberToImageChanger));
                if (_instance == null)
                {
                    Debug.LogError(typeof(NumberToImageChanger) + "is nothing");
                }
            }
            return _instance;
        }
    }
    #endregion Singleton

    // 数字を画像に変換する
    public GameObject changeNumberToImage(int num, ALIGN_KIND align = ALIGN_KIND.LEFT)
    {
        int digit = num.ToString().Length;

        // 親を生成
        GameObject parent = Instantiate(_parentPrefab, this.transform);
        parent.transform.SetParent(this.transform);
        parent.transform.localPosition = Vector3.zero;

        for (int i=0; i<digit; i++)
        {
            string n = num.ToString()[i].ToString();
            int index = int.Parse(n);

            // 画像生成
            GameObject img = Instantiate(_imagePrefab.gameObject, parent.transform);
            Vector3 pos = calcImagePosition(digit, i, align);
            img.transform.localPosition = pos;

            img.GetComponent<Image>().sprite = _numbers[index];
            img.GetComponent<RectTransform>().sizeDelta = _numbers[index].bounds.size * 100.0f;

            img.transform.SetParent(parent.transform);

            Debug.Log("n:" + n + " i:" + i + " index:" + index+" size:"+_numbers[index].bounds.size);
        }

        return parent;
    }

    // 画像位置を計算
    private Vector3 calcImagePosition(int digit, int index, ALIGN_KIND align)
    {
        // 長さ
        float length = (digit - 1) * _numbers[0].bounds.size.x * 100.0f;

        // Xの位置
        float x = index * _numbers[0].bounds.size.x * 100.0f;

        // 右寄りの場合
        if(align == ALIGN_KIND.RIGHT)
        {
            x -= length;
        }
        // 中央寄りの場合
        else if(align == ALIGN_KIND.CENTER)
        {
            x -= length / 2.0f;
        }

        return new Vector3(x, 0.0f, 0.0f);
    }
}
