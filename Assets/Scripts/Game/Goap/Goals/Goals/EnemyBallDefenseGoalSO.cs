using UnityEngine;

namespace Game.Goap.Goals
{
    /// <summary>
    /// 相手がボールを保持しているとき、守備位置へ寄る最小ゴール。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyBallDefenseGoalSO", menuName = "GOAP/Goals/EnemyBallDefenseGoalSO")]
    public class EnemyBallDefenseGoalSO : GoapGoalSO
    {
        [Header("Priority")]
        [SerializeField] private float _basePriority = 5f;
        [SerializeField] private float _enemyBallPriority = 85f;
        [SerializeField] private float _alreadyInPositionPenalty = 30f;

        protected override void OnEnable()
        {
            base.OnEnable();
            _goalName = "EnemyBallDefense";
            SetRequiredFacts(
                new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
                new GoapCondition(SymbolTag.Basic.IS_MOVING, true)
            );
        }

        public override float EvaluatePriority(PlayerBlackboard bb, TeamBlackboard tb)
        {
            if (TeammateNpcDefensePlanning.ShouldUseTacticalDefenseGoal(bb))
            {
                return _basePriority;
            }

            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (teamBB == null || !teamBB.BallInfo.EnemyHasBall)
            {
                return _basePriority;
            }

            if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
            {
                return _basePriority;
            }

            float p = _enemyBallPriority;
            if (bb.GetFact(new Fact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, "true")) == true)
            {
                p -= _alreadyInPositionPenalty;
            }

            return TeammateNpcGoapRoleDifferentiation.AdjustGoalPriority(
                Mathf.Max(0f, p), bb, TeammateNpcTacticalMode.Defend);
        }

        public override bool IsAchievable(PlayerBlackboard bb)
        {
            if (TeammateNpcDefensePlanning.ShouldUseTacticalDefenseGoal(bb))
            {
                return false;
            }

            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (teamBB == null || !teamBB.BallInfo.EnemyHasBall) return false;
            if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true) return false;
            if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true) return false;

            if (bb.GetFact(new Fact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, "true")) == true
                && bb.GetFact(new Fact(SymbolTag.Basic.IS_MOVING, "true")) == true)
            {
                return false;
            }

            return true;
        }
    }
}
