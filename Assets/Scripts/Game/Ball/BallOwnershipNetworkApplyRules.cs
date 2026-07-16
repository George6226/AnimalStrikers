/// <summary>
/// 6-E P0: ボール所有権のネットワーク適用ルール（EditMode 向け純関数）。
/// RPC 受信側はローカルの kickoff suppress / changeOwnership guard を踏まずに論理 owner を合わせる。
/// </summary>
public static class BallOwnershipNetworkApplyRules
{
    /// <summary>ownerID からリモート適用すべき BallState を決める。</summary>
    public static BallManager_State.BALL_STATE ResolveBallStateFromOwnerId(int ownerId) =>
        ownerId > 0
            ? BallManager_State.BALL_STATE.HOLD
            : BallManager_State.BALL_STATE.FREE;

    /// <summary>ネットワーク適用は kickoff suppress で拒否しない。</summary>
    public static bool ShouldBypassKickoffSuppressForNetworkApply() => true;

    /// <summary>
    /// Photon 側 BallOwnerID をネットワーク値で上書きすべきか。
    /// 常に true（TeamBB と isHoldBall のズレを毎 RPC で解消する）。
    /// </summary>
    public static bool ShouldApplyPhotonOwnerId(int networkOwnerId, int currentPhotonOwnerId) =>
        true;

    /// <summary>適用後に isHoldBall(owner) が成立する論理 owner。</summary>
    public static int ResolveAppliedPhotonOwnerId(int networkOwnerId) => networkOwnerId;
}
