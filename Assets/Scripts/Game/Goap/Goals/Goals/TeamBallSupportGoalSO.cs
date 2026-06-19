using System.Collections.Generic;
using UnityEngine;

namespace Game.Goap.Goals
{
    /// <summary>
    /// 味方がボールを保持しているとき、パスを受けやすいサポート位置へ寄る最小ゴール。
    /// </summary>
    [CreateAssetMenu(fileName = "TeamBallSupportGoalSO", menuName = "GOAP/Goals/TeamBallSupportGoalSO")]
    public class TeamBallSupportGoalSO : GoapGoalSO
    {
        [Header("Priority")]
        [SerializeField] private float _basePriority = 5f;
        [SerializeField] private float _teamBallPriority = 85f;
        [SerializeField] private float _alreadyInPositionPenalty = 30f;

        protected override void OnEnable()
        {
            base.OnEnable();
            _goalName = "TeamBallSupport";
            SetRequiredFacts(
                new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
                new GoapCondition(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, true)
            );
        }

        public override float EvaluatePriority(PlayerBlackboard bb, TeamBlackboard tb)
        {
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (!TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB))
            {
                return _basePriority;
            }

            if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
            {
                return _basePriority;
            }

            float p = TeammateNpcSupportPlanning.ShouldUseTacticalSupportGoal(bb)
                ? TeammateNpcSupportPlanning.TeamBallTacticalSupportPriority
                : _teamBallPriority;

            if (!TeammateNpcSupportPlanning.ShouldUseTacticalSupportGoal(bb)
                && bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) == true)
            {
                p -= _alreadyInPositionPenalty;
            }

            return TeammateNpcGoapRoleDifferentiation.AdjustGoalPriority(
                Mathf.Max(0f, p), bb, TeammateNpcTacticalMode.Support);
        }

        public override bool IsAchievable(PlayerBlackboard bb)
        {
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (!TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB))
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

            if (TeammateNpcSupportPlanning.ShouldIgnorePassReceivePositionGate(bb))
            {
                return true;
            }

            if (bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) == true)
            {
                return false;
            }

            return true;
        }

        public override List<GoapCondition> GetPlanningRequiredFacts(PlayerBlackboard bb)
        {
            if (TeammateNpcSupportPlanning.ShouldUseTacticalSupportGoal(bb))
            {
                return TeammateNpcSupportPlanning.GetTacticalSupportPlanningRequiredFacts();
            }

            return RequiredFacts;
        }
    }
}
