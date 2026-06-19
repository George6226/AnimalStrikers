using UnityEngine;
using System.Linq;

// ボールの状態と所有者情報を扱うハンドラー
public class BallManager_State : MonoBehaviour
{
    // ボールの状態
    public enum BALL_STATE
    {
        HOLD = 0,
        FREE,
        PASS,
        SHOOT,
    }

    // ボールの所属チーム
    public enum BELONG_TEAM {
        PLAYER = 0,
        ENEMY,
        FREE,
    }

    // ボールの状態
    private BALL_STATE _ballState = BALL_STATE.FREE;
    public BALL_STATE BallState
    {
        get { return _ballState; }
        set { _ballState = value; }
    }

    // ボールの所属チーム
    private BELONG_TEAM _belongTeam = BELONG_TEAM.FREE;
    public BELONG_TEAM BelongTeam
    {
        get { return _belongTeam; }
    }

    private float _ballStateTimer = 0.0f;
    private const float TIME_BALLSTATE_RESET = 1.0f;    // ボールの状態をリセットする時間


    // ボールがフリー状態になるかを更新
    public bool updateBallFree()
    {
        // パス中 OR シュート中
        if(_ballState == BallManager_State.BALL_STATE.PASS || _ballState == BallManager_State.BALL_STATE.SHOOT)
        {
            _ballStateTimer += Time.deltaTime;
            if(_ballStateTimer >= TIME_BALLSTATE_RESET)
            {
                _ballState = BallManager_State.BALL_STATE.FREE;
                _ballStateTimer = 0.0f;

                return true;
            }
        }
        return false;
    }

    // ボールの所持者の位置を取得
    public Vector3 getBallOwnerPosition(PhotonAvatarContainerChild character)
    {
        Vector3 ownerPosition = Vector3.zero;

        // 所属チームの設定
        if (character == null){
            _belongTeam = BELONG_TEAM.FREE;
        }
        else
        {
            _belongTeam = character.tag == "PlayerAgent" ? BELONG_TEAM.PLAYER : BELONG_TEAM.ENEMY;
            ownerPosition = character.transform.position;
        }
        return ownerPosition;
    }
}

