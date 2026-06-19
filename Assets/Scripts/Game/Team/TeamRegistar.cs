using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// チーム単位でアニマルを登録・管理するクラス。
/// 既存の PhotonAvatarContainer の代わりとして利用することを想定。
/// 管理の単位を PhotonAvatarContainerChild ではなく AnimalFacade に変更。
/// </summary>
public class TeamRegistar : MonoBehaviour, IEnumerable<AnimalFacade>
{

    [SerializeField] private GameObject _avatarContainer;
    // プレイヤーチーム
    private readonly List<AnimalFacade> _allyList = new();
    // 敵チーム
    private readonly List<AnimalFacade> _enemyList = new();
    // 全キャラクター（ボール以外）
    private readonly List<AnimalFacade> _allAnimals = new();

    /// <summary>味方チーム（旧: Allys として互換プロパティを提供）</summary>
    public List<AnimalFacade> Allys => _allyList.ToList();

    /// <summary>敵チーム（旧: Enemies として互換プロパティを提供）</summary>
    public List<AnimalFacade> Enemies => _enemyList.ToList();

    /// <summary>全ての動物（ボール以外）</summary>
    public List<AnimalFacade> AllAnimals => _allAnimals.ToList();

    /// <summary>リストからアニマルを取得</summary>
    public AnimalFacade this[int index] => _allAnimals[index];

    /// <summary>登録されているキャラクター数</summary>
    public int Count => _allAnimals.Count;

    /// <summary>味方のうち指定ロールのアニマルを取得する。</summary>
    public List<AnimalFacade> GetAllysByControlRole(AnimalControlRole role)
    {
        var result = new List<AnimalFacade>();
        foreach (var facade in _allyList)
        {
            if (facade == null)
            {
                continue;
            }

            var assignment = facade.GetComponent<AnimalControlAssignment>();
            if (assignment != null && assignment.Role == role)
            {
                result.Add(facade);
            }
        }

        return result;
    }

    /// <summary>
    /// 登録と味方/敵の振り分けを行う（タグから判定）
    /// </summary>
    public void Register(AnimalFacade facade)
    {
        if (facade == null) return;

        // 一度全リストから削除してから登録する
        Unregister(facade);

        // Facade が持つ Avatar のタグから味方/敵を判定
        var avatar = facade.GetAvatar();
        string tag = avatar != null ? avatar.tag : string.Empty;
        if (tag == ConstData.PLAYER_TAG)
        {
            if (!_allyList.Contains(facade))
            {
                Debug.Log("[TeamRegistar]味方に追加:"+facade.name);
                _allyList.Add(facade);
            }
        }
        else if (tag == ConstData.NPC_TAG || tag == ConstData.ENEMY_TAG)
        {
            if (!_enemyList.Contains(facade))
            {
                Debug.Log("[TeamRegistar]敵に追加:"+facade.name);
                _enemyList.Add(facade);
            }
        }

        // 親を設定
        facade.transform.SetParent(_avatarContainer.transform);
        // 共通リストに追加
        AddToAllAnimalsIfNeeded(facade);
    }

    /// <summary>
    /// 共通の登録処理（ボールは除外）
    /// </summary>
    private void AddToAllAnimalsIfNeeded(AnimalFacade facade)
    {
        if (facade == null) return;

        // Avatar が無い、またはボールなら AllAnimals には含めない
        var avatar = facade.GetAvatar();
        string tag = avatar != null ? avatar.tag : string.Empty;
        if (string.IsNullOrEmpty(tag) || tag == ConstData.BALL_TAG) return;

        if (!_allAnimals.Contains(facade))
        {
            _allAnimals.Add(facade);
        }
    }

    /// <summary>
    /// 登録解除（味方・敵・全キャラクターから削除）
    /// </summary>
    public void Unregister(AnimalFacade facade)
    {
        if (facade == null) return;

        _allyList.Remove(facade);
        _enemyList.Remove(facade);
        _allAnimals.Remove(facade);
    }

    /// <summary>
    /// 反復処理を取得する
    /// </summary>
    public IEnumerator<AnimalFacade> GetEnumerator()
    {
        return _allAnimals.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

