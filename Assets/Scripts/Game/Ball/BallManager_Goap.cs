using UnityEngine;

public class BallManager_Goap : MonoBehaviour
{
    // ボールを存在させる
    public void setExistBall()
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB != null)
        {
            teamBB.BallInfo.setExistBall();
        }
    }

    // ボールの状態を更新
    public void updateBallState(BallManager_State.BALL_STATE state)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB != null)
        {
            teamBB.BallInfo.updateBallState(state);
        }
    }

    // ボールの所持者のIDとを設定
    public void updateBallID(int ownerID, BallManager_State.BELONG_TEAM team, Vector3 ownerPosition)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB != null)
        {
            teamBB.BallInfo.updateBallID(ownerID, team, ownerPosition);
        }
    }
}

