using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun;

// アニマルのシュートアクション
public class AnimalAction_Shoot : AnimalAction_Base
{
    // このアクションが対応するボタンタイプ（bit演算で検索）
    public override int ButtonType => 1 << (int)AnimalButtonType.Shoot;

    [SerializeField] private AnimalFacade _myFacade;
    [SerializeField] private AnimalHandler _animalHandler;

    private Coroutine _shootCoroutine;

    public bool IsShootInProgress => _shootCoroutine != null;

    /// <summary>
    /// 基底クラスのExecuteメソッドの実装（プレイヤー操作前提）
    /// </summary>
    public override void Execute()
    {
        // ゲーム中以外か?
        if (!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME)) return;

        // このキャラがボールを保持しているかどうかを判定
        var teamFacade = TeamFacade.Instance;
        if (teamFacade == null || teamFacade.BallManager == null || _myFacade == null)
        {
            Debug.LogError("AnimalAction_Shoot: TeamFacade or BallManager or _myAvatar is null");
            return;
        }

        int ownerID = _myFacade.GetAvatar().ViewID;
        if (!teamFacade.BallManager.isHoldBall(ownerID))
        {
            // ボールを保持していない場合はシュートを行わない
            return;
        }

        // シュートを実行
        shoot();
    }

    // シュート
    public void shoot()
    {
        if (_shootCoroutine != null)
        {
            return;
        }

        // 自分のタグからゴールを取得（TeamFacade 経由）
        string tag = _myFacade.GetAvatar().gameObject.tag;
        var fieldHandler = TeamFacade.Instance != null ? TeamFacade.Instance.FieldObjectHandler : null;
        if (fieldHandler == null)
        {
            Debug.LogError("AnimalAction_Shoot: FieldObjectHandler is null");
            return;
        }
        GameObject targetGoal = fieldHandler.GetGoal(tag);

        Vector3 myPos = _myFacade.transform.position;
        Vector3 goalCenter = targetGoal.transform.position;
        Vector3 aimPoint = ResolveShootAimPoint(myPos, goalCenter, tag);
        Vector3 dir = (aimPoint - myPos).normalized;

        // アニメーションを先に行う
        _animalHandler.shoot();
        _myFacade.transform.forward = new Vector3(dir.x, 0.0f, dir.z);

        _shootCoroutine = StartCoroutine(executeShoot(aimPoint, tag));
    }

    private IEnumerator executeShoot(Vector3 aimPoint, string shooterTag)
    {
        const float windUpSeconds = 0.2f;
        yield return new WaitForSeconds(windUpSeconds);

        var teamFacade = TeamFacade.Instance;
        if (_myFacade == null || teamFacade == null || teamFacade.BallManager == null)
        {
            _shootCoroutine = null;
            yield break;
        }

        int ownerID = _myFacade.GetAvatar().ViewID;
        if (!teamFacade.BallManager.isHoldBall(ownerID))
        {
            _shootCoroutine = null;
            yield break;
        }

        Vector3 myPos = _myFacade.transform.position;
        Vector3 dir = (aimPoint - myPos).normalized;
        float distance = Vector3.Distance(myPos, aimPoint);
        _myFacade.transform.forward = new Vector3(dir.x, 0.0f, dir.z);

        BallHandler ball = teamFacade.BallManager.Ball;
        bool success = teamFacade.BallManager.changeOwnership(-1, BallManager_State.BALL_STATE.SHOOT);

        yield return new WaitUntil(() => !ball.SynchronizedNow);

        Vector3 kickDir = BuildShootKickVector(myPos, aimPoint);
        ball.kick(kickDir);

        var specialGauge = _myFacade.GetSpecialGauge();
        if (specialGauge != null)
        {
            specialGauge.AddGaugeValue(ConstData.SPECIAL_GAUGE_VALUE);
        }

        _shootCoroutine = null;
        yield return null;
    }

    private Vector3 ResolveShootAimPoint(Vector3 shooterPosition, Vector3 goalCenter, string shooterTag)
    {
        AnimalFacade defendingGk = ShootAimPolicy.FindDefendingGoalkeeper(_myFacade);
        Vector3? gkPos = defendingGk != null ? defendingGk.transform.position : null;
        return ShootAimPolicy.ResolveAimPoint(shooterPosition, goalCenter, gkPos);
    }

    private Vector3 BuildShootKickVector(Vector3 shooterPosition, Vector3 aimPoint)
    {
        AnimalInfo animalInfo = _myFacade != null ? _myFacade.GetAnimalInfo() : null;
        AnimalSpritInfo animalSpritInfo = _myFacade != null ? _myFacade.GetAnimalSpritInfo() : null;
        Param_SpritData paramSpritData = animalSpritInfo != null ? animalSpritInfo.ParamSpritData : null;

        float baseShoot = paramSpritData != null ? paramSpritData.GetBaseParameterValue(Param_SpritData.ParameterType.Shoot) : 0.8f;
        float increaseShoot = paramSpritData != null ? paramSpritData.GetIncreaseParameterValue(Param_SpritData.ParameterType.Shoot) : 0f;
        float spritShoot = animalInfo != null ? animalInfo.Shoot : 0f;
        bool hasDefendingGk = ShootAimPolicy.FindDefendingGoalkeeper(_myFacade) != null;

        return ShootAimPolicy.BuildKickVector(
            shooterPosition,
            aimPoint,
            spritShoot,
            baseShoot,
            increaseShoot,
            hasDefendingGk);
    }
}
