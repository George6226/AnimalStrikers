using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class SafeAreaPadding : MonoBehaviour
{
    //デバイスのセーフエリア判定を元に自動でUIカンバスのサイズ調整を行います。
    //カンバス直下のエンプティにコンポーネントとして装着し、各UIはエンプティの子として配置します。
    private DeviceOrientation postOrientation;

    RectTransform rect;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (Input.deviceOrientation != DeviceOrientation.Unknown && postOrientation == Input.deviceOrientation)
            return;

        postOrientation = Input.deviceOrientation;

       // rect = GetComponent<RectTransform>();
        var area = Screen.safeArea;

        rect.sizeDelta = Vector2.zero;
        rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);

        //Debug.Log("sizeDelta:" + rect.sizeDelta + " areaxMax:" + area.xMax + " areayMax:" + area.yMax + " areaxMin:" + area.xMin + " areayMin:" + area.yMin);
        //Debug.Log("anchorMax:" + rect.anchorMax + " anchorMin:" + rect.anchorMin);
    }
}