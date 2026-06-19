using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 黒枠カメラの保持
public class BlackEdgeCameraKeeper : MonoBehaviour
{
    #region Singleton
    // インスタンス
    private static BlackEdgeCameraKeeper _instance;
    public static BlackEdgeCameraKeeper Instance {
        get {
            // インスタンス
            if (_instance == null) {
                _instance = (BlackEdgeCameraKeeper)FindObjectOfType (typeof(BlackEdgeCameraKeeper));

                if (_instance == null) {
                    Debug.LogError (typeof(BlackEdgeCameraKeeper) + "is nothing");
                }
            }
            return _instance;
        }
    }
    #endregion Singleton

    [SerializeField] private Camera _blackEdgeCamera;
    public Camera BlackEdgeCamera{
        get { return _blackEdgeCamera; }
    }

    // 初期化
    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // シーンの読み込み時
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this != null){
            _blackEdgeCamera.transform.position = new Vector3(0.0f,0.0f,-10.0f);
        }
    }
}
