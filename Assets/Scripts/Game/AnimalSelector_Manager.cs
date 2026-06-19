using UnityEngine;

/// <summary>
/// アニマル選択まわりをまとめて管理するためのマネージャクラス。
/// 現時点では中身は未実装。
/// </summary>
public class AnimalSelector_Manager : MonoBehaviour
{
    [SerializeField] private AnimalSelector_Player _playerSelector;
    public AnimalSelector_Player PlayerSelector
    {
        get { return _playerSelector; }
    }

    [SerializeField] private AnimalSelector_NPC _npcSelector;
    public AnimalSelector_NPC NPCSelector
    {
        get { return _npcSelector; }
    }

    private void Start()
    {
        if (PlayerSelector == null)
        {
            Debug.LogError("AnimalSelector_Manager: PlayerSelector is null");
        }
        if (NPCSelector == null)
        {
            Debug.LogError("AnimalSelector_Manager: NPCSelector is null");
        }
    }

    // 選択されている動物を取得
    public AnimalFacade GetSelectAnimal(string type)
    {
        AnimalSelector_Base selector = GetAnimalSelector(type);
        if (selector == null)
        {
            Debug.LogError("AnimalSelector_Manager: selector is null");
            return null;
        }
        return selector.SelAnimalFacade;
    }

    // 選択動物を設定
    public void SetSelectAnimal(AnimalFacade animal, string tag)
    {
        // タグから適切な AnimalSelector を取得
        AnimalSelector_Base selector = GetAnimalSelector(tag);
        if (selector == null)
        {
            return;
        }
        selector.setSelectAnimal(animal);
    }

    // 動物セレクターを取得
    private AnimalSelector_Base GetAnimalSelector(string type)
    {
          if (type.Equals(ConstData.PLAYER_TAG) || type.Equals(ConstData.ENEMY_TAG))
          {
            return PlayerSelector;
          }
          else if (type.Equals(ConstData.NPC_TAG))
          {
            return NPCSelector;
          }
          else
          {
            Debug.LogError("AnimalSelector_Manager: type is not Player or NPC");
            return null;
          }
    }
}

