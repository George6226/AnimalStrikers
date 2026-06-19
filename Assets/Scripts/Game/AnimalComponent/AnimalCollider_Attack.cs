using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アニマルの攻撃当たり判定
public class AnimalCollider_Attack : MonoBehaviour
{
    [SerializeField] private AnimalFacade _myFacade;
    private bool _specialNow = false;
    public bool SpecialNow
    {
        set{_specialNow = value;}
    }

    private void Awake()
    {
        if (_myFacade == null)
        {
            _myFacade = GetComponentInParent<AnimalFacade>();
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        PhotonAvatarContainerChild myAvatar = _myFacade != null ? _myFacade.GetAvatar() : null;
        if (myAvatar == null)
        {
            return;
        }

        // Bodyに当たったら
        if (other.tag.Equals("Body"))
        {
            AnimalFacade target = other.transform.parent.GetComponent<AnimalFacade>();
            if(target == null){
                return;
            }
            // 相手側
            var targetAvatar = target.GetAvatar();
            string targetTag = targetAvatar != null ? targetAvatar.tag : string.Empty;
            // 同じタグならば
            if (myAvatar.tag == targetTag){
                return;
            }

            // 今ボールを持っていない（TeamFacade 経由で TeamBlackboard を参照）
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            int ballOwnerId = (teamBB != null && teamBB.BallInfo != null) ? teamBB.BallInfo.BallOwnerID : -1;
            int targetViewId = targetAvatar != null ? targetAvatar.ViewID : -1;
            if (teamBB == null || targetAvatar == null || targetAvatar.ViewID != teamBB.BallInfo.BallOwnerID){
                return;
            }

            // ボール情報
            BallHandler hBall = TeamFacade.Instance.BallManager.Ball;

            // TeamState がバリア中ならボール奪取自体を無効化
            bool hasBarrier = TeamFacade.Instance != null
                && TeamFacade.Instance.TeamState != null
                && TeamFacade.Instance.TeamState.HasBarrierByTag(targetAvatar.tag);

            // ID変更/所有権移動
            if (!hasBarrier && TeamFacade.Instance.BallManager.changeOwnership(myAvatar.ViewID, BallManager_State.BALL_STATE.HOLD))
            {
                // 相手キャラにダメージを与える
                float myAttack = _myFacade != null && _myFacade.GetAnimalInfo() != null ? _myFacade.GetAnimalInfo().Attack : 0.0f;
                bool hasMyTeamAttackBuff = TeamFacade.Instance != null
                    && TeamFacade.Instance.TeamState != null
                    && TeamFacade.Instance.TeamState.HasAttackBuffByTag(myAvatar.tag);
                bool hasTargetTeamAttackBuff = TeamFacade.Instance != null
                    && TeamFacade.Instance.TeamState != null
                    && TeamFacade.Instance.TeamState.HasAttackBuffByTag(targetAvatar.tag);
                if (hasMyTeamAttackBuff)
                {
                    myAttack *= 2.0f;
                }
                float targetDefense = target.GetAnimalInfo() != null ? target.GetAnimalInfo().Defense : 0.0f;
                float damage;
                if (_specialNow)
                {
                    damage = ConstData.SPECIAL_ATTACK_DAMAGE;
                }
                else if (hasTargetTeamAttackBuff)
                {
                    damage = 0.0f;
                }
                else
                {
                    damage = ConstData.BASE_ATTACK_DAMAGE + ((myAttack - targetDefense) / 2.0f);
                    damage = Mathf.Max(ConstData.MIN_ATTACK_DAMAGE, damage);
                }
                PhotonAnimalFacade.TryRequestApplyDamage(target, damage);
            }
        }
    }
}
