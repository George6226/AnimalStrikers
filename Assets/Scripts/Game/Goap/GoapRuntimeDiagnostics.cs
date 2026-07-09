/// <summary>
/// GOAP 観戦・バッチ検証向けログレベル。
/// Off=無効 / Summary=重要イベントのみファイル / Verbose=Console+全診断ファイル。
/// </summary>
public enum GoapDiagnosticLevel
{
    Off = 0,
    Summary = 1,
    Verbose = 2,
}

/// <summary>
/// GOAP 観戦・バッチ検証向けの詳細ログ（Console + ファイル I/O）を制御する。
/// 通常 Play では Off が既定で、Play 中の負荷を抑える。
/// </summary>
public static class GoapRuntimeDiagnostics
{
    public static GoapDiagnosticLevel Level { get; private set; } = GoapDiagnosticLevel.Off;

    public static bool SummaryLoggingEnabled => Level >= GoapDiagnosticLevel.Summary;

    public static bool VerboseLoggingEnabled => Level >= GoapDiagnosticLevel.Verbose;

    public static void SetLevel(GoapDiagnosticLevel level)
    {
        Level = level;
        GoapDiagnosticLog.SetEnabled(level >= GoapDiagnosticLevel.Verbose);
    }

    public static void EnableVerboseLogging()
    {
        SetLevel(GoapDiagnosticLevel.Verbose);
    }

    public static void DisableVerboseLogging()
    {
        SetLevel(GoapDiagnosticLevel.Off);
    }

    /// <summary>
    /// Summary レベルでファイルに残す行かどうか（PlanCosts や毎回の Replan ノイズを除外）。
    /// </summary>
    public static bool ShouldIncludeInSummaryLog(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return false;
        }

        if (message.StartsWith("PlanCosts("))
        {
            // バッチ本番選出検証は PlanCosts の selected= を参照する。
            return GoapBatchVerifyEnvironment.IsActive;
        }

        if (message.StartsWith("PlanningStart(")
            || message.StartsWith("NoGoalIdle(")
            || message.StartsWith("ReplanDeferred(")
            || message.StartsWith("ReplanCooldown(")
            || message.StartsWith("AbortDeferred(")
            || message.StartsWith("ActionDeferred(")
            || message.StartsWith("SkipPlanning(")
            || message.StartsWith("GoalAlreadyAchieved("))
        {
            return false;
        }

        return message.StartsWith("ActionStart(")
            || message.StartsWith("ActionComplete(")
            || message.StartsWith("GoalChanged(")
            || message.StartsWith("FailureReason(")
            || message.StartsWith("ActionRejected(")
            || message.StartsWith("ActionSkipped(")
            || message.StartsWith("PassReceiveComplete(")
            || message.StartsWith("PassIssued(")
            || message.StartsWith("ReceivePassOutcome(")
            || message.StartsWith("ReceivePassTransition(")
            || message.Contains("Forced")
            || message.StartsWith("PlanSuccess(")
            || message.StartsWith("PlanFailure(")
            || message.StartsWith("NoGoalSelected(")
            || message.StartsWith("Aborted(")
            || message.StartsWith("PlanCleared(")
            || message.StartsWith("Initialized");
    }
}
