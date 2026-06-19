using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// チーム単位の情報や操作への窓口。
/// 個々のアニマルは AnimalFacade からアクセスし、チーム全体の情報はこのクラスからまとめて取得する想定。
/// </summary>
public class TeamFacade : MonoBehaviour
{
    private static TeamFacade _instance;
    public static TeamFacade Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TeamFacade>();
            }
            return _instance;
        }
    }

    [SerializeField] private TeamRegistar _teamRegistar;
    public TeamRegistar TeamRegist
    {
        get { return _teamRegistar; }
    }

    // 選択中アニマル関連の窓口（プレイヤーとNPCのセレクターを管理）
    [SerializeField] private AnimalSelector_Manager _animalSelectorManager;
    public AnimalSelector_Manager AnimalSelectorManager
    {
        get { return _animalSelectorManager; }
    }

    // チーム全体の状態共有用ブラックボード
    [SerializeField] private TeamBlackboard _teamBlackboard;
    public TeamBlackboard TeamBlackboard
    {
        get { return _teamBlackboard; }
    }

    // チーム全体の状態
    [SerializeField] private TeamState _teamState;
    public TeamState TeamState
    {
        get { return _teamState; }
    }

    // ボール管理への窓口
    [SerializeField] private BallManager _ballManager;
    public BallManager BallManager
    {
        get { return _ballManager; }
    }

    // フィールドオブジェクト（ゴールなど）への窓口
    [SerializeField] private FieldObject_Handler _fieldObjectHandler;
    public FieldObject_Handler FieldObjectHandler
    {
        get { return _fieldObjectHandler; }
    }

    // 自チームの操作割当（人間1 + 味方NPC2 + GK NPC）
    [SerializeField] private SquadControlController _squadControl;
    public SquadControlController SquadControl
    {
        get { return _squadControl; }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;

        // 参照が未設定ならシーンから補完
        if (_teamRegistar == null)
        {
            _teamRegistar = FindObjectOfType<TeamRegistar>();
        }
        if (_animalSelectorManager == null)
        {
            _animalSelectorManager = FindObjectOfType<AnimalSelector_Manager>();
        }
        if (_teamBlackboard == null)
        {
            _teamBlackboard = FindObjectOfType<TeamBlackboard>();
        }
        if (_teamState == null)
        {
            _teamState = FindObjectOfType<TeamState>();
        }
        if (_ballManager == null)
        {
            _ballManager = FindObjectOfType<BallManager>();
        }
        if (_fieldObjectHandler == null)
        {
            _fieldObjectHandler = FindObjectOfType<FieldObject_Handler>();
        }
        if (_squadControl == null)
        {
            _squadControl = FindObjectOfType<SquadControlController>();
        }
    }
}

