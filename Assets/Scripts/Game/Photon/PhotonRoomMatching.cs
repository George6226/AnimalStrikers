
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Photonによる部屋のマッチングを行う
public class PhotonRoomMatching : MonoBehaviourPunCallbacks
{
    // 部屋マッチングの終了時実行
    [SerializeField] private PhotonRoomMatchingExecutor_Base _execute;
    // マッチング制限時間
    [SerializeField] private float MATCHING_TIMEOUT = 60f;
    private const float BatchVerifyMatchingTimeoutSeconds = 2f;

    // 最大プレイヤー数
    private const byte MAX_PLAYERS = 2;
    // マッチング経過時間
    private float _matchingTimer = 0f;
    // タイムアウトフラグ
    private bool _isMatchingTimedOut = false;
    // ゲーム開始処理の二重実行防止
    private bool _isStartingGame = false;
    // 前戦のルーム退出後に JoinRandomRoom するか
    private bool _pendingJoinAfterLeave = false;

    private void Start()
    {
        BeginMatching();
    }

    // マッチング開始（再プレイ時は前戦ルームから退出してから参加）
    private void BeginMatching()
    {
        _matchingTimer = 0f;
        _isMatchingTimedOut = false;
        _isStartingGame = false;
        _pendingJoinAfterLeave = false;
        PhotonNetwork.IsMessageQueueRunning = true;

        if (PhotonNetwork.LocalPlayer != null)
        {
            PhotonNetwork.LocalPlayer.resetMatchProperties();
        }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            if (PhotonNetwork.InRoom)
            {
                _pendingJoinAfterLeave = true;
                PhotonNetwork.LeaveRoom();
            }
            else
            {
                TryJoinRandomRoom();
            }
            return;
        }

        PhotonNetwork.ConnectUsingSettings();
    }

    private void TryJoinRandomRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom)
        {
            return;
        }

        PhotonNetwork.JoinRandomRoom();
    }

    private void Update()
    {
        // ルーム入っている & まだ二人じゃない && タイムアウトしていない
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount < MAX_PLAYERS && !_isMatchingTimedOut)
        {
            _matchingTimer += Time.deltaTime;

            // タイムアウト => NPCの生成
            if (_matchingTimer >= GetEffectiveMatchingTimeout())
            {
                // タイムアウト状態
                _isMatchingTimedOut = true;
                // ゲームの開始のチェック
                CheckAndStartGame(ConstData.BATTLE_MODE.NPC);
            }
        }
    }

    // Masterに接続
    public override void OnConnectedToMaster()
    {
        TryJoinRandomRoom();
    }

    // 前戦のルーム退出後、新しい部屋へ参加
    public override void OnLeftRoom()
    {
        if (!_pendingJoinAfterLeave)
        {
            return;
        }

        _pendingJoinAfterLeave = false;
        TryJoinRandomRoom();
    }
    // ランダムな部屋が見つからなかった場合、新しい部屋を作成
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // ルーム規則(二人/存在/公開)
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = MAX_PLAYERS,
            IsVisible = true,
            IsOpen = true
        };
        // ルームの生成
        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    // ルームに入る
    public override void OnJoinedRoom()
    {
        // ニックネームを設定
        if (PhotonNetwork.IsMasterClient){
            PhotonNetwork.NickName = "PlayerMaster";
        }
        else{
            PhotonNetwork.NickName = "PlayerSub";
        }

        // ゲームの開始のチェック
        CheckAndStartGame(ConstData.BATTLE_MODE.NORMAL);
    }

    // 他のプレイヤーがルーム参加したときに呼ばれる
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // ゲームの開始のチェック
        CheckAndStartGame(ConstData.BATTLE_MODE.NORMAL);
    }

    // ゲームの開始チェック
    private void CheckAndStartGame(ConstData.BATTLE_MODE mode)
    {
        if (_isStartingGame || !PhotonNetwork.InRoom)
        {
            return;
        }

        Debug.Log($"プレイヤー数: {PhotonNetwork.CurrentRoom.PlayerCount}");

        // NPC or 通常&二人目の場合
        if ((mode == ConstData.BATTLE_MODE.NPC) || 
            (mode == ConstData.BATTLE_MODE.NORMAL && PhotonNetwork.CurrentRoom.PlayerCount == MAX_PLAYERS))
        {
            _isStartingGame = true;
            // 部屋を閉じる
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }

            // 全プレイヤーがゲームモードを設定
            if (PhotonNetwork.LocalPlayer != null)
            {
                Debug.Log("ゲームモードを設定:" + mode);
                PhotonNetwork.LocalPlayer.setBattleMode(mode);
            }

            // プロパティの同期を待ってシーン遷移
            StartCoroutine(WaitForPropertiesAndLoadScene());
        }
    }

    // 同期後ゲームシーンへ
    private IEnumerator WaitForPropertiesAndLoadScene()
    {
        // プロパティの同期を待つ
        yield return new WaitForSeconds(0.1f);
        // プレイヤー情報を初期化
        PhotonPlayerInfo.Instance.Initialize(PhotonNetwork.LocalPlayer);
        // メッセージキューを停止
        PhotonNetwork.IsMessageQueueRunning = false;

        // 各種ゲームデータの初期化は GameDataInitializer.OnEnable で実行する

        // マッチング終了時の実行
        _execute.executeRoomMatching();
    }

    private float GetEffectiveMatchingTimeout() =>
        GoapBatchVerifyEnvironment.IsActive
            ? BatchVerifyMatchingTimeoutSeconds
            : MATCHING_TIMEOUT;
}
