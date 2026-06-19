using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ゲーム内情報表示クラス
public class DebugPlace_GameInfoDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _infoPanel; // 情報パネル
    [SerializeField] private TextMeshProUGUI _gameTimeText; // ゲーム時間テキスト
    [SerializeField] private TextMeshProUGUI _scoreText; // スコアテキスト
    
    [Header("Time Setting Buttons")]
    [SerializeField] private Button _addMinuteButton; // 分追加ボタン
    [SerializeField] private Button _addSecondButton; // 秒追加ボタン
    [SerializeField] private Button _subtractMinuteButton; // 分減算ボタン
    [SerializeField] private Button _subtractSecondButton; // 秒減算ボタン
    [SerializeField] private Button _resetTimeButton; // 時間リセットボタン
    
    [Header("Score Setting Buttons")]
    [SerializeField] private Button _addPlayerScoreButton; // プレイヤースコア追加ボタン
    [SerializeField] private Button _subtractPlayerScoreButton; // プレイヤースコア減算ボタン
    [SerializeField] private Button _addEnemyScoreButton; // 敵スコア追加ボタン
    [SerializeField] private Button _subtractEnemyScoreButton; // 敵スコア減算ボタン
    [SerializeField] private Button _resetScoreButton; // スコアリセットボタン
    
    [Header("Settings")]
    [SerializeField] private bool _showGameTime = true; // ゲーム時間を表示するか
    [SerializeField] private bool _showScore = true; // スコアを表示するか
    [SerializeField] private float _updateInterval = 0.1f; // 更新間隔（秒）
    
    // 内部変数
    private float _gameTime = 180f;
    private int _playerScore = 0;
    private int _enemyScore = 0;
    private bool _isGameRunning = false;
    private Coroutine _updateCoroutine;
    
    void Start()
    {
        // 常に表示
        if (_infoPanel != null)
        {
            _infoPanel.SetActive(true);
        }
        
        // 時間設定ボタンの初期設定
        SetupTimeButtons();
        
        // スコア設定ボタンの初期設定
        SetupScoreButtons();
        
        // ES3から初期化
        LoadInitialValuesFromES3();
        
        // 更新コルーチンを開始
        StartUpdateCoroutine();
    }
    
    void OnDestroy()
    {
        // 更新コルーチンを停止
        StopUpdateCoroutine();
    }
    
    /// <summary>
    /// 時間設定ボタンの初期設定
    /// </summary>
    private void SetupTimeButtons()
    {
        // 分追加ボタン
        if (_addMinuteButton != null)
        {
            _addMinuteButton.onClick.RemoveAllListeners();
            _addMinuteButton.onClick.AddListener(OnAddMinuteClicked);
        }
        
        // 秒追加ボタン
        if (_addSecondButton != null)
        {
            _addSecondButton.onClick.RemoveAllListeners();
            _addSecondButton.onClick.AddListener(OnAddSecondClicked);
        }
        
        // 分減算ボタン
        if (_subtractMinuteButton != null)
        {
            _subtractMinuteButton.onClick.RemoveAllListeners();
            _subtractMinuteButton.onClick.AddListener(OnSubtractMinuteClicked);
        }
        
        // 秒減算ボタン
        if (_subtractSecondButton != null)
        {
            _subtractSecondButton.onClick.RemoveAllListeners();
            _subtractSecondButton.onClick.AddListener(OnSubtractSecondClicked);
        }
        
        // 時間リセットボタン
        if (_resetTimeButton != null)
        {
            _resetTimeButton.onClick.RemoveAllListeners();
            _resetTimeButton.onClick.AddListener(OnResetTimeClicked);
        }
    }
    
    /// <summary>
    /// スコア設定ボタンの初期設定
    /// </summary>
    private void SetupScoreButtons()
    {
        // プレイヤースコア追加ボタン
        if (_addPlayerScoreButton != null)
        {
            _addPlayerScoreButton.onClick.RemoveAllListeners();
            _addPlayerScoreButton.onClick.AddListener(OnAddPlayerScoreClicked);
        }
        
        // プレイヤースコア減算ボタン
        if (_subtractPlayerScoreButton != null)
        {
            _subtractPlayerScoreButton.onClick.RemoveAllListeners();
            _subtractPlayerScoreButton.onClick.AddListener(OnSubtractPlayerScoreClicked);
        }
        
        // 敵スコア追加ボタン
        if (_addEnemyScoreButton != null)
        {
            _addEnemyScoreButton.onClick.RemoveAllListeners();
            _addEnemyScoreButton.onClick.AddListener(OnAddEnemyScoreClicked);
        }
        
        // 敵スコア減算ボタン
        if (_subtractEnemyScoreButton != null)
        {
            _subtractEnemyScoreButton.onClick.RemoveAllListeners();
            _subtractEnemyScoreButton.onClick.AddListener(OnSubtractEnemyScoreClicked);
        }
        
        // スコアリセットボタン
        if (_resetScoreButton != null)
        {
            _resetScoreButton.onClick.RemoveAllListeners();
            _resetScoreButton.onClick.AddListener(OnResetScoreClicked);
        }
    }
    
    /// <summary>
    /// 更新コルーチンを開始
    /// </summary>
    private void StartUpdateCoroutine()
    {
        StopUpdateCoroutine();
        _updateCoroutine = StartCoroutine(UpdateInfoCoroutine());
    }
    
    /// <summary>
    /// 更新コルーチンを停止
    /// </summary>
    private void StopUpdateCoroutine()
    {
        if (_updateCoroutine != null)
        {
            StopCoroutine(_updateCoroutine);
            _updateCoroutine = null;
        }
    }
    
    /// <summary>
    /// 情報更新コルーチン
    /// </summary>
    private IEnumerator UpdateInfoCoroutine()
    {
        while (true)
        {
            UpdateAllInfo();
            yield return new WaitForSeconds(_updateInterval);
        }
    }
    
    /// <summary>
    /// 全情報を更新
    /// </summary>
    private void UpdateAllInfo()
    {
        UpdateGameTime();
        UpdateScore();
    }
    
    /// <summary>
    /// ゲーム時間を更新
    /// </summary>
    private void UpdateGameTime()
    {
        if (_showGameTime && _gameTimeText != null)
        {
            if (_isGameRunning)
            {
                _gameTime += _updateInterval;
            }
            
            int minutes = Mathf.FloorToInt(_gameTime / 60f);
            int seconds = Mathf.FloorToInt(_gameTime % 60f);
            int milliseconds = Mathf.FloorToInt((_gameTime % 1f) * 100f);
            
            _gameTimeText.text = $"ゲーム時間: {minutes:00}:{seconds:00}.{milliseconds:00}";
        }
    }
    
    /// <summary>
    /// スコアを更新
    /// </summary>
    private void UpdateScore()
    {
        if (_showScore && _scoreText != null)
        {
            _scoreText.text = $"スコア: プレイヤー {_playerScore} - {_enemyScore} 敵";
        }
    }
    
    /// <summary>
    /// 分追加ボタンクリック時の処理
    /// </summary>
    private void OnAddMinuteClicked()
    {
        _gameTime += 60f; // 1分追加
        Debug.Log($"1分追加: {_gameTime}秒");
    }
    
    /// <summary>
    /// 秒追加ボタンクリック時の処理
    /// </summary>
    private void OnAddSecondClicked()
    {
        _gameTime += 1f; // 1秒追加
        Debug.Log($"1秒追加: {_gameTime}秒");
    }
    
    /// <summary>
    /// 分減算ボタンクリック時の処理
    /// </summary>
    private void OnSubtractMinuteClicked()
    {
        _gameTime -= 60f; // 1分減算
        _gameTime = Mathf.Max(1f, _gameTime); // 1秒未満にならないように制限
        Debug.Log($"1分減算: {_gameTime}秒");
    }
    
    /// <summary>
    /// 秒減算ボタンクリック時の処理
    /// </summary>
    private void OnSubtractSecondClicked()
    {
        _gameTime -= 1f; // 1秒減算
        _gameTime = Mathf.Max(1f, _gameTime); // 1秒未満にならないように制限
        Debug.Log($"1秒減算: {_gameTime}秒");
    }
    
    /// <summary>
    /// 時間リセットボタンクリック時の処理
    /// </summary>
    private void OnResetTimeClicked()
    {
        _gameTime = 180f;
        Debug.Log("時間をリセットしました");
    }
    
    /// <summary>
    /// プレイヤースコア追加ボタンクリック時の処理
    /// </summary>
    private void OnAddPlayerScoreClicked()
    {
        _playerScore += 1;
        Debug.Log($"プレイヤースコア追加: +1 (合計: {_playerScore})");
    }
    
    /// <summary>
    /// プレイヤースコア減算ボタンクリック時の処理
    /// </summary>
    private void OnSubtractPlayerScoreClicked()
    {
        _playerScore -= 1;
        _playerScore = Mathf.Max(0, _playerScore); // 0以下にならないように制限
        Debug.Log($"プレイヤースコア減算: -1 (合計: {_playerScore})");
    }
    
    /// <summary>
    /// 敵スコア追加ボタンクリック時の処理
    /// </summary>
    private void OnAddEnemyScoreClicked()
    {
        _enemyScore += 1;
        Debug.Log($"敵スコア追加: +1 (合計: {_enemyScore})");
    }
    
    /// <summary>
    /// 敵スコア減算ボタンクリック時の処理
    /// </summary>
    private void OnSubtractEnemyScoreClicked()
    {
        _enemyScore -= 1;
        _enemyScore = Mathf.Max(0, _enemyScore); // 0以下にならないように制限
        Debug.Log($"敵スコア減算: -1 (合計: {_enemyScore})");
    }
    
    /// <summary>
    /// スコアリセットボタンクリック時の処理
    /// </summary>
    private void OnResetScoreClicked()
    {
        _playerScore = 0;
        _enemyScore = 0;
        Debug.Log("スコアをリセットしました");
    }
    
    /// <summary>
    /// 時間を設定
    /// </summary>
    public void SetTime(float timeInSeconds)
    {
        _gameTime = Mathf.Max(1f, timeInSeconds);
        Debug.Log($"時間を設定しました: {_gameTime}秒");
    }
    
    /// <summary>
    /// 時間を設定（分:秒形式）
    /// </summary>
    public void SetTime(int minutes, int seconds)
    {
        _gameTime = Mathf.Max(1f, minutes * 60f + seconds);
        Debug.Log($"時間を設定しました: {minutes}分{seconds}秒 ({_gameTime}秒)");
    }
    
    /// <summary>
    /// 現在の時間を取得
    /// </summary>
    public float GetCurrentTime()
    {
        return _gameTime;
    }
    
    /// <summary>
    /// プレイヤースコアを取得
    /// </summary>
    public int GetPlayerScore()
    {
        return _playerScore;
    }
    
    /// <summary>
    /// 敵スコアを取得
    /// </summary>
    public int GetEnemyScore()
    {
        return _enemyScore;
    }
    
    /// <summary>
    /// 情報表示を表示/非表示切り替え
    /// </summary>
    public void ToggleInfo()
    {
        if (_infoPanel != null)
        {
            bool isActive = _infoPanel.activeSelf;
            _infoPanel.SetActive(!isActive);
            
            if (!isActive)
            {
                // 表示時に情報を更新
                UpdateAllInfo();
            }
        }
    }
    
    /// <summary>
    /// 情報表示を強制表示
    /// </summary>
    public void ShowInfo()
    {
        if (_infoPanel != null)
        {
            _infoPanel.SetActive(true);
            UpdateAllInfo();
        }
    }
    
    /// <summary>
    /// 情報表示を非表示
    /// </summary>
    public void HideInfo()
    {
        if (_infoPanel != null)
        {
            _infoPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// ゲーム開始
    /// </summary>
    public void StartGame()
    {
        _isGameRunning = true;
        Debug.Log("ゲームを開始しました");
    }
    
    /// <summary>
    /// ゲーム停止
    /// </summary>
    public void StopGame()
    {
        _isGameRunning = false;
        Debug.Log("ゲームを停止しました");
    }
    
    /// <summary>
    /// ゲームリセット
    /// </summary>
    public void ResetGame()
    {
        _isGameRunning = false;
        _gameTime = 180f;
        _playerScore = 0;
        _enemyScore = 0;
        UpdateAllInfo();
        Debug.Log("ゲームをリセットしました");
    }
    
    /// <summary>
    /// プレイヤースコアを追加
    /// </summary>
    public void AddPlayerScore(int points = 1)
    {
        _playerScore += points;
        Debug.Log($"プレイヤースコア追加: +{points} (合計: {_playerScore})");
    }
    
    /// <summary>
    /// 敵スコアを追加
    /// </summary>
    public void AddEnemyScore(int points = 1)
    {
        _enemyScore += points;
        Debug.Log($"敵スコア追加: +{points} (合計: {_enemyScore})");
    }
    
    /// <summary>
    /// 設定を更新
    /// </summary>
    public void UpdateSettings(bool showGameTime, bool showScore)
    {
        _showGameTime = showGameTime;
        _showScore = showScore;
        
        // 設定変更時に情報を更新
        UpdateAllInfo();
    }
    
    /// <summary>
    /// 更新間隔を設定
    /// </summary>
    public void SetUpdateInterval(float interval)
    {
        _updateInterval = Mathf.Max(0.01f, interval);
        StartUpdateCoroutine(); // コルーチンを再開始
    }

    /// <summary>
    /// ES3に保存された残り時間・スコアで初期化
    /// </summary>
    private void LoadInitialValuesFromES3()
    {
        try
        {
            // 残り時間
            float loadedTime = (float)ES3.Load<int>(DataKey.DATAKEY_GAME_INFO + DataKey.INT_REMAINING_GAME_TIME, ConstData.TIME_GAME);
            _gameTime = Mathf.Max(1f, loadedTime);
            
            // プレイヤースコア
            int loadedPlayer = ES3.Load<int>(DataKey.DATAKEY_GAME_INFO + DataKey.INT_TEAM_SCORE, _playerScore);
            _playerScore = Mathf.Max(0, loadedPlayer);
            
            // 敵スコア
            int loadedEnemy = ES3.Load<int>(DataKey.DATAKEY_GAME_INFO + DataKey.INT_ENEMY_SCORE, _enemyScore);
            _enemyScore = Mathf.Max(0, loadedEnemy);
            
            // 表示更新
            UpdateAllInfo();
            
            Debug.Log($"ES3初期化: time={_gameTime:F1}, player={_playerScore}, enemy={_enemyScore}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("ES3からの初期化に失敗しました: " + e.Message);
        }
    }
}
