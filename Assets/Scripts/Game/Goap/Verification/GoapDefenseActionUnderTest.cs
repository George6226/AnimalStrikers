/// <summary>DefensivePositioning 単体検証で候補を1アクションに絞る対象。</summary>
public enum GoapDefenseActionUnderTest
{
    None = 0,
    MoveToDefensivePosition = 1,
}

public static class GoapDefenseActionUnderTestExtensions
{
    public static string ToActionName(this GoapDefenseActionUnderTest action)
    {
        return action switch
        {
            GoapDefenseActionUnderTest.MoveToDefensivePosition => "MoveToDefensivePosition",
            _ => null,
        };
    }

    public static bool MatchesAction(this GoapDefenseActionUnderTest action, GoapActionSO actionSo)
    {
        if (action == GoapDefenseActionUnderTest.None || actionSo == null)
        {
            return false;
        }

        string name = action.ToActionName();
        return !string.IsNullOrEmpty(name) && actionSo.ActionName == name;
    }
}
