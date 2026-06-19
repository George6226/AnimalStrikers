using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CreateSupportAngle の位置選定。
/// 保持者の攻撃軸（Z）に追従し、3レーン幅（左・中央・右）を埋めるサポート位置を選ぶ。
/// </summary>
public static class CreateSupportAnglePositioning
{
    private const float OwnerWingEnterRatio = 0.12f;
    private const float OwnerWingExitRatio = 0.10f;
    private const float CentralLaneRatio = 0.50f;

    /// <summary>保持者の縦位置（中央 / 右サイド / 左サイド）。</summary>
    public enum OwnerZone
    {
        Central,
        Right,
        Left,
    }

    /// <summary>サポーターが埋める横レーン。</summary>
    public enum SupportLaneRole
    {
        LeftWide,
        RightWide,
        CentralChannel,
    }

    /// <summary>保持者ゾーン判定のヒステリシス状態（呼び出し側で保持）。</summary>
    public struct WingChannelState
    {
        public OwnerZone Zone;
    }

    public struct Settings
    {
        public float ForwardLeadRatio;
        public float WingLaneRatio;
        public float OptimalDistanceRatio;
        public float MinDistanceRatio;
        public float MaxDistanceRatio;
        public float AngleTolerance;
    }

    private readonly struct WidthLayoutContext
    {
        public readonly float OwnerLateral;
        public readonly OwnerZone Zone;
        public readonly int OwnerFormationSlot;
        public readonly float WingEnterThreshold;
        public readonly float WingExitThreshold;

        public WidthLayoutContext(
            float ownerLateral,
            OwnerZone zone,
            int ownerFormationSlot,
            float wingEnterThreshold,
            float wingExitThreshold)
        {
            OwnerLateral = ownerLateral;
            Zone = zone;
            OwnerFormationSlot = ownerFormationSlot;
            WingEnterThreshold = wingEnterThreshold;
            WingExitThreshold = wingExitThreshold;
        }

        public SupportLaneRole GetSlotLaneRole(int slot) =>
            ResolveSlotLaneRole(slot, Zone, OwnerFormationSlot);

        public bool SlotUsesCentralChannel(int slot) =>
            GetSlotLaneRole(slot) == SupportLaneRole.CentralChannel;
    }

    /// <summary>翼追従位置計算の診断用スナップショット。</summary>
    public struct PositioningSnapshot
    {
        public int Slot;
        public Vector3 SelfPosition;
        public Vector3 OwnerPosition;
        public Vector3 AnchorPosition;
        public Vector3 TargetPosition;
        public float ForwardOffset;
        public float LateralOffset;
        public float OwnerZ;
        public float OwnerLateral;
        public float TargetCenterLateral;
        public float TargetZ;
        public float SelfZ;
        public float ZLead;
        public OwnerZone OwnerZone;
        public int OwnerFormationSlot;
        public SupportLaneRole LaneRole;
        public float WingEnterThreshold;
        public float WingExitThreshold;
        public bool UsesInsideChannel;
        public float OwnerDistance;
        public bool PassLaneClear;
        public bool MaintainingSupport;
        public float BestScore;
    }

    /// <summary>
    /// 保持者ゾーンに応じた横レーン割当。
    /// 中央: 左ワイド + 右ワイド / 右サイド: 左ワイド + 中央 / 左サイド: 右ワイド + 中央。
    /// 翼保持者がボールを持つ場合は slot0 を中央レーンに割り当てる。
    /// </summary>
    public static SupportLaneRole ResolveSlotLaneRole(int slot, OwnerZone zone, int ownerFormationSlot = -1)
    {
        if (ownerFormationSlot == 0)
        {
            return slot switch
            {
                1 => SupportLaneRole.RightWide,
                2 => SupportLaneRole.LeftWide,
                _ => SupportLaneRole.CentralChannel,
            };
        }

        if (ownerFormationSlot == 2)
        {
            return slot switch
            {
                1 => SupportLaneRole.RightWide,
                0 => SupportLaneRole.CentralChannel,
                _ => SupportLaneRole.CentralChannel,
            };
        }

        if (ownerFormationSlot == 1)
        {
            return slot switch
            {
                2 => SupportLaneRole.LeftWide,
                0 => SupportLaneRole.CentralChannel,
                _ => SupportLaneRole.CentralChannel,
            };
        }

        return zone switch
        {
            OwnerZone.Central => slot switch
            {
                1 => SupportLaneRole.RightWide,
                2 => SupportLaneRole.LeftWide,
                _ => SupportLaneRole.CentralChannel,
            },
            OwnerZone.Right => slot switch
            {
                1 => SupportLaneRole.CentralChannel,
                2 => SupportLaneRole.LeftWide,
                _ => SupportLaneRole.CentralChannel,
            },
            OwnerZone.Left => slot switch
            {
                1 => SupportLaneRole.RightWide,
                2 => SupportLaneRole.CentralChannel,
                _ => SupportLaneRole.CentralChannel,
            },
            _ => SupportLaneRole.CentralChannel,
        };
    }

    public static Settings CreateDefaultSettings()
    {
        return new Settings
        {
            ForwardLeadRatio = 0.08f,
            WingLaneRatio = 0.30f,
            OptimalDistanceRatio = 0.20f,
            MinDistanceRatio = 0.10f,
            MaxDistanceRatio = 0.40f,
            AngleTolerance = 45f,
        };
    }

    /// <summary>保持者の編成スロット（RW=1, LW=2）。未解決時は -1。</summary>
    public static int ResolveBallOwnerFormationSlot(TeamBlackboard teamBB)
    {
        if (teamBB == null || !teamBB.BallInfo.TeamHasBall)
        {
            return -1;
        }

        int ownerId = teamBB.BallInfo.BallOwnerID;
        if (ownerId < 0)
        {
            return -1;
        }

        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return -1;
        }

        foreach (var ally in regist.Allys)
        {
            if (ally == null || ally.IsGK())
            {
                continue;
            }

            var view = ally.GetComponentInParent<Photon.Pun.PhotonView>();
            if (view == null || view.ViewID != ownerId)
            {
                continue;
            }

            var formationSlot = ally.GetComponentInParent<AnimalFormationSlot>()
                ?? ally.GetComponent<AnimalFormationSlot>();
            return formationSlot != null && formationSlot.IsAssigned ? formationSlot.Index : -1;
        }

        return -1;
    }

    /// <summary>プランニング用の即時ゾーン判定（ヒステリシスなし）。</summary>
    public static OwnerZone ResolveOwnerZone(TeamBlackboard teamBB)
    {
        if (teamBB == null)
        {
            return OwnerZone.Central;
        }

        Vector3 ownerPos = ResolveBallOwnerPosition(teamBB);
        var field = teamBB.FieldInfo;
        Vector3 toGoal = (field.EnemyGoalPosition - ownerPos).normalized;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        float wingEnterThreshold = field.FieldWidth * OwnerWingEnterRatio;
        float ownerLateral = Vector3.Dot(ownerPos - field.FieldCenter, right);
        OwnerZone geometricZone = ResolveGeometricOwnerZone(ownerLateral, wingEnterThreshold);
        int ownerSlot = ResolveBallOwnerFormationSlot(teamBB);

        return ApplyOwnerSlotZoneBias(geometricZone, ownerSlot);
    }

    /// <summary>割当レーンのアンカー（refinement 前）。</summary>
    public static Vector3 GetAssignedLaneAnchor(int slot, TeamBlackboard teamBB) =>
        GetAssignedLaneAnchor(slot, teamBB, CreateDefaultSettings());

    public static Vector3 GetAssignedLaneAnchor(int slot, TeamBlackboard teamBB, Settings settings)
    {
        if (teamBB == null)
        {
            return Vector3.zero;
        }

        Vector3 ownerPos = ResolveBallOwnerPosition(teamBB);
        var field = teamBB.FieldInfo;
        float fieldLength = field.FieldLength;
        float fieldWidth = field.FieldWidth;

        Vector3 toGoal = (field.EnemyGoalPosition - ownerPos).normalized;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        float forwardLead = fieldLength * settings.ForwardLeadRatio;
        float wingLane = fieldWidth * settings.WingLaneRatio;
        float wingEnterThreshold = fieldWidth * OwnerWingEnterRatio;
        float wingExitThreshold = fieldWidth * OwnerWingExitRatio;
        int ownerFormationSlot = ResolveBallOwnerFormationSlot(teamBB);
        var wingState = default(WingChannelState);
        var layoutContext = ResolveWidthLayoutContext(
            ownerPos,
            field.FieldCenter,
            right,
            wingEnterThreshold,
            wingExitThreshold,
            ownerFormationSlot,
            ref wingState);
        SupportLaneRole laneRole = layoutContext.GetSlotLaneRole(slot);
        Vector3 anchor = BuildLaneAnchor(
            ownerPos,
            field.FieldCenter,
            toGoal,
            right,
            forwardLead,
            wingLane,
            slot,
            laneRole);

        return ClampToField(anchor, field);
    }

    /// <summary>保持者位置基準のレイアウト理想位置（検証配置・到達判定用）。selfPos に依存しない。</summary>
    public static Vector3 GetLayoutIdealPosition(int slot, TeamBlackboard teamBB) =>
        SelectBestPosition(ResolveBallOwnerPosition(teamBB), slot, teamBB, CreateDefaultSettings());

    public static Vector3 SelectBestPosition(
        Vector3 selfPos,
        int slot,
        TeamBlackboard teamBB,
        Settings settings)
    {
        return SelectBestPosition(selfPos, slot, teamBB, settings, out _);
    }

    public static Vector3 SelectBestPosition(
        Vector3 selfPos,
        int slot,
        TeamBlackboard teamBB,
        Settings settings,
        out PositioningSnapshot snapshot)
    {
        var wingState = default(WingChannelState);
        return SelectBestPosition(selfPos, slot, teamBB, settings, ref wingState, out snapshot);
    }

    public static Vector3 SelectBestPosition(
        Vector3 selfPos,
        int slot,
        TeamBlackboard teamBB,
        Settings settings,
        ref WingChannelState wingState,
        out PositioningSnapshot snapshot)
    {
        if (teamBB == null)
        {
            snapshot = default;
            return selfPos;
        }

        Vector3 ownerPos = ResolveBallOwnerPosition(teamBB);
        var field = teamBB.FieldInfo;
        float fieldLength = field.FieldLength;
        float fieldWidth = field.FieldWidth;

        Vector3 toGoal = (field.EnemyGoalPosition - ownerPos).normalized;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        float forwardLead = fieldLength * settings.ForwardLeadRatio;
        float wingLane = fieldWidth * settings.WingLaneRatio;
        float wingEnterThreshold = fieldWidth * OwnerWingEnterRatio;
        float wingExitThreshold = fieldWidth * OwnerWingExitRatio;
        int ownerFormationSlot = ResolveBallOwnerFormationSlot(teamBB);
        var layoutContext = ResolveWidthLayoutContext(
            ownerPos,
            field.FieldCenter,
            right,
            wingEnterThreshold,
            wingExitThreshold,
            ownerFormationSlot,
            ref wingState);
        SupportLaneRole laneRole = layoutContext.GetSlotLaneRole(slot);
        Vector3 anchor = BuildLaneAnchor(
            ownerPos,
            field.FieldCenter,
            toGoal,
            right,
            forwardLead,
            wingLane,
            slot,
            laneRole);

        float minDist = fieldLength * settings.MinDistanceRatio;
        float optimalDist = fieldLength * settings.OptimalDistanceRatio;
        float maxDist = fieldLength * settings.MaxDistanceRatio;

        Vector3 bestPosition = ClampToField(anchor, field);
        float bestScore = float.MinValue;

        foreach (Vector3 candidate in BuildRefineCandidates(anchor, toGoal, right, slot, fieldLength, fieldWidth, layoutContext))
        {
            Vector3 clamped = ClampToField(candidate, field);
            float score = EvaluateCandidate(
                clamped,
                selfPos,
                ownerPos,
                toGoal,
                right,
                slot,
                teamBB,
                settings,
                minDist,
                optimalDist,
                maxDist,
                fieldLength,
                fieldWidth,
                layoutContext,
                wingEnterThreshold);
            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = clamped;
            }
        }

        snapshot = BuildSnapshot(
            selfPos,
            slot,
            ownerPos,
            anchor,
            bestPosition,
            toGoal,
            right,
            bestScore,
            teamBB,
            fieldLength,
            layoutContext,
            laneRole);

        return bestPosition;
    }

    public static string FormatDiagnosticLine(string phase, in PositioningSnapshot snap, float targetDelta = 0f, float ownerDelta = 0f)
    {
        return $"{phase} slot={snap.Slot} " +
               $"self={FormatVec(snap.SelfPosition)} owner={FormatVec(snap.OwnerPosition)} " +
               $"anchor={FormatVec(snap.AnchorPosition)} target={FormatVec(snap.TargetPosition)} " +
               $"ownerZ={snap.OwnerZ:F2} ownerLat={snap.OwnerLateral:F2} targetCenterLat={snap.TargetCenterLateral:F2} targetZ={snap.TargetZ:F2} selfZ={snap.SelfZ:F2} zLead={snap.ZLead:F2} " +
               $"fwd={snap.ForwardOffset:F2} lat={snap.LateralOffset:F2} ownerDist={snap.OwnerDistance:F2} " +
               $"ownerZone={snap.OwnerZone} ownerSlot={snap.OwnerFormationSlot} lane={snap.LaneRole} " +
               $"wingEnter={snap.WingEnterThreshold:F2} wingExit={snap.WingExitThreshold:F2} inside={snap.UsesInsideChannel} " +
               $"passLane={snap.PassLaneClear} supportRel={snap.MaintainingSupport} score={snap.BestScore:F2} " +
               $"targetDelta={targetDelta:F2} ownerDelta={ownerDelta:F2}";
    }

    private static string FormatVec(Vector3 v) => $"({v.x:F2},{v.y:F2},{v.z:F2})";

    private static PositioningSnapshot BuildSnapshot(
        Vector3 selfPos,
        int slot,
        Vector3 ownerPos,
        Vector3 anchor,
        Vector3 target,
        Vector3 toGoal,
        Vector3 right,
        float bestScore,
        TeamBlackboard teamBB,
        float fieldLength,
        in WidthLayoutContext layoutContext,
        SupportLaneRole laneRole)
    {
        Vector3 toTarget = target - ownerPos;
        toTarget.y = 0f;
        float passBlockRange = fieldLength * 0.06f;
        bool passLaneClear = PlayerBlackboardCalculator.IsPassRouteClear(
            target,
            ownerPos,
            teamBB.BasicInfo.EnemyPositions,
            passBlockRange);
        bool maintainingSupport = PlayerBlackboardCalculator.IsMaintainingSupportRelationship(
            selfPos,
            ownerPos,
            teamBB.FieldInfo.EnemyGoalPosition,
            fieldLength);

        return new PositioningSnapshot
        {
            Slot = slot,
            SelfPosition = selfPos,
            OwnerPosition = ownerPos,
            AnchorPosition = anchor,
            TargetPosition = target,
            ForwardOffset = Vector3.Dot(toTarget, toGoal),
            LateralOffset = Vector3.Dot(toTarget, right),
            OwnerZ = ownerPos.z,
            OwnerLateral = layoutContext.OwnerLateral,
            TargetCenterLateral = Vector3.Dot(target - teamBB.FieldInfo.FieldCenter, right),
            TargetZ = target.z,
            SelfZ = selfPos.z,
            ZLead = target.z - ownerPos.z,
            OwnerZone = layoutContext.Zone,
            OwnerFormationSlot = layoutContext.OwnerFormationSlot,
            LaneRole = laneRole,
            WingEnterThreshold = layoutContext.WingEnterThreshold,
            WingExitThreshold = layoutContext.WingExitThreshold,
            UsesInsideChannel = layoutContext.SlotUsesCentralChannel(slot),
            OwnerDistance = Vector3.Distance(target, ownerPos),
            PassLaneClear = passLaneClear,
            MaintainingSupport = maintainingSupport,
            BestScore = bestScore,
        };
    }

    private static IEnumerable<Vector3> BuildRefineCandidates(
        Vector3 anchor,
        Vector3 toGoal,
        Vector3 right,
        int slot,
        float fieldLength,
        float fieldWidth,
        WidthLayoutContext layoutContext)
    {
        yield return anchor;

        float forwardStep = fieldLength * 0.03f;
        float lateralStep = fieldWidth * 0.05f;
        float[] forwardOffsets = { forwardStep, -forwardStep * 0.6f, forwardStep * 1.6f };
        float[] lateralOffsets = { lateralStep, -lateralStep, lateralStep * 1.5f, -lateralStep * 1.5f };

        foreach (float forward in forwardOffsets)
        {
            yield return anchor + toGoal * forward;
        }

        foreach (float lateral in lateralOffsets)
        {
            Vector3 shifted = anchor + right * lateral;
            if (IsOnAssignedLane(slot, anchor, shifted, right, fieldLength, layoutContext))
            {
                yield return shifted;
            }
        }

        foreach (float forward in forwardOffsets)
        {
            foreach (float lateral in lateralOffsets)
            {
                Vector3 shifted = anchor + toGoal * forward + right * lateral;
                if (IsOnAssignedLane(slot, anchor, shifted, right, fieldLength, layoutContext))
                {
                    yield return shifted;
                }
            }
        }
    }

    private static OwnerZone ResolveGeometricOwnerZone(float ownerLateral, float wingEnterThreshold)
    {
        if (ownerLateral > wingEnterThreshold)
        {
            return OwnerZone.Right;
        }

        if (ownerLateral < -wingEnterThreshold)
        {
            return OwnerZone.Left;
        }

        return OwnerZone.Central;
    }

    private static OwnerZone ApplyOwnerSlotZoneBias(OwnerZone geometricZone, int ownerFormationSlot)
    {
        return ownerFormationSlot switch
        {
            0 => OwnerZone.Central,
            1 => OwnerZone.Right,
            2 => OwnerZone.Left,
            _ => geometricZone,
        };
    }

    private static WidthLayoutContext ResolveWidthLayoutContext(
        Vector3 ownerPos,
        Vector3 fieldCenter,
        Vector3 right,
        float wingEnterThreshold,
        float wingExitThreshold,
        int ownerFormationSlot,
        ref WingChannelState wingState)
    {
        float ownerLateral = Vector3.Dot(ownerPos - fieldCenter, right);
        OwnerZone geometricZone;

        if (ownerLateral > wingEnterThreshold)
        {
            geometricZone = OwnerZone.Right;
        }
        else if (ownerLateral < -wingEnterThreshold)
        {
            geometricZone = OwnerZone.Left;
        }
        else if (wingState.Zone == OwnerZone.Right && ownerLateral > wingExitThreshold)
        {
            geometricZone = OwnerZone.Right;
        }
        else if (wingState.Zone == OwnerZone.Left && ownerLateral < -wingExitThreshold)
        {
            geometricZone = OwnerZone.Left;
        }
        else
        {
            geometricZone = OwnerZone.Central;
        }

        OwnerZone zone = ApplyOwnerSlotZoneBias(geometricZone, ownerFormationSlot);
        wingState.Zone = zone;

        return new WidthLayoutContext(
            ownerLateral,
            zone,
            ownerFormationSlot,
            wingEnterThreshold,
            wingExitThreshold);
    }

    /// <summary>
    /// ワイド・中央レーンともフィールド基準の固定横位置 + 保持者 Z 追従。
    /// </summary>
    private static Vector3 BuildLaneAnchor(
        Vector3 ownerPos,
        Vector3 fieldCenter,
        Vector3 toGoal,
        Vector3 right,
        float forwardLead,
        float wingLane,
        int slot,
        SupportLaneRole laneRole)
    {
        Vector3 forwardPoint = ownerPos + toGoal * forwardLead;
        float forwardFromCenter = Vector3.Dot(forwardPoint - fieldCenter, toGoal);

        if (laneRole == SupportLaneRole.LeftWide || laneRole == SupportLaneRole.RightWide)
        {
            float lateralFromCenter = laneRole == SupportLaneRole.LeftWide ? -wingLane : wingLane;
            return fieldCenter + right * lateralFromCenter + toGoal * forwardFromCenter;
        }

        if (laneRole == SupportLaneRole.CentralChannel)
        {
            return fieldCenter + toGoal * forwardFromCenter;
        }

        return forwardPoint;
    }

    private static Vector3 GetLaneOffset(int slot, SupportLaneRole laneRole, Vector3 right, float wingLane)
    {
        float centralLane = wingLane * CentralLaneRatio;

        return laneRole switch
        {
            SupportLaneRole.RightWide => right * wingLane,
            SupportLaneRole.LeftWide => -right * wingLane,
            SupportLaneRole.CentralChannel => slot switch
            {
                1 => -right * centralLane,
                2 => right * centralLane,
                _ => Vector3.zero,
            },
            _ => Vector3.zero,
        };
    }

    private static bool IsOnAssignedLane(
        int slot,
        Vector3 anchor,
        Vector3 candidate,
        Vector3 right,
        float fieldLength,
        in WidthLayoutContext layoutContext)
    {
        float minWingOffset = fieldLength * 0.04f;
        float lateral = Vector3.Dot(candidate - anchor, right);

        if (layoutContext.SlotUsesCentralChannel(slot))
        {
            return slot switch
            {
                0 => Mathf.Abs(lateral) <= fieldLength * 0.12f,
                1 => lateral <= minWingOffset,
                2 => lateral >= -minWingOffset,
                _ => true,
            };
        }

        return slot switch
        {
            1 => lateral >= -minWingOffset * 0.25f,
            2 => lateral <= minWingOffset * 0.25f,
            _ => true,
        };
    }

    private static float EvaluateCandidate(
        Vector3 position,
        Vector3 selfPos,
        Vector3 ownerPos,
        Vector3 toGoal,
        Vector3 right,
        int slot,
        TeamBlackboard teamBB,
        Settings settings,
        float minDist,
        float optimalDist,
        float maxDist,
        float fieldLength,
        float fieldWidth,
        in WidthLayoutContext layoutContext,
        float wingEnterThreshold)
    {
        float score = 0f;
        float ownerDistance = Vector3.Distance(position, ownerPos);
        float lateralOffset = Vector3.Dot(position - ownerPos, right);
        float minWingOffset = fieldLength * 0.04f;
        bool useCentralChannel = layoutContext.SlotUsesCentralChannel(slot);
        float wingLane = fieldWidth * settings.WingLaneRatio;
        float centralLane = wingLane * CentralLaneRatio;
        Vector3 fieldCenter = teamBB.FieldInfo.FieldCenter;
        float signedCenterLat = Vector3.Dot(position - fieldCenter, right);
        float centerLateral = Mathf.Abs(signedCenterLat);
        SupportLaneRole assignedLane = layoutContext.GetSlotLaneRole(slot);

        switch (assignedLane)
        {
            case SupportLaneRole.RightWide:
                score += (1f - Mathf.Clamp01(Mathf.Abs(signedCenterLat - wingLane) / Mathf.Max(wingLane * 0.4f, 0.1f))) * 4f;
                break;
            case SupportLaneRole.LeftWide:
                score += (1f - Mathf.Clamp01(Mathf.Abs(signedCenterLat + wingLane) / Mathf.Max(wingLane * 0.4f, 0.1f))) * 4f;
                break;
            case SupportLaneRole.CentralChannel:
                score += (1f - Mathf.Clamp01(centerLateral / Mathf.Max(wingLane * 0.35f, 0.1f))) * 4f;
                break;
        }

        float ownerExclusionRadius = fieldLength * 0.14f;
        if (ownerDistance < ownerExclusionRadius)
        {
            score -= 8f * (1f - ownerDistance / ownerExclusionRadius);
        }

        switch (slot)
        {
            case 0 when useCentralChannel:
                score += (1f - Mathf.Clamp01(centerLateral / Mathf.Max(wingLane * 0.35f, 0.1f))) * 4f;
                if (IsSameWingAsOwner(lateralOffset, layoutContext, wingEnterThreshold))
                {
                    score -= 5f;
                }

                break;
            case 1 when useCentralChannel:
                if (lateralOffset > wingLane * 0.15f)
                {
                    return float.MinValue + 1f;
                }

                score += Mathf.Clamp01(-lateralOffset / Mathf.Max(centralLane, 0.1f)) * 3f;
                break;
            case 1:
                if (lateralOffset < minWingOffset)
                {
                    return float.MinValue + 1f;
                }

                score += Mathf.Clamp01(lateralOffset / Mathf.Max(wingLane, 0.1f)) * 3f;
                break;
            case 2 when useCentralChannel:
                if (lateralOffset < -wingLane * 0.15f)
                {
                    return float.MinValue + 1f;
                }

                score += Mathf.Clamp01(lateralOffset / Mathf.Max(centralLane, 0.1f)) * 3f;
                break;
            case 2:
                if (lateralOffset > -minWingOffset)
                {
                    return float.MinValue + 1f;
                }

                score += Mathf.Clamp01(-lateralOffset / Mathf.Max(wingLane, 0.1f)) * 3f;
                break;
            default:
                score += (1f - Mathf.Clamp01(Mathf.Abs(lateralOffset) / Mathf.Max(fieldWidth * 0.2f, 0.1f))) * 0.8f;
                break;
        }

        if (IsSameWingAsOwner(lateralOffset, layoutContext, wingEnterThreshold))
        {
            score -= 4f;
        }

        float forwardOffset = Vector3.Dot(position - ownerPos, toGoal);
        if (forwardOffset < fieldLength * 0.02f)
        {
            score -= 2f;
        }
        else
        {
            score += Mathf.Clamp01(forwardOffset / Mathf.Max(fieldLength * settings.ForwardLeadRatio * 2f, 0.1f)) * 2.5f;
        }

        if (ownerDistance < minDist || ownerDistance > maxDist)
        {
            score -= 3f;
        }
        else
        {
            float halfBand = Mathf.Max(0.1f, (maxDist - minDist) * 0.5f);
            score += (1f - Mathf.Clamp01(Mathf.Abs(ownerDistance - optimalDist) / halfBand)) * 2f;
        }

        Vector3 toCandidate = position - ownerPos;
        toCandidate.y = 0f;
        if (toCandidate.sqrMagnitude > 0.01f && toGoal.sqrMagnitude > 0.01f)
        {
            float supportAngle = Vector3.Angle(toGoal, toCandidate.normalized);
            if (supportAngle <= settings.AngleTolerance)
            {
                score += (1f - supportAngle / Mathf.Max(settings.AngleTolerance, 1f)) * 1.2f;
            }
            else
            {
                score -= Mathf.Clamp01((supportAngle - settings.AngleTolerance) / settings.AngleTolerance) * 0.5f;
            }
        }

        float minTeammateDistance = float.MaxValue;
        foreach (Vector3 teammatePos in teamBB.BasicInfo.TeammatePositions)
        {
            if ((teammatePos - selfPos).sqrMagnitude < 0.01f)
            {
                continue;
            }

            float dist = Vector3.Distance(position, teammatePos);
            minTeammateDistance = Mathf.Min(minTeammateDistance, dist);

            float teammateLateral = Vector3.Dot(teammatePos - ownerPos, right);
            bool sameWing = lateralOffset * teammateLateral > 0f
                && Mathf.Abs(teammateLateral) > minWingOffset;
            if (sameWing && dist < fieldLength * 0.35f)
            {
                score -= (1f - dist / (fieldLength * 0.35f)) * 2.5f;
            }
        }

        if (minTeammateDistance > fieldLength * 0.15f)
        {
            score += 1.0f;
        }

        float minEnemyDistance = float.MaxValue;
        foreach (Vector3 enemyPos in teamBB.BasicInfo.EnemyPositions)
        {
            minEnemyDistance = Mathf.Min(minEnemyDistance, Vector3.Distance(position, enemyPos));
        }

        if (minEnemyDistance > fieldLength * 0.1f)
        {
            score += 0.5f;
        }

        float passBlockRange = fieldLength * 0.06f;
        if (PlayerBlackboardCalculator.IsPassRouteClear(position, ownerPos, teamBB.BasicInfo.EnemyPositions, passBlockRange))
        {
            score += 1.5f;
        }
        else
        {
            score -= 1f;
        }

        float moveDist = Vector3.Distance(position, selfPos);
        score += (1f - Mathf.Clamp01(moveDist / (fieldLength * 0.35f))) * 0.3f;

        return score;
    }

    private static bool IsSameWingAsOwner(float supporterLateral, in WidthLayoutContext layoutContext, float wingEnterThreshold)
    {
        if (layoutContext.Zone == OwnerZone.Central)
        {
            return false;
        }

        if (layoutContext.Zone == OwnerZone.Right)
        {
            return supporterLateral > wingEnterThreshold * 0.35f;
        }

        return supporterLateral < -wingEnterThreshold * 0.35f;
    }

    private static Vector3 ClampToField(Vector3 pos, TeamFieldInfo field)
    {
        float halfW = field.FieldWidth * 0.5f;
        float halfL = field.FieldLength * 0.5f;
        Vector3 center = field.FieldCenter;
        return new Vector3(
            Mathf.Clamp(pos.x, center.x - halfW, center.x + halfW),
            pos.y,
            Mathf.Clamp(pos.z, center.z - halfL, center.z + halfL));
    }

    private static Vector3 ResolveBallOwnerPosition(TeamBlackboard teamBB)
    {
        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        if (ownerPos.sqrMagnitude < 0.01f)
        {
            ownerPos = teamBB.BallInfo.BallPosition;
        }

        return ownerPos;
    }
}
