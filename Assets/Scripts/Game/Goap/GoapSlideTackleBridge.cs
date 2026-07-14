using UnityEngine;

/// <summary>
/// F4: GOAP から既存の AnimalAction_Sliding を呼び出す。
/// </summary>
public static class GoapSlideTackleBridge
{
    public static bool HasSlidingAction(PlayerBlackboard bb)
    {
        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(bb);
        return facade != null && facade.GetComponentInChildren<AnimalAction_Sliding>(true) != null;
    }

    public static bool TryExecuteSliding(PlayerBlackboard bb)
    {
        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(bb);
        if (facade == null)
        {
            return false;
        }

        var sliding = facade.GetComponentInChildren<AnimalAction_Sliding>(true);
        if (sliding == null)
        {
            return false;
        }

        FaceTowardBallOwner(bb, facade);
        sliding.Execute();
        return true;
    }

    private static void FaceTowardBallOwner(PlayerBlackboard bb, AnimalFacade facade)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || facade == null)
        {
            return;
        }

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        Vector3 selfPos = bb != null && bb.PhysicalState != null
            ? bb.PhysicalState.Position
            : facade.transform.position;
        Vector3 delta = ownerPos - selfPos;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.01f)
        {
            return;
        }

        var handler = facade.GetAnimalHandler();
        if (handler != null)
        {
            float radian = Mathf.Atan2(-delta.x, delta.z);
            handler.rotate(radian);
        }
    }
}
