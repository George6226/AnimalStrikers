using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParentedUniqueEffect : MonoBehaviour
{
    [SerializeField] AnimalSpecialActionBase _specialAction;
    [SerializeField] ParentedEffect[] _effects;

    public void AnimEvent_EmitParentedEffect(int num)
    {
        if(_effects.Length <= num){
            Debug.Log("name:"+this.name+" 発動エフェクトがない length:"+_effects.Length+" index:"+num);
            return;
        }
        //�ŗL�e�q�t�G�t�F�N�g����
        GameObject effect = Instantiate(_effects[num]._effect, _effects[num]._parentTo);
        effect.transform.localEulerAngles = _effects[num]._localRot;

        Debug.Log("effect name:"+_effects[num]._effect.name+" 番号:"+num);

        if(_specialAction != null){
            _specialAction.SetEffectCallback(effect);
        }
    }
}

[Serializable]
public struct ParentedEffect
{
    public GameObject _effect;
    public Transform _parentTo;
    public Vector3 _localRot;
}
