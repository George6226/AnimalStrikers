using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

/// <summary>
/// ボール保持者がプレッシャーを受けているとき、
/// ボールを守るための安全なパスサポート位置へ「移動のみ」を行うアクションのSO。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Attack/ProtectPassSupportMove", fileName = "ProtectPassSupportMoveActionSO")]
public class ProtectPassSupportMoveActionSO : GoapActionSO
{
    [Header("実行パラメータ")]
    [Tooltip("安全なサポート位置に移動するまでの目標時間（秒）。この時間を超えるとアクションを中断する想定。")]
    [SerializeField] private float _executionTime = 2.5f;

    [Tooltip("サポート位置へ向かう際の目標移動速度。AI が接近するスピード感を調整します。")]
    [SerializeField] private float _moveSpeed = 3.5f;

    [Header("位置調整（フィールド比）")]
    [Tooltip("ボール保持者より前方にどれだけ離れるか（フィールド長に対する割合）。パスラインを作る距離。")]
    [SerializeField, Range(0.05f, 0.5f)] private float _supportDistanceRatio = 0.18f;

    [Tooltip("左右どちらかにどれだけ外れるか（フィールド幅に対する割合）。密集を避ける横方向の逃げ幅。")]
    [SerializeField, Range(0.0f, 0.4f)] private float _lateralAdjustRatio = 0.10f;

    [Header("発動条件")]
    [Tooltip("ボール保持者が感じているプレッシャー（0-1）がこの値以上のときのみ発動を許可。")]
    [SerializeField, Range(0f,1f)] private float _pressureThreshold = 0.5f;

    // ランタイムから参照されるプロパティ
    public float ExecutionTime => _executionTime;
    public float MoveSpeed => _moveSpeed;
    public float SupportDistanceRatio => _supportDistanceRatio;
    public float LateralAdjustRatio => _lateralAdjustRatio;
    public float PressureThreshold => _pressureThreshold;

    public override string Description =>
        "プレッシャーを受けている味方ボール保持者をサポートするため、安全な角度と距離へ素早く移動しパスコースを確保する。";
    
    protected override void OnEnable()
    {
        base.OnEnable();
        // if (Mathf.Approximately(_baseCost, 1f))
        // {
        //     _baseCost = 2.0f;
        // }
        _actionName = "ProtectPassSupportMove";
        SetupPreconditionsAndEffects();
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new ProtectPassSupportMoveActionRuntime(this, debugName);
    }

    /// <summary>
    /// 前提条件と効果
    /// </summary>
    private void SetupPreconditionsAndEffects()
    {
        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, true),  // チームがボール保持
            new GoapCondition(SymbolTag.Basic.HAS_BALL, false),         // 自分は未保持
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),         // 移動可能
            new GoapCondition(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, false) // まだ位置にいない
        });

        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            // パスを受けやすい位置に付くことをゴール側が利用できるようにフラグ
            new GoapCondition(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, true)
        });
    }

    /// <summary>
    /// 状況依存のコスト調整
    /// </summary>
    protected override float CalculateSituationalAdjustment(PlayerBlackboard bb)
    {
        float adj = 0f;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return adj;
        DebugLogger.Log($"[ProtectPassSupportMove] baseCost={_baseCost}");
        // TeamBlackboardの情報を利用してボール保持者がプレッシャーを受けていればコスト減（実行しやすく）
        int pressureCount = teamBB.BallInfo.IsBallOwnerUnderPressure;
        if (pressureCount >= 2)
        {
            adj -= 6.0f;
            DebugLogger.Log($"[ProtectPassSupportMove] ボール保持者プレッシャーでコスト減少 -6.0 (人数={pressureCount}) (adjustment={adj:F2})");
        }

        // // ボールからの距離によるコスト調整（最適距離帯のみコスト減少、近すぎ・遠すぎはコスト増加）
        // float ballDistance = bb.BallState.BallDistance;
        // var teamBB2 = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        // if (teamBB2 == null) return adj;
        // float fieldLength = teamBB2.FieldInfo.FieldLength;
        // float minDistance = fieldLength * _supportDistanceRatio;
        // float maxDistance = fieldLength * (_supportDistanceRatio + _lateralAdjustRatio);
        // float optimalDistance = (minDistance + maxDistance) / 2f;

        // // 許容距離帯内かどうか
        // if (ballDistance >= minDistance && ballDistance <= maxDistance)
        // {
        //     // 最適距離に近いほどコスト減少（中央を最適値、両端で減少がゼロ）
        //     float normalized = Mathf.Abs(ballDistance - optimalDistance) / ((maxDistance - minDistance) / 2f);
        //     normalized = Mathf.Clamp01(normalized);
        //     float value = (1f - normalized) * 1f;
        //     adj -= value; // 距離が最適なら-1、端なら0
        //     DebugLogger.Log($"[ProtectPassSupportMove] 距離コスト調整: 距離={ballDistance:F2}, 最適={optimalDistance:F2}, min={minDistance:F2}, max={maxDistance:F2}, 減点={value:F2} (調整値={adj:F2})");
        // }
        // else
        // {
        //     // 許容範囲外はコストを増加
        //     adj += 1f;
        //     DebugLogger.Log($"[ProtectPassSupportMove] 距離コスト増加: 許容外 (距離={ballDistance:F2}, min={minDistance:F2}, max={maxDistance:F2}) +1 (調整値={adj:F2})");
        // }

        // // 敵が近くにいる場合のコスト増加
        // if (bb.GetFact(new Fact(SymbolTag.Position.NEAR_ENEMY_NO_BALL, "true")) == true)
        // {
        //     adj += 1f; // 敵が近くにいると移動が困難
        //     DebugLogger.Log($"[ProtectPassSupportMove] 敵接近によるコスト増加 +1 (調整値={adj:F2}) [{bb.BasicData.Self.name}]");
        // }

        DebugLogger.Log($"[ProtectPassSupportMove] total adjustment={adj:F2}");
        return adj;
    }

    private float CalculateBallOwnerPressureLevel()
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        List<Vector3> enemies = teamBB.BasicInfo.EnemyPositions;
        if (enemies.Count == 0) return 0f;
        float minDist = float.MaxValue;
        foreach (var e in enemies)
            minDist = Mathf.Min(minDist, Vector3.Distance(ownerPos, e));
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float threshold = fieldLength * 0.10f;
        if (minDist <= threshold) return 1f - (minDist / threshold);
        return 0f;
    }
}


