using System;
using System.IO;
using UnityEngine;

/// <summary>
/// パス実行の調査用ログ（GoapDiag_latest.txt + GoapSummary_latest.txt に [GOAP_PASS] で追記）。
/// </summary>
public static class GoapPassDiagnostic
{
    private const string Tag = "GOAP_PASS";
    private static string _summaryFilePath;
    private static bool _summaryInitialized;

    public static void Log(AnimalFacade passer, string message)
    {
        if (!GoapRuntimeDiagnostics.VerboseLoggingEnabled)
        {
            return;
        }

        string actor = FormatActor(passer);
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{Tag}] [{actor}] {message}";
        Debug.Log(line);
        GoapDiagnosticLog.Write(line);
        AppendSummary(line);
    }

    public static void LogPhase(
        AnimalFacade passer,
        AnimalFacade target,
        string phase,
        Vector3 passerPos,
        Vector3 targetPos,
        float distance,
        bool needsLob,
        string extra = null)
    {
        string targetName = target != null ? target.name : "null";
        string suffix = string.IsNullOrEmpty(extra) ? string.Empty : $" {extra}";
        Log(
            passer,
            $"{phase} target={targetName} " +
            $"passerPos={FormatVector(passerPos)} targetPos={FormatVector(targetPos)} " +
            $"dist={distance:F2} needsLob={needsLob}{suffix}");
    }

    public static void LogKick(
        AnimalFacade passer,
        AnimalFacade target,
        Vector3 passerPosStart,
        Vector3 targetPosStart,
        Vector3 passerPosKick,
        Vector3 targetPosKick,
        bool needsLobStart,
        bool needsLobKick,
        float passStat,
        Vector3 kickDir)
    {
        float startDist = Vector3.Distance(passerPosStart, targetPosStart);
        float kickDist = Vector3.Distance(passerPosKick, targetPosKick);
        float targetShift = Vector3.Distance(targetPosStart, targetPosKick);
        float aimShift = Vector3.Angle(targetPosStart - passerPosStart, targetPosKick - passerPosKick);
        bool deterministic = AnimalActionAccuracyPolicy.UseDeterministicDirection;
        Log(
            passer,
            $"Kick target={target?.name ?? "null"} " +
            $"startDist={startDist:F2} kickDist={kickDist:F2} targetShift={targetShift:F2} aimDeltaDeg={aimShift:F1} " +
            $"lobStart={needsLobStart} lobKick={needsLobKick} passStat={passStat:F0} " +
            $"deterministic={deterministic} kickSpeed={kickDir.magnitude:F2} " +
            $"passerKick={FormatVector(passerPosKick)} targetKick={FormatVector(targetPosKick)}");
    }

    public static string FormatVector(Vector3 v) => $"({v.x:F2},{v.y:F2},{v.z:F2})";

    private static string FormatActor(AnimalFacade passer)
    {
        if (passer == null)
        {
            return "passer=unknown";
        }

        return $"passer={passer.name}";
    }

    private static void AppendSummary(string line)
    {
        if (!GoapRuntimeDiagnostics.ShouldIncludeInSummaryLog(line))
        {
            return;
        }

        try
        {
            if (!_summaryInitialized)
            {
                string dir = Path.Combine(Application.dataPath, "DebugLog");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                _summaryFilePath = Path.Combine(dir, "GoapSummary_latest.txt");
                _summaryInitialized = true;
            }

            File.AppendAllText(_summaryFilePath, line + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{Tag}] summary write failed: {e.Message}");
        }
    }
}
