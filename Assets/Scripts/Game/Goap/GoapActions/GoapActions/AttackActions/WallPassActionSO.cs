using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Goap;

/// <summary>
/// 壁パスを実行するアクション
/// 4vs4の狭いフィールドで、ボール保持者から短いパスを受け取り、
/// 素早く返すことで敵の守備を突破する
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Attack/WallPass")]
public class WallPassActionSO : GoapActionSO
{
    [Header("距離設定")]
    [Tooltip("壁パスの基準距離（フィールド長に対する割合）。この距離だけ離れてパスを受けに動く。4vs4専用で短距離推奨。")]
    [SerializeField] private float _wallPassDistanceRatio = 0.1f; // 壁パス距離比率（フィールド長に対する）

    [Tooltip("壁パスの最大距離（フィールド長に対する割合）。ポジション選択時の制限に使う。")]
    [SerializeField] private float _maxWallPassDistanceRatio = 0.15f; // 最大壁パス距離比率（フィールド長に対する）

    [Header("動作パラメータ")]
    [Tooltip("壁パスを返す際の移動・反応速度。高いほど素早く返す。")]
    [SerializeField] private float _returnSpeed = 8f;         // 返し速度

    [Tooltip("壁パス1往復に想定する最大実行時間（秒）。これを超えても位置に着かないと失敗扱い。")]
    [SerializeField] private float _executionTime = 2f;         // 実行時間

    [Header("戦術制約")]
    [Tooltip("ボール保持者と敵ゴール方向の角度許容範囲（度単位）。サポート位置の前方/後方制御。")]
    [SerializeField] private float _angleTolerance = 30f;     // 角度許容範囲（度）

    [Tooltip("味方ボール保持者がこれ以上のプレッシャー（0-1）を受けた場合のみ壁パスを解禁。")]
    [SerializeField] private float _pressureThreshold = 0.6f; // プレッシャー閾値

    [Header("成功率")]
    [Tooltip("AIが壁パスを成功する確率。シミュレーションや失敗表現の挙動制御に利用。")]
    [SerializeField] private float _wallPassSuccessRate = 0.8f; // 壁パス成功率
    
    // プロパティをpublicにしてランタイムクラスからアクセス可能にする
    public float WallPassDistanceRatio => _wallPassDistanceRatio;
    public float MaxWallPassDistanceRatio => _maxWallPassDistanceRatio;
    public float ReturnSpeed => _returnSpeed;
    public float ExecutionTime => _executionTime;
    public float AngleTolerance => _angleTolerance;
    public float PressureThreshold => _pressureThreshold;
    public float WallPassSuccessRate => _wallPassSuccessRate;
    
    public override string Description =>
        "ボール保持者に近づいて壁パスを受け渡すことでプレッシャーをいなし、短い距離でリズム良く攻撃を前進させる。";
    
    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new WallPassActionRuntime(this, debugName);
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        // if (Mathf.Approximately(_baseCost, 1f))
        // {
        //     _baseCost = 2f;
        // }
        _actionName = "WallPass";
        
        // プログラム上でPreconditionとEffectを設定
        SetupPreconditionsAndEffects();
    }
    
    /// <summary>
    /// 前提条件と効果を設定
    /// </summary>
    private void SetupPreconditionsAndEffects()
    {
        // 前提条件の設定
        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, true),           // チームがボールを保持
            new GoapCondition(SymbolTag.Basic.HAS_BALL, false),                 // 自分はボールを持っていない
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),                 // 移動可能
            new GoapCondition(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, false) // まだパス受信位置にいない
        });
        
        // 効果の設定
        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, true) // パス受信位置にいる
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
        if (teamBB == null) return adjustment;
        DebugLogger.Log($"[WallPass] baseCost={_baseCost}");
        
        // // ボールからの距離による調整
        // float ballDistance = bb.BallState.BallDistance;
        // var teamBB2 = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        // if (teamBB2 == null) return adjustment;
        // float fieldLength = teamBB2.FieldInfo.FieldLength;
        // float wallPassDistance = fieldLength * _wallPassDistanceRatio;
        // float maxWallPassDistance = fieldLength * _maxWallPassDistanceRatio;
        
        // if (ballDistance >= wallPassDistance && ballDistance <= maxWallPassDistance)
        // {
        //     // 最適距離に近いほどコスト減少（中央を最適値、両端で減少がゼロ）
        //     float optimalDistance = (wallPassDistance + maxWallPassDistance) / 2f;
        //     float normalized = Mathf.Abs(ballDistance - optimalDistance) / ((maxWallPassDistance - wallPassDistance) / 2f);
        //     normalized = Mathf.Clamp01(normalized);
        //     float value = (1f - normalized) * 2f;
        //     adjustment -= value;
        //     DebugLogger.Log($"[WallPass] 距離コスト調整: 距離={ballDistance:F2}, 最適={optimalDistance:F2}, min={wallPassDistance:F2}, max={maxWallPassDistance:F2}, 減点={value:F2} (調整値={adjustment:F2})");
        // }
        // else if (ballDistance < wallPassDistance)
        // {
        //     adjustment += 1.5f; // ボールに近すぎる場合はコスト増加
        //     DebugLogger.Log($"[WallPass] 距離コスト増加: 近すぎ (距離={ballDistance:F2}, min={wallPassDistance:F2}) +1.5 (調整値={adjustment:F2})");
        // }
        // else if (ballDistance > maxWallPassDistance)
        // {
        //     adjustment += 2f; // ボールから遠すぎる場合はコスト増加
        //     DebugLogger.Log($"[WallPass] 距離コスト増加: 遠すぎ (距離={ballDistance:F2}, max={maxWallPassDistance:F2}) +2 (調整値={adjustment:F2})");
        // }
        
        // ボール保持者のプレッシャーによる調整（TeamBlackboardを使用）
        int pressureCount = teamBB.BallInfo.IsBallOwnerUnderPressure;
        if (pressureCount >= 1 && pressureCount < 2)
        {
            adjustment -= 6.0f; // プレッシャーが高い場合は壁パスが有効
            DebugLogger.Log($"[WallPass] ボール保持者プレッシャー中でコスト減少 -6.0 (人数={pressureCount}) (adjustment={adjustment:F2})");
        }
        
        // // 敵の分布による調整
        // float enemyDensity = CalculateEnemyDensity(bb);
        // if (enemyDensity > 0.7f)
        // {
        //     adjustment -= 1f; // 敵が多い場合は壁パスが有効
        //     DebugLogger.Log($"[WallPass] 敵密度高でコスト減少 -1 (密度={enemyDensity:F2}) (調整値={adjustment:F2})");
        // }
        // else if (enemyDensity < 0.3f)
        // {
        //     adjustment += 0.5f; // 敵が少ない場合は壁パスが不要
        //     DebugLogger.Log($"[WallPass] 敵密度低でコスト増加 +0.5 (密度={enemyDensity:F2}) (調整値={adjustment:F2})");
        // }

        DebugLogger.Log($"[WallPass] total adjustment={adjustment:F2}");
        return adjustment;
    }
    
    /// <summary>
    /// 敵密度を計算
    /// </summary>
    /// <param name="bb">プレイヤーブラックボード</param>
    /// <returns>敵密度（0-1の範囲）</returns>
    private float CalculateEnemyDensity(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        Vector3 ballOwnerPosition = teamBB.BallInfo.BallOwnerPosition;
        List<Vector3> enemyPositions = teamBB.BasicInfo.EnemyPositions;
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float checkRadius = fieldLength * 0.2f; // フィールド長の20%
        
        int enemyCount = 0;
        foreach (Vector3 enemyPos in enemyPositions)
        {
            float distanceToBallOwner = Vector3.Distance(enemyPos, ballOwnerPosition);
            if (distanceToBallOwner <= checkRadius)
            {
                enemyCount++;
            }
        }
        
        return enemyCount / 4f; // 最大4人の敵
    }
}
