using Photon.Pun;
using UnityEngine;

/// <summary>
/// 6-B: FREE ボールのアウトオブプレイを検知しセットプレイを開始する。
/// P1=ゴールキック / P2=スローイン（コーナーは未接続）。
/// </summary>
public class SetPieceRuntimeController : MonoBehaviour
{
    [SerializeField] private BallManager _ballManager;
    [SerializeField] private float _cooldownSeconds = GoalKickSetPieceRules.DefaultCooldownSeconds;
    [SerializeField] private float _suppressSeconds = GoalKickSetPieceRules.DefaultSuppressSeconds;

    private float _cooldownUntil;

    private void Awake()
    {
        if (_ballManager == null)
        {
            _ballManager = GetComponent<BallManager>();
        }

        if (_ballManager == null)
        {
            _ballManager = FindObjectOfType<BallManager>();
        }
    }

    private void Update()
    {
        TryBeginSetPiece();
    }

    private void TryBeginSetPiece()
    {
        if (!ShouldAuthorityHandleSetPiece())
        {
            return;
        }

        var teamFacade = TeamFacade.Instance;
        var teamBB = teamFacade != null ? teamFacade.TeamBlackboard : null;
        var ballManager = _ballManager != null ? _ballManager : teamFacade != null ? teamFacade.BallManager : null;
        if (teamBB == null || ballManager == null || ballManager.State == null)
        {
            return;
        }

        bool matchPlay = StateManager.Instance != null
            && StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME);
        if (!GoalKickSetPieceRules.ShouldEvaluate(
                matchPlay,
                teamBB.BallInfo.IsExistBall,
                ballManager.State.BallState,
                Time.time,
                _cooldownUntil))
        {
            return;
        }

        var ball = ballManager.Ball;
        if (ball == null)
        {
            return;
        }

        bool? lastTouchByOther = ThrowInSetPieceRules.ResolveLastTouchByOtherTeam(
            teamBB.BallInfo.LastPossessionBelongTeam);
        var classify = OutOfPlayClassifier.Classify(
            ball.transform.position,
            teamBB.FieldInfo,
            lastTouchByOtherTeam: lastTouchByOther);

        if (GoalKickSetPieceRules.IsGoalKickCandidate(classify))
        {
            if (TryAssignGoalKick(ballManager, teamBB, classify))
            {
                MarkCooldown();
            }

            return;
        }

        if (ThrowInSetPieceRules.IsThrowInCandidate(classify))
        {
            if (TryAssignThrowIn(ballManager, teamBB, classify, ball.transform.position))
            {
                MarkCooldown();
            }
        }
    }

    private bool TryAssignGoalKick(
        BallManager ballManager,
        TeamBlackboard teamBB,
        OutOfPlayClassifier.Result classify)
    {
        var gk = SetPieceAssignmentRules.FindRestartingGoalkeeper(classify.RestartTeamIsOther);
        if (gk == null)
        {
            return false;
        }

        var avatar = gk.GetAvatar();
        if (avatar == null || avatar.ViewID <= 0)
        {
            return false;
        }

        float depth = GoalKickSetPieceRules.ResolveHomeDepth(classify.RestartTeamIsOther);
        Vector3 ballPos = SetPieceAssignmentRules.ResolveGoalKickBallPosition(
            teamBB.FieldInfo,
            classify.RestartTeamIsOther,
            depth);
        ballPos.y = Mathf.Max(0.35f, ballPos.y);

        if (!ballManager.AssignGoalKickPossession(avatar.ViewID, ballPos, _suppressSeconds))
        {
            return false;
        }

        Debug.Log(
            $"[SetPiece] GoalKick assigned gk={gk.name} otherTeam={classify.RestartTeamIsOther} " +
            $"ballPos={ballPos}");
        return true;
    }

    private bool TryAssignThrowIn(
        BallManager ballManager,
        TeamBlackboard teamBB,
        OutOfPlayClassifier.Result classify,
        Vector3 ballWorld)
    {
        Vector3 ballPos = SetPieceAssignmentRules.ResolveThrowInBallPosition(
            teamBB.FieldInfo,
            classify.SideSignX,
            ballWorld.z);
        ballPos.y = Mathf.Max(0.35f, ballPos.y);

        var taker = SetPieceAssignmentRules.FindNearestRestartingFieldPlayer(
            classify.RestartTeamIsOther,
            ballPos);
        if (taker == null)
        {
            return false;
        }

        var avatar = taker.GetAvatar();
        if (avatar == null || avatar.ViewID <= 0)
        {
            return false;
        }

        if (!ballManager.AssignThrowInPossession(avatar.ViewID, ballPos, _suppressSeconds))
        {
            return false;
        }

        Debug.Log(
            $"[SetPiece] ThrowIn assigned taker={taker.name} otherTeam={classify.RestartTeamIsOther} " +
            $"side={classify.SideSignX} ballPos={ballPos}");
        return true;
    }

    private void MarkCooldown()
    {
        _cooldownUntil = Time.time + Mathf.Max(0.5f, _cooldownSeconds);
    }

    private static bool ShouldAuthorityHandleSetPiece()
    {
        if (PhotonPlayerInfo.Instance != null
            && PhotonPlayerInfo.Instance.BattleMode == ConstData.BATTLE_MODE.NPC)
        {
            return true;
        }

        return PhotonNetwork.IsMasterClient;
    }
}
