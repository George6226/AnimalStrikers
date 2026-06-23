/// <summary>
/// <c>-goapBatchVerify</c> CLI で選択するバッチ検証プロファイル。
/// </summary>
public enum GoapBatchVerifyProfile
{
    /// <summary>統合本番選出（CombinedSupportRegression #2〜12）。</summary>
    Combined = 0,

    /// <summary>翼保持ドライブ追従ランタイム（WingOwnerDrive #17/#18）。</summary>
    WingDrive = 1,
}
