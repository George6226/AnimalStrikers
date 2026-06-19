using System.Collections;
using UnityEngine;

// ゴリラのスペシャルアクション
public class GorillaSpecialAction : AnimalSpecialActionBase
{
    [SerializeField] private AnimalFacade _myFacade;
    private string _buffTeamTag = ConstData.ENEMY_TAG;

    private void Awake()
    {
        if (_myFacade == null)
        {
            _myFacade = GetComponentInParent<AnimalFacade>();
        }
    }

    public override bool CanExecuteSpecial()
    {
        return true;
    }

    [SerializeField] private float _idleDuration = 1.5f; // 移動せず待機する時間
    private Coroutine _idleRoutine;

    public override void ExecuteSpecial()
    {
        // 移動せず待機するだけ（アニメーション終了は AnimalSpecialCallback 側で onSpecialFinished()）
        if (_idleRoutine != null)
        {
            StopCoroutine(_idleRoutine);
            _idleRoutine = null;
        }

        _idleRoutine = StartCoroutine(idleRoutine());
    }

    private IEnumerator idleRoutine()
    {
        yield return new WaitForSeconds(_idleDuration);
        _idleRoutine = null;
    }

    public override void SetEffectCallback(GameObject effect)
    {
        if (effect == null)
        {
            return;
        }

        var buff = effect.GetComponent<SpecialAttackBuffEffect>();
        if (buff == null)
        {
            buff = effect.GetComponentInChildren<SpecialAttackBuffEffect>();
        }
        if (buff == null)
        {
            return;
        }

        var avatar = _myFacade != null ? _myFacade.GetAvatar() : null;
        string tag = avatar != null ? avatar.tag : string.Empty;
        if (tag == ConstData.PLAYER_TAG)
        {
            _buffTeamTag = ConstData.PLAYER_TAG;
        }
        else
        {
            _buffTeamTag = ConstData.ENEMY_TAG;
        }

        TeamFacade.Instance.TeamState.SetAttackBuffByTag(_buffTeamTag, true);
        buff.SetCallbackTarget(this);
    }

    public override void callBackEffect()
    {
        TeamFacade.Instance.TeamState.SetAttackBuffByTag(_buffTeamTag, false);
    }
}
