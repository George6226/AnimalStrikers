using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// NPC 用のアニマルセレクタ。
/// 将来的に GOAP / TeamBlackboard 連携で NPC の操作対象を切り替える際の窓口とする想定。
/// 現状は共通基盤のみを利用し、具体的な選択ロジックは今後実装する。
/// </summary>
public class AnimalSelector_NPC : AnimalSelector_Base
{
    public void Update()
    {
        var teamFacade = TeamFacade.Instance;
        var ballManager = teamFacade != null ? teamFacade.BallManager : null;
        var state = ballManager != null ? ballManager.State : null;

        // BallManager / State が無い
        if (ballManager == null || state == null){
            Debug.LogError("AnimalSelector: BallManager or State is null");
            return;
        }
        // ボールがまだ
        if(ballManager.Ball == null){
            return;
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

    // チームリストの取得
    protected override IEnumerable<AnimalFacade> getTeamList()
    {
        return TeamFacade.Instance.TeamRegist.Enemies;
    }
}

