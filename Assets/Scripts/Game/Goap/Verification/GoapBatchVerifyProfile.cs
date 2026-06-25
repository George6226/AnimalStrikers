/// <summary>
/// <c>-goapBatchVerify</c> CLI で選択するバッチ検証プロファイル。
/// </summary>
public enum GoapBatchVerifyProfile
{
    /// <summary>統合本番選出（CombinedSupportRegression #2〜12）。</summary>
    Combined = 0,

    /// <summary>翼保持ドライブ追従ランタイム（WingOwnerDrive #17/#18）。</summary>
    WingDrive = 1,

    /// <summary>CF 保持ドライブ追従ランタイム（CfOwnerDrive #13〜16）。</summary>
    CfDrive = 2,

    /// <summary>守備基本本番選出（DefenseBaseline #2〜#3）。</summary>
    DefenseBaseline = 3,
}
