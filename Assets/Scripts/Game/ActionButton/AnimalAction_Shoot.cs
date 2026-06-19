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
        // 自分のタグからゴールを取得（TeamFacade 経由）
        string tag = _myFacade.GetAvatar().gameObject.tag;
        var fieldHandler = TeamFacade.Instance != null ? TeamFacade.Instance.FieldObjectHandler : null;
        if (fieldHandler == null)
        {
            Debug.LogError("AnimalAction_Shoot: FieldObjectHandler is null");
            return;
        }
        GameObject targetGoal = fieldHandler.GetGoal(tag);

        // 自分の位置とゴールから距離と方向を計算
        Vector3 myPos = _myFacade.transform.position;
        Vector3 targetPos = targetGoal.transform.position;
        Vector3 dir = (targetPos - myPos).normalized;
        float d = Vector3.Distance(myPos, targetPos);

        // アニメーションを先に行う
        _animalHandler.shoot();
        _myFacade.transform.forward = new Vector3(dir.x, 0.0f, dir.z);

        // シュート実行
        StartCoroutine(executeShoot(dir, d));
    }

    private IEnumerator executeShoot(Vector3 dir, float distance)
    {
        Debug.Log("シュートの方向:" + dir);
        yield return new WaitForSeconds(0.2f);

        BallHandler ball = TeamFacade.Instance.BallManager.Ball;
        // ボールの所有権をフリーに
        bool success = TeamFacade.Instance.BallManager.changeOwnership(-1, BallManager_State.BALL_STATE.SHOOT);

        // ボールの同期を待機
        yield return new WaitUntil(() => !ball.SynchronizedNow);

        Vector3 kickDir = BuildShootKickVector(dir, distance);

        ball.kick(kickDir);
        // スペシャルゲージの増加
        var specialGauge = _myFacade.GetSpecialGauge();
        if (specialGauge != null)
        {
            specialGauge.AddGaugeValue(ConstData.SPECIAL_GAUGE_VALUE);
        }

        yield return null;
    }

    private Vector3 BuildShootKickVector(Vector3 dir, float distance)
    {
        AnimalInfo animalInfo = _myFacade != null ? _myFacade.GetAnimalInfo() : null;
        AnimalSpritInfo animalSpritInfo = _myFacade != null ? _myFacade.GetAnimalSpritInfo() : null;
        Param_SpritData paramSpritData = animalSpritInfo != null ? animalSpritInfo.ParamSpritData : null;

        // シュートの強さ（到達時間）
        float baseShoot = paramSpritData != null ? paramSpritData.GetBaseParameterValue(Param_SpritData.ParameterType.Shoot) : 0.8f;
        float increaseShoot = paramSpritData != null ? paramSpritData.GetIncreaseParameterValue(Param_SpritData.ParameterType.Shoot) : 0f;
        float spritShoot = animalInfo != null ? animalInfo.Shoot : 0f;
        float shootTime = baseShoot + (increaseShoot * spritShoot / 100.0f);
        shootTime = Mathf.Max(0.01f, shootTime);

        // Shoot(0-100)に応じて水平方向のブレを付与（100ならブレなし）
        float clampedShoot = Mathf.Clamp(spritShoot, 0f, 100f);
        float inaccuracy = 1.0f - (clampedShoot / 100.0f);
        float spreadAngle = Random.Range(-ConstData.MAX_SHOOT_SPREAD_ANGLE, ConstData.MAX_SHOOT_SPREAD_ANGLE) * inaccuracy;
        Vector3 adjustedDir = Quaternion.AngleAxis(spreadAngle, Vector3.up) * dir.normalized;

        float speed = distance / shootTime;
        return adjustedDir * speed;
    }
}
