using UnityEngine;

/// <summary>
/// サイなどスペシャル衝撃波エリアの当たり判定。Body に触れた敵陣営に <see cref="PhotonAnimalFacade.TryRequestApplyDamage"/> でダメージを与える。
/// </summary>
public class FieldCollider_SpecialShockwave : MonoBehaviour
{
    /// <summary>オーナーとなるAnimalFacade</summary>
    private AnimalFacade _ownerFacade;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("FieldCollider_SpecialShockwave:1 OnTriggerEnter " + other.name+" tag:"+other.gameObject.tag);
        if (!other.gameObject.tag.Equals("Body"))
        {
            return;
        }
        Debug.Log("FieldCollider_SpecialShockwave:2 OnTriggerEnter " + _ownerFacade);
        if (_ownerFacade == null)
        {
            return;
        }
        Debug.Log("FieldCollider_SpecialShockwave:3 OnTriggerEnter " + _ownerFacade.name);
        var ownerAvatar = _ownerFacade.GetAvatar();
        if (ownerAvatar == null)
        {
            return;
        }
        Debug.Log("FieldCollider_SpecialShockwave:4 OnTriggerEnter " + ownerAvatar.name);
        AnimalFacade target = other.transform.parent.GetComponent<AnimalFacade>();
        if (target == null || target == _ownerFacade)
        {
            return;
        }
        Debug.Log("FieldCollider_SpecialShockwave:5 OnTriggerEnter " + target.name);
        var targetAvatar = target.GetAvatar();
        if (targetAvatar == null)
        {
            return;
        }
        Debug.Log("FieldCollider_SpecialShockwave:6 OnTriggerEnter " + targetAvatar.name);
        if (IsSameFaction(ownerAvatar, targetAvatar))
        {
            return;
        }
        Debug.Log("FieldCollider_SpecialShockwave:7 OnTriggerEnter " + target.name);
        float ownerAttack = _ownerFacade.GetAnimalInfo() != null ? _ownerFacade.GetAnimalInfo().Attack : 0.0f;
        float targetDefense = target.GetAnimalInfo() != null ? target.GetAnimalInfo().Defense : 0.0f;
        bool hasTargetTeamAttackBuff = TeamFacade.Instance != null
            && TeamFacade.Instance.TeamState != null
            && TeamFacade.Instance.TeamState.HasAttackBuffByTag(targetAvatar.tag);
        float damage;
        if (hasTargetTeamAttackBuff)
        {
            damage = 0.0f;
        }
        else
        {
            damage = ConstData.BASE_ATTACK_DAMAGE + ((ownerAttack - targetDefense) / 2.0f);
            damage = Mathf.Max(ConstData.MIN_ATTACK_DAMAGE, damage);
        }
        PhotonAnimalFacade.TryRequestApplyDamage(target, damage);
    }

    private static bool IsSameFaction(PhotonAvatarContainerChild a, PhotonAvatarContainerChild b)
    {
        return string.Equals(TagForCompare(a), TagForCompare(b), System.StringComparison.Ordinal);
    }

    private static string TagForCompare(PhotonAvatarContainerChild avatar)
    {
        string tag = avatar.CurrentTag;
        if (string.IsNullOrEmpty(tag))
        {
            tag = avatar.gameObject.tag;
        }
        return tag;
    }

    public void SetOwnerFacade(AnimalFacade animalFacade)
    {
        _ownerFacade = animalFacade;
    }
}
