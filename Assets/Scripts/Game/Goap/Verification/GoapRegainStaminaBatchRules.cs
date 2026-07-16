using UnityEngine;

/// <summary>
/// 6-H P1: RegainStamina バッチ検証で slot0 を Main 選出枠として扱う純判定。
/// </summary>
public static class GoapRegainStaminaBatchRules
{
    public const int MainSelectionSlot = 0;

    public static bool IsActiveBatchProfile() =>
        GoapBatchVerifyEnvironment.IsActive
        && GoapBatchVerifyEnvironment.Profile == GoapBatchVerifyProfile.RegainStamina;

    public static bool IsMainSelectionSlot(AnimalFacade facade)
    {
        if (facade == null)
        {
            return false;
        }

        var slot = facade.GetComponent<AnimalFormationSlot>();
        return slot != null && slot.IsAssigned && slot.Index == MainSelectionSlot;
    }

    /// <summary>バッチ用に slot0 のスタミナを閾値未満へ下げる。</summary>
    public static bool TryApplyLowStaminaForBatch(AnimalFacade facade)
    {
        if (facade == null)
        {
            return false;
        }

        PhotonHPGauge gauge = facade.GetHPGauge();
        if (gauge == null)
        {
            return false;
        }

        float maxHp = gauge.MaxHP;
        if (maxHp <= 0f)
        {
            return false;
        }

        float threshold = ConstData.STAMINA_LOW_MOVE_RATIO_THRESHOLD;
        float targetCurrent = maxHp * Mathf.Max(0f, threshold - 0.08f);
        float drain = gauge.CurrentHP - targetCurrent;
        if (drain <= 0f)
        {
            return false;
        }

        gauge.useHP(drain);
        return true;
    }
}
