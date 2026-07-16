using NUnit.Framework;
using UnityEngine;

public sealed class GoapSlideTackleBatchRulesEditModeTests
{
    [Test]
    public void IsActiveBatchProfile_FalseWhenNotBatch()
    {
        Assert.That(GoapSlideTackleBatchRules.IsActiveBatchProfile(), Is.False);
    }

    [Test]
    public void IsMainSelectionSlot_TrueForAssignedSlotZero()
    {
        var go = new GameObject("ally");
        go.AddComponent<AnimalFacade>();
        go.AddComponent<AnimalFormationSlot>().Initialize(0);

        try
        {
            Assert.That(GoapSlideTackleBatchRules.IsMainSelectionSlot(go.GetComponent<AnimalFacade>()), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void IsMainSelectionSlot_FalseForSlotOne()
    {
        var go = new GameObject("ally");
        go.AddComponent<AnimalFacade>();
        go.AddComponent<AnimalFormationSlot>().Initialize(1);

        try
        {
            Assert.That(GoapSlideTackleBatchRules.IsMainSelectionSlot(go.GetComponent<AnimalFacade>()), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
