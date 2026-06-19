using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

/// <summary>
/// 裏抜けランを実行するアクション
/// 4vs4の狭いフィールドで、敵の最終ラインを突破し、
/// スルーパスや決定機を狙うための裏抜けランを実行する
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Attack/MakeRunBehind")]
public class MakeRunBehindActionSO : GoapActionSO
{
    [Header("距離パラメータ（フィールド比）")]
    [Tooltip("裏抜け開始位置として許容する最短距離（フィールド長に対する割合）。")]
    [SerializeField] private float _minRunDistanceRatio = 0.2f;

    [Tooltip("裏抜け開始位置として許容する最長距離（フィールド長に対する割合）。")]
    [SerializeField] private float _maxRunDistanceRatio = 0.5f;

    [Tooltip("敵最終ラインとの至近距離をどこまで許容するか（フィールド長に対する割合）。ライン裏に潜むかの判断材料。")]
    [SerializeField] private float _enemyLineDistanceRatio = 0.4f;

    [Header("スピード & タイミング")]
    [Tooltip("裏抜け時の目標疾走速度。高いほど積極的にライン裏へ飛び出す。")]
    [SerializeField] private float _runSpeed = 6f;

    [Tooltip("裏抜け全体に許容する最大時間（秒）。この時間を超えると完遂扱い。")]
    [SerializeField] private float _executionTime = 5f;

    [Tooltip("パス発生を待つ際の余裕時間（秒）。この間にタイミングが来なければ強制的に走り出す。")]
    [SerializeField] private float _timingWindow = 2f;

    [Header("角度・評価指標")]
    [Tooltip("保持者-ゴール方向との角度差の許容値（度）。走り出す方向の自由度を調整。")]
    [SerializeField] private float _runAngleTolerance = 45f;

    [Tooltip("裏抜けの可否を判定する突破ポテンシャルの閾値（0-1）。これ以上で積極的に走る。")]
    [SerializeField] private float _breakthroughThreshold = 0.7f;
    
    // プロパティをpublicにしてランタイムクラスからアクセス可能にする
    public float MinRunDistanceRatio => _minRunDistanceRatio;
    public float MaxRunDistanceRatio => _maxRunDistanceRatio;
    public float RunAngleTolerance => _runAngleTolerance;
    public float RunSpeed => _runSpeed;
    public float ExecutionTime => _executionTime;
    public float TimingWindow => _timingWindow;
    public float EnemyLineDistanceRatio => _enemyLineDistanceRatio;
    public float BreakthroughThreshold => _breakthroughThreshold;
    
    public override string Description =>
        "敵最終ラインの裏へ抜ける走りを準備し、スルーパスや決定機を狙うためのスペースを作り出す。";
    
    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new MakeRunBehindActionRuntime(this, debugName);
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost >= 1.8f)
        {
            _baseCost = 1.15f;
        }
        _actionName = "MakeRunBehind";
        
        // プログラム上でPreconditionとEffectを設定
        RefreshPlanningFacts();
    }

    protected override void RefreshPlanningFacts()
    {
        SetupPreconditionsAndEffects();
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        return TeammateNpcSupportPlanning.ComputeDynamicCost(
            this, bb, _baseCost, CalculateSituationalAdjustment(bb));
    }
    
    /// <summary>
    /// 前提条件と効果を設定
    /// </summary>
    private void SetupPreconditionsAndEffects()
    {
        // IS_IN_PASS_RECEIVE=false は WM ラグで後方連鎖が失敗するため SO 前提に含めない（未達時のみ実行は Runtime で判定）
        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
        });
        
        // 効果の設定
        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Basic.IS_MOVING, true),
            new GoapCondition(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, true),
        });
    }
    
    /// <summary>
    /// 状況に応じたコスト調整値を計算
    /// </summary>
    /// <param name="bb">プレイヤーのブラックボード</param>
    /// <returns>調整値</returns>
    protected override float CalculateSituationalAdjustment(PlayerBlackboard bb)
    {
        float adjustment = 0f;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || !TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB))
        {
            return adjustment;
        }

        // ボール保持者のプレッシャーによる調整（TeamBlackboardを使用）
        int pressureCount = teamBB.BallInfo.IsBallOwnerUnderPressure;
        if (pressureCount <= 0)
        {
            adjustment -= 1f;
        }
        else if (pressureCount >= 2)
        {
            adjustment += 0.35f;
        }
        
        // // ボールからの距離による調整
        // float ballDistance = bb.BallState.BallDistance;
        float fieldLength = teamBB.FieldInfo.FieldLength;
        // float minDistance = fieldLength * _minRunDistanceRatio;
        // float maxDistance = fieldLength * _maxRunDistanceRatio;
        
        // if (ballDistance >= minDistance && ballDistance <= maxDistance)
        // {
        //     float optimalDistance = (minDistance + maxDistance) / 2f;
        //     float normalized = Mathf.Abs(ballDistance - optimalDistance) / ((maxDistance - minDistance) / 2f);
        //     normalized = Mathf.Clamp01(normalized);
        //     float value = (1f - normalized) * 2f;
        //     adjustment -= value;
        //     DebugLogger.Log($"[MakeRunBehind] 距離コスト調整: 距離={ballDistance:F2}, 最適={optimalDistance:F2}, min={minDistance:F2}, max={maxDistance:F2}, 減点={value:F2} (調整値={adjustment:F2})");
        // }
        // else if (ballDistance < minDistance)
        // {
        //     adjustment += 2f;
        //     DebugLogger.Log($"[MakeRunBehind] 距離コスト増加: 近すぎ (距離={ballDistance:F2}, min={minDistance:F2}) +2 (調整値={adjustment:F2})");
        // }
        // else if (ballDistance > maxDistance)
        // {
        //     adjustment += 1f;
        //     DebugLogger.Log($"[MakeRunBehind] 距離コスト増加: 遠すぎ (距離={ballDistance:F2}, max={maxDistance:F2}) +1 (調整値={adjustment:F2})");
        // }
        
        // 敵の最終ラインからの距離による調整
        float enemyLineDistance = CalculateDistanceToEnemyLine(bb);
        float enemyLineThreshold = fieldLength * _enemyLineDistanceRatio;
        if (enemyLineDistance <= enemyLineThreshold)
        {
            adjustment -= 0.4f;
        }

        float breakthroughPotential = CalculateBreakthroughPotential(bb);
        if (breakthroughPotential >= _breakthroughThreshold)
        {
            adjustment -= 0.45f;
        }
        else if (breakthroughPotential < 0.3f)
        {
            adjustment += 0.55f;
        }

        return adjustment;
    }
    
    /// <summary>
    /// 敵の最終ラインからの距離を計算
    /// </summary>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <returns>敵の最終ラインからの距離</returns>
    private float CalculateDistanceToEnemyLine(PlayerBlackboard bb)
    {
        Vector3 myPosition = bb.PhysicalState.Position;
        Vector3 enemyGoalPosition = TeamFacade.Instance.TeamBlackboard.FieldInfo.EnemyGoalPosition;
        
        return Vector3.Distance(myPosition, enemyGoalPosition);
    }
    
    /// <summary>
    /// 突破可能性を計算
    /// </summary>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <returns>突破可能性（0-1の範囲）</returns>
    private float CalculateBreakthroughPotential(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        Vector3 myPosition = bb.PhysicalState.Position;
        Vector3 ballOwnerPosition = teamBB.BallInfo.BallOwnerPosition;
        Vector3 enemyGoalPosition = teamBB.FieldInfo.EnemyGoalPosition;
        List<Vector3> enemyPositions = teamBB.BasicInfo.EnemyPositions;
        
        float score = 0f;
        
        // ボール保持者より敵ゴールに近い場合は有利
        float distanceToEnemyGoal = Vector3.Distance(myPosition, enemyGoalPosition);
        float ballOwnerDistanceToEnemyGoal = Vector3.Distance(ballOwnerPosition, enemyGoalPosition);
        
        if (distanceToEnemyGoal < ballOwnerDistanceToEnemyGoal)
        {
            score += 0.5f; // 敵ゴールに近い位置にいる
        }
        
        // 敵の守備選手との距離を考慮
        float minEnemyDistance = float.MaxValue;
        foreach (Vector3 enemyPos in enemyPositions)
        {
            float distanceToEnemy = Vector3.Distance(myPosition, enemyPos);
            minEnemyDistance = Mathf.Min(minEnemyDistance, distanceToEnemy);
        }
        
        if (minEnemyDistance > 5f)
        {
            score += 0.5f; // 敵から十分離れている
        }
        else if (minEnemyDistance < 2f)
        {
            score -= 0.3f; // 敵が近すぎる
        }
        
        return Mathf.Clamp(score, 0f, 1f);
    }
}
