using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

// アニマルのパスアクション
public class AnimalAction_Pass : AnimalAction_Base
{
    // このアクションが対応するボタンタイプ（bit演算で検索）
    public override int ButtonType => 1 << (int)AnimalButtonType.Pass;

    // 検索と物理演算
    [SerializeField] private AnimalPass_Search _animalPassSearch;
    [SerializeField] private AnimalPass_Physics _animalPassPhysics;

    [SerializeField] private AnimalFacade _myFacade;
    [SerializeField] private AnimalHandler _animalHandler;

    private Coroutine _passCoroutine;

    public bool IsPassInProgress => _passCoroutine != null;

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
            Debug.LogError("AnimalAction_Pass: TeamFacade or BallManager or _myAvatar is null");
            return;
        }

        int ownerID = _myFacade.GetAvatar().ViewID;
        if (!teamFacade.BallManager.isHoldBall(ownerID))
        {
            // ボールを保持していない場合はパスを行わない
            return;
        }

        // パスを実行
        pass();
    }

    // パスをおこなう
    public void pass()
    {
        pass(null);
    }

    public void pass(AnimalFacade explicitTarget)
    {
        if (_passCoroutine != null)
        {
            GoapPassDiagnostic.Log(_myFacade, "Skipped duplicate pass request");
            return;
        }

        // 一番近くの味方を検索する
        AnimalFacade ally = explicitTarget;
        if (ally == null)
        {
            ally = _animalPassSearch != null ? _animalPassSearch.FindAllyForPass(_myFacade) : null;
        }

        if (ally == null){
            Debug.Log("[AnimalAction_Pass]:パス相手がいない");
            return;
        }
        // パス方向・距離は「ボール保持位置（BallKeep）基準」で計算する
        GameObject myBallKeep = _myFacade != null ? _myFacade.GetBallKeep() : null;
        GameObject allyBallKeep = ally != null ? ally.GetBallKeep() : null;
        Vector3 myPos = myBallKeep != null ? myBallKeep.transform.position : _myFacade.transform.position;
        Vector3 allyPos = allyBallKeep != null ? allyBallKeep.transform.position : ally.transform.position;

        Vector3 dir = (allyPos - myPos).normalized;
        float d = Vector3.Distance(myPos, allyPos);

        // パスコース上に他のキャラクターが存在するかチェックする
        bool needsLob = _animalPassSearch != null && _animalPassSearch.IsCharacterInPassLine(_myFacade.gameObject, ally.gameObject);

        GoapPassDiagnostic.LogPhase(_myFacade, ally, "Start", myPos, allyPos, d, needsLob);
        GoapPassFlightTracker.RegisterPass(_myFacade, ally);
        GoapPassFlightTracker.SetIntendedReceivePosition(allyPos);

        // 味方に選択を移す
        var allyAvatar = ally.GetAvatar();
        if (allyAvatar != null)
        {
            TeamFacade.Instance.AnimalSelectorManager.SetSelectAnimal(ally, allyAvatar.tag);
        }

        // アニメーションを先に行う
        _animalHandler.shoot();
        _myFacade.transform.forward = new Vector3(dir.x, 0.0f, dir.z);

        _passCoroutine = StartCoroutine(executePass(ally, needsLob, myPos, allyPos));
    }

    private IEnumerator executePass(AnimalFacade target, bool needsLobStart, Vector3 passerPosStart, Vector3 targetPosStart)
    {
        const float windUpSeconds = 0.2f;
        float windUpStart = Time.time;
        yield return new WaitForSeconds(windUpSeconds);

        var teamFacade = TeamFacade.Instance;
        if (target == null || _myFacade == null || teamFacade == null || teamFacade.BallManager == null)
        {
            GoapPassDiagnostic.Log(_myFacade, "Cancelled reason=missing_refs");
            GoapPassFlightTracker.Clear();
            _passCoroutine = null;
            yield break;
        }

        int ownerID = _myFacade.GetAvatar().ViewID;
        if (!teamFacade.BallManager.isHoldBall(ownerID))
        {
            GoapPassDiagnostic.Log(
                _myFacade,
                $"Cancelled reason=not_holding_ball windUp={(Time.time - windUpStart) * 1000f:F0}ms");
            GoapPassFlightTracker.Clear();
            _passCoroutine = null;
            yield break;
        }

        GameObject myBallKeep = _myFacade.GetBallKeep();
        GameObject allyBallKeep = target.GetBallKeep();
        Vector3 myPos = myBallKeep != null ? myBallKeep.transform.position : _myFacade.transform.position;
        // 受け手 GOAP は停止受けで成功率が高い。リードキックは受け手が走り続ける前提のため外す。
        Vector3 allyPos = PassLeadPolicy.ResolveKickTargetPosition(
            target,
            myPos,
            estimatedFlightSeconds: 0f,
            receiverIsMoving: false);
        GoapPassFlightTracker.SetIntendedReceivePosition(allyPos);
        Vector3 dir = (allyPos - myPos).normalized;
        float distance = PassLeadPolicy.ClampPassDistance(Vector3.Distance(myPos, allyPos));
        bool needsLobKick = _animalPassSearch != null
            && _animalPassSearch.IsCharacterInPassLine(_myFacade.gameObject, target.gameObject);
        needsLobKick = !PassLeadPolicy.ShouldPreferGroundPass(distance, receiverIsMoving: false, needsLobKick)
            && needsLobKick;
        _myFacade.transform.forward = new Vector3(dir.x, 0.0f, dir.z);

        GoapPassDiagnostic.LogPhase(
            _myFacade,
            target,
            "PreKick",
            myPos,
            allyPos,
            distance,
            needsLobKick,
            $"windUpMs={(Time.time - windUpStart) * 1000f:F0}");

        BallHandler ball = teamFacade.BallManager.Ball;

        teamFacade.BallManager.changeOwnership(-1, BallManager_State.BALL_STATE.PASS);

        yield return new WaitUntil(() => !ball.SynchronizedNow);

        AnimalInfo animalInfo = _myFacade.GetAnimalInfo();
        float passStat = animalInfo != null ? animalInfo.Pass : 0f;
        Vector3 kickDir = _animalPassPhysics != null
            ? _animalPassPhysics.CalcKick(dir, distance, needsLobKick, passStat)
            : Vector3.zero;

        GoapPassDiagnostic.LogKick(
            _myFacade,
            target,
            passerPosStart,
            targetPosStart,
            myPos,
            allyPos,
            needsLobStart,
            needsLobKick,
            passStat,
            kickDir);

        ball.kick(kickDir);
        GoapPassDiagnostic.Log(_myFacade, $"Kicked lob={needsLobKick} dist={distance:F2}");

        var specialGauge = _myFacade.GetSpecialGauge();
        if (specialGauge != null)
        {
            specialGauge.AddGaugeValue(ConstData.SPECIAL_GAUGE_VALUE_ON_PASS);
        }

        _passCoroutine = null;
        yield return null;
    }
}
