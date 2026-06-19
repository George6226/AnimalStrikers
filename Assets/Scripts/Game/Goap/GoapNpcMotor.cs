using UnityEngine;

/// <summary>
/// GOAPランタイムから味方NPCを動かす共通経路（AnimalActionSelector → AnimalHandler）。
/// PlayerBlackboard は Goap 子階層にあるため、BasicData.Self から親の AnimalFacade を解決する。
/// </summary>
public static class GoapNpcMotor
{
    public static Vector3 GetSelfWorldPosition(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self != null)
        {
            return bb.BasicData.Self.transform.position;
        }

        return bb != null ? bb.PhysicalState.Position : Vector3.zero;
    }

    public static bool TryResolve(
        PlayerBlackboard bb,
        out AnimalFacade facade,
        out AnimalActionSelector selector,
        out AnimalHandler handler)
    {
        facade = null;
        selector = null;
        handler = null;

        if (bb == null || bb.BasicData.Self == null)
        {
            return false;
        }

        facade = bb.BasicData.Self.GetComponentInParent<AnimalFacade>();
        if (facade == null)
        {
            return false;
        }

        selector = facade.GetActionSelector();
        handler = facade.GetAnimalHandler();
        return selector != null || handler != null;
    }

    public static void MoveToward(PlayerBlackboard bb, Vector3 worldTarget, float moveIntensity = 1f, string debugCategory = "Motor")
    {
        if (!TryResolve(bb, out _, out var selector, out var handler))
        {
            GoapMovementDiagnostic.LogThrottled(
                debugCategory,
                $"MoveToward skipped: {GoapMovementDiagnostic.FormatMotorResolve(bb)}",
                bb,
                0.5f);
            return;
        }

        if (!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME))
        {
            GoapMovementDiagnostic.LogThrottled(
                debugCategory,
                "MoveToward skipped: not GAME state",
                bb,
                0.5f);
            return;
        }

        Vector3 transformPos = GetSelfWorldPosition(bb);
        Vector3 bbPos = bb.PhysicalState.Position;
        Vector3 pos = transformPos;
        Vector3 toTarget = worldTarget - pos;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        if (toTarget.sqrMagnitude <= 0.04f)
        {
            GoapMovementDiagnostic.LogThrottled(
                debugCategory,
                $"MoveToward skip (target~=self) dist={dist:F3} — Stopは呼ばない",
                bb,
                0.35f);
            return;
        }

        float radian = Mathf.Atan2(-toTarget.x, toTarget.z);
        string path = selector != null ? "selector" : "handler";
        if (selector != null)
        {
            selector.ExecuteMoveAction(moveIntensity, radian);
        }
        else
        {
            handler.move(moveIntensity, 1f);
            handler.rotate(radian);
        }

        GoapMovementDiagnostic.LogThrottled(
            debugCategory,
            $"MoveToward ok path={path} intensity={moveIntensity:F2} dist={dist:F2} " +
            $"transformPos={GoapMovementDiagnostic.FormatVector(transformPos)} bbPos={GoapMovementDiagnostic.FormatVector(bbPos)} " +
            $"target={GoapMovementDiagnostic.FormatVector(worldTarget)} radDeg={(radian * Mathf.Rad2Deg):F1}",
            bb,
            0.5f);
    }

    public static void Stop(PlayerBlackboard bb, string debugCategory = "Motor")
    {
        if (!TryResolve(bb, out _, out var selector, out var handler))
        {
            GoapMovementDiagnostic.Log(debugCategory, $"Stop skipped: {GoapMovementDiagnostic.FormatMotorResolve(bb)}", bb);
            return;
        }

        if (selector != null)
        {
            selector.ExecuteMoveAction(0f, 0f);
        }
        else
        {
            handler?.stand();
        }

        GoapMovementDiagnostic.LogThrottled(debugCategory, "Stop called (ExecuteMoveAction 0,0 or stand)", bb, 0.35f);
    }
}
