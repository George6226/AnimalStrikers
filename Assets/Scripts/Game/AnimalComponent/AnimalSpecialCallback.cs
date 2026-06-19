using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// スペシャルアニメーションのコールバック
public class AnimalSpecialCallback : MonoBehaviour
{
    [SerializeField] private AnimalAction_Special _specialAction;

    // スペシャルアニメーション終了時のコールバック
    public void AnimEvent_SpecialEnd()
    {
        if (_specialAction != null)
        {   
            // スペシャルアクションの終了処理を呼び出す
            _specialAction.onSpecialFinished();
            
            Debug.Log("スペシャルアニメーション終了");
        }
    }
}
