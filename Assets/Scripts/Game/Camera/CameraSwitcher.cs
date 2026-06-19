using UnityEngine;
using Cinemachine;
using Photon.Pun;
using System.Collections;

public class CameraSwitcher : MonoBehaviourPunCallbacks
{
    [SerializeField] private CinemachineVirtualCamera[] virtualCameras;  // 切り替えるカメラの配列
    private int currentCameraIndex = 0;  // 現在のカメラインデックス

    void Start()
    {
        StartCoroutine(initCameraCoroutine());
    }

    private IEnumerator initCameraCoroutine()
    {
        // 部屋に入るまで待機
        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        // マスター/サブに応じた初期カメラインデックスを設定
        currentCameraIndex = PhotonNetwork.IsMasterClient ? 0 : 1;

        // 初期カメラ以外を無効化
        for (int i = 0; i < virtualCameras.Length; i++)
        {
            virtualCameras[i].gameObject.SetActive(i == currentCameraIndex);
        }

        Debug.Log($"初期カメラ設定: Player={PhotonNetwork.NickName}, Camera={currentCameraIndex}");
    }
} 