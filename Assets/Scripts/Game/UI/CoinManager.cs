using UnityEngine;
using System;

/// <summary>
/// コイン管理クラス
/// シーン上に1つだけ配置し、どこからでも参照できるようにする
/// （UIフォルダ内に配置することを想定）
/// </summary>
public class CoinManager : MonoBehaviour
{
    private static CoinManager _instance;
    public static CoinManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // シーン上から検索
                _instance = FindObjectOfType<CoinManager>();
                if (_instance == null)
                {
                    Debug.LogError("CoinManager がシーン上に存在しません。UI 用のオブジェクトにアタッチしてください。");
                }
            }
            return _instance;
        }
    }

    /// <summary>現在のコイン枚数</summary>
    public int CurrentCoin { get; private set; }

    /// <summary>コイン枚数が変更されたときに通知されるイベント（引数：現在のコイン枚数）</summary>
    public event Action<int> OnCoinChanged;

    private void Awake()
    {
        // シングルトン化
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        LoadCoin();
        // 初期値も通知
        NotifyCoinChanged();
    }

    /// <summary>
    /// コインを読み込む（EasySave）
    /// </summary>
    private void LoadCoin()
    {
        // InitData で初期値は 0 で保存される前提
        CurrentCoin = ES3.Load<int>(DataKey.DATAKEY_GAME_INFO + DataKey.INT_COIN, 0);
    }

    /// <summary>
    /// コインを増やす
    /// </summary>
    public void AddCoin(int value)
    {
        CurrentCoin += value;
        // コインが0未満にならないように制御
        if (CurrentCoin < 0)
        {
            CurrentCoin = 0;
        }
        SaveCoin();
        NotifyCoinChanged();
    }

    /// <summary>
    /// コインを消費する（足りなければ false）
    /// </summary>
    public bool UseCoin(int value)
    {
        if (value <= 0) return true;
        if (CurrentCoin < value) return false;

        CurrentCoin -= value;
        SaveCoin();
        NotifyCoinChanged();
        return true;
    }

    /// <summary>
    /// コインを指定値にセットする
    /// </summary>
    public void SetCoin(int value)
    {
        CurrentCoin = Mathf.Max(0, value);
        SaveCoin();
        NotifyCoinChanged();
    }

    /// <summary>
    /// コインを保存（EasySave）
    /// </summary>
    private void SaveCoin()
    {
        ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.INT_COIN, CurrentCoin);
    }

    /// <summary>
    /// コイン変更イベントを発火
    /// </summary>
    private void NotifyCoinChanged()
    {
        OnCoinChanged?.Invoke(CurrentCoin);
    }
}


