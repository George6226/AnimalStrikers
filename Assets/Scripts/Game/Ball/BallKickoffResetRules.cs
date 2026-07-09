/// <summary>
/// ゴール後キックオフ再開中のボール所有権ガード（EditMode テスト可能な純粋ロジック）。
/// </summary>
public static class BallKickoffResetRules
{
    public static bool ShouldRejectOwnershipClaim(int ownerId, float suppressUntil, float now)
    {
        return ownerId > 0 && now < suppressUntil;
    }
}
