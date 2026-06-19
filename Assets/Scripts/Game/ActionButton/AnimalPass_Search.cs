using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        if (passer == null){
            return null;
        }

        // passerからPhotonAvatarを取得しタグで判断
        var avatar = passer.GetAvatar();
        if (avatar != null)
        {
            // PhotonAvatarのGameObjectのタグ（"Player"や"NPC"など）で判別
            string tag = avatar.gameObject.tag;
            Debug.Log("パサーのPhotonAvatarタグ: " + tag);

            // 例：タグが"Player"ならプレイヤー用のパス先を検索
            if (tag == ConstData.PLAYER_TAG)
            {
                return findAllyForPassInPlayer(passer);
            }
            // タグが"NPC"ならNPC用のロジック（未実装の場合はデフォルト処理）
            else if (tag == ConstData.NPC_TAG)
            {
                return findAllyForPassInNPC(passer);
            }
        }

        return null;
    }

    // プレイヤー用のパス先検索
    private AnimalFacade findAllyForPassInPlayer(AnimalFacade passer)
    {
        const float ANGLE_THRESHOLD = 30.0f;  // 角度の閾値
        List<AnimalFacade> candidates = new List<AnimalFacade>();
        float rY = 360.0f - passer.transform.localEulerAngles.y;
        Vector3 oPos = passer.transform.position;

        // passer のタグに基づいて味方リストを取得
        IEnumerable<AnimalFacade> list = TeamFacade.Instance.TeamRegist.Allys;
        // Debug.Log("[AnimalPass_Search]味方リスト:"+list.Count());

        // Debug.Log("[AnimalPass_Search]パサー:"+passer.name + " 位置:"+passer.transform.position);

        foreach(AnimalFacade child in list)
        {
            // パサーとGKはスキップ
            if (child == passer || child.GetAnimalInfo().IsGK)
            {
                continue;
            }

            Vector3 tPos = child.transform.position;
            float theta = Mathf.Atan2(tPos.z - oPos.z, tPos.x - oPos.x) * Mathf.Rad2Deg - 90.0f;
            theta = (theta < 0.0f) ? theta + 360.0f : theta;

            float dd = Mathf.Abs(rY - theta);
            // Debug.Log("[AnimalPass_Search]角度差:"+dd+" child:"+child.name+" 角度:"+theta+" rY:"+rY + " 位置:"+tPos);

            if (dd <= ANGLE_THRESHOLD)
            {
                // Debug.Log("[AnimalPass_Search]角度差が30度以内の候補に追加:"+child.name);
                candidates.Add(child);
            }
        }

        // 30度以内の候補がいる場合はランダムに選択
        if (candidates.Count > 0)
        {
            // Debug.Log("[AnimalPass_Search]角度差が30度以内の候補がある:"+candidates.Count);
            return candidates[Random.Range(0, candidates.Count)];
        }

        // 30度以内の候補がいない場合は最も角度の近いものを選択
        AnimalFacade nearestAlly = list.Where(x => {
            return x.gameObject != passer.gameObject && !x.GetAnimalInfo().IsGK;
        }).OrderBy(x => {
            Vector3 tPos = x.transform.position;
            float theta = Mathf.Atan2(tPos.z - oPos.z, tPos.x - oPos.x) * Mathf.Rad2Deg - 90.0f;
            theta = (theta < 0.0f) ? theta + 360.0f : theta;
            return Mathf.Abs(rY - theta);
        }).FirstOrDefault();

        // Debug.Log("[AnimalPass_Search]最も角度の近いもの:"+nearestAlly.name);
        return nearestAlly;
    }

    private AnimalFacade findAllyForPassInNPC(AnimalFacade passer) 
    {
        // Enemies の中からランダムで（自分とGKを除く）選ぶ
        IEnumerable<AnimalFacade> list = TeamFacade.Instance.TeamRegist.Enemies;

        var candidates = list
            .Where(x => x != null && x.gameObject != passer.gameObject && !x.GetAnimalInfo().IsGK)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, candidates.Count);
        return candidates[index];

        // TODO: 将来的に GOAP でより賢くパス先を選択する
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

        // キャラクターの半径（調整が必要な場合は変更してください）
        float characterRadius = 1.0f;

        // 全てのキャラクターをチェック
        foreach (var character in TeamFacade.Instance.TeamRegist.AllAnimals)
        {
            // パサーとレシーバーはスキップ
            if (character.gameObject == passer || character.gameObject == receiver)
                continue;

            // キャラクターとパスラインの距離を計算
            Vector3 characterToPasserVector = character.transform.position - passer.transform.position;
            Vector3 projection = Vector3.Project(characterToPasserVector, passDirection);

            // パスラインまでの垂直距離を計算
            float distanceToPassLine = Vector3.Distance(characterToPasserVector, projection);

            // パスラインまでの距離がキャラクターの半径より小さく、
            // かつ投影点がパサーとレシーバーの間にある場合
            if (distanceToPassLine < characterRadius &&
                projection.magnitude < passDistance &&
                Vector3.Dot(projection, passDirection) > 0)
            {
                return true;
            }
        }

        return false;
    }
}

