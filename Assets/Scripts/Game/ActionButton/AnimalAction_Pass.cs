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

    // パスをおこなす
    public void pass()
    {
        // 一番近くの味方を検索する
        AnimalFacade ally = _animalPassSearch != null ? _animalPassSearch.FindAllyForPass(_myFacade) : null;
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

        // 味方に選択を移す
        var allyAvatar = ally.GetAvatar();
        if (allyAvatar != null)
        {
            TeamFacade.Instance.AnimalSelectorManager.SetSelectAnimal(ally, allyAvatar.tag);
        }

        // アニメーションを先に行う
        _animalHandler.shoot();
        _myFacade.transform.forward = new Vector3(dir.x, 0.0f, dir.z);

        // パス
        StartCoroutine(executePass(dir, d, needsLob));
    }

    private IEnumerator executePass(Vector3 dir, float distance, bool needsLob)
    {
        yield return new WaitForSeconds(0.2f);
        // ボール
        BallHandler ball = TeamFacade.Instance.BallManager.Ball;
        
        // ボールの所有権をパスに変更
        bool success = TeamFacade.Instance.BallManager.changeOwnership(-1, BallManager_State.BALL_STATE.PASS);
        
        // 同期終了まで待機
        yield return new WaitUntil(() => !ball.SynchronizedNow);

        // パスベクトルを計算してキック
        AnimalInfo animalInfo = _myFacade != null ? _myFacade.GetAnimalInfo() : null;
        float passStat = animalInfo != null ? animalInfo.Pass : 0f;
        Vector3 kickDir = _animalPassPhysics != null ? _animalPassPhysics.CalcKick(dir, distance, needsLob, passStat) : Vector3.zero;
        ball.kick(kickDir);

        yield return null;
    }
}
