using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

public class ScoreManager : MonoBehaviourPunCallbacks
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] 
    private TextMeshProUGUI player1ScoreText;  // プレイヤー1のスコアを表示するUI要素
    [SerializeField] 
    private TextMeshProUGUI player2ScoreText;  // プレイヤー2のスコアを表示するUI要素

    [SerializeField] 
    private BattleTimeHandler timeHandler;  // BattleTimeHandlerへの参照を追加

    private Dictionary<string, int> playerScores = new Dictionary<string, int>();
    private string player1Name = "Player1";
    private string player2Name = "Player2";
    private bool isPlayer1Set = false;
    private bool isPlayer2Set = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeScores();
        UpdateScoreDisplay();
    }

    // プレイヤー名を設定（RPCを使用して同期）
    public void SetPlayerName(string playerName)
    {
        Debug.Log("SetPlayerName:" + playerName + " isPlayer1Set:" + isPlayer1Set + " isPlayer2Set:" + isPlayer2Set);
        bool isMaster = PhotonPlayerInfo.Instance.IsMasterClient;
        if (playerName == "NPC")
        {
            isMaster = false;
        }
        photonView.RPC("RPCSetPlayerName", RpcTarget.All, playerName, isMaster);
    }

    [PunRPC]
    private void RPCSetPlayerName(string playerName, bool isSentByMaster)
    {
        if (isSentByMaster)
        {
            player1Name = playerName;
            Debug.Log("Master playerName:" + playerName + " player1Name:" + player1Name + " player2Name:" + player2Name);
            isPlayer1Set = true;
        }
        else
        {
            player2Name = playerName;
            Debug.Log("Sub playerName:" + playerName + " player1Name:" + player1Name + " player2Name:" + player2Name);
            isPlayer2Set = true;
        }

        // 両方のプレイヤーが設定されたら初期化とゲーム開始
        if (isPlayer1Set && isPlayer2Set)
        {
            InitializeScores();
            UpdateScoreDisplay();
            
            // タイマーを開始
            if (timeHandler != null)
            {
                timeHandler.StartGame();
            }
        }
    }

    // スコアの初期化
    private void InitializeScores()
    {
        playerScores.Clear();
        // EasySaveからスコアを取得
        int teamScore = ES3.Load<int>(DataKey.DATAKEY_GAME_INFO + DataKey.INT_TEAM_SCORE, 0);
        int enemyScore = ES3.Load<int>(DataKey.DATAKEY_GAME_INFO + DataKey.INT_ENEMY_SCORE, 0);
        
        // Player1(Team)のスコアを設定
        playerScores[player1Name] = teamScore;
        // Player2(Enemy)のスコアを設定
        playerScores[player2Name] = enemyScore;
    }

    // スコアを加算（RPCを使用して同期）
    public void AddScore(bool isMasterScore, int points)
    {
        photonView.RPC("RPCAddScore", RpcTarget.All, isMasterScore, points);
    }

    [PunRPC]
    private void RPCAddScore(bool isMasterScore, int points)
    {
        string playerName = isMasterScore ? player1Name : player2Name;
        if (playerScores.ContainsKey(playerName))
        {
            playerScores[playerName] += points;
            UpdateScoreDisplay();
            Debug.Log($"Score added: {playerName} got {points} points. Total: {playerScores[playerName]}");
        }
        else
        {
            Debug.LogWarning($"プレイヤー '{playerName}' が見つかりません。");
        }
    }

    // スコアをリセット
    public void ResetScore()
    {
        InitializeScores();
        UpdateScoreDisplay();
    }

    // スコアの表示を更新
    private void UpdateScoreDisplay()
    {
        if (PhotonPlayerInfo.Instance.IsMasterClient)
        {
            player1ScoreText.text = $"{player1Name}: {playerScores[player1Name]}";
            player2ScoreText.text = $"{player2Name}: {playerScores[player2Name]}";
        }
        else
        {
            player1ScoreText.text = $"{player2Name}: {playerScores[player2Name]}";
            player2ScoreText.text = $"{player1Name}: {playerScores[player1Name]}";
        }
    }

    // 指定したプレイヤーのスコアを取得
    public int GetScore(bool isMasterClient)
    {
        string playerName = isMasterClient ? player1Name : player2Name;
        if (playerScores.ContainsKey(playerName))
        {
            return playerScores[playerName];
        }
        Debug.LogWarning($"プレイヤー '{playerName}' が見つかりません。");
        return 0;
    }

    // プレイヤー名を取得
    public string GetPlayerName(bool isMasterClient)
    {
        return isMasterClient ? player1Name : player2Name;
    }
} 