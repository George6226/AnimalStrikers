using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 攻守交代によるゴールの変更
public class AIContextSwitcher : MonoBehaviour
{
    [SerializeField] private GoapAgent _agent;
    private bool _lastTeamHasBall;
    private bool _lastEnemyHasBall;

    private void Awake()
    {
        if (_agent == null)
        {
            _agent = GetComponent<GoapAgent>();
        }

        if (_agent == null)
        {
            _agent = GetComponentInParent<GoapAgent>();
        }
    }

    void Start()
    {
        // 初期値を保存
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB != null)
        {
            _lastTeamHasBall = teamBB.BallInfo.TeamHasBall;
            _lastEnemyHasBall = teamBB.BallInfo.EnemyHasBall;
        }
    }

    void Update()
    {
        //bool hasBall = blackboard.GetFact(new Fact("hasBall", "true")) == true;
        //agent.CurrentGoal = hasBall ? attackGoal : defendGoal;

        // 所有権の変化を検知
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null) return;
        bool nowTeamHasBall = teamBB.BallInfo.TeamHasBall;
        bool nowEnemyHasBall = teamBB.BallInfo.EnemyHasBall;
        
        if (nowTeamHasBall != _lastTeamHasBall || nowEnemyHasBall != _lastEnemyHasBall)
        {
            Abort();
            _lastTeamHasBall = nowTeamHasBall;
            _lastEnemyHasBall = nowEnemyHasBall;
        }
    }

    public void Abort()
    {
        if (_agent != null)
            _agent.AbortCurrentPlan();
    }
}
