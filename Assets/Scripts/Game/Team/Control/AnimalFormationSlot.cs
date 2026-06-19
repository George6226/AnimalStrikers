using UnityEngine;

/// <summary>
/// チーム編成上のスロット番号（0〜2: フィールド、3: GK）。
/// PhotonAvatarCreator の生成ループで設定する。
/// </summary>
public class AnimalFormationSlot : MonoBehaviour
{
    [SerializeField] private int _index = -1;

    public int Index => _index;
    public bool IsAssigned => _index >= 0;

    public void Initialize(int index)
    {
        _index = index;
    }
}
