using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// プレイヤー用のアニマルセレクタ。
/// BallManager の状態に応じて、操作対象の味方アニマルを自動で切り替える。
/// </summary>
public class AnimalSelector_Player : AnimalSelector_Base
{
    [SerializeField] private CameraTargetGroupHandler _cameraTarget;
    [SerializeField] private UI_SpecialGauge _specialGauge;   // スペシャルゲージ UI

    public void Update()
    {
        if (GoapMainNpcVerifyEnvironment.IsActive)
        {
            return;
        }

        var teamFacade = TeamFacade.Instance;
        var ballManager = teamFacade != null ? teamFacade.BallManager : null;
        var state = ballManager != null ? ballManager.State : null;

        // BallManager / Ball / State が無い
        if (ballManager == null || state == null)
        {
            Debug.LogError("AnimalSelector: BallManager or State is null");
            return;
        }
        // ボールがない
        if(ballManager.Ball == null){
            return;
        }

        // スペシャルゲージの更新
        var selectedSpecialGauge = _selAnimalFacade != null ? _selAnimalFacade.GetSpecialGauge() : null;
        if (selectedSpecialGauge != null && selectedSpecialGauge.UpdateGauge)
        {
            updateSpecialGauge(_selAnimalFacade);
        }

        // ディフェンス状態 = 敵側 OR フリー
        if(state.BelongTeam == BallManager_State.BELONG_TEAM.ENEMY ||
           state.BelongTeam == BallManager_State.BELONG_TEAM.FREE)
        {
            // パス中とシュート中は更新しない?
            if(state.BallState == BallManager_State.BALL_STATE.PASS ||
               state.BallState == BallManager_State.BALL_STATE.SHOOT)
            {
                return;
            }
            TrackDefenceBallContext(state);
            selectDefenceAnimal();
        }
    }

    // チームリストの取得（自動切り替え候補＝GK以外の味方フィールド全員）
    protected override IEnumerable<AnimalFacade> getTeamList()
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            yield break;
        }

        foreach (var facade in regist.Allys)
        {
            if (facade != null && !facade.IsGK())
            {
                yield return facade;
            }
        }
    }

    protected override void OnSelectAnimalChanged(AnimalFacade newFacade)
    {
        if (GoapMainNpcVerifyEnvironment.IsActive)
        {
            return;
        }

        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        squad?.SetActiveHumanPlayer(newFacade);
    }

    protected override void updateCameraAndHPGauge(AnimalFacade newFacade)
    {
        // カメラ変更（Facade 経由で transform を取得）
        _cameraTarget.AddTarget(newFacade.transform, 1.0f, 1.0f);
        if (newFacade != null)
        {
            _cameraTarget.RemoveTarget(newFacade.transform);
        }

        // HPゲージの表示/非表示を更新
        UpdateHPGauge(_selAnimalFacade, newFacade);
    }

    // HPゲージの表示状態を更新する
    private void UpdateHPGauge(AnimalFacade previousFacade, AnimalFacade newFacade)
    {
        // 以前の選択キャラの HP ゲージを非表示
        if (previousFacade != null)
        {
            var lastHPGauge = previousFacade.GetHPGauge();
            if (lastHPGauge != null)
            {
                lastHPGauge.SetHPGaugeVisibility(false);
            }
        }

        // 新しい選択キャラの HP ゲージを表示
        if (newFacade != null)
        {
            var hpGauge = newFacade.GetHPGauge();
            if (hpGauge != null)
            {
                hpGauge.SetHPGaugeVisibility(true);
            }
        }
    }

    // スペシャルゲージの更新
    protected override void updateSpecialGauge(AnimalFacade newFacade)
    {
        if (_specialGauge != null && newFacade != null)
        {
            var facadeSpecialGauge = newFacade.GetSpecialGauge();
            if (facadeSpecialGauge == null)
            {
                return;
            }

            _specialGauge.UpdateGauge(facadeSpecialGauge.GaugeValue);
            facadeSpecialGauge.UpdateGaugeComplete();
        }
    }
}

