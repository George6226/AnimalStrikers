using UnityEngine;

/// <summary>
/// キャラプレハブ子階層の GOAP コンポーネント（Goap 子オブジェクト配下）を解決する。
/// </summary>
public struct AnimalGoapBrainComponents
{
    public GoapAgent Agent;
    public PlayerBlackboard Blackboard;
    public AIContextSwitcher ContextSwitcher;

    public bool HasAgent => Agent != null;

    public static AnimalGoapBrainComponents Resolve(AnimalFacade facade)
    {
        if (facade == null)
        {
            return default;
        }

        return Resolve(facade.gameObject);
    }

    public static AnimalGoapBrainComponents Resolve(GameObject root)
    {
        var result = new AnimalGoapBrainComponents();
        if (root == null)
        {
            return result;
        }

        result.Agent = root.GetComponentInChildren<GoapAgent>(true);
        if (result.Agent != null)
        {
            result.Blackboard = result.Agent.GetComponentInChildren<PlayerBlackboard>(true);
            result.ContextSwitcher = result.Agent.GetComponentInChildren<AIContextSwitcher>(true);
        }
        else
        {
            result.Blackboard = root.GetComponentInChildren<PlayerBlackboard>(true);
            result.ContextSwitcher = root.GetComponentInChildren<AIContextSwitcher>(true);
        }

        return result;
    }

    public void SetActive(bool active)
    {
        if (Agent != null)
        {
            Agent.enabled = active;
        }

        if (ContextSwitcher != null)
        {
            ContextSwitcher.enabled = active;
        }

        if (!active && Agent != null && Agent.isActiveAndEnabled)
        {
            Agent.AbortCurrentPlan();
        }
    }
}
