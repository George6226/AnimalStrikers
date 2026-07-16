using UnityEngine;

/// <summary>
/// 6-H P0: SlideTackle バッチ検証で slot0 を Main 選出枠として扱う純判定。
/// </summary>
public static class GoapSlideTackleBatchRules
{
    public const int MainSelectionSlot = 0;

    public static bool IsActiveBatchProfile() =>
        GoapBatchVerifyEnvironment.IsActive
        && GoapBatchVerifyEnvironment.Profile == GoapBatchVerifyProfile.SlideTackle;

    public static bool IsMainSelectionSlot(AnimalFacade facade)
    {
        if (facade == null)
        {
            return false;
        }

        var slot = facade.GetComponent<AnimalFormationSlot>();
        return slot != null && slot.IsAssigned && slot.Index == MainSelectionSlot;
    }
}
