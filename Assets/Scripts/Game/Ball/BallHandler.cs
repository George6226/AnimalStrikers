using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

// ボールのバフ状態
public enum BallBuffKind
{
    None,
    Ally,
    Enemy,
}

// ボールの操作
public class BallHandler : MonoBehaviourPunCallbacks, IPunOwnershipCallbacks
{
    // 物理/当たり判定
    [SerializeField] private Rigidbody _rigidbody;
    public Rigidbody Rigid
    {
        get { return _rigidbody; }
    }
    [SerializeField] private Collider _collider;

    // ボールに付与されたバフ（味方由来／敵由来／なし）
    private BallBuffKind _ballBuffKind = BallBuffKind.None;
    public BallBuffKind BuffKind => _ballBuffKind;
    private float _ballBuffAttack = 0.0f;
    public float BuffAttack => _ballBuffAttack;

    // 同期中
    private bool _synchronizedNow = false;
    public bool SynchronizedNow{
        get { return _synchronizedNow; }
    }

    // はじめにBallManagerに登録する
    private void Start()
    {
        TeamFacade.Instance.BallManager.RegisterBall(this);
    }

    // ボールの状態を同期する
    public void synchronizedBallState(int ownerID)
    {
        Debug.Log("ボール同期開始:" + ownerID);
        _synchronizedNow = true;
        photonView.RPC(nameof(synchronizedBallState), RpcTarget.All, ownerID);
    }

    // アバターを設定する(同期)
    [PunRPC]
    private void synchronizedBallState(int ownerID, PhotonMessageInfo info)
    {
        Debug.Log("ボール同期中:" + ownerID);
        // ボールの所持者IDとチームを変更・同期する
        TeamFacade.Instance.BallManager.setBallOwnerIDAndTeam(ownerID);
        // ボールの当たり判定/物理を変更・同期する
        changeBallCollider(ownerID);
        // ボールの階層構造を変更・同期する
        changeBallParent(ownerID);
        // 同期終了
        _synchronizedNow = false;
    }

    // ボールの当たり判定を変更する
    private void changeBallCollider(int ownerID)
    {
        _rigidbody.useGravity = ownerID < 0;
        _collider.enabled = ownerID < 0;
        _rigidbody.isKinematic = ownerID >= 0;
    }

    // ボールの親を変更する
    private void changeBallParent(int ownerID)
    {
        // オーナーがある場合
        if (ownerID > 0)
        {
            var teamRegist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
            if (teamRegist == null)
            {
                return;
            }

            var facade = teamRegist.FirstOrDefault(x =>
            {
                if (x == null) return false;
                var avatar = x.GetAvatar();
                return avatar != null && avatar.ViewID == ownerID;
            });
            if (facade == null){
                return;
            }
            GameObject ballKeep = facade.GetBallKeep();
            if (ballKeep != null)
            {
                this.transform.SetParent(ballKeep.transform);
                this.transform.localPosition = Vector3.zero;
                this.transform.localRotation = Quaternion.identity;
            }
        }
        // オーナーがない場合 = フリー状態
        else{
            TeamFacade.Instance.BallManager.changeBallParent();
        }
    }

    // ボールを止める
    public void stop()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        Debug.Log("ボールRigidbodyStop:" + _rigidbody.linearVelocity);
    }

    public void SetBallBuff(BallBuffKind kind, float attack = 0.0f)
    {
        _ballBuffKind = kind;
        _ballBuffAttack = kind == BallBuffKind.None ? 0.0f : Mathf.Max(0.0f, attack);
    }

    // 蹴る
    public void kick(Vector3 dir)
    {
        //_rigidbody.isKinematic = false;
        _rigidbody.linearVelocity = dir;
        Debug.Log("ボールRigidbodyKick:" + _rigidbody.linearVelocity);
    }

    // 所有権のリクエストが行われた時に呼ばれるコールバック
    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        //Debug.Log("リクエスト:" + targetView.ViewID + " request:" + requestingPlayer.NickName);
        //if(targetView.IsMine && targetView.ViewID == photonView.ViewID)
        //{
        //    targetView.TransferOwnership(requestingPlayer);
        //}
    }

    // 所有権の移譲が行われた時に呼ばれるコールバック
    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        if(targetView.ViewID == photonView.ViewID)
        {
            // Debug.Log("ViewId:" + targetView.ViewID + "の所有権が" + previousOwner.NickName + "から" + targetView.Owner.NickName + "に移譲されました");

            //// 委譲された側のみが送る
            //if(photonView.IsMine)
            //{
            //    // アバターを設定する(同期)
            //    //photonView.RPC(nameof(setAvatarBelongRPC), RpcTarget.All, _nextID);
            //}
        }
    }

    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
    {
        //throw new System.NotImplementedException();
        // Debug.Log("移譲エラー:" + senderOfFailedRequest.NickName + " targetView:"+targetView.ViewID);
    }

    
}
