using Photon.Pun;
using UnityEngine;

/// <summary>
/// 6-B P1: FREE ボールがゴールキック条件を満たしたら守備 GK に HOLD を割り当てる。
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
        TryBeginGoalKick();
    }

    private void TryBeginGoalKick()
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

        var classify = OutOfPlayClassifier.Classify(
            ball.transform.position,
            teamBB.FieldInfo);
        if (!GoalKickSetPieceRules.IsGoalKickCandidate(classify))
        {
            return;
        }

        var gk = SetPieceAssignmentRules.FindRestartingGoalkeeper(classify.RestartTeamIsOther);
        if (gk == null)
        {
            return;
        }

        var avatar = gk.GetAvatar();
        if (avatar == null || avatar.ViewID <= 0)
        {
            return;
        }

        float depth = GoalKickSetPieceRules.ResolveHomeDepth(classify.RestartTeamIsOther);
        Vector3 ballPos = SetPieceAssignmentRules.ResolveGoalKickBallPosition(
            teamBB.FieldInfo,
            classify.RestartTeamIsOther,
            depth);
        ballPos.y = Mathf.Max(0.35f, ballPos.y);

        if (!ballManager.AssignGoalKickPossession(avatar.ViewID, ballPos, _suppressSeconds))
        {
            return;
        }

        _cooldownUntil = Time.time + Mathf.Max(0.5f, _cooldownSeconds);
        Debug.Log(
            $"[SetPiece] GoalKick assigned gk={gk.name} otherTeam={classify.RestartTeamIsOther} " +
            $"ballPos={ballPos}");
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
