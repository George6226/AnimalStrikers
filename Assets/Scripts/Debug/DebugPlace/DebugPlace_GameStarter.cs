using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;

// デバッグ用のゲームスタート管理クラス
public class DebugPlace_GameStarter : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button _startGameButton; // ゲームスタートボタン
    
    [Header("Scene Settings")]
    [SerializeField] private SceneFader _sceneFader; // シーンフェーダー
    [SerializeField] private string _targetSceneName = "GameScene"; // 遷移先シーン名
    
    [Header("Debug Data")]
    [SerializeField] private DebugPlace_GameInfoDisplay _gameInfoDisplay; // ゲーム情報表示クラス
    [SerializeField] private DebugPlace_CharaMarkerCreator _charaMarkerCreator; // キャラマーカークリエーター
    
    void Start()
    {
        // ボタンの初期設定
        SetupButtons();
    }
    
    /// <summary>
    /// ボタンの初期設定
    /// </summary>
    private void SetupButtons()
    {
        // スタートボタン
        if (_startGameButton != null)
        {
            _startGameButton.onClick.RemoveAllListeners();
            _startGameButton.onClick.AddListener(OnStartGameClicked);
        }
    }
    
    /// <summary>
    /// スタートボタンクリック時の処理
    /// </summary>
    private void OnStartGameClicked()
    {
        TransitionToGameScene();
    }
    
    /// <summary>
    /// GameSceneに遷移
    /// </summary>
    private void TransitionToGameScene()
    {
        // デバッグデータを保存
        SaveDebugData();

        // NPCとの対戦を設定
        if (PhotonNetwork.LocalPlayer != null)
        {
            PhotonNetwork.LocalPlayer.setBattleMode(ConstData.BATTLE_MODE.NPC);
            // プレイヤー情報を初期化
            PhotonPlayerInfo.Instance.Initialize(PhotonNetwork.LocalPlayer);
        }
        
        if (_sceneFader != null)
        {
            // SceneFaderを使用してシーン遷移
            _sceneFader.changeScene(_targetSceneName);
        }
        else
        {
            Debug.LogWarning("SceneFaderが見つかりません。直接シーン遷移します。");
            SceneManager.LoadScene(_targetSceneName);
        }
        
        Debug.Log($"GameSceneに遷移します: {_targetSceneName}");
    }
    
    /// <summary>
    /// デバッグデータをEasySaveで保存
    /// </summary>
    private void SaveDebugData()
    {
        try
        {
            // ゲーム情報表示からデータを取得
            if (_gameInfoDisplay != null)
            {
                // 残り試合時間を保存
                float gameTime = _gameInfoDisplay.GetCurrentTime();
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.INT_REMAINING_GAME_TIME, (int)gameTime);
                Debug.Log($"残り試合時間を保存: {gameTime}秒");
                
                // プレイヤースコアを保存
                int playerScore = _gameInfoDisplay.GetPlayerScore();
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.INT_TEAM_SCORE, playerScore);
                Debug.Log($"プレイヤースコアを保存: {playerScore}");
                
                // 敵スコアを保存
                int enemyScore = _gameInfoDisplay.GetEnemyScore();
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.INT_ENEMY_SCORE, enemyScore);
                Debug.Log($"敵スコアを保存: {enemyScore}");
                
                // キャラクタ情報を取得・保存（PLAYER側とNPC側を分けて保存）
                var playerData = GetCharacterDataByTeam(0); // チーム0 = PLAYER側
                var npcData = GetCharacterDataByTeam(1);    // チーム1 = NPC側
                
                // PLAYER側のデータを保存
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.LIST_VECTOR3_CHARACTER_POSITION_PLAYER, playerData.positions);
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.ARRAY_INT_TEAM_FORMATION_PLAYER, playerData.names);
                Debug.Log($"PLAYER側キャラクタ配置リストを保存: {playerData.positions.Count}個");
                Debug.Log($"PLAYER側キャラクタ名リストを保存: {playerData.names.Count}個");
                
                // NPC側のデータを保存
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.LIST_VECTOR3_CHARACTER_POSITION_NPC, npcData.positions);
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.ARRAY_INT_TEAM_FORMATION_NPC, npcData.names);
                Debug.Log($"NPC側キャラクタ配置リストを保存: {npcData.positions.Count}個");
                Debug.Log($"NPC側キャラクタ名リストを保存: {npcData.names.Count}個");
                
                // ボールの位置と所持者を保存（PLAYER側は0~3、NPC側は4~7）
                Vector3 ballPosition = playerData.ballPosition != Vector3.zero ? playerData.ballPosition : npcData.ballPosition;
                int ballOwnerIndex = -1;
                if (playerData.ballOwnerIndex >= 0)
                {
                    // PLAYER側: 0~3のまま
                    ballOwnerIndex = playerData.ballOwnerIndex;
                }
                else if (npcData.ballOwnerIndex >= 0)
                {
                    // NPC側: 0~3を4~7に変換
                    ballOwnerIndex = npcData.ballOwnerIndex + 4;
                }
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.VECTOR3_BALL_POSITION, ballPosition);
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.INT_BALL_OWNER, ballOwnerIndex);
                Debug.Log($"ボールの位置を保存: ({ballPosition.x:F1}, {ballPosition.y:F1}, {ballPosition.z:F1})");
                Debug.Log($"ボールの所持者を保存: インデックス{ballOwnerIndex}");
            }
            else
            {
                Debug.LogWarning("GameInfoDisplayが見つかりません。デフォルト値を保存します。");
                
                // デフォルト値を保存
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.INT_REMAINING_GAME_TIME, ConstData.TIME_GAME);
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.INT_TEAM_SCORE, 0);
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.INT_ENEMY_SCORE, 0);
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.VECTOR3_BALL_POSITION, Vector3.zero);
                ES3.Save(DataKey.DATAKEY_GAME_INFO+DataKey.INT_BALL_OWNER, -1);
            }
            
            Debug.Log("デバッグデータを保存しました: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (System.Exception e)
        {
            Debug.LogError("デバッグデータの保存に失敗しました: " + e.Message);
        }
    }
    
    /// <summary>
    /// 指定チームのキャラクタデータを取得
    /// </summary>
    /// <param name="team">チーム番号（0=PLAYER側, 1=NPC側, -1=ボール）</param>
    private CharacterData GetCharacterDataByTeam(int team)
    {
        var characterData = new CharacterData
        {
            positions = new List<Vector3>(),
            names = new List<Param_AnimalInfo.AnimalType>(),
            ballPosition = Vector3.zero,
            ballOwnerIndex = -1
        };
        
        if (_charaMarkerCreator != null)
        {
            var markers = _charaMarkerCreator.GetCreatedMarkers();
            int characterIndex = 0;
            
            foreach (var marker in markers)
            {
                if (marker != null)
                {
                    int markerTeam = marker.GetTeam();
                    
                    // 指定チームのマーカーのみ処理（ボールは除外）
                    if (markerTeam != team)
                    {
                        // ボールの位置情報のみ取得（チーム-1）
                        if (markerTeam == -1 && marker.AnimalType == Param_AnimalInfo.AnimalType.Ball)
                        {
                            Vector3 ballCanvasPosition = marker.transform.localPosition;
                            Vector3 ballPosition = ConvertMarkerLocalToGamePosition(ballCanvasPosition);
                            characterData.ballPosition = ballPosition;
                            Debug.Log($"ボールの位置を設定: 位置({ballPosition.x:F1}, {ballPosition.y:F1}, {ballPosition.z:F1})");
                        }
                        continue;
                    }
                    
                    // 位置を取得
                    Vector3 canvasPosition = marker.transform.localPosition;
                    Vector3 position = ConvertMarkerLocalToGamePosition(canvasPosition);
                    characterData.positions.Add(position);
                    
                    // AnimalTypeを取得
                    var animalType = marker.AnimalType;
                    characterData.names.Add(animalType);
                    
                    // ボール所持状態をチェック
                    if (marker.HasBall())
                    {
                        characterData.ballOwnerIndex = characterIndex;
                        characterData.ballPosition = position;
                        Debug.Log($"ボール所持者発見（チーム{team}）: {marker.Name}, インデックス{characterIndex}, 位置({position.x:F1}, {position.y:F1}, {position.z:F1})");
                    }
                    
                    Debug.Log($"キャラクタ情報取得（チーム{team}）: {marker.Name}, 位置({position.x:F1}, {position.y:F1}, {position.z:F1}), 種類{animalType}, ボール所持{marker.HasBall()}");
                    characterIndex++;
                    
                    // 4つまでに制限（1チームのキャラクター数）
                    if (characterIndex >= 4)
                    {
                        break;
                    }
                }
            }
            
            // ボール所持者が見つからない場合
            if (characterData.ballOwnerIndex == -1)
            {
                Debug.Log($"チーム{team}のボール所持者が見つかりませんでした。");
            }
        }
        else
        {
            Debug.LogWarning("CharaMarkerCreatorが見つかりません。空のリストを保存します。");
        }
        
        return characterData;
    }
    
    /// <summary>
    /// DebugPlace上のローカル座標をゲーム座標へ変換する
    /// X(±800) -> Z(±20), Y(±300) -> X(±7)。Y(高さ)は0。
    /// </summary>
    private Vector3 ConvertMarkerLocalToGamePosition(Vector3 localPosition)
    {
        float clampedX = Mathf.Clamp(localPosition.x, -800f, 800f);
        float clampedY = Mathf.Clamp(localPosition.y, -300f, 300f);
        float mappedZ = (clampedX / 800f) * 20f;
        float mappedX = (-clampedY / 300f) * 7f;
        return new Vector3(mappedX, 0f, mappedZ);
    }
    
    /// <summary>
    /// 遷移先シーン名を設定
    /// </summary>
    public void SetTargetSceneName(string sceneName)
    {
        _targetSceneName = sceneName;
    }
}

// キャラクタデータの構造体
[System.Serializable]
public class CharacterData
{
    public List<Vector3> positions;
    public List<Param_AnimalInfo.AnimalType> names;
    public Vector3 ballPosition;
    public int ballOwnerIndex;
}
