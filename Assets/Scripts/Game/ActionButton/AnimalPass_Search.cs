using UnityEngine;

/// <summary>
/// パス先の検索やパスコース上の障害物チェックなど、
/// パスに関連する「検索系」の処理をまとめる補助クラス。
/// </summary>
public class AnimalPass_Search : MonoBehaviour
{
    /// <summary>
    /// パスを出す側（パサー）から見て、パス先に選びたい味方を検索する。
    /// </summary>
    public AnimalFacade FindAllyForPass(AnimalFacade passer)
    {
        if (passer == null)
        {
            return null;
        }

        var avatar = passer.GetAvatar();
        if (avatar == null)
        {
            return null;
        }

        string tag = avatar.gameObject.tag;
        if (tag == ConstData.PLAYER_TAG)
        {
            return findAllyForPassInPlayer(passer);
        }

        if (tag == ConstData.NPC_TAG)
        {
            return findAllyForPassInNPC(passer);
        }

        return null;
    }

    private AnimalFacade findAllyForPassInPlayer(AnimalFacade passer)
    {
        return GoapPassTargetSelection.TrySelectBestAlly(passer, out AnimalFacade target)
            ? target
            : null;
    }

    private AnimalFacade findAllyForPassInNPC(AnimalFacade passer)
    {
        return GoapPassTargetSelection.TrySelectBestEnemyTeammate(passer, out AnimalFacade target)
            ? target
            : null;
    }

    /// <summary>
    /// パスコース上に他のキャラクターが存在するかチェックする。
    /// （元々 AnimalAction_Pass 内にあったロジックをこちらに移す想定）
    /// </summary>
    public bool IsCharacterInPassLine(GameObject passer, GameObject receiver)
    {
        if (passer == null || receiver == null)
        {
            return false;
        }

        Vector3 passDirection = (receiver.transform.position - passer.transform.position).normalized;
        float passDistance = Vector3.Distance(passer.transform.position, receiver.transform.position);

        float characterRadius = 1.0f;

        foreach (var character in TeamFacade.Instance.TeamRegist.AllAnimals)
        {
            if (character.gameObject == passer || character.gameObject == receiver)
            {
                continue;
            }

            Vector3 characterToPasserVector = character.transform.position - passer.transform.position;
            Vector3 projection = Vector3.Project(characterToPasserVector, passDirection);

            float distanceToPassLine = Vector3.Distance(characterToPasserVector, projection);

            if (distanceToPassLine < characterRadius
                && projection.magnitude < passDistance
                && Vector3.Dot(projection, passDirection) > 0)
            {
                return true;
            }
        }

        return false;
    }
}
