using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun;

// アクションボタンの表示切り替えを行うクラス
public class ActionButtonsViewSwitcher : MonoBehaviour
{
    private void Awake()
    {
        // 最初はボタンを不可状態に
        changeButtons(false);
    }

    // ボール所持中のボタン/ぼーじ未所持中のボタン
    [SerializeField] private GameObject _hasBallButtons;
    [SerializeField] private GameObject _dontHasBallButtons;

    // ボタンの状態
    private bool _isHasBall = false;
    public bool IsHasBall
    {
        get { return _isHasBall; }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // ゲーム中以外
        if (!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME)){
            return;
        }
        // チームがボールを所持しているか
        var teamFacade = TeamFacade.Instance;
        var ballManager = teamFacade != null ? teamFacade.BallManager : null;
        var state = ballManager != null ? ballManager.State : null;
        bool has = state != null && state.BelongTeam == BallManager_State.BELONG_TEAM.PLAYER;
        changeButtons(has);
    }

    private void changeButtons(bool has)
    {
        _hasBallButtons.SetActive(has);
        _dontHasBallButtons.SetActive(!has);
        _isHasBall = has;
    }
}
