public static class GoapSupportLayoutDrivePatternLibrary
{
    public static bool TryGetAutoDriveOverride(
        GoapSupportLayoutPatternId pattern,
        out GoapBallOwnerAutoDriveMode mode)
    {
        switch (pattern)
        {
            case GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForward:
            case GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes_DriveForward:
                mode = GoapBallOwnerAutoDriveMode.Forward;
                return true;
            case GoapSupportLayoutPatternId.RwOwner_WingHold_DriveForward:
            case GoapSupportLayoutPatternId.LwOwner_WingHold_DriveForward:
                mode = GoapBallOwnerAutoDriveMode.ForwardBack;
                return true;
            case GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForwardBack:
                mode = GoapBallOwnerAutoDriveMode.ForwardBack;
                return true;
            case GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveLateralRight:
                mode = GoapBallOwnerAutoDriveMode.LateralRight;
                return true;
            default:
                mode = GoapBallOwnerAutoDriveMode.None;
                return false;
        }
    }

    public static float ResolveAmplitudeRatio(
        GoapSupportLayoutPatternId pattern,
        float defaultRatio,
        float wingOwnerRatio)
    {
        return pattern switch
        {
            GoapSupportLayoutPatternId.RwOwner_WingHold_DriveForward => wingOwnerRatio,
            GoapSupportLayoutPatternId.LwOwner_WingHold_DriveForward => wingOwnerRatio,
            _ => defaultRatio,
        };
    }
}
