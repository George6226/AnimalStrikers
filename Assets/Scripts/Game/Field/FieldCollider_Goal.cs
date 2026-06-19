using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// ゴールの当たり判定
public class FieldCollider_Goal : MonoBehaviour
{
    // このゴールがMaster側のゴールかどうか（GameScene の isMasterGoal と一致させる）
    [SerializeField] private bool isMasterGoal = true;
    // 処理中フラグ
    private bool _isProcessing = false;
    // クールダウン時間
    private float _processingCooldown = 1.0f;  

    // ぶつかったとき(Trigger)
    private void OnTriggerEnter(Collider other)
    {
        // 処理中なら無視
        if (_isProcessing) return;
        
        // ボールタグを持つオブジェクトが衝突した場合
        if (other.CompareTag(ConstData.BALL_TAG))
        {
            // 処理中に変更
            _isProcessing = true;
            
            // スコアは Master のみが集計して RPC 同期（両ゴールの判定を一本化）
            if (PhotonPlayerInfo.Instance != null && ScoreManager.Instance != null)
            {
                bool masterReportsScore =
                    PhotonPlayerInfo.Instance.BattleMode == ConstData.BATTLE_MODE.NPC ||
                    PhotonNetwork.IsMasterClient;

                if (masterReportsScore)
                {
                    // Master 側のゴールに入った = Sub の得点 / Sub 側のゴール = Master の得点
                    bool scorerIsMaster = !isMasterGoal;
                    ScoreManager.Instance.AddScore(scorerIsMaster, 1);
                    Debug.Log($"Goal! scorer={(scorerIsMaster ? "Master" : "Sub")} isMasterGoal={isMasterGoal}");
                }
            }

            // 全てのキャラクターとボールを初期位置に戻す
            ResetAllPositions();

            // クールダウン後にフラグをリセット
            Invoke(nameof(ResetProcessing), _processingCooldown);
        }
    }

    // 全ての位置をリセット
    private void ResetAllPositions()
    {
        var teamReg = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (teamReg != null)
        {
            // 全てのアバターを初期位置に戻す
            foreach (var facade in teamReg.AllAnimals)
            {
                if (facade == null) continue;
                var avatar = facade.GetAvatar();
                if (avatar != null)
                {
                    avatar.ResetToInitialPosition();
                }
            }
        }
        TeamFacade.Instance.BallManager.ResetBallPosition();
    }

    // 処理終了
    private void ResetProcessing()
    {
        _isProcessing = false;
    }
}
