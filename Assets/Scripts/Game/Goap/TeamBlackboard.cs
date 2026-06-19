using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// チーム用のブラックボード
public class TeamBlackboard : MonoBehaviour
{
    // === 統合されたデータ構造 ===
    public TeamBasicInfo BasicInfo = new();
    public TeamBallInfo BallInfo = new();
    public TeamTacticalInfo TacticalInfo = new();
//     public TeamEnemyInfo EnemyInfo = new();
    public TeamFieldInfo FieldInfo = new();
//     public TeamDangerAssessment DangerAssessment = new();
//     public TeamGameState GameState = new();
//     public TeamMemberState MemberState = new();
    
    // 初期化
    private void Start()
    {
        // 統合されたデータ構造の初期化
        InitializeDataStructures();
    }

    // 統合されたデータ構造の初期化
    private void InitializeDataStructures()
    {
        // 基本情報の初期化
        BasicInfo.Initialize();
        // ボール情報の初期化
        BallInfo.Initialize();
        // 戦術情報の初期化
        TacticalInfo.Initialize();
        // フィールド情報の初期化（Z=長さ、X=幅）
        FieldInfo.Initialize(ConstData.FIELD_SIZE_Z, ConstData.FIELD_SIZE_X);
    }
    
    // 更新処理
    private void Update()
    {
        UpdateBallInfo();
        UpdateBallOwnerPosition();
        UpdateTeamState();
        UpdateTacticalInfo();
        UpdateBallOwnerPressure();
    }
    
    // ボール情報の更新
    private void UpdateBallInfo()
    {
        if (BallInfo.IsExistBall && TeamFacade.Instance != null && TeamFacade.Instance.BallManager != null)
        {
            Vector3 pos = TeamFacade.Instance.BallManager.Ball.transform.position;
            Vector3 v = TeamFacade.Instance.BallManager.Ball.Rigid.linearVelocity;
            BallInfo.updateBallPhysics(pos, v);
        }
    }

    /// <summary>
    /// ボール保持者の位置を毎フレーム同期する。
    /// 所有権変更時のみの更新だとドリブル中に古い座標のままになる。
    /// </summary>
    private void UpdateBallOwnerPosition()
    {
        if (!BallInfo.IsExistBall || BallInfo.BallOwnerID < 0)
        {
            return;
        }

        var ballManager = TeamFacade.Instance != null ? TeamFacade.Instance.BallManager : null;
        if (ballManager == null)
        {
            return;
        }

        if (ballManager.TryResolveBallOwnerWorldPosition(BallInfo.BallOwnerID, out Vector3 ownerPos))
        {
            BallInfo.updateBallOwnerPosition(ownerPos);
            return;
        }

        if (BallInfo.BallState == BallManager_State.BALL_STATE.HOLD
            && BallInfo.BallPosition.sqrMagnitude > 0.0001f)
        {
            BallInfo.updateBallOwnerPosition(BallInfo.BallPosition);
        }
    }
    
    // チーム状態の更新
    private void UpdateTeamState()
    {
        // TeamFacade / TeamRegistar から味方と敵の位置を取得
        List<Vector3> allyPositions = new List<Vector3>();
        List<Vector3> enemyPositions = new List<Vector3>();

        var teamRegist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (teamRegist != null)
        {
            // 味方の位置を取得
            foreach (var ally in teamRegist.Allys)
            {
                if (ally != null)
                {
                    allyPositions.Add(ally.transform.position);
                }
            }

            // 敵の位置を取得
            foreach (var enemy in teamRegist.Enemies)
            {
                if (enemy != null)
                {
                    enemyPositions.Add(enemy.transform.position);
                }
            }
        }

        // BasicInfoを更新
        BasicInfo.Update(enemyPositions, allyPositions);
    }

    // 戦術情報の更新
    private void UpdateTacticalInfo()
    {
        // ボールの所持チームを取得
        BallManager_State.BELONG_TEAM team = BallInfo.BallBelongTeam;
        // チームがボールを持っているかどうか
        bool teamHasBall = team == BallManager_State.BELONG_TEAM.PLAYER;
        // 敵がボールを持っているかどうか
        bool enemyHasBall = team == BallManager_State.BELONG_TEAM.ENEMY;

        // 戦術情報を更新
        TacticalInfo.Update(teamHasBall, enemyHasBall);
    }
    
    // ボール保持者のプレッシャー状態の更新
    private void UpdateBallOwnerPressure()
    {
        // ボール保持者のプレッシャー状態を更新
        BallInfo.UpdateBallOwnerPressure(BasicInfo.EnemyPositions, FieldInfo.FieldLength);
    }
}
