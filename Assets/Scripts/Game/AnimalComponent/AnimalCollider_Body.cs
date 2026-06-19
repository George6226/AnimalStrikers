using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// アニマルの体に対する当たり判定(足/胸/頭)
public class AnimalCollider_Body : MonoBehaviour
{
    [SerializeField] private AnimalFacade _animalFacade;
    [SerializeField] private Transform ballHoldPosition; // ボールを保持する位置

    // アニマルの選択（TeamFacade 経由で取得）
    private AnimalSelector_Manager _animalSelect;
    void Start(){
        _animalSelect = TeamFacade.Instance != null ? TeamFacade.Instance.AnimalSelectorManager : null;
    }

    // ぶつかった場合
    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("Body:OnTriggerEnter:"+other.name);

        // ボールにぶつかった
        if (other.gameObject.tag.Equals("Ball"))
        {
            GameObject ball = other.gameObject;
            BallHandler hBall = ball.GetComponent<BallHandler>();
            if (hBall == null)
            {
                return;
            }

            BallBuffKind currentBuff = hBall.BuffKind;
            Debug.Log("Ball Buff State: " + currentBuff);
            if (_animalFacade != null
                && IsOppositeBallBuffToSelf(_animalFacade.GetAvatar(), currentBuff))
            {
                float ballAttack = hBall.BuffAttack;
                float myDefense = _animalFacade.GetAnimalInfo() != null ? _animalFacade.GetAnimalInfo().Defense : 0.0f;
                float damage = ConstData.BASE_ATTACK_DAMAGE + ((ballAttack - myDefense) / 2.0f);
                damage = Mathf.Max(ConstData.MIN_ATTACK_DAMAGE, damage);
                PhotonAnimalFacade.TryRequestApplyDamage(_animalFacade, damage);
                hBall.SetBallBuff(BallBuffKind.None);
                return;
            }
            else
            {
                // 共通処理に委譲
                AcquireBallInternal(hBall);
            }
        }
    }

    // ボールの所有権変更
    private IEnumerator changeBallOwershipCoroutine(BallHandler hBall)
    {
        Debug.Log("Bodyタッチによる所有権変更を開始");
        var avatar = _animalFacade.GetAvatar();
        // ボールの所有権を変更する
        if (TeamFacade.Instance.BallManager.changeOwnership(avatar.ViewID, BallManager_State.BALL_STATE.HOLD))
        {
            // 同期が終わるまで待機
            yield return new WaitUntil(() => !hBall.SynchronizedNow);
            // ボールを止める
            hBall.stop();

            // ボールを所有したら
            if (avatar.tag.Equals(ConstData.PLAYER_TAG) && _animalSelect != null){
                _animalSelect.SetSelectAnimal(_animalFacade, avatar.tag);
            }
        }
    }

    // 共通処理: タグ条件に応じて所有権変更と選択状態を更新
    private void AcquireBallInternal(BallHandler hBall)
    {
        if (hBall == null) return;
        var avatar = _animalFacade.GetAvatar();
        // 自分の所持キャラ OR NPC
        if(avatar.tag.Equals(ConstData.PLAYER_TAG) || avatar.tag.Equals(ConstData.NPC_TAG))
        {
            // ボールの所有権を変更する
            StartCoroutine(changeBallOwershipCoroutine(hBall));
        }
        // 味方じゃない場合 = Sub or NPCの処理
        if(!avatar.tag.Equals(ConstData.PLAYER_TAG) && _animalSelect != null)
        {
            _animalSelect.SetSelectAnimal(null, avatar.tag);
        }
    }

    /// <summary>
    /// ボールのバフ陣営が自分（アバターの CurrentTag 基準の味方／敵）と異なるか。
    /// </summary>
    private static bool IsOppositeBallBuffToSelf(PhotonAvatarContainerChild avatar, BallBuffKind ballBuff)
    {
        if (ballBuff == BallBuffKind.None)
        {
            return false;
        }

        BallBuffKind mySide = ResolveBuffFactionFromAvatar(avatar);
        if (mySide == BallBuffKind.None)
        {
            return false;
        }

        return mySide != ballBuff;
    }

    /// <summary>LionSpecialAction と同基準でアバターから <see cref="BallBuffKind"/> を求める。</summary>
    private static BallBuffKind ResolveBuffFactionFromAvatar(PhotonAvatarContainerChild avatar)
    {
        if (avatar == null)
        {
            return BallBuffKind.None;
        }

        string tag = avatar.CurrentTag;
        if (string.IsNullOrEmpty(tag))
        {
            tag = avatar.gameObject.tag;
        }
        if (tag.Equals(ConstData.PLAYER_TAG))
        {
            return BallBuffKind.Ally;
        }
        if (tag.Equals(ConstData.NPC_TAG) || tag.Equals(ConstData.ENEMY_TAG))
        {
            return BallBuffKind.Enemy;
        }
        return BallBuffKind.None;
    }

    // 外部からボール所持を要求（BallHandler指定）
    public void TryAcquireBall(BallHandler hBall)
    {
        AcquireBallInternal(hBall);
    }


    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.gameObject.tag.Equals("Ball"))
    //    {
    //        if (BallManager.Instance.BallOwnerID == _avatar.ViewID)
    //        {
    //            BallManager.Instance.changeOwnership
    //        }
    //    }
    //}
}
