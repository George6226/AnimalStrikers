using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

// マッチング中のUI表示を管理するクラス
public class UI_RoomMatchingMessage : MonoBehaviour
{
    // マッチング表示用テキスト
    [SerializeField] private TextMeshProUGUI _matchingText;  

    // マッチング中メッセージ
    private const string MATCHING_MESSAGE = "マッチング中...({0}秒)";
    // 最大プレイヤー数
    private const byte MAX_PLAYERS = 2;
    // マッチング経過時間
    private float _matchingTimer = 0f;      

    // 更新
    void Update()
    {
        // ルームに入っている & まだ二人じゃない
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount < MAX_PLAYERS)
        {
            _matchingTimer += Time.deltaTime;
            
            // マッチング経過時間を表示（小数点以下切り捨て）
            if (_matchingText != null)
            {
                _matchingText.text = string.Format(MATCHING_MESSAGE, Mathf.Floor(_matchingTimer));
            }
        }
    }

    // マッチングタイマーをリセット
    public void resetMatchingTimer()
    {
        _matchingTimer = 0f;
    }
}
