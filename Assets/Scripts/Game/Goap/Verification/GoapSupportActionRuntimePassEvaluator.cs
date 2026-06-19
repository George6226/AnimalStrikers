using System;
using System.Collections.Generic;

public static class GoapSupportActionRuntimePassEvaluator
{
    public static GoapSupportActionRuntimePassResult EvaluatePattern(
        GoapSupportLayoutPatternId pattern,
        IGoapSupportActionRuntimePassCriteria criteria,
        IList<string> diagLines,
        IList<string> summaryLines,
        Func<int, int?> resolvePlayerIdForSlot)
    {
        var result = new GoapSupportActionRuntimePassResult();
        if (criteria == null)
        {
            result.DetailText = "no criteria";
            return result;
        }

        if (!criteria.TryGetEvaluationSlot(pattern, out int slot))
        {
            result.ShouldEvaluate = false;
            result.DetailText = "pattern not applicable";
            return result;
        }

        result.ShouldEvaluate = true;
        result.TargetSlot = slot;

        if (criteria.Action == GoapSupportActionUnderTest.WingOwnerDriveFollow)
        {
            return EvaluateWingOwnerDrive(pattern, diagLines, resolvePlayerIdForSlot, result);
        }

        if (criteria.Action == GoapSupportActionUnderTest.CreateSupportAngle)
        {
            return EvaluateCreateSupportAngle(pattern, diagLines, resolvePlayerIdForSlot, result);
        }

        if (criteria.Action == GoapSupportActionUnderTest.GetOpen)
        {
            return EvaluateGetOpen(pattern, diagLines, summaryLines, resolvePlayerIdForSlot, result);
        }

        int? playerId = resolvePlayerIdForSlot?.Invoke(slot);
        if (!playerId.HasValue)
        {
            result.PatternPass = false;
            result.DetailText = $"slot{slot} playerId unresolved";
            return result;
        }

        return EvaluateMoveToSupport(slot, playerId.Value, diagLines, summaryLines, result);
    }

    private static GoapSupportActionRuntimePassResult EvaluateMoveToSupport(
        int slot,
        int playerId,
        IList<string> diagLines,
        IList<string> summaryLines,
        GoapSupportActionRuntimePassResult result)
    {
        bool actionStarted = TryFindSummaryActionStart(summaryLines, playerId, "MoveToSupportPosition", out string startSource);
        bool executeStarted = TryFindDiagMessage(diagLines, playerId, "Support", "Execute start", out _);
        bool tacticalComplete = TryFindDiagMessage(
            diagLines, playerId, "Support", "Complete reason=tactical_in_position", out string completeLine);
        bool timeout = TryFindDiagMessage(diagLines, playerId, "Support", "Complete reason=timeout", out _);
        bool canceled = TryFindDiagMessage(diagLines, playerId, "Support", "Cancel", out _);

        if (tacticalComplete)
        {
            result.PatternPass = true;
            result.DetailText =
                $"slot{slot} playerId={playerId} tactical_in_position OK" +
                (actionStarted ? $" ActionStart({startSource})" : string.Empty);
            return result;
        }

        var reasons = new List<string>();
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
        else if (!tacticalComplete)
        {
            reasons.Add("no tactical_in_position");
        }

        if (actionStarted)
        {
            reasons.Add($"ActionStart({startSource})");
        }

        result.PatternPass = false;
        result.DetailText = $"slot{slot} playerId={playerId} NG: {string.Join(", ", reasons)}";
        if (!string.IsNullOrEmpty(completeLine))
        {
            result.DetailText += $" last={TrimForDetail(completeLine)}";
        }

        return result;
    }

    private static GoapSupportActionRuntimePassResult EvaluateCreateSupportAngle(
        GoapSupportLayoutPatternId pattern,
        IList<string> diagLines,
        Func<int, int?> resolvePlayerIdForSlot,
        GoapSupportActionRuntimePassResult result)
    {
        int ownerSlot = GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, 0);
        int passCount = 0;
        int evalCount = 0;
        var details = new List<string>();

        for (int wingSlot = 1; wingSlot <= 2; wingSlot++)
        {
            if (wingSlot == ownerSlot)
            {
                continue;
            }

            evalCount++;
            int? wingPlayerId = resolvePlayerIdForSlot?.Invoke(wingSlot);
            if (!wingPlayerId.HasValue)
            {
                details.Add($"slot{wingSlot} playerId unresolved NG");
                continue;
            }

            if (TryFindDiagMessage(
                    diagLines,
                    wingPlayerId.Value,
                    "SupportAngle",
                    "Finish passReceive=true (tactical check passed)",
                    out _))
            {
                passCount++;
                details.Add($"slot{wingSlot} passReceive=true OK");
            }
            else
            {
                details.Add($"slot{wingSlot} passReceive missing NG");
            }
        }

        if (evalCount == 0)
        {
            result.ShouldEvaluate = false;
            result.DetailText = "no wing slots to evaluate";
            return result;
        }

        result.PatternPass = passCount == evalCount;
        result.DetailText = string.Join("; ", details);
        return result;
    }

    private static GoapSupportActionRuntimePassResult EvaluateWingOwnerDrive(
        GoapSupportLayoutPatternId pattern,
        IList<string> diagLines,
        Func<int, int?> resolvePlayerIdForSlot,
        GoapSupportActionRuntimePassResult result)
    {
        int ownerSlot = GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, 0);
        int wingSlot = ownerSlot == 1 ? 2 : 1;
        const int centralSlot = 0;
        int passCount = 0;
        int evalCount = 0;
        var details = new List<string>();

        evalCount++;
        if (EvaluateCentralDriveFollow(centralSlot, diagLines, resolvePlayerIdForSlot, out string centralDetail))
        {
            passCount++;
            details.Add(centralDetail);
        }
        else
        {
            details.Add(centralDetail);
        }

        evalCount++;
        if (EvaluateWingDriveFollow(wingSlot, diagLines, resolvePlayerIdForSlot, out string wingDetail))
        {
            passCount++;
            details.Add(wingDetail);
        }
        else
        {
            details.Add(wingDetail);
        }

        result.PatternPass = passCount == evalCount;
        result.DetailText = string.Join("; ", details);
        return result;
    }

    private static bool EvaluateCentralDriveFollow(
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

        int retargetCount = CountDiagMessages(diagLines, playerId.Value, "Support", "Retarget");
        bool executeStarted = TryFindDiagMessage(diagLines, playerId.Value, "Support", "Execute start", out _);
        bool timedOut = TryFindDiagMessage(diagLines, playerId.Value, "Support", "Complete reason=timeout", out _);

        if (retargetCount >= GoapWingOwnerDriveRuntimePassCriteria.MinRetargetCount)
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

    private static bool EvaluateWingDriveFollow(
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

        int retargetCount = CountDiagMessages(diagLines, playerId.Value, "SupportAngle", "Retarget");
        bool continueMoving = TryFindDiagMessage(
            diagLines, playerId.Value, "SupportAngle", "ContinueMoving drift=", out _);
        bool passReceive = TryFindDiagMessage(
            diagLines, playerId.Value, "SupportAngle", "Finish passReceive=true (tactical check passed)", out _);
        bool executeStarted = TryFindDiagMessage(diagLines, playerId.Value, "SupportAngle", "Execute", out _);
        bool timedOut = TryFindDiagMessage(
            diagLines, playerId.Value, "SupportAngle", "Finish(timeout)", out _)
            || TryFindDiagMessage(diagLines, playerId.Value, "SupportAngle", "Finish(cancel)", out _);

        bool followOk = retargetCount >= GoapWingOwnerDriveRuntimePassCriteria.MinRetargetCount
            && (continueMoving || passReceive || retargetCount >= 2);

        if (followOk)
        {
            string mode = passReceive ? "passReceive" : (continueMoving ? "ContinueMoving" : "Retargetx2");
            detail = $"slot{slot} {mode} Retarget={retargetCount} OK";
            return true;
        }

        var reasons = new List<string> { $"Retarget={retargetCount}" };
        if (!continueMoving && !passReceive)
        {
            reasons.Add("no ContinueMoving/passReceive");
        }

        if (!executeStarted)
        {
            reasons.Add("no Execute");
        }

        if (timedOut)
        {
            reasons.Add("timeout/cancel");
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

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            if (line.Contains(categoryToken) && line.Contains(playerToken) && line.Contains(messageToken))
            {
                count++;
            }
        }

        return count;
    }

    private static GoapSupportActionRuntimePassResult EvaluateGetOpen(
        GoapSupportLayoutPatternId pattern,
        IList<string> diagLines,
        IList<string> summaryLines,
        Func<int, int?> resolvePlayerIdForSlot,
        GoapSupportActionRuntimePassResult result)
    {
        int ownerSlot = GoapSupportLayoutPatternLibrary.GetBallOwnerSlotForPattern(pattern, 0);
        int passCount = 0;
        int evalCount = 0;
        var details = new List<string>();

        for (int wingSlot = 1; wingSlot <= 2; wingSlot++)
        {
            if (wingSlot == ownerSlot)
            {
                continue;
            }

            evalCount++;
            int? wingPlayerId = resolvePlayerIdForSlot?.Invoke(wingSlot);
            if (!wingPlayerId.HasValue)
            {
                details.Add($"slot{wingSlot} playerId unresolved NG");
                continue;
            }

            bool actionStarted = TryFindSummaryActionStart(
                summaryLines, wingPlayerId.Value, "GetOpen", out string startSource);
            bool executeStarted = TryFindDiagMessage(
                diagLines, wingPlayerId.Value, "GetOpen", "Execute target=", out _);
            bool tacticalComplete = TryFindDiagMessage(
                diagLines, wingPlayerId.Value, "GetOpen", "Finish arrived=true", out _);
            bool timedOut = TryFindDiagMessage(
                diagLines, wingPlayerId.Value, "GetOpen", "Finish arrived=false", out _);

            if (tacticalComplete)
            {
                passCount++;
                details.Add(
                    $"slot{wingSlot} arrived=true OK" +
                    (actionStarted ? $" ActionStart({startSource})" : string.Empty));
            }
            else
            {
                var reasons = new List<string>();
                if (timedOut)
                {
                    reasons.Add("timeout");
                }

                if (!executeStarted)
                {
                    reasons.Add("no Execute");
                }
                else
                {
                    reasons.Add("no arrived=true");
                }

                if (actionStarted)
                {
                    reasons.Add($"ActionStart({startSource})");
                }

                details.Add($"slot{wingSlot} NG: {string.Join(", ", reasons)}");
            }
        }

        if (evalCount == 0)
        {
            result.ShouldEvaluate = false;
            result.DetailText = "no wing slots to evaluate";
            return result;
        }

        result.PatternPass = passCount == evalCount;
        result.DetailText = string.Join("; ", details);
        return result;
    }

    private static bool TryFindSummaryActionStart(
        IList<string> lines,
        int playerId,
        string actionName,
        out string source)
    {
        source = null;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            string line = lines[i];
            if (!line.Contains("[GOAP_SUMMARY]")
                || !line.Contains("ActionStart(action=")
                || !line.Contains($"playerId={playerId}"))
            {
                continue;
            }

            string actual = ExtractBetween(line, "ActionStart(action=", ",");
            if (string.IsNullOrEmpty(actual))
            {
                continue;
            }

            if (actual.StartsWith(actionName, StringComparison.Ordinal))
            {
                source = "Summary";
                return true;
            }
        }

        return false;
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

        for (int i = lines.Count - 1; i >= 0; i--)
        {
            string line = lines[i];
            if (!line.Contains(categoryToken) || !line.Contains(playerToken) || !line.Contains(messageToken))
            {
                continue;
            }

            matchedLine = line;
            return true;
        }

        return false;
    }

    private static string ExtractBetween(string line, string startToken, string endToken)
    {
        int start = line.IndexOf(startToken, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += startToken.Length;
        int end = line.IndexOf(endToken, start, StringComparison.Ordinal);
        if (end < 0)
        {
            return null;
        }

        return line.Substring(start, end - start);
    }

    private static string TrimForDetail(string line)
    {
        const string moveToken = "[GOAP_MOVE]";
        int index = line.IndexOf(moveToken, StringComparison.Ordinal);
        return index >= 0 ? line.Substring(index) : line;
    }
}
