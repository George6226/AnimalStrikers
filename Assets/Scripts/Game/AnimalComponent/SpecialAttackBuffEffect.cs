using UnityEngine;

/// <summary>
/// スペシャル用アタックバフのエフェクト。パーティクル停止時に <see cref="AnimalSpecialActionBase.callBackEffect"/> を呼ぶ。
/// <see cref="ParticleSystem"/> と同一 GameObject に付けること（<see cref="OnParticleSystemStopped"/> の前提）。
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SpecialAttackBuffEffect : MonoBehaviour
{
    private AnimalSpecialActionBase _callbackTarget;

    /// <summary>
    /// スペシャル側（<see cref="ParentedUniqueEffect"/> 経由の SetEffectCallback）から登録する。
    /// </summary>
    public void SetCallbackTarget(AnimalSpecialActionBase specialAction)
    {
        _callbackTarget = specialAction;
    }

    /// <summary>
    /// Particle System の Main > Stop Action が Callback のとき、再生終了で Unity から呼ばれる。
    /// </summary>
    private void OnParticleSystemStopped()
    {
        _callbackTarget?.callBackEffect();
        Destroy(gameObject);
    }
}
