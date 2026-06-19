using System.Collections;
using UnityEngine;

// ライオンのスペシャル：自分がボール保持中のみ。ためののちシュート。
public class LionSpecialAction : AnimalSpecialActionBase
{
    [SerializeField] private AnimalFacade _myFacade;
    [SerializeField] private AnimalAction_Shoot _shootAction;

    /// <summary>
    /// <see cref="SetEffectCallback"/> で対象エフェクトのコールバック設定後に true。秒数待ちではなくこのフラグでシュートタイミングを合わせる。
    /// </summary>
    private bool _shootTriggerReady;

    private Coroutine _routine;

    private void Awake()
    {
        if (_myFacade == null)
        {
            _myFacade = GetComponentInParent<AnimalFacade>();
        }
    }

    public override bool CanExecuteSpecial()
    {
        var teamFacade = TeamFacade.Instance;
        if (teamFacade == null || teamFacade.BallManager == null || _myFacade == null)
        {
            return false;
        }

        var avatar = _myFacade.GetAvatar();
        if (avatar == null)
        {
            return false;
        }

        return teamFacade.BallManager.isHoldBall(avatar.ViewID);
    }

    public override void ExecuteSpecial()
    {
        if (_shootAction == null)
        {
            Debug.LogError("LionSpecialAction: AnimalAction_Shoot が未設定です");
            return;
        }

        if (!CanExecuteSpecial())
        {
            return;
        }

        faceTowardGoal();

        _shootTriggerReady = false;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _routine = StartCoroutine(specialShootRoutine());
    }

    /// <summary>
    /// 自分のタグに対応するゴール方向へ体を向ける（<see cref="AnimalAction_Shoot.shoot"/> と同じ基準）
    /// </summary>
    private void faceTowardGoal()
    {
        if (_myFacade == null)
        {
            return;
        }

        var avatar = _myFacade.GetAvatar();
        if (avatar == null)
        {
            return;
        }

        var fieldHandler = TeamFacade.Instance != null ? TeamFacade.Instance.FieldObjectHandler : null;
        if (fieldHandler == null)
        {
            return;
        }

        GameObject targetGoal = fieldHandler.GetGoal(avatar.gameObject.tag);
        if (targetGoal == null)
        {
            return;
        }

        Vector3 myPos = _myFacade.transform.position;
        Vector3 targetPos = targetGoal.transform.position;
        Vector3 flat = new Vector3(targetPos.x - myPos.x, 0.0f, targetPos.z - myPos.z);
        if (flat.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        _myFacade.transform.forward = flat.normalized;
    }

    /// <summary>
    /// <see cref="PhotonAvatarContainer"/> の Ally / Enemy 判定と同じ基準でボールバフ陣営を決める。
    /// </summary>
    private static BallBuffKind ResolveBallBuffKindFromAvatarTag(PhotonAvatarContainerChild avatar)
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

    private IEnumerator specialShootRoutine()
    {
        yield return new WaitUntil(() => _shootTriggerReady);

        _shootTriggerReady = false;

        if (!CanExecuteSpecial())
        {
            _routine = null;
            yield break;
        }

        if (_shootAction != null)
        {
            var ballManager = TeamFacade.Instance != null ? TeamFacade.Instance.BallManager : null;
            if (ballManager != null && ballManager.Ball != null)
            {
                var avatar = _myFacade != null ? _myFacade.GetAvatar() : null;
                BallBuffKind buffKind = ResolveBallBuffKindFromAvatarTag(avatar);
                if (buffKind != BallBuffKind.None)
                {
                    float attackerAttack = _myFacade != null && _myFacade.GetAnimalInfo() != null ? _myFacade.GetAnimalInfo().Attack : 0.0f;
                    ballManager.Ball.SetBallBuff(buffKind, attackerAttack);
                }
            }
            _shootAction.shoot();
        }

        _routine = null;
    }

    public override void SetEffectCallback(GameObject effect)
    {
        if (effect == null)
        {
            return;
        }

        // Debug.Log("LionSpecialAction: SetEffectCallback " + effect.name);

        // ライオン用エフェクト：コールバックが設定されたタイミングで routine 側がシュートする
        if (effect.name.Contains("SPAttack_Lion"))
        {
            _shootTriggerReady = true;
        }
    }
}
