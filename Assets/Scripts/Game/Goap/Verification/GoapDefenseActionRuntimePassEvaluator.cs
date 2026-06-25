using System;
using System.Collections.Generic;

public static class GoapDefenseActionRuntimePassEvaluator
{
    public static GoapDefenseActionRuntimePassResult EvaluatePattern(
        GoapDefenseLayoutPatternId pattern,
        IGoapDefenseActionRuntimePassCriteria criteria,
        IList<string> diagLines,
        IList<string> summaryLines,
        Func<int, int?> resolvePlayerIdForSlot)
    {
        var result = new GoapDefenseActionRuntimePassResult();
        if (criteria == null)
        {
            result.DetailText = "no criteria";
            return result;
        }

        if (criteria.Action == GoapDefenseActionUnderTest.EnemyOwnerDriveFollow)
        {
            return EvaluateEnemyOwnerDrive(pattern, diagLines, resolvePlayerIdForSlot, result);
        }

        if (!criteria.TryGetEvaluationSlot(pattern, out int slot))
        {
            result.ShouldEvaluate = false;
            result.DetailText = "pattern not applicable";
            return result;
        }

        result.ShouldEvaluate = true;
        result.TargetSlot = slot;

        int? playerId = resolvePlayerIdForSlot?.Invoke(slot);
        if (!playerId.HasValue)
        {
            result.PatternPass = false;
            result.DetailText = $"slot{slot} playerId unresolved";
            return result;
        }

        return EvaluateMoveToDefensivePosition(slot, playerId.Value, diagLines, summaryLines, result);
    }

    private static GoapDefenseActionRuntimePassResult EvaluateEnemyOwnerDrive(
        GoapDefenseLayoutPatternId pattern,
        IList<string> diagLines,
        Func<int, int?> resolvePlayerIdForSlot,
        GoapDefenseActionRuntimePassResult result)
    {
        if (pattern != GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies_DriveForward
            && pattern != GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield_DriveForward)
        {
            result.ShouldEvaluate = false;
            result.DetailText = "pattern not applicable";
            return result;
        }

        result.ShouldEvaluate = true;
        int passCount = 0;
        int evalCount = 0;
        var details = new List<string>();

        for (int slot = 0; slot <= 2; slot++)
        {
            evalCount++;
            if (EvaluateDefendDriveFollow(slot, diagLines, resolvePlayerIdForSlot, out string detail))
            {
                passCount++;
                details.Add(detail);
            }
            else
            {
                details.Add(detail);
            }
        }

        result.PatternPass = passCount == evalCount;
        result.DetailText = string.Join("; ", details);
        return result;
    }

    private static GoapDefenseActionRuntimePassResult EvaluateMoveToDefensivePosition(
        int slot,
        int playerId,
        IList<string> diagLines,
        IList<string> summaryLines,
        GoapDefenseActionRuntimePassResult result)
    {
        bool actionStarted = TryFindSummaryActionStart(summaryLines, playerId, "MoveToDefensivePosition", out string startSource);
        bool executeStarted = TryFindDiagMessage(diagLines, playerId, "Defend", "Execute start", out _);
        int retargetCount = CountDiagMessages(diagLines, playerId, "Defend", "Retarget");
        bool tacticalComplete = TryFindDiagMessage(
            diagLines, playerId, "Defend", "Complete reason=tactical_in_position", out _);
        bool timeout = TryFindDiagMessage(diagLines, playerId, "Defend", "Complete reason=timeout", out _);
        bool canceled = TryFindDiagMessage(diagLines, playerId, "Defend", "Cancel", out _);

        if (retargetCount >= GoapMoveToDefensivePositionRuntimePassCriteria.MinRetargetCount
            || tacticalComplete)
        {
            result.PatternPass = true;
            string mode = tacticalComplete ? "tactical_in_position" : $"Retarget={retargetCount}";
            result.DetailText =
                $"slot{slot} playerId={playerId} {mode} OK" +
                (actionStarted ? $" ActionStart({startSource})" : string.Empty);
            return result;
        }

        var reasons = new List<string> { $"Retarget={retargetCount}" };
        if (timeout)
        {
            reasons.Add("timeout");
        }

        if (canceled)
        {
            reasons.Add("cancel");
        }

        if (!executeStarted)
        {
            reasons.Add("no Execute start");
        }

        if (actionStarted)
        {
            reasons.Add($"ActionStart({startSource})");
        }

        result.PatternPass = false;
        result.DetailText = $"slot{slot} playerId={playerId} NG: {string.Join(", ", reasons)}";
        return result;
    }

    private static bool EvaluateDefendDriveFollow(
        int slot,
        IList<string> diagLines,
        Func<int, int?> resolvePlayerIdForSlot,
        out string detail)
    {
        int? playerId = resolvePlayerIdForSlot?.Invoke(slot);
        if (!playerId.HasValue)
        {
            detail = $"slot{slot} playerId unresolved NG";
            return false;
        }

        int retargetCount = CountDiagMessages(diagLines, playerId.Value, "Defend", "Retarget");
        bool executeStarted = TryFindDiagMessage(diagLines, playerId.Value, "Defend", "Execute start", out _);
        bool timedOut = TryFindDiagMessage(diagLines, playerId.Value, "Defend", "Complete reason=timeout", out _);

        if (retargetCount >= GoapEnemyOwnerDriveRuntimePassCriteria.MinRetargetCount)
        {
            detail = $"slot{slot} Retarget={retargetCount} OK";
            return true;
        }

        var reasons = new List<string> { $"Retarget={retargetCount}" };
        if (!executeStarted)
        {
            reasons.Add("no Execute start");
        }

        if (timedOut)
        {
            reasons.Add("timeout");
        }

        detail = $"slot{slot} NG: {string.Join(", ", reasons)}";
        return false;
    }

    private static int CountDiagMessages(
        IList<string> lines,
        int playerId,
        string category,
        string messageToken)
    {
        int count = 0;
        string categoryToken = $"[GOAP_MOVE][{category}]";
        string playerToken = $"playerId={playerId}";

        foreach (string line in lines)
        {
            if (line.Contains(categoryToken) && line.Contains(playerToken) && line.Contains(messageToken))
            {
                count++;
            }
        }

        return count;
    }

    private static bool TryFindDiagMessage(
        IList<string> lines,
        int playerId,
        string category,
        string messageToken,
        out string matchedLine)
    {
        matchedLine = null;
        string categoryToken = $"[GOAP_MOVE][{category}]";
        string playerToken = $"playerId={playerId}";

        foreach (string line in lines)
        {
            if (line.Contains(categoryToken) && line.Contains(playerToken) && line.Contains(messageToken))
            {
                matchedLine = line;
                return true;
            }
        }

        return false;
    }

    private static bool TryFindSummaryActionStart(
        IList<string> lines,
        int playerId,
        string actionName,
        out string source)
    {
        source = null;
        string playerToken = $"playerId={playerId}";

        foreach (string line in lines)
        {
            if (!line.Contains("[GOAP_SUMMARY]") || !line.Contains(playerToken))
            {
                continue;
            }

            if (line.Contains($"ActionStart(action={actionName}")
                || line.Contains($"action={actionName}"))
            {
                source = "summary";
                return true;
            }
        }

        return false;
    }
}
