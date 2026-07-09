using UnityEngine;

namespace Game.Goap.Goals
{
    /// <summary>
    /// パス飛行中に指定された受け手がボールへ寄って受け取るゴール（P0）。
    /// </summary>
    [CreateAssetMenu(fileName = "IncomingPassReceiveGoalSO", menuName = "GOAP/Goals/IncomingPassReceiveGoalSO")]
    public class IncomingPassReceiveGoalSO : GoapGoalSO
    {
        [Header("Priority")]
        [SerializeField] private float _basePriority = 5f;
        [SerializeField] private float _incomingPassPriority = 96f;

        protected override void OnEnable()
        {
            base.OnEnable();
            _goalName = "IncomingPassReceive";
            SetRequiredFacts(
                new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
                new GoapCondition(SymbolTag.Position.NEAR_BALL, true)
            );
        }

        public override float EvaluatePriority(PlayerBlackboard bb, TeamBlackboard tb)
        {
            if (!IncomingPassPlanning.IsIncomingPassReceiveContext(bb))
            {
                return _basePriority;
            }

            return _incomingPassPriority;
        }

        public override bool IsAchievable(PlayerBlackboard bb)
        {
            if (!IncomingPassPlanning.IsIncomingPassReceiveContext(bb))
            {
                return false;
            }

            if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
            {
                return false;
            }

            if (IncomingPassPlanning.IsReceiveCatchPhase(bb))
            {
                return true;
            }

            return IncomingPassPlanning.TryGetReceiveMoveTarget(bb, out _);
        }

        public override string GetGoalDescription()
        {
            return "パス飛行中にボールへ寄って受け取る";
        }
    }
}
