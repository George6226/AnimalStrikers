using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

/// <summary>
/// 翼追従サポートアクション。
/// 保持者の攻撃軸（Z）に追従し、やや前方のサイドレーンでパスオプションを提供する。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Attack/CreateSupportAngle")]
public class CreateSupportAngleActionSO : GoapActionSO
{
    [Header("翼追従（フィールド比）")]
    [Tooltip("保持者より攻撃方向へ先行する距離（フィールド長に対する割合）。Z追従の前リード。")]
    [SerializeField] private float _forwardLeadRatio = 0.08f;

    [Tooltip("サイドレーンの横位置（フィールド幅に対する割合）。RW/LW の翼幅。")]
    [SerializeField] private float _wingLaneRatio = 0.30f;

    [Header("距離設定（フィールド比）")]
    [Tooltip("理想とする支持距離（フィールド長に対する割合）。保持者との直線距離の目安。")]
    [SerializeField] private float _optimalDistanceRatio = 0.2f;

    [Tooltip("支持位置として許容する最短距離（フィールド長に対する割合）。これより内側は近すぎと判断。")]
    [SerializeField] private float _minDistanceRatio = 0.1f;

    [Tooltip("支持位置として許容する最長距離（フィールド長に対する割合）。これより外側は離れすぎと判断。")]
    [SerializeField] private float _maxDistanceRatio = 0.4f;

    [Header("動作パラメータ")]
    [Tooltip("保持者-ゴール方向との角度差をどこまで許容するか（度）。三角形の開き具合を調整。")]
    [SerializeField] private float _angleTolerance = 45f;

    [Tooltip("支持角度が決まった後に移動する際の目標速度。数値が大きいほど素早く移動。")]
    [SerializeField] private float _movementSpeed = 5f;

    [Tooltip("支持角度形成を完了するまでに許容する最大時間（秒）。")]
    [SerializeField] private float _executionTime = 10f;

    [Tooltip("保持者移動に合わせて目標位置を更新する間隔（秒）。")]
    [SerializeField] private float _retargetInterval = 0.25f;
    
    // プロパティをpublicにしてランタイムクラスからアクセス可能にする
    public float ForwardLeadRatio => _forwardLeadRatio;
    public float WingLaneRatio => _wingLaneRatio;
    public float OptimalDistanceRatio => _optimalDistanceRatio;
    public float MinDistanceRatio => _minDistanceRatio;
    public float MaxDistanceRatio => _maxDistanceRatio;
    public float AngleTolerance => _angleTolerance;
    public float MovementSpeed => _movementSpeed;
    public float ExecutionTime => _executionTime;
    public float RetargetInterval => _retargetInterval;
    
    public override string Description =>
        "保持者の攻撃軸に追従し、やや前方の翼レーンへ移動してパスオプションを確保する。";
    
    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new CreateSupportAngleActionRuntime(this, debugName);
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "CreateSupportAngle";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost <= 0.01f)
        {
            _baseCost = 1.05f;
        }

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

        // // 許容距離帯内かどうか
        // if (ballDistance >= minDistance && ballDistance <= maxDistance)
        // {
        //     // 最適距離に近いほどコスト減少（中央を最適値、両端で減少がゼロ）
        //     float normalized = Mathf.Abs(ballDistance - optimalDistance) / ((maxDistance - minDistance) / 2f);
        //     normalized = Mathf.Clamp01(normalized);
        //     float value = (1f - normalized) * 1f;
        //     adjustment -= value; // 距離が最適なら-1、端なら0
        //     DebugLogger.Log($"[CreateSupportAngle] 距離コスト調整: 距離={ballDistance:F2}, 最適={optimalDistance:F2}, min={minDistance:F2}, max={maxDistance:F2}, 減点={value:F2} (調整値={adjustment:F2})");
        // }
        // else
        // {
        //     // 許容範囲外はコストを増加
        //     adjustment += 1f;
        //     DebugLogger.Log($"[CreateSupportAngle] 距離コスト増加: 許容外 (距離={ballDistance:F2}, min={minDistance:F2}, max={maxDistance:F2}) +1 (調整値={adjustment:F2})");
        // }
        
        if (bb.GetFact(new Fact(SymbolTag.Position.NEAR_ENEMY_NO_BALL, "true")) == true)
        {
            adjustment += 0.30f;
        }

        int pressureCount = teamBB.BallInfo.IsBallOwnerUnderPressure;
        if (pressureCount <= 0)
        {
            adjustment -= 0.15f;
        }

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        int teammatesNearOwner = 0;
        float nearRadius = teamBB.FieldInfo.FieldLength * 0.12f;
        foreach (var allyPos in teamBB.BasicInfo.TeammatePositions)
        {
            if (Vector3.Distance(allyPos, bb.PhysicalState.Position) < 0.1f)
            {
                continue;
            }

            if (Vector3.Distance(allyPos, ownerPos) <= nearRadius)
            {
                teammatesNearOwner++;
            }
        }

        if (teammatesNearOwner >= 1)
        {
            adjustment -= 0.25f;
        }

        return adjustment;
    }
}
