#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class GoapMatchPlayGateEditModeTests
{
    [TearDown]
    public void TearDown()
    {
        var existing = Object.FindObjectsOfType<StateManager>();
        foreach (var sm in existing)
        {
            Object.DestroyImmediate(sm.gameObject);
        }

        typeof(StateManager)
            .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, null);
    }

    [Test]
    public void IsMatchPlayActive_TrueWhenNoStateManager()
    {
        Assert.That(GoapMatchPlayGate.IsMatchPlayActive(), Is.True);
    }

    [Test]
    public void IsMatchPlayActive_FalseWhenReady()
    {
        var go = new GameObject("stateManager");
        go.AddComponent<StateManager>();
        SetStateKind(go.GetComponent<StateManager>(), StateManager.STATE_KIND.READY);

        try
        {
            Assert.That(GoapMatchPlayGate.IsMatchPlayActive(), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void IsMatchPlayActive_TrueWhenGame()
    {
        var go = new GameObject("stateManager");
        go.AddComponent<StateManager>();
        SetStateKind(go.GetComponent<StateManager>(), StateManager.STATE_KIND.GAME);

        try
        {
            Assert.That(GoapMatchPlayGate.IsMatchPlayActive(), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ProductionShouldEnableGoap_FalseWhenReadyEvenWithBall()
    {
        var stateGo = new GameObject("stateManager");
        var stateManager = stateGo.AddComponent<StateManager>();
        SetStateKind(stateManager, StateManager.STATE_KIND.READY);

        var human = new GameObject("human");
        human.AddComponent<AnimalFacade>();
        human.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.Human);
        var bbGo = new GameObject("bb");
        bbGo.transform.SetParent(human.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(human);
        bb.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true"), true);

        GoapMainNpcProductionEnvironment.Sync(true);
        try
        {
            Assert.That(
                GoapMainNpcProductionEnvironment.ShouldEnableGoap(bb, human.GetComponent<AnimalFacade>()),
                Is.False);
        }
        finally
        {
            GoapMainNpcProductionEnvironment.Sync(false);
            Object.DestroyImmediate(human);
            Object.DestroyImmediate(stateGo);
        }
    }

    private static void SetStateKind(StateManager stateManager, StateManager.STATE_KIND kind)
    {
        var field = typeof(StateManager).GetField(
            "_stateKind",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(stateManager, kind);
    }
}
#endif
