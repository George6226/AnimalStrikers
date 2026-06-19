using UnityEngine;

// プレイヤーのキー入力を処理するクラス
public class AnimalInputKey_Player : MonoBehaviour
{
    [SerializeField] private AnimalInputHandler _playerInputHandler;
    [SerializeField] private ActionButtonsViewSwitcher _actionButtonsViewSwitcher;

    // Update is called once per frame
    private void Update()
    {
#if UNITY_EDITOR
        // ゲーム中以外
        if (!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME)) return;

        // アニマルを取得(プレイヤー)
        var animal = TeamFacade.Instance.AnimalSelectorManager.GetSelectAnimal(ConstData.PLAYER_TAG);
        if (animal == null) return;

        // ボタン入力
        ButtonInput();
#endif
    }

    private void ButtonInput()
    {
        // ダッシュ
        if (Input.GetKeyDown(KeyCode.A))
        {
            _playerInputHandler.OnButtonPressed((int)AnimalButtonType.DashDown);
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            _playerInputHandler.OnButtonPressed((int)AnimalButtonType.DashUp);
        }

        // パス or スライディング（ボール保持状況で切り替え）
        if (Input.GetKeyDown(KeyCode.S))
        {
            AnimalButtonType buttonType = _actionButtonsViewSwitcher.IsHasBall
                ? AnimalButtonType.Pass
                : AnimalButtonType.Sliding;
            _playerInputHandler.OnButtonPressed((int)buttonType);
        }

        // シュート or アタック（ボール保持状況で切り替え）
        if (Input.GetKeyDown(KeyCode.D))
        {
            AnimalButtonType buttonType = _actionButtonsViewSwitcher.IsHasBall
                ? AnimalButtonType.Shoot
                : AnimalButtonType.Attack;
            _playerInputHandler.OnButtonPressed((int)buttonType);
        }

        // スペシャル
        if (Input.GetKeyDown(KeyCode.W))
        {
            _playerInputHandler.OnButtonPressed((int)AnimalButtonType.Special);
        }
    }
}

