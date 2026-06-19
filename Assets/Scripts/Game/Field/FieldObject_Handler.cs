using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// フィールドのオブジェクト
public class FieldObject_Handler : MonoBehaviour
{
    [SerializeField]
    private GameObject masterGoal;  // Master側のゴール

    [SerializeField]
    private GameObject subGoal;     // Sub側のゴール

    // ゴールの参照を取得するためのメソッド
    // tag: ConstData.PLAYER_TAG / ConstData.NPC_TAG などを想定
    public GameObject GetGoal(string tag)
    {
        bool isMaster = PhotonPlayerInfo.Instance != null && PhotonPlayerInfo.Instance.IsMasterClient;

        // プレイヤーの視点では、自分の攻撃方向のゴールを返す
        if (tag == ConstData.PLAYER_TAG)
        {
            // Master クライアントなら subGoal 側へ攻める、そうでなければ masterGoal 側へ、など
            return isMaster ? subGoal : masterGoal;
        }
        // NPCとサブは逆方向
        else
        {
            return isMaster ? masterGoal : subGoal;
        }

        Debug.LogError($"FieldObject_Handler: 未対応のタグです tag={tag}");
        return null;
    }
}
