using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アニマルの操作
public class AnimalHandler : MonoBehaviour
{
    // アニメーションの変更
    [SerializeField] private AnimalAnime_Changer _animeChange;
    // HPゲージ
    [SerializeField] private PhotonHPGauge _hpGauge;
    // 攻撃エリア
    [SerializeField] private GameObject _attackArea;
    // Rigidbody
    [SerializeField] private Rigidbody _rb;

    [Header("スペシャル突進：壁抜け防止（掃引・Wall レイヤー / Wall タグ）")]
    [SerializeField] private float _specialMoveSweepSkin = 0.03f;
    [SerializeField] private float _specialMoveFallbackSphereRadius = 0.45f;

    private AnimalSpecialMoveWallSweep _specialMoveWallSweep;
    private AnimalFacade _myFacade;

    [Header("サメスペシャル泡エリア")]
    [SerializeField, Range(0.05f, 1f)] private float _sharkBubbleMoveSpeedMultiplier = 0.35f;

    private int _sharkBubbleSlowdownCount;

    /// <summary>サメの泡コライダ内にいる間 true（複数重なりは参照カウント）。</summary>
    public bool IsSlowedBySharkBubble => _sharkBubbleSlowdownCount > 0;

    private void Awake()
    {
        if (_rb != null)
        {
            _specialMoveWallSweep = new AnimalSpecialMoveWallSweep(_rb, _specialMoveSweepSkin, _specialMoveFallbackSphereRadius);
        }

        if (_myFacade == null)
        {
            _myFacade = GetComponentInParent<AnimalFacade>();
        }
    }

    // 角度変更
    public void rotate(float rad)
    {
        // スペシャル中
        if (AnimalAction_Special.IsSpecialActive){
            return;
        }
        rotateCommon(rad);
    }

    /// <summary>
    /// スペシャル中の回転
    /// </summary>
    /// <param name="rad"></param>
    public void specialRotate(float rad)
    {
        // スペシャル中でない
        if (!AnimalAction_Special.IsSpecialActive){
            return;
        }
        rotateCommon(rad);
    }

    private void rotateCommon(float rad)
    {
        float theta = 360.0f - ((rad / Mathf.PI) * 180.0f);
        _rb.gameObject.transform.localEulerAngles = new Vector3(0.0f, theta, 0.0f);
    }

    // 移動する
    public void move(float per, float speedMag)
    {
        // スペシャル中
        if (AnimalAction_Special.IsSpecialActive){
            return;
        }

        // 移動
        moveCommon(per, speedMag);

        //Debug.Log("移動:" + this.transform.parent.name+" per:"+per+" speedMag:"+speedMag);

        // 移動アニメーション
        _animeChange.changeAnimation((int)AnimalAnime_State.PLAYER_ANIME_KIND.MOVE);

        if(_hpGauge != null)
        {
            var myAvatar = _myFacade != null ? _myFacade.GetAvatar() : null;
            string myTag = myAvatar != null ? myAvatar.tag : string.Empty;
            bool hasAttackBuff = TeamFacade.Instance != null
                && TeamFacade.Instance.TeamState != null
                && TeamFacade.Instance.TeamState.HasAttackBuffByTag(myTag);
            if (hasAttackBuff)
            {
                return;
            }

            float drainPerSec = (speedMag <= 1.0f) ? 10.0f : 20.0f;
            // 毎秒の減少量を「値」で計算
            float useValue = per * Time.deltaTime * drainPerSec;
            _hpGauge.useHP(useValue);
        }
    }

    /// <summary>
    /// スペシャル中の移動
    /// </summary>
    /// <param name="per"></param>
    /// <param name="speedMag"></param>
    public void moveSpecial(float per, float speedMag)
    {
        // スペシャル中でない
        if (!AnimalAction_Special.IsSpecialActive){
            return;
        }
        moveCommon(per, speedMag, true);
    }

    private void moveCommon(float per, float speedMag)
    {
        moveCommon(per, speedMag, false);
    }

    private void moveCommon(float per, float speedMag, bool useWallSweep)
    {
        float speed = 3.0f;
        float bubbleMul = IsSlowedBySharkBubble ? _sharkBubbleMoveSpeedMultiplier : 1f;
        Vector3 delta = _rb.transform.forward * per * speedMag * Time.deltaTime * speed * bubbleMul;

        if (useWallSweep && delta.sqrMagnitude > 1e-10f && _specialMoveWallSweep != null)
        {
            delta = _specialMoveWallSweep.ClampMoveDelta(_rb.transform.position, delta);
        }

        _rb.transform.position += delta;

        float x = Mathf.Clamp(_rb.transform.position.x, -11.5f, 11.5f);
        float z = Mathf.Clamp(_rb.transform.position.z, -21.5f, 21.5f);
        _rb.transform.position = new Vector3(x, _rb.transform.position.y, z);
    }

    // 立っている状態
    public void stand()
    {
        // スペシャル中
        if (AnimalAction_Special.IsSpecialActive){
            return;
        }

        _animeChange.changeAnimation((int)AnimalAnime_State.PLAYER_ANIME_KIND.STAND);

        if(_hpGauge != null)
        {
            // 毎秒の回復量を「値」で計算
            float healValue = Time.deltaTime * ConstData.STAND_HEAL_PER_SECOND;
            _hpGauge.healHP(healValue);
        }
    }

    // シュート
    public void shoot()
    {
        // スペシャル中
        if (AnimalAction_Special.IsSpecialActive){
            return;
        }
        _animeChange.changeAnimation((int)AnimalAnime_State.PLAYER_ANIME_KIND.SHOOT);
    }

    // 攻撃エリアの表示
    public void attack()
    {
        // スペシャル中
        if (AnimalAction_Special.IsSpecialActive){
            return;
        }
        _animeChange.changeAnimation((int)AnimalAnime_State.PLAYER_ANIME_KIND.ATTACK);
        _attackArea.SetActive(true);
        Invoke(nameof(hideAttackArea), 0.5f);
    }
    private void hideAttackArea()
    {
        _attackArea.SetActive(false);
    }

    // スライディング
    public void sliding()
    {
        // スペシャル中
        if (AnimalAction_Special.IsSpecialActive){
            return;
        }
        _animeChange.changeAnimation((int)AnimalAnime_State.PLAYER_ANIME_KIND.SLIDING);
        _attackArea.SetActive(true);
        Invoke(nameof(hideAttackArea), 0.5f);
    }

    // ダメージを受ける
    public void damage(float damageAmount)
    {
        // Debug.Log("[BoarSpecialAction] AnimalCollider_Attack damage ダメージを受ける:"+this.transform.parent.name+", damageAmount:"+damageAmount + " hpGauge:"+_hpGauge);
        _animeChange.changeAnimation((int)AnimalAnime_State.PLAYER_ANIME_KIND.DAMAGE_F);

        if (_hpGauge != null)
        {
            // Debug.Log("[BoarSpecialAction] AnimalHandler damage ダメージを受ける:"+damageAmount);
            _hpGauge.useHP(damageAmount);
        }
    }

    public void special()
    {
        _animeChange.changeAnimation((int)AnimalAnime_State.PLAYER_ANIME_KIND.SPECIAL);
    }

    /// <summary>泡エリア侵入時（コライダごとに呼ぶ。重なりは <see cref="RemoveSharkBubbleSlowdownSource"/> と対になる）。</summary>
    public void AddSharkBubbleSlowdownSource()
    {
        _sharkBubbleSlowdownCount++;
    }

    /// <summary>泡エリアから出たとき。</summary>
    public void RemoveSharkBubbleSlowdownSource()
    {
        _sharkBubbleSlowdownCount = Mathf.Max(0, _sharkBubbleSlowdownCount - 1);
    }
}
