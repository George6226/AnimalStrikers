/// <summary>
/// 編成スロット（0〜3）の戦術略称。PhotonAvatarCreator の配置と対応。
/// </summary>
public static class FormationTacticalRole
{
    public static string GetShortName(int slotIndex)
    {
        return slotIndex switch
        {
            0 => "CF",
            1 => "RW",
            2 => "LW",
            3 => "GK",
            _ => "?",
        };
    }

    public static string GetDisplayName(int slotIndex)
    {
        return slotIndex switch
        {
            0 => "Center Forward",
            1 => "Right Wing",
            2 => "Left Wing",
            3 => "Goalkeeper",
            _ => "Unknown",
        };
    }
}
