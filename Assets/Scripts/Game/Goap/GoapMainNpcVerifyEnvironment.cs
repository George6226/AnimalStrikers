using UnityEngine;

/// <summary>
/// Phase M0: 味方プレイヤー操作を一時停止し slot0 をメイン NPC GOAP 検証枠として扱うモード。
/// </summary>
public static class GoapMainNpcVerifyEnvironment
{
    private static bool _isActive;
    private static int _mainFormationSlot;
    private static bool _bootstrapComplete;

    public static bool IsActive => _isActive;
    public static int MainFormationSlot => _mainFormationSlot;
    public static bool IsBootstrapComplete => !_isActive || _bootstrapComplete;
    public static bool RequiresBootstrap => _isActive && !_bootstrapComplete;

    public static void Sync(bool active, int mainFormationSlot)
    {
        bool wasActive = _isActive;
        _isActive = active;
        _mainFormationSlot = Mathf.Clamp(mainFormationSlot, 0, 2);
        if (!active || !wasActive)
        {
            _bootstrapComplete = false;
        }
    }

    public static void MarkBootstrapComplete()
    {
        _bootstrapComplete = true;
    }

    public static GoapNpcTier ResolveTier(AnimalFacade facade)
    {
        if (!_isActive || facade == null)
        {
            return GoapNpcTier.Sub;
        }

        var formationSlot = facade.GetComponent<AnimalFormationSlot>();
        int slot = formationSlot != null && formationSlot.IsAssigned
            ? formationSlot.Index
            : -1;
        return slot == _mainFormationSlot ? GoapNpcTier.Main : GoapNpcTier.Sub;
    }
}
