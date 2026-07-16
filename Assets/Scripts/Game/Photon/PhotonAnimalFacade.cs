using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// AnimalFacade への Photon 経由の操作をまとめる窓口。
/// このコンポーネントは <see cref="PhotonView"/> と同じ GameObject に付けること。
/// </summary>
public class PhotonAnimalFacade : MonoBehaviourPunCallbacks
{
    [SerializeField] private AnimalFacade _animalFacade;

    private void Awake()
    {
        if (_animalFacade == null)
        {
            _animalFacade = GetComponentInParent<AnimalFacade>();
        }
    }

    /// <summary>
    /// 指定した <see cref="AnimalFacade"/> の <see cref="PhotonAnimalFacade"/> へダメージ適用を依頼する。
    /// </summary>
    /// <returns>依頼できた場合 true</returns>
    public static bool TryRequestApplyDamage(AnimalFacade animalFacade, float damageAmount)
    {
        if (animalFacade == null)
        {
            return false;
        }

        PhotonAnimalFacade photonFacade = animalFacade.GetPhotonAnimalFacade();
        if (photonFacade == null)
        {
            Debug.LogWarning("[PhotonAnimalFacade] AnimalFacade に PhotonAnimalFacade が設定されていません。");
            return false;
        }

        photonFacade.RequestApplyDamage(damageAmount);
        return true;
    }

    /// <summary>
    /// 他クライアントからのダメージ適用依頼。オーナーへ RPC し、ローカルなら即適用。
    /// </summary>
    public void RequestApplyDamage(float damageAmount)
    {
        if (photonView == null)
        {
            if (_animalFacade != null)
            {
                ApplyDamageLocal(damageAmount);
            }
            return;
        }

        if (photonView.IsMine)
        {
            ApplyDamageLocal(damageAmount);
            return;
        }

        Player owner = photonView.Owner;
        if (owner == null)
        {
            ApplyDamageLocal(damageAmount);
            return;
        }

        photonView.RPC(nameof(RPC_ApplyDamage), owner, damageAmount);
    }

    [PunRPC]
    public void RPC_ApplyDamage(float damageAmount)
    {
        ApplyDamageLocal(damageAmount);
    }

    /// <summary>サメスペシャル泡の配置を他クライアントへ同期（オーナーのみ呼ぶ）。</summary>
    public void BroadcastSharkBubblePositions(float baseY, float[] posX, float[] posZ)
    {
        if (photonView == null || !photonView.IsMine)
        {
            return;
        }

        photonView.RPC(nameof(RPC_SpawnSharkBubbles), RpcTarget.Others, baseY, posX, posZ);
    }

    [PunRPC]
    private void RPC_SpawnSharkBubbles(float baseY, float[] posX, float[] posZ)
    {
        if (photonView != null && photonView.IsMine)
        {
            return;
        }

        if (_animalFacade == null)
        {
            return;
        }

        SharkSpecialAction shark = _animalFacade.GetComponentInChildren<SharkSpecialAction>(true);
        if (shark != null)
        {
            shark.ApplyNetworkBubblePositions(baseY, posX, posZ);
        }
    }

    /// <summary>6-E 残: 必殺発動を他クライアントへ同期（オーナーのみ呼ぶ）。</summary>
    public void BroadcastSpecialActivated()
    {
        if (photonView == null || !SpecialNetworkRules.ShouldOwnerBroadcastSpecialRpc(photonView.IsMine))
        {
            return;
        }

        if (!SpecialNetworkRules.ShouldSyncSpecialOverPhoton(ResolveBattleMode(), PhotonNetwork.InRoom))
        {
            return;
        }

        photonView.RPC(nameof(RPC_SpecialActivated), RpcTarget.Others);
    }

    [PunRPC]
    private void RPC_SpecialActivated()
    {
        if (photonView != null && photonView.IsMine)
        {
            return;
        }

        AnimalAction_Special special = ResolveSpecialAction();
        if (special != null)
        {
            special.ApplySpecialActivatedFromNetwork();
        }
    }

    /// <summary>6-E 残: 必殺終了を他クライアントへ同期（オーナーのみ呼ぶ）。</summary>
    public void BroadcastSpecialFinished()
    {
        if (photonView == null || !SpecialNetworkRules.ShouldOwnerBroadcastSpecialRpc(photonView.IsMine))
        {
            return;
        }

        if (!SpecialNetworkRules.ShouldSyncSpecialOverPhoton(ResolveBattleMode(), PhotonNetwork.InRoom))
        {
            return;
        }

        photonView.RPC(nameof(RPC_SpecialFinished), RpcTarget.Others);
    }

    [PunRPC]
    private void RPC_SpecialFinished()
    {
        if (photonView != null && photonView.IsMine)
        {
            return;
        }

        AnimalAction_Special special = ResolveSpecialAction();
        if (special != null)
        {
            special.ApplySpecialFinishedFromNetwork();
        }
    }

    private AnimalAction_Special ResolveSpecialAction()
    {
        return _animalFacade != null
            ? _animalFacade.GetComponentInChildren<AnimalAction_Special>(true)
            : null;
    }

    private static ConstData.BATTLE_MODE ResolveBattleMode()
    {
        return PhotonPlayerInfo.Instance != null
            ? PhotonPlayerInfo.Instance.BattleMode
            : ConstData.BATTLE_MODE.NPC;
    }

    private void ApplyDamageLocal(float damageAmount)
    {
        if (_animalFacade == null)
        {
            return;
        }

        if (IsBarrierBlockingDamage())
        {
            return;
        }

        AnimalHandler handler = _animalFacade.GetAnimalHandler();
        if (handler != null)
        {
            handler.damage(damageAmount);
        }

        AnimalAction_Gauge specialGauge = _animalFacade.GetSpecialGauge();
        if (specialGauge != null)
        {
            specialGauge.AddGaugeValue(ConstData.SPECIAL_GAUGE_VALUE);
        }
    }

    /// <summary>
    /// 被ダメ対象のチームにバリアがある場合はダメージを適用しない。
    /// </summary>
    private bool IsBarrierBlockingDamage()
    {
        TeamFacade teamFacade = TeamFacade.Instance;
        if (teamFacade == null || _animalFacade == null)
        {
            return false;
        }

        var avatar = _animalFacade.GetAvatar();
        if (avatar == null)
        {
            return false;
        }

        TeamState teamState = teamFacade.TeamState;
        return teamState != null && teamState.HasBarrierByTag(avatar.tag);
    }
}
