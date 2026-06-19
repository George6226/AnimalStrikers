using UnityEngine;

/// <summary>
/// サイなど衝撃波スペシャル用エフェクト。<see cref="OnParticleSystemStopped"/> で <see cref="AnimalSpecialActionBase.callBackEffect"/> を呼ぶ。
/// <see cref="ParticleSystem"/> と同一 GameObject に付けること。
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SpecialShockwaveEffect : MonoBehaviour
{
    [SerializeField] private FieldCollider_SpecialShockwave _shockwaveCollider;

    /// <summary>
    /// FieldCollider_SpecialShockwave（衝撃波エリア判定用）にAnimalFacadeを設定する
    /// </summary>
    /// <param name="animalFacade">オーナーとなるAnimalFacade</param>
    public void SetOwnerAnimalFacade(AnimalFacade animalFacade)
    {
        if (_shockwaveCollider != null)
        {
            _shockwaveCollider.SetOwnerFacade(animalFacade);
        }
    }
}
