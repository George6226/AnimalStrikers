/// <summary>TeamBallSupport 単体検証で候補を1アクションに絞る対象。</summary>
public enum GoapSupportActionUnderTest
{
    None = 0,
    CreateSupportAngle = 1,
    MoveToSupportPosition = 2,
    GetOpen = 3,
    /// <summary>翼保持ドライブ時の追従（#17/#18）。単体候補絞り込みは行わない。</summary>
    WingOwnerDriveFollow = 4,
    /// <summary>CF 保持ドライブ時の翼追従（#13〜16）。単体候補絞り込みは行わない。</summary>
    CfOwnerDriveFollow = 5,
}

public static class GoapSupportActionUnderTestExtensions
{
    public static string ToActionName(this GoapSupportActionUnderTest action)
    {
        return action switch
        {
            GoapSupportActionUnderTest.CreateSupportAngle => "CreateSupportAngle",
            GoapSupportActionUnderTest.MoveToSupportPosition => "MoveToSupportPosition",
            GoapSupportActionUnderTest.GetOpen => "GetOpen",
            _ => null,
        };
    }

    public static bool MatchesAction(this GoapSupportActionUnderTest action, GoapActionSO actionSo)
    {
        if (action == GoapSupportActionUnderTest.None || actionSo == null)
        {
            return false;
        }

        string name = action.ToActionName();
        return !string.IsNullOrEmpty(name) && actionSo.ActionName == name;
    }
}
