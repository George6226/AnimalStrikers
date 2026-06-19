using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A*のためのノード
public class Node
{
    // Nodeの親
    private Node _parent;
    public Node Parent
    {
        get { return _parent; }
    }
    // 実行アクション
    private GoapActionSO _action;
    public GoapActionSO GoapAction
    {
        get { return _action; }
    }
    // コスト
    private float _cost;
    public float Cost
    {
        get { return _cost; }
    }
    // 状態リスト
    private Dictionary<GoapCondition, bool> _state;
    public Dictionary<GoapCondition, bool> State
    {
        get { return _state; }
    }

    // コンストラクタ
    public Node(Node parent, GoapActionSO action, float cost, Dictionary<GoapCondition, bool> state)
    {
        this._parent = parent;
        this._action = action;
        this._cost = cost;
        this._state = state;
    }
}
