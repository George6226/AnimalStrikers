using UnityEngine;

namespace Game.Goap.Goals
{
    /// <summary>
    /// 6-C P0: スタミナ不足時に待機回復を目指す。緊急（FREE / 敵保持 / 自保持）では選ばない。
    /// </summary>
    [CreateAssetMenu(fileName = "RegainStaminaGoalSO", menuName = "GOAP/Goals/RegainStaminaGoalSO")]
    public class RegainStaminaGoalSO : GoapGoalSO
    {
        [Header("Priority")]
        [SerializeField] private float _basePriority = 5f;
        [SerializeField] private float _exhaustedPriority = 32f;

        protected override void OnEnable()
        {
            base.OnEnable();
            _goalName = "RegainStamina";
            SetRequiredFacts(
                new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
                new GoapCondition(SymbolTag.Basic.HAS_STAMINA, true)
            );
        }

        public override float EvaluatePriority(PlayerBlackboard bb, TeamBlackboard tb)
        {
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            if (!GoapStaminaPlanning.ShouldConsiderRegain(bb, teamBB))
            {
                return _basePriority;
            }

            return _exhaustedPriority;
        }

        public override bool IsAchievable(PlayerBlackboard bb)
        {
            var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
            return GoapStaminaPlanning.ShouldConsiderRegain(bb, teamBB);
        }

        public override string GetGoalDescription()
        {
            return "スタミナを十分まで回復する";
        }

        public override string GetGoalCategory()
        {
            return "Utility";
        }
    }
}
