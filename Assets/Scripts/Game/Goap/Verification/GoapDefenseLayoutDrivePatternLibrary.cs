public static class GoapDefenseLayoutDrivePatternLibrary
{
    public static bool TryGetAutoDriveOverride(
        GoapDefenseLayoutPatternId pattern,
        out GoapBallOwnerAutoDriveMode mode)
    {
        switch (pattern)
        {
            case GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies_DriveForward:
            case GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield_DriveForward:
            case GoapDefenseLayoutPatternId.EnemyOwner_RetreatToDefensiveLine:
                mode = GoapBallOwnerAutoDriveMode.ForwardBack;
                return true;
            default:
                mode = GoapBallOwnerAutoDriveMode.None;
                return false;
        }
    }

    public static float ResolveAmplitudeRatio(
        GoapDefenseLayoutPatternId pattern,
        float defaultRatio,
        float driveRatio)
    {
        return TryGetAutoDriveOverride(pattern, out _) ? driveRatio : defaultRatio;
    }
}
