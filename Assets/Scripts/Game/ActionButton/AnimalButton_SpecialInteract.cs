using UnityEngine;
using UnityEngine.UI;

// スペシャルボタンの有効無効を管理するクラス
public class AnimalButton_SpecialInteract : MonoBehaviour
{
    [Header("参照")]
    
    [SerializeField] private Button _specialButton;

    // 現在選択されているキャラを保持
    private AnimalFacade _currentSelectedAnimal;
    private AnimalFacade _previousSelectedAnimal;
    private AnimalSelector_Manager _animalSelector;

    private void Start()
    {
        // TeamFacade 経由で AnimalSelector を補完
        if (_animalSelector == null && TeamFacade.Instance != null)
        {
            _animalSelector = TeamFacade.Instance.AnimalSelectorManager;
        }
    }

    private void Update()
    {
        if (_animalSelector == null)
        {
            SetSpecialButtonInteractable(false);
            return;
        }

        _currentSelectedAnimal = _animalSelector.GetSelectAnimal(ConstData.PLAYER_TAG);

        // キャラが変更されたら即座に更新
        if (_currentSelectedAnimal != _previousSelectedAnimal)
        {
            _previousSelectedAnimal = _currentSelectedAnimal;
            UpdateSpecialButtonState();
            return;
        }

        // ゲージや状態変化に追従するため毎フレーム評価
        UpdateSpecialButtonState();
    }

    private void UpdateSpecialButtonState()
    {
        if (_currentSelectedAnimal == null)
        {
            SetSpecialButtonInteractable(false);
            return;
        }

        var actionSelector = _currentSelectedAnimal.GetActionSelector();
        if (actionSelector == null)
        {
            SetSpecialButtonInteractable(false);
            return;
        }

        SetSpecialButtonInteractable(actionSelector.CanExecuteSpecial());
    }

    private void SetSpecialButtonInteractable(bool isInteractable)
    {
        if (_specialButton == null)
        {
            Debug.LogWarning("[AnimalButton_SpecialInteract] Special Button が設定されていません。");
            return;
        }

        _specialButton.interactable = isInteractable;
    }
}
