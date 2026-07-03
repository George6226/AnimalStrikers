using UnityEngine;

/// <summary>
/// AnimalPrefab から TeamRegistar へ登録するためのクラス。
/// プレハブにアタッチし、紐づく AnimalFacade を TeamRegistar に登録する。
/// </summary>
public class AnimalTeamRegistrar : MonoBehaviour
{
    [SerializeField] private AnimalFacade _facade;

    private void Awake()
    {
        // Inspector で未設定なら、親階層から自動取得
        if (_facade == null)
        {
            _facade = GetComponentInParent<AnimalFacade>();
        }
    }

    private void Start()
    {
        // TeamRegistar が存在し、Facade も取得できていれば登録（TeamFacade 経由）
        var teamRegist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (teamRegist != null && _facade != null)
        {
            teamRegist.Register(_facade);
            NotifySquadControl(_facade);
        }
    }

    private void OnDestroy()
    {
        var teamRegist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (teamRegist != null && _facade != null)
        {
            teamRegist.Unregister(_facade);
            var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
            squad?.OnLocalAllyUnregistered(_facade);
            var enemySquad = TeamFacade.Instance != null ? TeamFacade.Instance.EnemySquadControl : null;
            enemySquad?.OnLocalEnemyUnregistered(_facade);
        }
    }

    private static void NotifySquadControl(AnimalFacade facade)
    {
        if (IsLocalEnemy(facade))
        {
            var enemySquad = TeamFacade.Instance != null ? TeamFacade.Instance.EnemySquadControl : null;
            enemySquad?.OnLocalEnemyRegistered(facade);
            return;
        }

        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        squad?.OnLocalAllyRegistered(facade);
    }

    private static bool IsLocalEnemy(AnimalFacade facade)
    {
        var avatar = facade?.GetAvatar();
        if (avatar == null || !avatar.IsMine)
        {
            return false;
        }

        string tag = string.IsNullOrEmpty(avatar.CurrentTag) ? avatar.tag : avatar.CurrentTag;
        return tag == ConstData.NPC_TAG || tag == ConstData.ENEMY_TAG;
    }
}

