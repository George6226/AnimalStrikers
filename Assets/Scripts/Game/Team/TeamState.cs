using UnityEngine;
using Photon.Pun;

/// <summary>
/// チーム全体の状態（試合フェーズ、スコアなど）をまとめる窓口。
/// 詳細なフィールドは必要に応じて追加する。
/// </summary>
public class TeamState : MonoBehaviourPunCallbacks
{
    /// <summary>
    /// Player 側チームにバリアが貼ってあるかどうか
    /// </summary>
    [SerializeField]
    private bool _hasBarrierPlayer = false;

    /// <summary>
    /// Enemy/NPC 側チームにバリアが貼ってあるかどうか
    /// </summary>
    [SerializeField]
    private bool _hasBarrierEnemy = false;

    public bool HasBarrierPlayer
    {
        get { return _hasBarrierPlayer; }
    }

    public bool HasBarrierEnemy
    {
        get { return _hasBarrierEnemy; }
    }

    /// <summary>
    /// チームにアタックバフが掛かっているか（ゴリラSP等）
    /// </summary>
    [SerializeField]
    private bool _hasAttackBuffPlayer = false;

    [SerializeField]
    private bool _hasAttackBuffEnemy = false;

    public bool HasAttackBuffPlayer
    {
        get { return _hasAttackBuffPlayer; }
    }

    public bool HasAttackBuffEnemy
    {
        get { return _hasAttackBuffEnemy; }
    }

    /// <summary>
    /// タグに対応するチームのアタックバフ状態を返す。
    /// </summary>
    public bool HasAttackBuffByTag(string tag)
    {
        if (IsPlayerTeamTag(tag))
        {
            return _hasAttackBuffPlayer;
        }

        if (IsEnemyTeamTag(tag))
        {
            return _hasAttackBuffEnemy;
        }

        return false;
    }

    /// <summary>
    /// タグに対応するチームのアタックバフ状態を更新し、必要なら Photon RPC で他クライアントへ通知する。
    /// </summary>
    public void SetAttackBuffByTag(string tag, bool hasAttackBuff)
    {
        if (IsPlayerTeamTag(tag))
        {
            SetAttackBuffStateInternal(ConstData.PLAYER_TAG, hasAttackBuff, true);
            return;
        }

        if (IsEnemyTeamTag(tag))
        {
            SetAttackBuffStateInternal(ConstData.ENEMY_TAG, hasAttackBuff, true);
        }
    }

    /// <summary>
    /// タグに対応するチームのバリア状態を返す。
    /// </summary>
    public bool HasBarrierByTag(string tag)
    {
        if (IsPlayerTeamTag(tag))
        {
            return _hasBarrierPlayer;
        }

        if (IsEnemyTeamTag(tag))
        {
            return _hasBarrierEnemy;
        }

        return false;
    }

    /// <summary>
    /// タグに対応するチームのバリア状態を更新し、必要なら Photon RPC で他クライアントへ通知する。
    /// </summary>
    public void SetBarrierByTag(string tag, bool hasBarrier)
    {
        if (IsPlayerTeamTag(tag))
        {
            SetBarrierStateInternal(ConstData.PLAYER_TAG, hasBarrier, true);
            return;
        }

        if (IsEnemyTeamTag(tag))
        {
            SetBarrierStateInternal(ConstData.ENEMY_TAG, hasBarrier, true);
        }
    }

    private void SetBarrierStateInternal(string teamTag, bool hasBarrier, bool syncPhoton)
    {
        bool changed = false;
        if (teamTag == ConstData.PLAYER_TAG)
        {
            if (_hasBarrierPlayer != hasBarrier)
            {
                _hasBarrierPlayer = hasBarrier;
                changed = true;
            }
        }
        else if (teamTag == ConstData.ENEMY_TAG)
        {
            if (_hasBarrierEnemy != hasBarrier)
            {
                _hasBarrierEnemy = hasBarrier;
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        if (!syncPhoton)
        {
            return;
        }

        if (photonView == null)
        {
            Debug.LogWarning("[TeamState] PhotonView が無いため HasBarrier を同期できません。");
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        photonView.RPC(nameof(RPC_SetBarrierState), RpcTarget.Others, teamTag, hasBarrier);
    }

    [PunRPC]
    private void RPC_SetBarrierState(string teamTag, bool hasBarrier)
    {
        SetBarrierStateInternal(teamTag, hasBarrier, false);
    }

    private void SetAttackBuffStateInternal(string teamTag, bool hasAttackBuff, bool syncPhoton)
    {
        bool changed = false;
        if (teamTag == ConstData.PLAYER_TAG)
        {
            if (_hasAttackBuffPlayer != hasAttackBuff)
            {
                _hasAttackBuffPlayer = hasAttackBuff;
                changed = true;
            }
        }
        else if (teamTag == ConstData.ENEMY_TAG)
        {
            if (_hasAttackBuffEnemy != hasAttackBuff)
            {
                _hasAttackBuffEnemy = hasAttackBuff;
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        if (!syncPhoton)
        {
            return;
        }

        if (photonView == null)
        {
            Debug.LogWarning("[TeamState] PhotonView が無いため HasAttackBuff を同期できません。");
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        photonView.RPC(nameof(RPC_SetAttackBuffState), RpcTarget.Others, teamTag, hasAttackBuff);
    }

    [PunRPC]
    private void RPC_SetAttackBuffState(string teamTag, bool hasAttackBuff)
    {
        SetAttackBuffStateInternal(teamTag, hasAttackBuff, false);
    }

    private static bool IsPlayerTeamTag(string tag)
    {
        return tag == ConstData.PLAYER_TAG;
    }

    private static bool IsEnemyTeamTag(string tag)
    {
        return tag == ConstData.ENEMY_TAG || tag == ConstData.NPC_TAG;
    }
}
