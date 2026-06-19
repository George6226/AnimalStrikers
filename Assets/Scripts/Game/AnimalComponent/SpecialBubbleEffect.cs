using UnityEngine;

/// <summary>
/// サメのスペシャル用の泡エフェクト。<see cref="OnParticleSystemStopped"/> で <see cref="AnimalSpecialActionBase.callBackEffect"/> を呼ぶ。
/// <see cref="ParticleSystem"/> と同一 GameObject に付けること。
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SpecialBubbleEffect : MonoBehaviour
{
    [SerializeField] private FieldCollider_SpecialBubble _bubbleCollider;

    /// <summary>
    /// <see cref="FieldCollider_SpecialBubble"/>（泡エリア判定用）にオーナーを設定する。
    /// </summary>
    public void SetOwnerAnimalFacade(AnimalFacade animalFacade)
    {
        if (_bubbleCollider != null)
        {
            _bubbleCollider.SetOwnerFacade(animalFacade);
        }
    }
}
