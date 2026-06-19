using UnityEngine;

// NPCのキー入力を処理するクラス
public class AnimalInputKey_NPC : MonoBehaviour
{
    [SerializeField] private AnimalInputHandler_NPC _npcInputHandler;

    // ボールを所持しているか
    private bool _isHasBall = false;

    // Update is called once per frame
    void Update()
    {
        // Editorのみ
#if UNITY_EDITOR
        // ゲーム中以外
        if(!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME)) return;

        // アニマルを取得(NPC)
        var animal = TeamFacade.Instance.AnimalSelectorManager.GetSelectAnimal(ConstData.NPC_TAG);
        if(animal == null) return;

        // 移動入力
        moveInput();

        // ボタン入力
        buttonInput();
#endif
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
        bool has = state != null && state.BelongTeam == BallManager_State.BELONG_TEAM.ENEMY;
        _isHasBall = has;
    }

    // 矢印キーによる移動入力を取得
    private void moveInput()
    {
        float slideScale = 0.0f;
        float radian = 0.0f;
        float moveX = 0.0f;
        float moveZ = 0.0f;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            moveX -= 1.0f;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            moveX += 1.0f;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            moveZ -= 1.0f;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            moveZ += 1.0f;
        }

        if(moveX != 0 || moveZ != 0){
            slideScale = 1.0f;
            radian = Mathf.Atan2(moveZ, moveX);
        }
        else{
            slideScale = 0.0f;
            radian = 0.0f;
        }

        _npcInputHandler.SlideScale = slideScale;
        _npcInputHandler.Radian = radian;
    }

    private void buttonInput()
    {
        // ダッシュ
        if(Input.GetKeyDown(KeyCode.J))
        {
            _npcInputHandler.OnButtonPressed((int)AnimalButtonType.DashDown);
        }
        else if(Input.GetKeyUp(KeyCode.J))
        {
            _npcInputHandler.OnButtonPressed((int)AnimalButtonType.DashUp);
        }

        // パス
        if(Input.GetKeyDown(KeyCode.K))
        {
            AnimalButtonType buttonType = _isHasBall ? AnimalButtonType.Pass : AnimalButtonType.Sliding;
            _npcInputHandler.OnButtonPressed((int)buttonType);
        }
        // シュート
        if(Input.GetKeyDown(KeyCode.L))
        {
            AnimalButtonType buttonType = _isHasBall ? AnimalButtonType.Shoot : AnimalButtonType.Attack;
            _npcInputHandler.OnButtonPressed((int)buttonType);
        }
        // スペシャル
        if(Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("NPCスペシャルボタン[I]");
            _npcInputHandler.OnButtonPressed((int)AnimalButtonType.Special);
        }
    }
}
