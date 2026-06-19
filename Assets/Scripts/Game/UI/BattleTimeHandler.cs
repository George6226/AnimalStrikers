using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

//試合時間の管理
public class BattleTimeHandler : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField]
    private TextMeshProUGUI timeText;  // 時間表示用のテキスト
    
    private float gameTime = 180f;     // 試合時間（3分）
    private float currentTime;         // 現在の時間
    private bool isGameActive = false;  // 初期状態はfalse
    private bool isTimeUp = false;      // タイムアップフラグを追加

    private PhotonView photonView;     // PhotonViewコンポーネント

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        currentTime = (float)ES3.Load<int>(DataKey.DATAKEY_GAME_INFO + DataKey.INT_REMAINING_GAME_TIME, ConstData.TIME_GAME);
        UpdateTimeDisplay();
    }

    // Update is called once per frame
    private void Update()
    {
        // ゲーム中以外は時間を更新しない
        if(!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME)) return;

        // Masterクライアントのみが時間を更新
        if (!isGameActive || !PhotonPlayerInfo.Instance.IsMasterClient) return;

        currentTime -= Time.deltaTime;
        if (currentTime <= 0 && !isTimeUp)
        {
            currentTime = 0;
            isGameActive = false;
            isTimeUp = true;
            // 全クライアントでリザルト画面に遷移
            photonView.RPC("OnTimeUp", RpcTarget.All);
        }
        UpdateTimeDisplay();
    }

    private void UpdateTimeDisplay()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    [PunRPC]
    private void OnTimeUp()
    {
        StateManager.Instance.changeState(StateManager.STATE_KIND.RESULT);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Masterクライアントがデータを送信
            stream.SendNext(currentTime);
            stream.SendNext(isGameActive);
            stream.SendNext(isTimeUp);
        }
        else
        {
            // 他のクライアントがデータを受信
            currentTime = (float)stream.ReceiveNext();
            isGameActive = (bool)stream.ReceiveNext();
            isTimeUp = (bool)stream.ReceiveNext();
            UpdateTimeDisplay();
        }
    }

    // 試合開始
    public void StartGame()
    {
        isGameActive = true;
        Time.timeScale = 1;
    }

    // 試合一時停止
    public void PauseGame()
    {
        isGameActive = false;
        Time.timeScale = 0;
    }
}