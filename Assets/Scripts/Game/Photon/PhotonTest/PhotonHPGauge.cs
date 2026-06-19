using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Photon.Pun;

public class PhotonHPGauge : MonoBehaviourPunCallbacks, IPunObservable
{
    // HPバー
    [SerializeField] private Image _hpBar = default;
    [SerializeField] private GameObject _hpBarObject = default;  // HPバーのGameObject

    // 最大HP/現在HP
    [SerializeField] private AnimalFacade _myFacade;
    private float _maxHP = ConstData.DEFAULT_HP;
    private float _currentHP = ConstData.DEFAULT_HP;
    
    // HPバーの表示状態
    private bool _isVisible = true;

    private void Start()
    {
        if (_myFacade == null)
        {
            _myFacade = GetComponentInParent<AnimalFacade>();
        }

        AnimalInfo animalInfo = _myFacade != null ? _myFacade.GetAnimalInfo() : null;
        AnimalSpritInfo animalSpritInfo = _myFacade != null ? _myFacade.GetAnimalSpritInfo() : null;
        Param_SpritData paramSpritData = animalSpritInfo != null ? animalSpritInfo.ParamSpritData : null;

        float baseStamina = paramSpritData != null ? paramSpritData.GetBaseParameterValue(Param_SpritData.ParameterType.Stamina) : ConstData.DEFAULT_HP;
        float increaseStamina = paramSpritData != null ? paramSpritData.GetIncreaseParameterValue(Param_SpritData.ParameterType.Stamina) : 0.0f;
        float stamina = animalInfo != null ? animalInfo.Stamina : 0.0f;

        _maxHP = Mathf.Max(1.0f, baseStamina + (increaseStamina * stamina / 100.0f));
        _currentHP = _maxHP;
        changeGuage();

        // 初期状態は表示
        SetHPGaugeVisibility(true);
    }

    // HPバーの表示/非表示を切り替える
    public void SetHPGaugeVisibility(bool isVisible)
    {
        isVisible = true;
        if (_hpBarObject != null)
        {
            _isVisible = isVisible;
            _hpBarObject.SetActive(_isVisible);
        }
    }

    // HPバーの表示状態を取得
    public bool IsHPGaugeVisible()
    {
        return _isVisible;
    }

    // HPを使う
    public void useHP(float value)
    {
        // if(photonView.IsMine)
        // {
            //Debug.Log("値:"+value+" 最大HP:"+_maxHP);
            //Debug.Log("ゲージを減らす:"+_currentHP);
            _currentHP = Mathf.Max(0.0f, _currentHP - value);
            changeGuage();
        // }
    }
    // HPを回復する
    public void healHP(float value)
    {
        if(photonView.IsMine)
        {
            _currentHP = Mathf.Min(_currentHP + value, _maxHP);
            // Debug.Log("PhotonHPGauge:healHP:"+_currentHP+" 最大HP:"+_maxHP +" 回復量:"+value);
            changeGuage();
        }
    }
    // ゲージを変更する
    private void changeGuage()
    {
        // 親要素の幅を取得
        float parentWidth = _hpBar.rectTransform.parent.GetComponent<RectTransform>().rect.width;
        
        // HPに応じて左からの距離を計算（HPが減ると左からの距離が増える）
        float leftOffset = parentWidth * (1 - _currentHP / _maxHP);

        // Debug.Log("PhotonHPGauge:changeGuage: _currentHP:"+_currentHP+" _maxHP:"+_maxHP+" leftOffset:"+leftOffset+" parentWidth:"+parentWidth+" 親の名前:"+_hpBar.rectTransform.parent.name);
        
        // 左端の位置を設定
        _hpBar.rectTransform.sizeDelta = new Vector2(0, 0);  // Stretchを維持
        _hpBar.rectTransform.offsetMin = new Vector2(leftOffset, _hpBar.rectTransform.offsetMin.y);
        _hpBar.rectTransform.offsetMax = new Vector2(0, _hpBar.rectTransform.offsetMax.y);
    }

    // Photonのデータ監視
    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            //Debug.Log("HPGaugeSend:" + PhotonNetwork.NickName+" hp:"+_currentHP);
            stream.SendNext(_currentHP);
        }
        else
        {
            float previousHP = _currentHP;
            _currentHP = (float)stream.ReceiveNext();
            if (!Mathf.Approximately(previousHP, _currentHP))
            {
                // Debug.Log("PhotonHPGauge:外部同期でHP変化 previous:" + previousHP + " current:" + _currentHP);
            }
            changeGuage();
            //Debug.Log("HPGaugeReceive:" + PhotonNetwork.NickName + " hp:" + _currentHP);
        }
    }

    private void LateUpdate()
    {
        // HPバーオブジェクトをカメラの方向に向ける
        if (Camera.main != null && _hpBarObject != null)
        {
            _hpBarObject.transform.LookAt(
                _hpBarObject.transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up
            );
        }
    }
}
