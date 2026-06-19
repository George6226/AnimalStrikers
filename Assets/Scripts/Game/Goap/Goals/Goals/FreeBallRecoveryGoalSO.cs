using UnityEngine;

namespace Game.Goap.Goals
{
    /// <summary>
    /// ボールがFREEのときに、まずボール近傍へ寄るための最小ゴール。
    /// </summary>
    [CreateAssetMenu(fileName = "FreeBallRecoveryGoalSO", menuName = "GOAP/Goals/FreeBallRecoveryGoalSO")]
    public class FreeBallRecoveryGoalSO : GoapGoalSO
    {
        [Header("Priority")]
        [SerializeField] private float _basePriority = 5f;
        [SerializeField] private float _freeBallPriority = 90f;

        protected override void OnEnable()
        {
            base.OnEnable();
            _goalName = "FreeBallRecovery";
            SetRequiredFacts(
                new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
                new GoapCondition(SymbolTag.Position.NEAR_BALL, true)
            );
        }

        public override float EvaluatePriority(PlayerBlackboard bb, TeamBlackboard tb)
        {
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (teamBB == null)
            {
                return _basePriority;
            }

            bool isFreeBall = teamBB.BallInfo.BallState == BallManager_State.BALL_STATE.FREE
                && !teamBB.BallInfo.TeamHasBall
                && !teamBB.BallInfo.EnemyHasBall;
            if (!isFreeBall)
            {
                return _basePriority;
            }

            if (!TeammateNpcGoapRoleDifferentiation.ShouldDelegateFreeBallChaseToNpc())
            {
                return _basePriority;
            }

            float p = _freeBallPriority;
            float dist = TeammateNpcGoapRoleDifferentiation.GetDistanceToBall(bb);
            if (dist <= TeammateNpcGoapRoleDifferentiation.FreeBallPursueMinDistance)
            {
                p -= 30f;
            }

            return TeammateNpcGoapRoleDifferentiation.AdjustGoalPriority(
                Mathf.Max(0f, p), bb, TeammateNpcTacticalMode.ChaseBall);
        }

        public override bool IsAchievable(PlayerBlackboard bb)
        {
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (teamBB == null) return false;
            if (teamBB.BallInfo.BallState != BallManager_State.BALL_STATE.FREE) return false;
            if (!TeammateNpcGoapRoleDifferentiation.ShouldDelegateFreeBallChaseToNpc()) return false;
            if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true) return false;
            if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true) return false;

            if (bb.GetFact(new Fact(SymbolTag.Position.NEAR_BALL, "true")) == true
                && TeammateNpcGoapRoleDifferentiation.GetDistanceToBall(bb) <= 3f)
            {
                return false;
            }

            if (TeammateNpcGoapRoleDifferentiation.GetDistanceToBall(bb)
                <= TeammateNpcGoapRoleDifferentiation.FreeBallPursueMinDistance)
            {
                return false;
            }

            if (TeammateNpcGoapRoleDifferentiation.Enabled
                && !TeammateNpcGoapRoleDifferentiation.ShouldLeadFreeBallChase(bb))
            {
                return false;
            }

            return true;
        }
    }
}
