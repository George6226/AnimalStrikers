using UnityEngine;

/// <summary>パス先のリード位置と距離制限。</summary>
public static class PassLeadPolicy
{
    public static Vector3 ResolveKickTargetPosition(
        AnimalFacade receiver,
        Vector3 passerPosition,
        float estimatedFlightSeconds,
        bool receiverIsMoving)
    {
        Vector3 receivePos = ResolveReceivePosition(receiver);
        if (!receiverIsMoving || receiver == null)
        {
            return receivePos;
        }

        Vector3 moveDir = EstimateMoveDirection(receiver);
        if (moveDir.sqrMagnitude < 0.0001f)
        {
            return receivePos;
        }

        float leadSeconds = Mathf.Clamp(estimatedFlightSeconds * 0.65f, 0.08f, 0.55f);
        return receivePos + moveDir * EstimateMoveSpeed(receiver) * leadSeconds;
    }

    public static float EstimateGroundPassFlightSeconds(float distance)
    {
        if (distance <= 3f)
        {
            return 0.4f;
        }

        if (distance <= 8f)
        {
            return 0.7f;
        }

        return 1.0f;
    }

    public static bool ShouldPreferGroundPass(float distance, bool receiverIsMoving, bool laneNeedsLob)
    {
        if (!laneNeedsLob)
        {
            return true;
        }

        if (!receiverIsMoving)
        {
            return false;
        }

        return distance <= ConstData.PASS_GROUND_PREFER_MAX_DISTANCE;
    }

    public static float ClampPassDistance(float distance)
    {
        return Mathf.Min(distance, ConstData.MAX_PASS_DISTANCE);
    }

    private static Vector3 ResolveReceivePosition(AnimalFacade receiver)
    {
        GameObject ballKeep = receiver.GetBallKeep();
        return ballKeep != null ? ballKeep.transform.position : receiver.transform.position;
    }

    private static Vector3 EstimateMoveDirection(AnimalFacade receiver)
    {
        PlayerBlackboard bb = receiver.GetComponentInChildren<PlayerBlackboard>();
        if (bb != null && bb.PhysicalState.IsMoving)
        {
            Vector3 forward = receiver.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                return forward.normalized;
            }
        }

        return Vector3.zero;
    }

    private static float EstimateMoveSpeed(AnimalFacade receiver)
    {
        AnimalInfo info = receiver.GetAnimalInfo();
        if (info == null)
        {
            return 3.5f;
        }

        return Mathf.Clamp(2.5f + info.Speed * 0.03f, 2.5f, 5.5f);
    }
}
