using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GOAP移動アクション調査用ログ（Assets/DebugLog/GoapDiag_latest.txt に追記）。
/// </summary>
public static class GoapMovementDiagnostic
{
    private static readonly Dictionary<string, float> _nextLogTime = new Dictionary<string, float>();

    public static void Log(string category, string message, PlayerBlackboard bb = null, float throttleSeconds = 0f)
    {
        string actor = FormatActor(bb);
        string line = $"[GOAP_MOVE][{category}] [{actor}] {message}";
        Debug.Log(line);

        if (throttleSeconds > 0f)
        {
            string key = $"{category}|{actor}|{message.GetHashCode()}";
            if (_nextLogTime.TryGetValue(key, out float next) && Time.time < next)
            {
                return;
            }

            _nextLogTime[key] = Time.time + throttleSeconds;
        }

        GoapDiagnosticLog.Write(line);
    }

    public static void LogThrottled(string category, string message, PlayerBlackboard bb, float intervalSeconds = 0.5f)
    {
        string actor = FormatActor(bb);
        string key = $"{category}|{actor}";
        if (_nextLogTime.TryGetValue(key, out float next) && Time.time < next)
        {
            return;
        }

        _nextLogTime[key] = Time.time + intervalSeconds;
        Log(category, message, bb);
    }

    public static string FormatActor(PlayerBlackboard bb)
    {
        if (bb == null || bb.BasicData.Self == null)
        {
            return "actor=unknown";
        }

        string name = bb.BasicData.Self.name;
        int playerId = bb.BasicData.PlayerID;
        return $"actor={name},playerId={playerId}";
    }

    public static string FormatVector(Vector3 v) => $"({v.x:F2},{v.y:F2},{v.z:F2})";

    public static string FormatMotorResolve(PlayerBlackboard bb)
    {
        if (bb == null || bb.BasicData.Self == null)
        {
            return "resolve=false reason=null_bb_or_self";
        }

        var facade = bb.BasicData.Self.GetComponentInParent<AnimalFacade>();
        if (facade == null)
        {
            return "resolve=false reason=no_facade";
        }

        var selector = facade.GetActionSelector();
        var handler = facade.GetAnimalHandler();
        string path = selector != null ? "selector" : handler != null ? "handler" : "none";
        return $"resolve=true path={path} facade={facade.name}";
    }
}
