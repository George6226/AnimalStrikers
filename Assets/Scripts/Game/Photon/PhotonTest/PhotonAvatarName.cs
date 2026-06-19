using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class PhotonAvatarName : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    void Start()
    {
        // var nameLabel = GetComponent<TextMeshProUGUI>();
        // nameLabel.text = "" + photonView.Owner.NickName + ":" + photonView.OwnerActorNr;

        // // ローカルプレイヤー取得
        // Player p = PhotonNetwork.LocalPlayer;

        // Debug.Log("PhotonViewの名前:"+photonView.name);
    }
}
