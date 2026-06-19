using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalUniformChanger : MonoBehaviour
{
    // ユニフォームリスト
    [SerializeField] private UniformList _uniformList;

    // ユニフォームのタイプ
    private int _uniformType = -1;

    private IEnumerator Start()
    {
        // ユニフォームのタイプがある & タグが設定されているまで待機
        yield return new WaitUntil(() => (_uniformType >= 0 && _uniformList.transform.parent.gameObject.tag != "Untagged"));

        if (_uniformList != null)
        {
            bool isBlue = (_uniformList.transform.parent.gameObject.tag == ConstData.PLAYER_TAG);
            _uniformList.SetTeamColor(isBlue);
            _uniformList.ChangeUniform(_uniformType);
        }
    }

    // ユニフォームの種類を変更する
    public void setUniformType(int uniformType)
    {
        // ユニフォームの種類を設定
        _uniformType = uniformType;
    }
}
