using UnityEngine;
using Game.Goap;

/// <summary>
/// 守備トランジション時に、指定のリトリートラインへ最短復帰する移動アクション。
/// </summary>
[CreateAssetMenu(menuName = "GOAP/Action/Defense/RetreatToDefensiveLine", fileName = "RetreatToDefensiveLineActionSO")]
public class RetreatToDefensiveLineActionSO : GoapActionSO
{
    [Header("基本設定")]
    [SerializeField] private float _executionTime = 2.0f;
    [SerializeField] private float _moveSpeed = 4.0f;

    [Header("リトリートライン設定（フィールド長比）")]
    [SerializeField, Range(0.10f, 0.45f)] private float _retreatDepthRatio = 0.28f; // 自陣ゴールからの深さ
    [SerializeField, Range(0.0f, 1.0f)] private float _centralBias = 0.6f;           // 中央へ寄せる重み（0=ボール側優先,1=純中央）

    public float ExecutionTime => _executionTime;
    public float MoveSpeed => _moveSpeed;
    public float RetreatDepthRatio => _retreatDepthRatio;
    public float CentralBias => _centralBias;

    protected override void OnEnable()
    {
        base.OnEnable();
        _actionName = "RetreatToDefensiveLine";
        if (Mathf.Approximately(_baseCost, 1f) || _baseCost >= 1.4f)
        {
            _baseCost = 1.0f;
        }

        _preconditions.Clear();
        _preconditions.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, false),
            new GoapCondition(SymbolTag.Basic.HAS_BALL, false),
            new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
        });

        _effects.Clear();
        _effects.AddRange(new GoapCondition[]
        {
            new GoapCondition(SymbolTag.Basic.IS_MOVING, true),
            new GoapCondition(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, true),
        });
    }

    public override float CalculateDynamicCost(PlayerBlackboard bb)
    {
        return TeammateNpcDefensePlanning.ComputeDynamicCost(
            this, bb, _baseCost, CalculateSituationalAdjustment(bb));
    }

    public override GoapActionRuntime CreateRuntime(string debugName)
    {
        return new RetreatToDefensiveLineActionRuntime(this, debugName);
    }

    protected override float CalculateSituationalAdjustment(PlayerBlackboard bb)
    {
        // 目標ラインから離れているほどコスト減（=選ばれやすい）
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return 0f;

        float fieldLen = teamBB.FieldInfo.FieldLength;
        float depth = fieldLen * _retreatDepthRatio;
        Vector3 ownGoal = teamBB.FieldInfo.OwnGoalPosition;
        Vector3 center = teamBB.FieldInfo.FieldCenter;
        Vector3 ball = teamBB.BallInfo.BallPosition;

        Vector3 linePoint = Vector3.Lerp(center, new Vector3(center.x, center.y, ownGoal.z + depth * Mathf.Sign(center.z - ownGoal.z)), _centralBias);
        // ボール側に少し寄せる
        Vector3 toBallLateral = Vector3.ProjectOnPlane(ball - linePoint, Vector3.up);
        linePoint += Vector3.ClampMagnitude(toBallLateral, teamBB.FieldInfo.FieldWidth * 0.15f) * (1f - _centralBias);

        float d = Vector3.Distance(bb.PhysicalState.Position, linePoint);
        return -Mathf.Clamp(d / Mathf.Max(fieldLen * 0.5f, 0.01f), 0f, 1f) * 2.0f;
    }
}


