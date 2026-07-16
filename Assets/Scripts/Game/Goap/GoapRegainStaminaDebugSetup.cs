using UnityEngine;

/// <summary>
/// 6-H P1: 味方保持・低スタミナ時に slot0 が StandRecoverStamina を本番選出することを検証。
/// _batchPatternIndexStart/End = #8 RwOwner_WingHold（1パターン）。
/// </summary>
public class GoapRegainStaminaDebugSetup : GoapSupportActionVerificationSetup
{
    [Header("敵配置")]
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;

    protected override string SummaryLogTag => "GOAP_REGAIN_STAMINA_SETUP";

    protected override GoapSupportActionUnderTest ActionUnderTest =>
        GoapSupportActionUnderTest.None;

    protected override IGoapProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapProductionSelectionExpectations.RegainStamina;

    protected override string BatchVerificationBanner => "Regain stamina batch verification";

    protected override string ProductionSelectionVerificationBanner =>
        "Regain stamina production selection (StandRecoverStamina)";

    protected override void ApplyCompanionVerificationState(GoapSupportLayoutPatternId pattern)
    {
        if (_enemyLayouts == null)
        {
            _enemyLayouts = FindFirstObjectByType<GoapEnemyPositionDebugPatterns>();
        }

        if (_enemyLayouts != null)
        {
            _enemyLayouts.ApplyPattern(GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner);
            LogLine($"ApplyEnemyLayout({pattern}) layout=PressBallOwner");
        }
        else
        {
            LogLine($"ApplyEnemyLayout({pattern}) skipped: GoapEnemyPositionDebugPatterns not found");
        }

        ApplyLowStaminaToMainSlot();
    }

    private void ApplyLowStaminaToMainSlot()
    {
        var slots = FindObjectsByType<AnimalFormationSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AnimalFormationSlot slot in slots)
        {
            if (slot == null || !slot.IsAssigned || slot.Index != GoapRegainStaminaBatchRules.MainSelectionSlot)
            {
                continue;
            }

            AnimalFacade facade = slot.GetComponent<AnimalFacade>();
            if (facade == null)
            {
                facade = slot.GetComponentInParent<AnimalFacade>();
            }

            if (GoapRegainStaminaBatchRules.TryApplyLowStaminaForBatch(facade))
            {
                LogLine($"ApplyLowStamina slot={slot.Index} facade={facade.name}");
            }
        }
    }
}
