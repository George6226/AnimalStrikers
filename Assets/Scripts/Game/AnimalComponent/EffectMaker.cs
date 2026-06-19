using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectMaker : MonoBehaviour
{
    [SerializeField] private AnimalSpecialActionBase _specialAction;

    //アニメイベントからエフェクトを生成する
    [SerializeField] GameObject[] _effects;
    [SerializeField]GameObject _auraEffect;
    [SerializeField] GameObject _auraOffEffect;
    GameObject _currentAuraEffect;
    [SerializeField]Transform _auraPivot;

    public void AnimEvent_EmitEffect(int num)
    {
        if(num >= _effects.Length)
        {
            Debug.Log("EffectMaker: AnimEvent_EmitEffect out of range num:"+num+" length:"+_effects.Length);
            return;
        }
        GameObject effect = Instantiate(_effects[num]);
        effect.transform.SetParent(transform);
        effect.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        Debug.Log("EffectMaker: AnimEvent_EmitEffect effect:"+effect.name+" num:"+num);

        if(_specialAction != null){
            _specialAction.SetEffectCallback(effect);
        }
    }

    public void AnimEvent_SetAura()
    {
        //オーラエフェクト起動
        if (_currentAuraEffect == null) 
        {
            _currentAuraEffect = Instantiate(_auraEffect);
            _currentAuraEffect.transform.SetParent(_auraPivot);
            _currentAuraEffect.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
    public void AnimEvent_AuraOff()
    {
        //オーラエフェクト削除
        if (_currentAuraEffect != null)
        {
            Destroy(_currentAuraEffect);
            GameObject offEffect = Instantiate(_auraOffEffect);
            offEffect.transform.SetParent(_auraPivot);
            offEffect.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}
