using System.Collections;
using UnityEngine;

// サイのスペシャル：衝撃波エリアは SPEffect_Rhinoceros 生成時（SetEffectCallback）で ON、パーティクル終了コールバックで OFF。待機終了も時間ではなくコールバックのフラグで行う。
public class RhinocerosSpecialAction : AnimalSpecialActionBase
{
    [SerializeField] private AnimalFacade _myFacade;

    private void Awake()
    {
        if (_myFacade == null)
        {
            _myFacade = GetComponentInParent<AnimalFacade>();
        }
    }

    public override void SetEffectCallback(GameObject effect)
    {
        if (effect == null || !effect.name.Contains("SPEffect_Rhinoceros"))
        {
            return;
        }

        var shockwaveCallback = effect.GetComponent<SpecialShockwaveEffect>();
        if (shockwaveCallback == null)
        {
            return;
        }
        var teamFacade = TeamFacade.Instance;
        if (teamFacade != null)
        {
            FieldObject_Handler fieldHandler = teamFacade.FieldObjectHandler;
            if (fieldHandler != null)
            {
                effect.transform.SetParent(fieldHandler.transform, worldPositionStays: true);
                effect.transform.localScale = Vector3.one;
            }
        }
        shockwaveCallback.SetOwnerAnimalFacade(_myFacade);
    }
}
