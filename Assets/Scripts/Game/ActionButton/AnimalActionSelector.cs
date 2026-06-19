using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AnimalInputHandlerからデータを受け取り、適切なAnimalAction_*クラスを選択して実行するクラス
public class AnimalActionSelector : MonoBehaviour
{
    [System.Serializable]
    private struct ActionBinding
    {
        public AnimalButtonType Type;        // ボタン種別（bitフラグとしてintで表現）
        public AnimalAction_Base Action;     // 対応するアクション
    }

    [Header("参照")]
    // ボタン種別とアクションの対応（Inspectorであらかじめ設定）
    [SerializeField] private List<ActionBinding> _actions = new List<ActionBinding>();

    // ボタン種別からアクションを取得するヘルパー（bit演算で検索）
    private AnimalAction_Base FindAction(AnimalButtonType type)
    {
        int typeBit = 1 << (int)type;

        for (int i = 0; i < _actions.Count; i++)
        {
            // ActionBindingのTypeはenum値なので、bitフラグに変換
            int actionTypeBit = 1 << (int)_actions[i].Type;
            // bit演算でマッチング（typeBitがactionTypeBitに含まれているかチェック）
            if ((actionTypeBit & typeBit) != 0)
            {
                return _actions[i].Action;
            }
        }
        return null;
    }

    /// <summary>
    /// AnimalInputHandlerから呼び出される、ボタンタイプに応じて適切なアクションを選択・実行する
    /// </summary>
    /// <param name="buttonType">ボタンの種類</param>
    public void ExecuteAction(AnimalButtonType buttonType)
    {
        Debug.Log($"[AnimalActionSelector] ExecuteAction: {buttonType}");
        // ダッシュ関連の処理
        if (buttonType == AnimalButtonType.DashDown || buttonType == AnimalButtonType.DashUp)
        {
            var dashAction = FindAction(buttonType) as AnimalAction_Dash;
            if (dashAction != null)
            {
                dashAction.SetDash(buttonType == AnimalButtonType.DashDown);
                return;
            }
        }

        // 対応するアクションを取得
        var action = FindAction(buttonType);
        if (action != null)
        {
            Debug.Log("アクション実行:"+action);
            action.Execute();
        }
        else
        {
            Debug.LogWarning($"[AnimalActionSelector] {buttonType} に対応するアクションが見つかりません。");
        }
    }

    /// <summary>
    /// AnimalInputHandlerから呼び出される、Moveボタン用の拡張版
    /// slidePadのscaleとradianを一緒に渡して実行する
    /// </summary>
    public void ExecuteMoveAction(float slideScale, float radian)
    {
        var action = FindAction(AnimalButtonType.Move);
        if (action != null)
        {
            var moveAction = action as AnimalAction_Move;
            if (moveAction == null)
            {
                Debug.LogWarning("[AnimalActionSelector] Move用アクションがAnimalAction_Moveではありません。");
                return;
            }

            // Moveアクション側にscaleとradianを設定してからExecuteを実行
            moveAction.SetSlideValues(slideScale, radian);
            moveAction.Execute();
        }
        else
        {
            Debug.LogWarning("[AnimalActionSelector] Moveに対応するアクションが見つかりません。");
        }
    }

    /// <summary>
    /// Specialアクションが実行可能かを返す
    /// </summary>
    public bool CanExecuteSpecial()
    {
        var action = FindAction(AnimalButtonType.Special);
        if (action == null)
        {
            Debug.LogWarning("[AnimalActionSelector] Specialに対応するアクションが見つかりません。");
            return false;
        }

        var specialAction = action as AnimalAction_Special;
        if (specialAction == null)
        {
            Debug.LogWarning("[AnimalActionSelector] Special用アクションがAnimalAction_Specialではありません。");
            return false;
        }

        return specialAction.CanExecuteSpecial();
    }
}
