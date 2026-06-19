using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// GOAP 検証用: 敵フィールドプレイヤーの配置をパターン切替（移動 AI なし）。
/// TeamBlackboard は TeamRegist.Enemies の Transform を毎フレーム読むため、配置変更は次フレームから WM / コストに反映される。
/// </summary>
public class GoapEnemyPositionDebugPatterns : MonoBehaviour
{
    private const string SummaryTag = "GOAP_ENEMY_LAYOUT";

    public enum LayoutPattern
    {
        /// <summary>シーン開始時の位置に戻す。</summary>
        Baseline = 0,
        /// <summary>敵ゴール手前に横一線（デフォルトに近い守備ライン）。</summary>
        DefensiveLine = 1,
        /// <summary>保持者→味方サポート中心のパスレーン上に1体＋残りはライン。</summary>
        BlockPassLane = 2,
        /// <summary>保持者の右サイド（攻撃方向から見て）に寄せる。RW 向け GetOpen 検証。</summary>
        PressRightWing = 3,
        /// <summary>保持者の左サイドに寄せる。LW 向け検証。</summary>
        PressLeftWing = 4,
        /// <summary>保持者周囲に密集（プレッシャー・NEAR_ENEMY Fact 検証）。</summary>
        PressBallOwner = 5,
        /// <summary>シード固定ランダム（中盤〜攻撃側ゾーン）。</summary>
        RandomSeeded = 6,
    }

    [Header("Enable")]
    [SerializeField] private bool _enableHotkeys = true;
    [Tooltip("適用後に味方 GoapAgent のプランを中断し、再計画を促す")]
    [SerializeField] private bool _triggerGoapReplanAfterApply = true;

    [Header("Pattern Keys (Play Mode)")]
    [SerializeField] private KeyCode _keyBaseline = KeyCode.Alpha1;
    [SerializeField] private KeyCode _keyDefensiveLine = KeyCode.Alpha2;
    [SerializeField] private KeyCode _keyBlockPassLane = KeyCode.Alpha3;
    [SerializeField] private KeyCode _keyPressRightWing = KeyCode.Alpha4;
    [SerializeField] private KeyCode _keyPressLeftWing = KeyCode.Alpha5;
    [SerializeField] private KeyCode _keyPressOwner = KeyCode.Alpha6;
    [SerializeField] private KeyCode _keyRandom = KeyCode.Alpha7;

    [Header("Random")]
    [SerializeField] private int _randomSeed = 42;

    [Header("Layout Tuning (field length ratio)")]
    [SerializeField] private float _defensiveLineDepthRatio = 0.22f;
    [SerializeField] private float _lateralSpreadRatio = 0.18f;
    [SerializeField] private float _passLaneT = 0.5f;
    [SerializeField] private float _wingLateralRatio = 0.28f;
    [SerializeField] private float _pressOwnerRadiusRatio = 0.12f;

    private readonly List<Snapshot> _baseline = new List<Snapshot>();
    private LayoutPattern _activePattern = LayoutPattern.Baseline;
    private static string _summaryLogFilePath;
    private static bool _summaryLogInitialized;

    private struct Snapshot
    {
        public Transform Transform;
        public Vector3 Position;
    }

    private void Awake()
    {
        CaptureBaseline();
    }

    private void Update()
    {
        if (!_enableHotkeys || !Application.isPlaying)
        {
            return;
        }

        if (Input.GetKeyDown(_keyBaseline)) ApplyPattern(LayoutPattern.Baseline);
        if (Input.GetKeyDown(_keyDefensiveLine)) ApplyPattern(LayoutPattern.DefensiveLine);
        if (Input.GetKeyDown(_keyBlockPassLane)) ApplyPattern(LayoutPattern.BlockPassLane);
        if (Input.GetKeyDown(_keyPressRightWing)) ApplyPattern(LayoutPattern.PressRightWing);
        if (Input.GetKeyDown(_keyPressLeftWing)) ApplyPattern(LayoutPattern.PressLeftWing);
        if (Input.GetKeyDown(_keyPressOwner)) ApplyPattern(LayoutPattern.PressBallOwner);
        if (Input.GetKeyDown(_keyRandom)) ApplyPattern(LayoutPattern.RandomSeeded);
    }

    [ContextMenu("Capture Baseline (現在位置を保存)")]
    public void CaptureBaseline()
    {
        _baseline.Clear();
        foreach (var enemy in GetFieldEnemies())
        {
            if (enemy == null)
            {
                continue;
            }

            _baseline.Add(new Snapshot
            {
                Transform = enemy.transform,
                Position = enemy.transform.position,
            });
        }

        LogLine($"CaptureBaseline count={_baseline.Count}");
    }

    [ContextMenu("Apply Baseline")]
    public void ApplyBaseline() => ApplyPattern(LayoutPattern.Baseline);

    [ContextMenu("Apply DefensiveLine")]
    public void ApplyDefensiveLine() => ApplyPattern(LayoutPattern.DefensiveLine);

    [ContextMenu("Apply BlockPassLane")]
    public void ApplyBlockPassLane() => ApplyPattern(LayoutPattern.BlockPassLane);

    [ContextMenu("Apply PressRightWing")]
    public void ApplyPressRightWing() => ApplyPattern(LayoutPattern.PressRightWing);

    [ContextMenu("Apply PressLeftWing")]
    public void ApplyPressLeftWing() => ApplyPattern(LayoutPattern.PressLeftWing);

    [ContextMenu("Apply PressBallOwner")]
    public void ApplyPressBallOwner() => ApplyPattern(LayoutPattern.PressBallOwner);

    [ContextMenu("Apply RandomSeeded")]
    public void ApplyRandomSeeded() => ApplyPattern(LayoutPattern.RandomSeeded);

    public void ApplyPattern(LayoutPattern pattern)
    {
        var enemies = GetFieldEnemies();
        if (enemies.Count == 0)
        {
            LogLine($"ApplyPattern({pattern}) skipped: no field enemies");
            return;
        }

        if (pattern == LayoutPattern.Baseline)
        {
            if (_baseline.Count == 0)
            {
                CaptureBaseline();
            }

            RestoreBaseline();
            _activePattern = pattern;
            LogLine($"ApplyPattern({pattern}) restored={enemies.Count}");
            AfterApply(pattern);
            return;
        }

        if (!TryGetFieldContext(out FieldContext ctx))
        {
            LogLine($"ApplyPattern({pattern}) failed: TeamBlackboard unavailable");
            return;
        }

        var targets = ComputeTargets(pattern, ctx, enemies.Count);
        for (int i = 0; i < enemies.Count; i++)
        {
            float y = enemies[i].transform.position.y;
            Vector3 pos = targets[Mathf.Min(i, targets.Count - 1)];
            pos.y = y;
            enemies[i].transform.position = pos;
        }

        _activePattern = pattern;
        LogLine(
            $"ApplyPattern({pattern}) enemies={enemies.Count} owner={Fmt(ctx.OwnerPos)} " +
            $"passLaneClear={PlayerBlackboardCalculator.IsPassRouteClear(ctx.SupportCenter, ctx.OwnerPos, ctx.EnemyPositions, ctx.FieldLength * 0.06f)} " +
            $"positions={FmtList(targets)}");
        AfterApply(pattern);
    }

    private void AfterApply(LayoutPattern pattern)
    {
        if (_triggerGoapReplanAfterApply)
        {
            TriggerAllyGoapReplan();
        }
    }

    private static void TriggerAllyGoapReplan()
    {
        var agents = FindObjectsByType<GoapAgent>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var agent in agents)
        {
            if (agent == null)
            {
                continue;
            }

            agent.AbortCurrentPlan();
            count++;
        }

        LogLineStatic($"TriggerAllyGoapReplan agents={count}");
    }

    private void RestoreBaseline()
    {
        foreach (var snap in _baseline)
        {
            if (snap.Transform == null)
            {
                continue;
            }

            snap.Transform.position = snap.Position;
        }
    }

    private List<Vector3> ComputeTargets(LayoutPattern pattern, FieldContext ctx, int enemyCount)
    {
        var list = new List<Vector3>(enemyCount);
        switch (pattern)
        {
            case LayoutPattern.DefensiveLine:
                FillDefensiveLine(list, ctx, enemyCount);
                break;
            case LayoutPattern.BlockPassLane:
                FillBlockPassLane(list, ctx, enemyCount);
                break;
            case LayoutPattern.PressRightWing:
                FillWingPress(list, ctx, enemyCount, ctx.Right);
                break;
            case LayoutPattern.PressLeftWing:
                FillWingPress(list, ctx, enemyCount, -ctx.Right);
                break;
            case LayoutPattern.PressBallOwner:
                FillPressOwner(list, ctx, enemyCount);
                break;
            case LayoutPattern.RandomSeeded:
                FillRandom(list, ctx, enemyCount);
                break;
            default:
                FillDefensiveLine(list, ctx, enemyCount);
                break;
        }

        return list;
    }

    private void FillDefensiveLine(List<Vector3> list, FieldContext ctx, int count)
    {
        Vector3 lineCenter = Vector3.Lerp(ctx.EnemyGoal, ctx.OwnGoal, _defensiveLineDepthRatio);
        float halfWidth = ctx.FieldWidth * _lateralSpreadRatio;
        for (int i = 0; i < count; i++)
        {
            float t = count <= 1 ? 0f : (i / (float)(count - 1)) * 2f - 1f;
            list.Add(lineCenter + ctx.Right * (halfWidth * t));
        }
    }

    private void FillBlockPassLane(List<Vector3> list, FieldContext ctx, int count)
    {
        Vector3 lanePoint = Vector3.Lerp(ctx.OwnerPos, ctx.SupportCenter, Mathf.Clamp01(_passLaneT));
        if (count >= 1)
        {
            list.Add(lanePoint);
        }

        for (int i = list.Count; i < count; i++)
        {
            float t = count <= 1 ? 0f : ((i - 1) / (float)Mathf.Max(1, count - 2)) * 2f - 1f;
            list.Add(lanePoint + ctx.Right * (ctx.FieldWidth * 0.08f * t) + ctx.ToGoal * (ctx.FieldLength * 0.03f * i));
        }
    }

    private void FillWingPress(List<Vector3> list, FieldContext ctx, int count, Vector3 wingDir)
    {
        Vector3 basePos = ctx.OwnerPos + ctx.ToGoal * (ctx.FieldLength * 0.12f) + wingDir * (ctx.FieldWidth * _wingLateralRatio);
        for (int i = 0; i < count; i++)
        {
            list.Add(basePos + ctx.ToGoal * (i * ctx.FieldLength * 0.04f));
        }
    }

    private void FillPressOwner(List<Vector3> list, FieldContext ctx, int count)
    {
        float radius = ctx.FieldLength * _pressOwnerRadiusRatio;
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / Mathf.Max(1, count)) * i * Mathf.Deg2Rad;
            Vector3 offset = ctx.Right * Mathf.Cos(angle) * radius + ctx.ToGoal * Mathf.Sin(angle) * radius * 0.5f;
            list.Add(ctx.OwnerPos + offset);
        }
    }

    private void FillRandom(List<Vector3> list, FieldContext ctx, int count)
    {
        var rng = new System.Random(_randomSeed + (int)LayoutPattern.RandomSeeded * 31 + count);
        float minZ = Mathf.Min(ctx.OwnerPos.z, ctx.EnemyGoal.z);
        float maxZ = Mathf.Max(ctx.OwnerPos.z, ctx.EnemyGoal.z);
        float minX = ctx.FieldCenter.x - ctx.FieldWidth * 0.4f;
        float maxX = ctx.FieldCenter.x + ctx.FieldWidth * 0.4f;

        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(minX, maxX, (float)rng.NextDouble());
            float z = Mathf.Lerp(minZ, maxZ, (float)rng.NextDouble());
            list.Add(new Vector3(x, ctx.OwnerPos.y, z));
        }
    }

    private List<AnimalFacade> GetFieldEnemies()
    {
        var result = new List<AnimalFacade>();
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return result;
        }

        foreach (var enemy in regist.Enemies)
        {
            if (enemy == null || enemy.IsGK())
            {
                continue;
            }

            result.Add(enemy);
        }

        return result;
    }

    private bool TryGetFieldContext(out FieldContext ctx)
    {
        ctx = default;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return false;
        }

        var field = teamBB.FieldInfo;
        var ball = teamBB.BallInfo;
        Vector3 ownerPos = ball.BallOwnerPosition;
        if (ownerPos.sqrMagnitude < 0.01f)
        {
            ownerPos = ball.BallPosition;
        }

        if (ownerPos.sqrMagnitude < 0.01f)
        {
            ownerPos = field.FieldCenter;
        }

        Vector3 toGoal = field.EnemyGoalPosition - ownerPos;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }

        toGoal.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        Vector3 supportCenter = ComputeAllySupportCenter(teamBB, ownerPos, toGoal, right);

        ctx = new FieldContext
        {
            OwnerPos = ownerPos,
            SupportCenter = supportCenter,
            EnemyGoal = field.EnemyGoalPosition,
            OwnGoal = field.OwnGoalPosition,
            FieldCenter = field.FieldCenter,
            FieldLength = field.FieldLength,
            FieldWidth = field.FieldWidth,
            ToGoal = toGoal,
            Right = right,
            EnemyPositions = teamBB.BasicInfo.EnemyPositions,
        };
        return true;
    }

    private static Vector3 ComputeAllySupportCenter(TeamBlackboard teamBB, Vector3 ownerPos, Vector3 toGoal, Vector3 right)
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return ownerPos + toGoal * (teamBB.FieldInfo.FieldLength * 0.18f);
        }

        var points = new List<Vector3>();
        foreach (var ally in regist.Allys)
        {
            if (ally == null || ally.IsGK())
            {
                continue;
            }

            var assignment = ally.GetComponent<AnimalControlAssignment>();
            if (assignment != null && assignment.IsHumanControlled)
            {
                continue;
            }

            if (ally.GetComponent<GoapAgent>() == null)
            {
                continue;
            }

            Vector3 p = ally.transform.position;
            if (Vector3.Distance(p, ownerPos) < 0.5f)
            {
                continue;
            }

            points.Add(p);
        }

        if (points.Count == 0)
        {
            return ownerPos + toGoal * (teamBB.FieldInfo.FieldLength * 0.18f);
        }

        Vector3 sum = Vector3.zero;
        foreach (var p in points)
        {
            sum += p;
        }

        return sum / points.Count;
    }

    private struct FieldContext
    {
        public Vector3 OwnerPos;
        public Vector3 SupportCenter;
        public Vector3 EnemyGoal;
        public Vector3 OwnGoal;
        public Vector3 FieldCenter;
        public float FieldLength;
        public float FieldWidth;
        public Vector3 ToGoal;
        public Vector3 Right;
        public List<Vector3> EnemyPositions;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !TryGetFieldContext(out FieldContext ctx))
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(ctx.OwnerPos, 0.35f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(ctx.SupportCenter, 0.3f);
        Gizmos.color = Color.red;
        foreach (var enemy in GetFieldEnemies())
        {
            if (enemy != null)
            {
                Gizmos.DrawWireSphere(enemy.transform.position, 0.5f);
            }
        }
    }

    private void LogLine(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{SummaryTag}] {message}";
        Debug.Log(line);
        AppendSummaryFile(line);
    }

    private static void LogLineStatic(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{SummaryTag}] {message}";
        Debug.Log(line);
        AppendSummaryFile(line);
    }

    private static void AppendSummaryFile(string line)
    {
        try
        {
            if (!_summaryLogInitialized)
            {
                string dir = Path.Combine(Application.dataPath, "DebugLog");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                _summaryLogFilePath = Path.Combine(dir, "GoapSummary_latest.txt");
                _summaryLogInitialized = true;
            }

            File.AppendAllText(_summaryLogFilePath, line + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{SummaryTag}] file write failed: {e.Message}");
        }
    }

    private static string Fmt(Vector3 v) => $"({v.x:F1},{v.y:F1},{v.z:F1})";

    private static string FmtList(List<Vector3> list)
    {
        if (list == null || list.Count == 0)
        {
            return "[]";
        }

        var parts = new string[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            parts[i] = Fmt(list[i]);
        }

        return "[" + string.Join("|", parts) + "]";
    }
}
