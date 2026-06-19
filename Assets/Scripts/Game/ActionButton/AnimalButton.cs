using UnityEngine;
using UnityEngine.UI;

// UIボタンとAnimalInputHandlerをつなぐラッパーコンポーネント
public class AnimalButton : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private AnimalInputHandler _inputHandler;

    [Header("このボタンの種類")]
    [SerializeField] private AnimalButtonType _buttonType;

    private void Reset()
    {
        // 同じオブジェクト、またはシーン内から自動取得
        if (_inputHandler == null)
        {
            _inputHandler = FindObjectOfType<AnimalInputHandler>();
        }
    }

    // UnityのButtonコンポーネントのOnClickから呼ぶ
    public void OnClick()
    {
        if (_inputHandler == null)
        {
            Debug.LogWarning("[AnimalButton] AnimalInputHandler が設定されていません。");
            return;
        }

        // enumをintにキャストしてAnimalInputHandlerへ渡す
        _inputHandler.OnButtonPressed((int)_buttonType);
    }
}

