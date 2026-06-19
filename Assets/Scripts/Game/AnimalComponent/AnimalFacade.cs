using UnityEngine;

// アニマルの窓口
public class AnimalFacade : MonoBehaviour
{
    // アニマルの情報
    [SerializeField] private AnimalInfo _animalInfo;
    // アニマルのスプリット情報
    [SerializeField] private AnimalSpritInfo _animalSpritInfo;
    // アクションセレクター
    [SerializeField] private AnimalActionSelector _actionSelector;
    // アニマルの操作
    [SerializeField] private AnimalHandler _animal;
    // ユニフォーム変更
    [SerializeField] private AnimalUniformChanger _uniformChanger;
    // HPゲージ
    [SerializeField] private PhotonHPGauge _hpGauge;
    // Photon情報
    [SerializeField] private PhotonAvatarContainerChild _avatar;
    // Photon 操作用窓口（PhotonView と同一 GameObject に付与）
    [SerializeField] private PhotonAnimalFacade _photonAnimalFacade;
    // スペシャルゲージ
    [SerializeField] private AnimalAction_Gauge _specialGauge;

    private void Awake()
    {
        if (_photonAnimalFacade == null)
        {
            _photonAnimalFacade = GetComponentInChildren<PhotonAnimalFacade>(true);
        }
    }

    public AnimalInfo GetAnimalInfo()
    {
        return _animalInfo;
    }

    public AnimalSpritInfo GetAnimalSpritInfo()
    {
        return _animalSpritInfo;
    }

    public PhotonAvatarContainerChild GetAvatar()
    {
        return _avatar;
    }

    public PhotonAnimalFacade GetPhotonAnimalFacade()
    {
        return _photonAnimalFacade;
    }

    public AnimalActionSelector GetActionSelector()
    {
        return _actionSelector;
    }

    public PhotonHPGauge GetHPGauge()
    {
        return _hpGauge;
    }

    public AnimalAction_Gauge GetSpecialGauge()
    {
        return _specialGauge;
    }

    /// <summary>
    /// ボールの保持位置を取得する関数
    /// </summary>
    public GameObject GetBallKeep()
    {
        return _animalInfo != null ? _animalInfo.BallKeep : null;
    }

    public bool IsGK()
    {
        return _animalInfo.IsGK;
    }

    /// <summary>
    /// アニマル操作。ダメージ適用は <see cref="PhotonAnimalFacade.TryRequestApplyDamage"/> を使用すること。
    /// </summary>
    public AnimalHandler GetAnimalHandler()
    {
        return _animal;
    }

    /// <summary>
    /// ユニフォームの種類を変更する関数
    /// </summary>
    /// <param name="uniformType">ユニフォームの種類</param>
    public void SetUniformType(int uniformType)
    {
        if (_uniformChanger != null)
        {
            _uniformChanger.setUniformType(uniformType);
        }
    }
}
