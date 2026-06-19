using System.Collections.Generic;
using UnityEngine;

namespace Game.Goap.Goals
{
    /// <summary>
    /// 非保持（守備）時に、マーク/プレッシャー/パスコース遮断（必要に応じて）を
    /// 実現しやすい「適切な位置取り」を目指す汎用ゴール。
    /// </summary>
    [CreateAssetMenu(fileName = "DefensivePositioningGoalSO", menuName = "GOAP/Goals/Defense/DefensivePositioningGoalSO")]
    public class DefensivePositioningGoalSO : GoapGoalSO
    {
        [Header("Priority Settings")]
        [SerializeField] private float _basePriority = 45f;
        [SerializeField] private float _pressureBonus = 25f;     // 保持者への適正プレッシャー
        [SerializeField] private float _markingBonus = 22f;      // 危険な相手へのマーク近接
        [SerializeField] private float _passBlockBonus = 20f;    // パスコース遮断寄与
        [SerializeField] private float _tooClosePenalty = -35f;  // 接触レベルに近過ぎ
        [SerializeField] private float _tooFarPenalty = -25f;    // 離れ過ぎ

        [Header("Distance Settings (Field-size relative)")]
        [SerializeField] private float _optimalDistanceToOwnerRatio = 0.12f; // 保持者への最適距離
        [SerializeField] private float _minDistanceRatio = 0.06f;            // 近すぎ閾値（共通）
        [SerializeField] private float _maxDistanceRatio = 0.30f;            // 遠すぎ閾値（共通）

        protected override void OnEnable()
        {
            base.OnEnable();
            _goalName = "DefensivePositioning";

            // 守備側の基本前提
            SetRequiredFacts(
                new GoapCondition(SymbolTag.Tactical.TEAM_HAS_BALL, false), // チームは非保持
                new GoapCondition(SymbolTag.Basic.HAS_BALL, false),          // 自分は未保持
                new GoapCondition(SymbolTag.Action.CAN_MOVE, true),           // 移動可能
                new GoapCondition(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, true) // 守備位置にいる
            );
        }

        public override float EvaluatePriority(PlayerBlackboard bb, TeamBlackboard tb)
        {
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (!IsEnemyBallDefenseContext(teamBB))
            {
                return _basePriority;
            }

            if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
            {
                return _basePriority;
            }

            float priority = TeammateNpcDefensePlanning.ShouldUseTacticalDefenseGoal(bb)
                ? TeammateNpcDefensePlanning.DefensivePositioningEnemyBallPriority
                : _basePriority;

            Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
            float fieldLength = teamBB.FieldInfo.FieldLength;
            float minDist = fieldLength * _minDistanceRatio;
            float maxDist = fieldLength * _maxDistanceRatio;

            // 1) プレッシャー（保持者との最適距離＋横方向位置取り）
            float optimalOwner = fieldLength * _optimalDistanceToOwnerRatio;
            float distToOwner = Vector3.Distance(bb.PhysicalState.Position, ownerPos);
            float pressureDistanceScore = Mathf.Clamp01(1f - Mathf.Abs(distToOwner - optimalOwner) / Mathf.Max(optimalOwner, 0.01f));
            Vector3 enemyGoal = teamBB.FieldInfo.EnemyGoalPosition; // 基準ベクトルに使用
            Vector3 toGoal = (enemyGoal - ownerPos).normalized;
            Vector3 lateral = Vector3.Cross(toGoal, Vector3.up).normalized;
            float lateralAlign = Mathf.Abs(Vector3.Dot((bb.PhysicalState.Position - ownerPos).normalized, lateral));
            float pressureScore = 0.6f * pressureDistanceScore + 0.4f * lateralAlign;
            priority += _pressureBonus * pressureScore;

            // 2) マーク（最も危険な相手≒最も前方の敵に近接）
            Vector3 bestEnemy = ownerPos;
            float bestEnemyScore = -1f;
            foreach (var e in teamBB.BasicInfo.EnemyPositions)
            {
                // ゴール方向への前進度合いをスコア化
                float forwardness = Vector3.Dot((e - ownerPos).normalized, toGoal);
                if (forwardness > bestEnemyScore)
                {
                    bestEnemyScore = forwardness;
                    bestEnemy = e;
                }
            }
            float distToEnemy = Vector3.Distance(bb.PhysicalState.Position, bestEnemy);
            float idealMarkDist = fieldLength * 0.08f; // やや近め
            float markingScore = Mathf.Clamp01(1f - Mathf.Abs(distToEnemy - idealMarkDist) / Mathf.Max(idealMarkDist, 0.01f));
            priority += _markingBonus * markingScore;

            // 3) パスコース遮断（保持者→危険な相手ラインへの近接）
            float passBlockScore = 0f;
            Vector3 a = ownerPos;
            Vector3 b = bestEnemy;
            Vector3 p = bb.PhysicalState.Position;
            Vector3 ab = b - a;
            float abLenSq = Mathf.Max(0.001f, Vector3.Dot(ab, ab));
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / abLenSq);
            Vector3 closest = a + ab * t;
            float laneDist = Vector3.Distance(p, closest);
            float laneWidth = fieldLength * 0.04f; // 遮断有効帯
            passBlockScore = Mathf.Clamp01(1f - laneDist / Mathf.Max(laneWidth, 0.01f));
            priority += _passBlockBonus * passBlockScore;

            // 共通の距離ペナルティ（保持者ベース）
            if (distToOwner < minDist) priority += _tooClosePenalty;
            else if (distToOwner > maxDist) priority += _tooFarPenalty;

            return TeammateNpcGoapRoleDifferentiation.AdjustGoalPriority(
                Mathf.Max(0f, priority), bb, TeammateNpcTacticalMode.Defend);
        }

        public override bool IsAchievable(PlayerBlackboard bb)
        {
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (!IsEnemyBallDefenseContext(teamBB))
            {
                return false;
            }

            if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
            {
                return false;
            }

            if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
            {
                return false;
            }

            // 味方NPC戦術守備: 幾何学的に守備位置でも Mark/Block 等をコスト競争で選ぶ
            if (TeammateNpcDefensePlanning.ShouldIgnoreDefensivePositionGate(bb))
            {
                return true;
            }

            if (bb.GetFact(new Fact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, "true")) == true)
            {
                return false;
            }

            return true;
        }

        public override List<GoapCondition> GetPlanningRequiredFacts(PlayerBlackboard bb)
        {
            if (TeammateNpcDefensePlanning.ShouldUseTacticalDefenseGoal(bb))
            {
                return TeammateNpcDefensePlanning.GetTacticalDefensivePlanningRequiredFacts();
            }

            return RequiredFacts;
        }

        /// <summary>相手保持かつ味方非保持（FREE/味方ボール時は false）。</summary>
        private static bool IsEnemyBallDefenseContext(TeamBlackboard teamBB)
        {
            if (teamBB == null)
            {
                return false;
            }

            var ball = teamBB.BallInfo;
            if (ball.BallState == BallManager_State.BALL_STATE.FREE)
            {
                return false;
            }

            if (!ball.EnemyHasBall || ball.TeamHasBall)
            {
                return false;
            }

            return true;
        }

        public override string GetGoalDescription()
        {
            return "守備時の適切な位置取り（マーク/プレッシャー/パス遮断）を行う";
        }

        public override string GetGoalCategory()
        {
            return "Defense";
        }
    }
}


