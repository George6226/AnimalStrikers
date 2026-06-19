using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;  // 追加
using ExitGames.Client.Photon;

public class UI_ReadyViewer : MonoBehaviourPunCallbacks
{
    // アバター生成
    [SerializeField] private PhotonAvatarCreator _avatarCreator;

    [SerializeField] 
    private float fadeInDuration = 1f;  // フェードイン時間
    
    [SerializeField] 
    private float displayDuration = 3f;  // 表示時間
    
    [SerializeField] 
    private float fadeOutDuration = 1f;  // フェードアウト時間

    private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 初期状態は非表示
        canvasGroup.alpha = 0f;
    }

    private IEnumerator Start()
    {
        // NPCバトルの場合は2回生成する
        int num = (PhotonPlayerInfo.Instance.BattleMode == ConstData.BATTLE_MODE.NPC) ? 2 : 1;
        yield return new WaitUntil(() => _avatarCreator.Created >= num);

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitForPlayersAndStartFade());
        }
    }

    private IEnumerator WaitForPlayersAndStartFade()
    {
        // 両方のプレイヤーがキャラクターを生成するまで待機
        while (!PhotonNetwork.CurrentRoom.Players.Values.All(p => p.getIsCharacterSpawned()))
        {
            yield return new WaitForSeconds(0.1f);
        }

        // フェード開始
        ShowWithFade();
    }

    public void ShowWithFade()
    {
        StartCoroutine(FadeCoroutine());
    }

    private System.Collections.IEnumerator FadeCoroutine()
    {
        // フェードイン
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / fadeInDuration;
            canvasGroup.alpha = alpha;
            photonView.RPC("SyncAlpha", RpcTarget.Others, alpha);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        photonView.RPC("SyncAlpha", RpcTarget.Others, 1f);

        // 表示時間待機
        yield return new WaitForSeconds(displayDuration);

        // フェードアウト
        elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - (elapsedTime / fadeOutDuration);
            canvasGroup.alpha = alpha;
            photonView.RPC("SyncAlpha", RpcTarget.Others, alpha);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        photonView.RPC("SyncAlpha", RpcTarget.Others, 0f);

        // ゲーム状態に変更
        StateManager.Instance.changeState(StateManager.STATE_KIND.GAME);
    }

    [PunRPC]
    private void SyncAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
