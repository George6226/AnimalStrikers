using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アニマルコンポーネントの管理
public class AnimalComponentManager : MonoBehaviour
{
    // アニマルの操作
    [SerializeField] private AnimalHandler _animal;
    public AnimalHandler Animal
    {
        get { return _animal; }
    }

    // GKかどうか
    [SerializeField] private bool _isGK = false;
    public bool IsGK
    {
        get { return _isGK; }
    }

    // ボールの保持位置
    [SerializeField] private GameObject _ballKeep;
    public GameObject BallKeep
    {
        get { return _ballKeep; }
    }

    // ユニフォームの変更
    [SerializeField] private AnimalUniformChanger _uniformChanger;
    public AnimalUniformChanger UniformChanger
    {
        get { return _uniformChanger; }
    }

    // ユニフォームリスト
    [SerializeField] private UniformList _uniformList;
    public UniformList Uniform
    {
        get{return _uniformList;}
    }

    // アクションセレクター
    [SerializeField] private AnimalActionSelector _actionSelector;
    public AnimalActionSelector ActionSelector
    {
        get { return _actionSelector; }
    }
}
