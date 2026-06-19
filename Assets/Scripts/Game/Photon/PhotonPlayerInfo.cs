using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// Photonに関するPlayerの情報(インスタンス)
public class PhotonPlayerInfo : MonoBehaviourPunCallbacks
{
    #region Singleton

    private static PhotonPlayerInfo instance;

    public static PhotonPlayerInfo Instance
    {
        get
        {
            if (instance == null)
            {
                instance = (PhotonPlayerInfo)FindObjectOfType(typeof(PhotonPlayerInfo));
                if (instance == null)
                {
                    Debug.LogError(typeof(PhotonPlayerInfo) + "is nothing");
                }
            }

            return instance;
        }
    }

    #endregion Singleton

    // プレイヤー名
    [SerializeField] private string _playerName;
    // マスタークライアントかどうか
    [SerializeField] private bool _isMasterClient;
    // Photonプレイヤー情報
    [SerializeField] private Player _photonPlayer;
    // バトルモード
    [SerializeField] private ConstData.BATTLE_MODE _battleMode;  

    // PhotonのPlyaer情報の初期化
    public void Initialize(Player player)
    {
        _photonPlayer = player;
        _isMasterClient = player.IsMasterClient;
        _playerName = player.NickName;
        _battleMode = player.getBattleMode();

        Debug.Log("PlayerInfo.Initialize:playerName="+_playerName+" isMasterClient="+ _isMasterClient + " battleMode="+_battleMode);
    }

    // プレイヤー名の取得・設定
    public string PlayerName
    {
        get { return _playerName; }
        set { _playerName = value; }
    }

    // マスタークライアント状態の取得・設定
    public bool IsMasterClient
    {
        get { return _isMasterClient; }
        set { _isMasterClient = value; }
    }

    // バトルモードの取得・設定
    public ConstData.BATTLE_MODE BattleMode
    {
        get { return _battleMode; }
    }

    // Photonプレイヤー情報の取得
    public Player PhotonPlayer
    {
        get { return _photonPlayer; }
    }
} 