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
    /// パスコース上に味方または敵が存在するか（ロブキックが必要か）。
    /// </summary>
    public bool IsCharacterInPassLine(GameObject passer, GameObject receiver)
    {
        if (passer == null || receiver == null)
        {
            return false;
        }

        AnimalFacade passerFacade = passer.GetComponent<AnimalFacade>();
        AnimalFacade receiverFacade = receiver.GetComponent<AnimalFacade>();
        if (passerFacade == null || receiverFacade == null)
        {
            return false;
        }

        return PassLaneKickPolicy.NeedsLob(passerFacade, receiverFacade);
    }
}
