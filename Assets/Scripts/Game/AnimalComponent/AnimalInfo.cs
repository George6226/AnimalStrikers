using UnityEngine;

// アニマルの情報(固定データ)
public class AnimalInfo : MonoBehaviour
{
    [SerializeField] private Param_AnimalInfo _animalInfo;
    [SerializeField] private AnimalSpritInfo _animalSpritInfo;
    [SerializeField] private PhotonAvatarContainerChild _avatar;

    // GKかどうか
    public bool IsGK => _animalInfo.InfoParam.IsGK;
    public int Stamina => GetBaseParameter(Param_SpritData.ParameterType.Stamina) + GetSpritModifier(Param_SpritData.ParameterType.Stamina);
    public int Speed => GetBaseParameter(Param_SpritData.ParameterType.Speed) + GetSpritModifier(Param_SpritData.ParameterType.Speed);
    public int Shoot => GetBaseParameter(Param_SpritData.ParameterType.Shoot) + GetSpritModifier(Param_SpritData.ParameterType.Shoot);
    public int Pass => GetBaseParameter(Param_SpritData.ParameterType.Pass) + GetSpritModifier(Param_SpritData.ParameterType.Pass);
    public int Attack => GetBaseParameter(Param_SpritData.ParameterType.Attack) + GetSpritModifier(Param_SpritData.ParameterType.Attack);
    public int Defense => GetBaseParameter(Param_SpritData.ParameterType.Defense) + GetSpritModifier(Param_SpritData.ParameterType.Defense);
    public Param_AnimalInfo.AnimalType AnimalType => _animalInfo != null ? _animalInfo.InfoParam.AnimalType : Param_AnimalInfo.AnimalType.None;

    // ボールの保持位置
    [SerializeField] private GameObject _ballKeep;
    public GameObject BallKeep
    {
        get { return _ballKeep; }
    }

    void Start()
    {
        // タイプを取得
        // var animalType = _animalInfo.InfoParam.AnimalType;
    }

    private int GetBaseParameter(Param_SpritData.ParameterType type)
    {
        if (_animalInfo == null)
        {
            return 0;
        }

        var info = _animalInfo.InfoParam;
        switch (type)
        {
            case Param_SpritData.ParameterType.Stamina:
                return info.Stamina;
            case Param_SpritData.ParameterType.Speed:
                return info.Speed;
            case Param_SpritData.ParameterType.Shoot:
                return info.Shoot;
            case Param_SpritData.ParameterType.Pass:
                return info.Pass;
            case Param_SpritData.ParameterType.Attack:
                return info.Attack;
            case Param_SpritData.ParameterType.Defense:
                return info.Defense;
            default:
                return 0;
        }
    }

    private int GetSpritModifier(Param_SpritData.ParameterType type)
    {
        if (IsNpcAvatar())
        {
            return 0;
        }

        return _animalSpritInfo != null ? _animalSpritInfo.GetSpritModifier(type) : 0;
    }

    private bool IsNpcAvatar()
    {
        if (_avatar == null)
        {
            return false;
        }

        string tag = _avatar.CurrentTag;
        if (string.IsNullOrEmpty(tag))
        {
            tag = _avatar.tag;
        }

        return tag == ConstData.NPC_TAG;
    }
}
