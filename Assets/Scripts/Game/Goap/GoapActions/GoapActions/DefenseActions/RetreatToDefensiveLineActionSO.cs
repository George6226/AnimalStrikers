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

        if (!TeammateNpcDefensePlanning.TrySampleDefensiveRetreatLine(
                bb,
                _retreatDepthRatio,
                _centralBias,
                out TeammateNpcDefensePlanning.DefensiveRetreatLineSample line))
        {
            return 0f;
        }

        float fieldLen = line.FieldLength;
        if (line.GoalSide > fieldLen * 0.02f)
        {
            // ラインより自陣側にいるならリトリート不要
            return 1.65f;
        }

        if (line.GoalSide >= -fieldLen * 0.02f)
        {
            return 0.45f;
        }

        float retreatUrgency = TeammateNpcDefensePlanning.ComputeSevereRetreatOverextensionUrgency(
            bb,
            _retreatDepthRatio,
            _centralBias);
        if (retreatUrgency >= 0.40f)
        {
            return -Mathf.Lerp(1.55f, 2.45f, retreatUrgency);
        }

        Vector3 ballOwner = teamBB.BallInfo.BallOwnerPosition;
        float distToOwner = Vector3.Distance(bb.PhysicalState.Position, ballOwner);
        if (distToOwner <= fieldLen * 0.25f)
        {
            return 2.0f;
        }

        if (HasUnmarkedFreeEnemy(teamBB, bb.PhysicalState.Position))
        {
            return 2.5f;
        }

        float d = line.AheadOfLineDistance;
        return -Mathf.Clamp(d / Mathf.Max(fieldLen * 0.5f, 0.01f), 0f, 1f) * 2.0f;
    }

    private static bool HasUnmarkedFreeEnemy(TeamBlackboard teamBB, Vector3 playerPos)
    {
        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        float fieldLen = teamBB.FieldInfo.FieldLength;
        float markThreshold = fieldLen * 0.15f;

        foreach (Vector3 enemyPos in teamBB.BasicInfo.EnemyPositions)
        {
            if (Vector3.Distance(enemyPos, ownerPos) <= 0.1f)
            {
                continue;
            }

            bool isMarked = false;
            foreach (Vector3 allyPos in teamBB.BasicInfo.TeammatePositions)
            {
                if (Vector3.Distance(allyPos, playerPos) < 0.1f)
                {
                    continue;
                }

                if (Vector3.Distance(allyPos, enemyPos) <= markThreshold)
                {
                    isMarked = true;
                    break;
                }
            }

            if (!isMarked)
            {
                return true;
            }
        }

        return false;
    }
}


