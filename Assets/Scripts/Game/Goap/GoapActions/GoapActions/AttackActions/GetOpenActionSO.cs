using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

/// <summary>
/// フリーになるアクション
/// 4vs4の狭いフィールドで、ボール保持者から適切な距離と角度で
/// フリーになり、パスオプションを提供する
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Attack/GetOpen")]
public class GetOpenActionSO : GoapActionSO
{
    [Header("距離設定（フィールド比）")]
    [Tooltip("フリーになる際の理想的な離脱距離（フィールド長に対する割合）。")]
    [SerializeField] private float _optimalDistanceRatio = 0.2f;

    [Tooltip("この距離より近いとフリーとは見なさない最小距離（フィールド長に対する割合）。")]
    [SerializeField] private float _minDistanceRatio = 0.1f;

    [Tooltip("この距離より遠いとサポートから外れたとみなす最大距離（フィールド長に対する割合）。")]
    [SerializeField] private float _maxDistanceRatio = 0.4f;

    [Header("動作パラメータ")]
    [Tooltip("保持者-ゴール方向との角度差の許容範囲（度）。ポジショニングの自由度を調整。")]
    [SerializeField] private float _angleTolerance = 45f;

    [Tooltip("フリーになるために移動する際の目標速度。")]
    [SerializeField] private float _movementSpeed = 5f;

    [Tooltip("フリーになる動作全体に許容する最大時間（秒）。")]
    [SerializeField] private float _executionTime = 10f;
    
    // プロパティをpublicにしてランタイムクラスからアクセス可能にする
    public float OptimalDistanceRatio => _optimalDistanceRatio;
    public float MinDistanceRatio => _minDistanceRatio;
    public float MaxDistanceRatio => _maxDistanceRatio;
    public float AngleTolerance => _angleTolerance;
    public float MovementSpeed => _movementSpeed;
    public float ExecutionTime => _executionTime;
    
    public override string Description =>
        "ボール保持者から適切な距離と角度でフリーになり、パスを受けられるスペースを素早く確保する。";
    
    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new GetOpenActionRuntime(this, debugName);
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "GetOpen";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost <= 0.01f)
        {
            _baseCost = 1.05f;
        }

        RefreshPlanningFacts();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _actionName = "GetOpen";
        RefreshPlanningFacts();
    }
#endif

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
            new GoapCondition(SymbolTag.Action.IS_MAINTAINING_SUPPORT_RELATIONSHIP, true),
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

        // // ボールからの距離によるコスト調整（最適距離帯のみコスト減少、近すぎ・遠すぎはコスト増加）
        // float ballDistance = bb.BallState.BallDistance;
        // float fieldLength = TeamFacade.Instance.TeamBlackboard.FieldInfo.FieldLength;
        // float minDistance = fieldLength * _minDistanceRatio;
        // float maxDistance = fieldLength * _maxDistanceRatio;
        // float optimalDistance = fieldLength * _optimalDistanceRatio;

        // if (ballDistance >= minDistance && ballDistance <= maxDistance)
        // {
        //     float normalized = Mathf.Abs(ballDistance - optimalDistance) / ((maxDistance - minDistance) / 2f);
        //     normalized = Mathf.Clamp01(normalized);
        //     float value = (1f - normalized) * 1f;
        //     adjustment -= value; // 距離が最適なら-1、端なら0
        //     DebugLogger.Log($"[GetOpen] 距離コスト調整: 距離={ballDistance:F2}, 最適={optimalDistance:F2}, min={minDistance:F2}, max={maxDistance:F2}, 減点={value:F2} (調整値={adjustment:F2})");
        // }
        // else
        // {
        //     adjustment += 1f;
        //     DebugLogger.Log($"[GetOpen] 距離コスト増加: 許容外 (距離={ballDistance:F2}, min={minDistance:F2}, max={maxDistance:F2}) +1 (調整値={adjustment:F2})");
        // }

        // 敵が近くにいる場合のコスト減少（フリーになるため積極的に空く行動を評価）
        if (bb.GetFact(new Fact(SymbolTag.Position.NEAR_ENEMY_NO_BALL, "true")) == true)
        {
            adjustment -= 0.45f;
        }

        if (bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) != true)
        {
            adjustment -= 0.20f;
        }

        float laneWidth = teamBB.FieldInfo.FieldLength * 0.06f;
        if (!PlayerBlackboardCalculator.IsPassRouteClear(
                bb.PhysicalState.Position,
                teamBB.BallInfo.BallOwnerPosition,
                teamBB.BasicInfo.EnemyPositions,
                laneWidth))
        {
            adjustment -= 0.40f;
        }

        int pressureCount = teamBB.BallInfo.IsBallOwnerUnderPressure;
        if (pressureCount <= 0)
        {
            adjustment -= 0.15f;
        }
        else if (pressureCount >= 2)
        {
            adjustment += 0.2f;
        }

        return adjustment;
    }
}
