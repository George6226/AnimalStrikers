using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// photonの接続が切れるか監視する
public class PhotonTimeoutObserver : MonoBehaviourPunCallbacks
{
    // リザルト
    [SerializeField] private UI_ResultViewer resultViewer;  

    // インターバル
    private const float TIMEOUT_CHECK_INTERVAL = 3f;
    // 時間のチェック
    private float checkTimer = 0f;


    // 更新
    void Update()
    {
        // Photonがつながっているか?
        if (!PhotonNetwork.IsConnected) return;

        // ルームに参加している間、平均フレームレートが著しく低下したら、ルームから退出する
        if (PhotonNetwork.InRoom) {
            if (Time.smoothDeltaTime > 0.4f) {
                PhotonNetwork.LeaveRoom();
            }
        }

        // チェック時間経過
        checkTimer += Time.deltaTime;
        // 一定時間経過したら
        if (checkTimer >= TIMEOUT_CHECK_INTERVAL)
        {
            checkTimer = 0f;
            // 接続状態をチェック
            if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.DisconnectingFromGameServer ||
                PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.DisconnectingFromMasterServer)
            {
                Debug.Log("Photon切断:ネットワーク接続が切断されました。タイトルに戻ります。");
                HandleDisconnection();
            }
        }
    }

    // 接続が切れた時
    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.Log($"Photon切断:Photonから切断されました: {cause}");
        HandleDisconnection();
    }

    // プレイヤーが退出した時
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Photon切断:プレイヤーが退出しました: {PhotonPlayerInfo.Instance.PlayerName}");
        HandleDisconnection();
    }

    // 自分が退出した時
    public override void OnLeftRoom()
    {
        Debug.Log("Photon切断:部屋から退出しました");
        HandleDisconnection();
    }

    // 切断時の処理
    private void HandleDisconnection()
    {
        // NPCモードの場合は無視
        if (PhotonPlayerInfo.Instance.BattleMode == ConstData.BATTLE_MODE.NPC)
        {
            return;
        }

        Debug.Log("ネットワーク接続が切断されました。リザルトを表示します。");

        // ゲーム入力を止める（スライドパッド等が Update し続けないように）
        if (StateManager.Instance != null)
        {
            StateManager.Instance.changeStateLocal(StateManager.STATE_KIND.RESULT);
        }

        // リザルト表示
        if (resultViewer != null)
        {
            // 切断用のリザルト表示を呼び出し
            resultViewer.ShowDisconnectionResult();  
        }
    }
}
