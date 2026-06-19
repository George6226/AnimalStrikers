using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestory_Common : MonoBehaviour 
{
    #region Singleton
    // インスタンス
    private static DontDestory_Common _instance;
    public static DontDestory_Common Instance 
    {
        get 
        {
            // インスタンス
            if (_instance == null) 
            {
                _instance = (DontDestory_Common)FindObjectOfType(typeof(DontDestory_Common));

                if (_instance == null) 
                {
                    Debug.LogError(typeof(DontDestory_Common) + "is nothing");
                }
            }
            return _instance;
        }
    }
    #endregion Singleton

    // Use this for initialization
    void Awake() 
    {
        // すでに作成している場合
        if (this != Instance) 
        {
            Debug.Log("DontDestory_Common:Awake");
            Destroy(this.gameObject);
            return;
        }

        // このゲームオブジェクトを破棄しない
        DontDestroyOnLoad(this.gameObject);
    }
} 