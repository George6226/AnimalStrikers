using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class LightingSetting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // コルーチンで遅延実行
        StartCoroutine(DelayedLightingUpdate());
    }

    void OnEnable()
    {
        // コルーチンで遅延実行
        StartCoroutine(DelayedLightingUpdate());
    }

    private IEnumerator DelayedLightingUpdate()
    {
        // 0.5秒待機
        yield return new WaitForSeconds(0.5f);

        // ライティング設定を更新
        UpdateLightRotation();
        UnityEngine.RenderSettings.ambientIntensity = 1.0f;
        UnityEngine.RenderSettings.reflectionIntensity = 1.0f;

        // 影を無効化
        GetComponent<Light>().shadows = LightShadows.None;

        DynamicGI.UpdateEnvironment();
    }

    private void UpdateLightRotation()
    {
        // 現在のX軸回転を保持
        Vector3 currentRotation = transform.eulerAngles;

        Debug.Log("ライティング角度前:" + transform.eulerAngles + " master:" + PhotonNetwork.IsMasterClient);

        // マスターかサブかで角度を変更
        currentRotation.x = PhotonNetwork.IsMasterClient ? 120.0f : 60.0f;
        currentRotation.y = 90.0f;  // Y軸の回転を90度に固定
        currentRotation.z = 0.0f;  // Z軸の回転を0度に固定
        
        // 回転を適用
        transform.eulerAngles = currentRotation;

        Debug.Log("ライティング角度後:" + transform.eulerAngles + " master:"+PhotonNetwork.IsMasterClient);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
