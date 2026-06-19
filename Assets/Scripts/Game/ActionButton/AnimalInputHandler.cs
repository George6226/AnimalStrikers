using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// ボタンの種類
public enum AnimalButtonType
{
    Pass = 0,
    Shoot = 1,
    Sliding = 2,
    Special = 3,
    Attack = 4,
    DashDown = 5,
    DashUp = 6,
    Move = 7,   // 移動アクション用（現状ボタンからは未使用）
}

// ボタンやスライドパッドによる入力をAnimalSelectorによって選択されたキャラに伝えるクラス
public class AnimalInputHandler : MonoBehaviour
{
    [Header("参照")]
    // スライドパッド
    [SerializeField] private UI_SlidePad _slidePad;
    // 動物の選択（TeamFacade 経由で取得可能）
    protected AnimalSelector_Manager _animalSelector;

    // 前回の選択キャラクター（変更検知用、Facade 経由）
    protected AnimalFacade _previousSelectedAnimal = null;

    private void Start()
    {
        // TeamFacade 経由で AnimalSelector を補完
        if (_animalSelector == null && TeamFacade.Instance != null)
        {
            _animalSelector = TeamFacade.Instance.AnimalSelectorManager;
        }
    }

    // タグを取得
    protected virtual string getTag()
    {
        return ConstData.PLAYER_TAG;
    }

    // 定期的Update（物理演算と同期）
    private void FixedUpdate()
    {
        // ゲーム中以外か?
        if (!StateManager.Instance.isSameKind(StateManager.STATE_KIND.GAME)){
            return;
        }
        // 選択されたキャラクターがない場合は処理しない
        if (_animalSelector == null || _animalSelector.GetSelectAnimal(getTag()) == null){
            return;
        }

        // 選択キャラクターが変更された場合の処理
        if (_previousSelectedAnimal != _animalSelector.GetSelectAnimal(getTag()))
        {
            _previousSelectedAnimal = _animalSelector.GetSelectAnimal(getTag());
        }
        
        var actionSelector = GetActionSelector();
        if (actionSelector == null)
        {
            return;
        }
        
        var (slideScale, radian) = getMoveActionValues();
        actionSelector.ExecuteMoveAction(slideScale, radian);
    }

    protected virtual (float, float) getMoveActionValues()
    {
        if (_slidePad == null){
            Debug.LogWarning("[AnimalInputHandler] UI_SlidePad が設定されていません。");
            return (0.0f, 0.0f);
        }

        float slideScale = _slidePad.SlideScale;
        float radian = _slidePad.Radian;
        return (slideScale, radian);
    }

    /// <summary>
    /// AnimalFacade から AnimalActionSelector を取得
    /// </summary>
    /// <returns>AnimalActionSelector。取得できない場合はnull</returns>
    private AnimalActionSelector GetActionSelector()
    {
        if (_animalSelector == null || _animalSelector.GetSelectAnimal(getTag()) == null)
        {
            Debug.LogWarning("[AnimalInputHandler] AnimalSelector または SelAnimalFacade が設定されていません。");
            return null;
        }

        // AnimalFacade から AnimalActionSelector を取得
        var actionSelector = _animalSelector.GetSelectAnimal(getTag()).GetActionSelector();
        if (actionSelector == null)
        {
            Debug.LogWarning("[AnimalInputHandler] AnimalActionSelector が選択されたキャラクターに設定されていません。");
            return null;
        }
        return actionSelector;
    }

    /// <summary>
    /// ボタン押下時にenum(int)が渡される共通ハンドラ
    /// UnityEventのint引数に紐づけて使用する
    /// AnimalSelector経由でAnimalActionSelectorに処理を委譲する
    /// </summary>
    /// <param name="buttonTypeInt">AnimalButtonTypeをintにキャストした値</param>
    public void OnButtonPressed(int buttonTypeInt)
    {
        AnimalButtonType buttonType = (AnimalButtonType)buttonTypeInt;

        // AnimalSelectorから選択されたキャラクターのFacadeを取得
        if (_animalSelector == null || _animalSelector.GetSelectAnimal(getTag()) == null)
        {
            Debug.LogWarning("[AnimalInputHandler] AnimalSelector または SelAnimalFacade が設定されていません。");
            return;
        }

        // AnimalComponentManagerからAnimalActionSelectorを取得
        var actionSelector = GetActionSelector();
        if (actionSelector == null)
        {
            return;
        }
        // DashDown / DashUp を含む全てのアクションはセレクタ側に委譲
        actionSelector.ExecuteAction(buttonType);
    }
}