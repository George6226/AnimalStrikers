using UnityEngine;

namespace Game.Goap.Goals
{
    /// <summary>
    /// Phase M1: メイン NPC がボールを保持しているとき、パス/シュートで攻撃判断するゴール。
    /// プランナー到達状態は HAS_BALL=false（ボールを放つ）。
    /// </summary>
    [CreateAssetMenu(fileName = "BallPossessionAttackGoalSO", menuName = "GOAP/Goals/BallPossessionAttackGoalSO")]
    public class BallPossessionAttackGoalSO : GoapGoalSO
    {
        [Header("Priority")]
        [SerializeField] private float _basePriority = 5f;
        [SerializeField] private float _possessionAttackPriority = 92f;

        protected override void OnEnable()
        {
            base.OnEnable();
            _goalName = "BallPossessionAttack";
            SetRequiredFacts(
                new GoapCondition(SymbolTag.Basic.HAS_BALL, false)
            );
        }

        public override float EvaluatePriority(PlayerBlackboard bb, TeamBlackboard tb)
        {
            if (!MainNpcAttackPlanning.IsBallPossessionAttackContext(bb))
            {
                return _basePriority;
            }

            return _possessionAttackPriority;
        }

        public override bool IsAchievable(PlayerBlackboard bb)
        {
            if (!MainNpcAttackPlanning.IsBallPossessionAttackContext(bb))
            {
                return false;
            }

            if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
            {
                return false;
            }

            return MainNpcAttackPlanning.CanPassToTeammate(bb)
                || MainNpcAttackPlanning.CanShootAtGoal(bb);
        }
    }
}
