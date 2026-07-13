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

    [Header("スタミナ枯渇時の移動速度（F1）")]
    [SerializeField, Range(0.05f, 1f)] private float _exhaustedMoveSpeedMultiplier = 0.55f;
    [SerializeField, Range(0.05f, 1f)] private float _lowStaminaRatioThreshold = 0.25f;

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
    public void move(float per, float speedMag, bool isDashing = false)
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

        ApplyStaminaChangeForMove(per, isDashing);
    }

    private void ApplyStaminaChangeForMove(float moveIntensity, bool isDashing)
    {
        if (_hpGauge == null)
        {
            return;
        }

        var myAvatar = _myFacade != null ? _myFacade.GetAvatar() : null;
        string myTag = myAvatar != null ? myAvatar.tag : string.Empty;
        bool hasAttackBuff = TeamFacade.Instance != null
            && TeamFacade.Instance.TeamState != null
            && TeamFacade.Instance.TeamState.HasAttackBuffByTag(myTag);
        if (hasAttackBuff)
        {
            return;
        }

        float deltaPerSec = ComputeStaminaChangePerSecond(isDashing);
        float value = moveIntensity * Time.deltaTime * Mathf.Abs(deltaPerSec);
        if (deltaPerSec < 0f)
        {
            _hpGauge.useHP(value);
        }
        else
        {
            _hpGauge.healHP(value);
        }
    }

    /// <summary>移動種別ごとのスタミナ変化量（+/秒=回復、-/秒=消費）。</summary>
    public static float ComputeStaminaChangePerSecond(bool isDashing)
    {
        return isDashing
            ? -ConstData.STAMINA_DASH_DRAIN_PER_SECOND
            : ConstData.STAMINA_NORMAL_MOVE_HEAL_PER_SECOND;
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
        float staminaMul = GetStaminaMoveSpeedMultiplier();
        Vector3 delta = _rb.transform.forward * per * speedMag * Time.deltaTime * speed * bubbleMul * staminaMul;

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

        if (_myFacade != null && _myFacade.IsGK())
        {
            keeperStand();
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

    /// <summary>GK 待機（Stand）。</summary>
    public void keeperStand()
    {
        if (AnimalAction_Special.IsSpecialActive)
        {
            return;
        }

        _animeChange.changeAnimation((int)AnimalAnime_State.KEEPER_ANIME_KIND.STAND);
        ApplyKeeperIdleStaminaHeal();
    }

    /// <summary>GK のゴールライン横移動（X 軸のみ）。direction の符号で左右。</summary>
    public void moveGoalkeeperLateral(float direction, float speedMag = -1f)
    {
        if (AnimalAction_Special.IsSpecialActive)
        {
            return;
        }

        if (Mathf.Abs(direction) <= 0.001f)
        {
            keeperStand();
            return;
        }

        if (speedMag < 0f)
        {
            speedMag = ResolveFieldMoveSpeedMagnitude();
        }

        float speed = 3.0f;
        float bubbleMul = IsSlowedBySharkBubble ? _sharkBubbleMoveSpeedMultiplier : 1f;
        float staminaMul = GetStaminaMoveSpeedMultiplier();
        float deltaX = Mathf.Sign(direction) * Mathf.Abs(direction) * speedMag * Time.deltaTime * speed * bubbleMul * staminaMul;

        Vector3 pos = _rb.transform.position;
        pos.x = Mathf.Clamp(pos.x + deltaX, -11.5f, 11.5f);
        _rb.transform.position = pos;

        var kind = direction < 0f
            ? AnimalAnime_State.KEEPER_ANIME_KIND.MOVE_L
            : AnimalAnime_State.KEEPER_ANIME_KIND.MOVE_R;
        _animeChange.changeAnimation((int)kind);
    }

    /// <summary>GK キャッチ（Ball_Catch）。</summary>
    public void keeperCatch()
    {
        if (AnimalAction_Special.IsSpecialActive)
        {
            return;
        }

        _animeChange.changeAnimation((int)AnimalAnime_State.KEEPER_ANIME_KIND.BALL_CATCH);
    }

    /// <summary>GK パリィ待機（Parry_Stand）。</summary>
    public void keeperParryStand()
    {
        if (AnimalAction_Special.IsSpecialActive)
        {
            return;
        }

        _animeChange.changeAnimation((int)AnimalAnime_State.KEEPER_ANIME_KIND.PARRY_STAND);
    }

    private float ResolveFieldMoveSpeedMagnitude()
    {
        AnimalInfo animalInfo = _myFacade != null ? _myFacade.GetAnimalInfo() : null;
        AnimalSpritInfo animalSpritInfo = _myFacade != null ? _myFacade.GetAnimalSpritInfo() : null;
        Param_SpritData paramSpritData = animalSpritInfo != null ? animalSpritInfo.ParamSpritData : null;
        float baseSpeed = paramSpritData != null ? paramSpritData.GetBaseParameterValue(Param_SpritData.ParameterType.Speed) : 0f;
        float increaseSpeed = paramSpritData != null ? paramSpritData.GetIncreaseParameterValue(Param_SpritData.ParameterType.Speed) : 0f;
        float speedStat = animalInfo != null ? animalInfo.Speed : 0f;
        return baseSpeed + (increaseSpeed * speedStat / 100.0f);
    }

    private void ApplyKeeperIdleStaminaHeal()
    {
        if (_hpGauge == null)
        {
            return;
        }

        float healValue = Time.deltaTime * ConstData.STAND_HEAL_PER_SECOND;
        _hpGauge.healHP(healValue);
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

    private float GetStaminaMoveSpeedMultiplier()
    {
        if (_hpGauge == null)
        {
            return 1f;
        }

        return ComputeStaminaMoveSpeedMultiplier(
            _hpGauge.StaminaRatio,
            _lowStaminaRatioThreshold,
            _exhaustedMoveSpeedMultiplier);
    }

    /// <summary>
    /// 残量が閾値以下のとき線形で減速（閾値=1.0、0=枯渇倍率）。
    /// </summary>
    public static float ComputeStaminaMoveSpeedMultiplier(
        float staminaRatio,
        float lowStaminaRatioThreshold,
        float exhaustedMoveSpeedMultiplier)
    {
        float threshold = Mathf.Max(0.001f, lowStaminaRatioThreshold);
        if (staminaRatio >= threshold)
        {
            return 1f;
        }

        if (staminaRatio <= 0f)
        {
            return exhaustedMoveSpeedMultiplier;
        }

        return Mathf.Lerp(exhaustedMoveSpeedMultiplier, 1f, staminaRatio / threshold);
    }
}
