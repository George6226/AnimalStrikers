using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

// リザルトの表示
public class UI_ResultViewer : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI resultText;    // 勝敗テキスト
    
    [SerializeField]
    private TextMeshProUGUI leftTeamText;    // 左側のチーム名
    [SerializeField]
    private TextMeshProUGUI leftScoreText;   // 左側のスコア
    
    [SerializeField]
    private TextMeshProUGUI rightTeamText;   // 右側のチーム名
    [SerializeField]
    private TextMeshProUGUI rightScoreText;  // 右側のスコア

    private bool isDisconnectionResult = false;

    public void ShowDisconnectionResult()
    {
        isDisconnectionResult = true;
        gameObject.SetActive(true);
    }

    void OnEnable()
    {
        if (ScoreManager.Instance != null)
        {
            // PlayerInfoから状態を取得
            bool isMaster = PhotonPlayerInfo.Instance.IsMasterClient;
            string myName = ScoreManager.Instance.GetPlayerName(isMaster);
            string opponentName = ScoreManager.Instance.GetPlayerName(!isMaster);

            // 共通の名前設定
            leftTeamText.text = myName;
            rightTeamText.text = opponentName;

            if (isDisconnectionResult)
            {
                // 切断時は切られた側の勝利
                resultText.text = "あなたの勝ち";
                leftScoreText.text = "WIN";
                rightScoreText.text = "LOSE";
            }
            else
            {
                // スコア表示
                int myScore = ScoreManager.Instance.GetScore(isMaster);
                int opponentScore = ScoreManager.Instance.GetScore(!isMaster);
                leftScoreText.text = myScore.ToString();
                rightScoreText.text = opponentScore.ToString();

                // 勝敗判定
                if (myScore > opponentScore)
                {
                    resultText.text = "あなたの勝ち";
                }
                else if (myScore < opponentScore)
                {
                    resultText.text = "あなたの負け";
                }
                else
                {
                    resultText.text = "引き分け";
                }
            }
        }

        // フラグをリセット
        isDisconnectionResult = false;
    }
}
