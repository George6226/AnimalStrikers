#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

/// <summary>6-H P1: RegainStamina バッチの純判定。</summary>
public sealed class GoapRegainStaminaBatchRulesEditModeTests
{
    [Test]
    public void IsActiveBatchProfile_IsFalseByDefault()
    {
        Assert.That(GoapRegainStaminaBatchRules.IsActiveBatchProfile(), Is.False);
    }

    [Test]
    public void IsMainSelectionSlot_RequiresAssignedSlot0()
    {
        var go = new GameObject("facade");
        try
        {
            go.AddComponent<AnimalFacade>();
            var slot = go.AddComponent<AnimalFormationSlot>();
            slot.Initialize(0);
            Assert.That(GoapRegainStaminaBatchRules.IsMainSelectionSlot(go.GetComponent<AnimalFacade>()), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void IsMainSelectionSlot_FalseForOtherSlots()
    {
        var go = new GameObject("facade");
        try
        {
            go.AddComponent<AnimalFacade>();
            var slot = go.AddComponent<AnimalFormationSlot>();
            slot.Initialize(1);
            Assert.That(GoapRegainStaminaBatchRules.IsMainSelectionSlot(go.GetComponent<AnimalFacade>()), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
