using UnityEngine;

/// <summary>
/// デバッグ検証用の任意キャラ移動（Human / NPC 共通。AnimalHandler または ActionSelector 経由）。
/// </summary>
public static class GoapDebugAnimalMotor
{
    private const string DiagCategory = "DebugAutoDrive";

    public static bool TryMoveToward(AnimalFacade facade, Vector3 worldTarget, float moveIntensity = 0.85f)
    {
        if (facade == null)
        {
            return false;
        }

        if (StateManager.Instance == null
            || !StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME))
        {
            return false;
        }

        AnimalActionSelector selector = facade.GetActionSelector();
        AnimalHandler handler = facade.GetAnimalHandler();
        Vector3 pos = facade.transform.position;
        Vector3 toTarget = worldTarget - pos;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        if (toTarget.sqrMagnitude <= 0.04f)
        {
            Stop(facade);
            return true;
        }

        float radian = Mathf.Atan2(-toTarget.x, toTarget.z);
        if (selector != null)
        {
            selector.ExecuteMoveAction(moveIntensity, radian);
        }
        else if (handler != null)
        {
            handler.move(moveIntensity, 1f);
            handler.rotate(radian);
        }
        else
        {
            return false;
        }

        GoapMovementDiagnostic.Log(
            DiagCategory,
            $"MoveToward facade={facade.name} dist={dist:F2} target={GoapMovementDiagnostic.FormatVector(worldTarget)} intensity={moveIntensity:F2}",
            null,
            0.5f);
        return false;
    }

    public static void Stop(AnimalFacade facade)
    {
        if (facade == null)
        {
            return;
        }

        AnimalActionSelector selector = facade.GetActionSelector();
        AnimalHandler handler = facade.GetAnimalHandler();
        if (selector != null)
        {
            selector.ExecuteMoveAction(0f, 0f);
        }
        else
        {
            handler?.stand();
        }
    }
}
