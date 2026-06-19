using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// デバッグ用のキャラマーカーの生成
public class DebugPlace_CharaMarkerCreator : MonoBehaviour
{
    // プレハブ
    [SerializeField] private GameObject _charaMarkerPrefab;
    // 動物の情報リスト
    [SerializeField] private ParamList_AnimalInfo _animalInfoList;
    // マーカーセレクター
    [SerializeField] private DebugPlace_MarkerSelector _markerSelector;
    
    [Header("Reset Button")]
    [SerializeField] private Button _resetMarkersButton; // マーカーリセットボタン

    // マーカーの数
    private const int MARKER_COUNT = 4;
    // ボールのインデックス
    private const int BALL_INDEX = -1;
    // 中立チーム（ボール用）
    private const int NEUTRAL_TEAM = -1;
    
    // 生成されたマーカーのリスト
    private List<DebugPlace_CharaMarker> _createdMarkers = new List<DebugPlace_CharaMarker>();
    // 次回生成時のみES3保存データを無視するフラグ（リセット直後に使用）
    private bool _ignoreSavedDataOnce = false;

    // Start is called before the first frame update
    void Start()
    {
        // リセットボタンの初期設定
        SetupResetButton();
        
        // 初期マーカーを生成
        CreateInitialMarkers();
    }
    
    /// <summary>
    /// リセットボタンの初期設定
    /// </summary>
    private void SetupResetButton()
    {
        if (_resetMarkersButton != null)
        {
            _resetMarkersButton.onClick.RemoveAllListeners();
            _resetMarkersButton.onClick.AddListener(OnResetMarkersClicked);
        }
    }
    
    /// <summary>
    /// 初期マーカーを生成
    /// </summary>
    private void CreateInitialMarkers()
    {
        // まずはEasySaveから保存データを読み込んで再現を試みる（リセット直後は無視）
        if (!_ignoreSavedDataOnce)
        {
            try
            {
                // 新しい形式（PLAYER側とNPC側が分かれている）から読み込む
                var playerPositions = ES3.Load<List<Vector3>>(DataKey.DATAKEY_GAME_INFO + DataKey.LIST_VECTOR3_CHARACTER_POSITION_PLAYER);
                var playerTypes = ES3.Load<List<Param_AnimalInfo.AnimalType>>(DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_PLAYER);
                var npcPositions = ES3.Load<List<Vector3>>(DataKey.DATAKEY_GAME_INFO + DataKey.LIST_VECTOR3_CHARACTER_POSITION_NPC);
                var npcTypes = ES3.Load<List<Param_AnimalInfo.AnimalType>>(DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_NPC);
                // ボール所持者インデックスを読み込む（PLAYER側は0~3、NPC側は4~7）
                var ballOwnerIndex = ES3.Load<int>(DataKey.DATAKEY_GAME_INFO + DataKey.INT_BALL_OWNER, -1);
                int ballOwnerPlayer = -1;
                int ballOwnerNPC = -1;
                if (ballOwnerIndex >= 0)
                {
                    if (ballOwnerIndex < 4)
                    {
                        // PLAYER側: 0~3
                        ballOwnerPlayer = ballOwnerIndex;
                    }
                    else if (ballOwnerIndex >= 4 && ballOwnerIndex <= 7)
                    {
                        // NPC側: 4~7を0~3に変換
                        ballOwnerNPC = ballOwnerIndex - 4;
                    }
                }

                // 新しい形式のデータが存在する場合
                if ((playerPositions != null && playerTypes != null && playerPositions.Count > 0 && playerTypes.Count > 0) ||
                    (npcPositions != null && npcTypes != null && npcPositions.Count > 0 && npcTypes.Count > 0))
                {
                    // PLAYER側（チーム0）のマーカーを生成
                    if (playerPositions != null && playerTypes != null && playerPositions.Count > 0 && playerTypes.Count > 0)
                    {
                        int count = Mathf.Min(playerPositions.Count, playerTypes.Count);
                        for (int k = 0; k < count; k++)
                        {
                            var savedAnimalType = playerTypes[k];
                            Vector3 pos = playerPositions[k];
                            Vector3 canvasPosition = ToCanvasPositionIfNeeded(pos);
                            CreateMarker(savedAnimalType, canvasPosition, 0);

                            // ボール所持者のインデックスに一致する場合、所持状態を復元
                            if (ballOwnerPlayer >= 0 && k == ballOwnerPlayer && savedAnimalType != Param_AnimalInfo.AnimalType.Ball)
                            {
                                if (_createdMarkers.Count > 0 && _createdMarkers[_createdMarkers.Count - 1] != null)
                                {
                                    var ownerMarker = _createdMarkers[_createdMarkers.Count - 1];
                                    ownerMarker.SetBallPossession(true);
                                    if (_markerSelector != null)
                                    {
                                        _markerSelector.SetBallOwner(ownerMarker);
                                    }
                                }
                            }
                        }
                    }

                    // NPC側（チーム1）のマーカーを生成
                    if (npcPositions != null && npcTypes != null && npcPositions.Count > 0 && npcTypes.Count > 0)
                    {
                        int count = Mathf.Min(npcPositions.Count, npcTypes.Count);
                        for (int k = 0; k < count; k++)
                        {
                            var savedAnimalType = npcTypes[k];
                            Vector3 pos = npcPositions[k];
                            Vector3 canvasPosition = ToCanvasPositionIfNeeded(pos);
                            CreateMarker(savedAnimalType, canvasPosition, 1);

                            // ボール所持者のインデックスに一致する場合、所持状態を復元
                            if (ballOwnerNPC >= 0 && k == ballOwnerNPC && savedAnimalType != Param_AnimalInfo.AnimalType.Ball)
                            {
                                if (_createdMarkers.Count > 0 && _createdMarkers[_createdMarkers.Count - 1] != null)
                                {
                                    var ownerMarker = _createdMarkers[_createdMarkers.Count - 1];
                                    ownerMarker.SetBallPossession(true);
                                    if (_markerSelector != null)
                                    {
                                        _markerSelector.SetBallOwner(ownerMarker);
                                    }
                                }
                            }
                        }
                    }

                    // ボールの位置を取得（後方互換性のため旧キーからも読み込む）
                    Vector3 ballPos = ES3.Load<Vector3>(DataKey.DATAKEY_GAME_INFO + DataKey.VECTOR3_BALL_POSITION, Vector3.zero);
                    bool ballOwnerExists = (ballOwnerPlayer >= 0 || ballOwnerNPC >= 0);
                    
                    // ボール所持者がいない場合のみボールマーカーを生成
                    // ボール所持者がいる場合は、既に所持状態が設定されているため、ボールマーカーは生成しない
                    if (!ballOwnerExists)
                    {
                        Vector3 ballCanvasPosition;
                        if (ballPos != Vector3.zero)
                        {
                            // 保存されたボールの位置を使用
                            ballCanvasPosition = ToCanvasPositionIfNeeded(ballPos);
                        }
                        else
                        {
                            // ボールの位置が設定されていない場合はデフォルト位置（中央）に生成
                            ballCanvasPosition = Vector3.zero;
                        }
                        CreateMarker(Param_AnimalInfo.AnimalType.Ball, ballCanvasPosition, NEUTRAL_TEAM);
                        Debug.Log($"[DebugPlace_CharaMarkerCreator] ボール所持者がいないため、ボールマーカーを生成: 位置({ballCanvasPosition.x:F1}, {ballCanvasPosition.y:F1}, {ballCanvasPosition.z:F1})");
                    }
                    else
                    {
                        Debug.Log($"[DebugPlace_CharaMarkerCreator] ボール所持者がいるため、ボールマーカーは生成しません。所持者: PLAYER={ballOwnerPlayer}, NPC={ballOwnerNPC}");
                    }

                    // 保存データで生成したので終了
                    return;
                }

            }
            catch { /* 読み込み失敗時は従来処理にフォールバック */ }
        }
        // 一度だけの無視フラグはここで解除
        _ignoreSavedDataOnce = false;

        // 従来の初期生成
        // チーム分ループ
        for (int j = 0; j < 2; j++)
        {
            // マーカー分ループ
            for (int i = 0; i < MARKER_COUNT; i++)
            {
                // Canvas座標系での位置を取得
                Vector3 canvasPosition = GetMarkerPosition(j, i);
                var animalType = (i == 3) ? Param_AnimalInfo.AnimalType.Bear : Param_AnimalInfo.AnimalType.Boar;
                CreateMarker(animalType, canvasPosition, j);
            }
        }
        // ボールを生成（中立チーム）
        CreateMarker(Param_AnimalInfo.AnimalType.Ball, new Vector3(0.0f, 0.0f, 0.0f), NEUTRAL_TEAM);
    }
    
    /// <summary>
    /// マーカーリセットボタンクリック時の処理
    /// </summary>
    private void OnResetMarkersClicked()
    {
        ResetAllMarkers();
        Debug.Log("マーカーをリセットしました");
    }
    
    /// <summary>
    /// 全マーカーをリセット
    /// </summary>
    public void ResetAllMarkers()
    {
        // 既存のマーカーを全て削除
        ClearAllMarkers();
        
        // 次回生成時のみES3データを無視
        _ignoreSavedDataOnce = true;

        // 新しいマーカーを生成
        CreateInitialMarkers();
    }
    
    /// <summary>
    /// 全マーカーを削除
    /// </summary>
    private void ClearAllMarkers()
    {
        // マーカーセレクターから登録解除
        foreach (var marker in _createdMarkers)
        {
            if (marker != null && _markerSelector != null)
            {
                _markerSelector.UnregisterMarker(marker);
            }
        }
        
        // ゲームオブジェクトを削除
        foreach (var marker in _createdMarkers)
        {
            if (marker != null && marker.gameObject != null)
            {
                DestroyImmediate(marker.gameObject);
            }
        }
        
        // リストをクリア
        _createdMarkers.Clear();
    }

    /// <summary>
    /// マーカーを生成する
    /// </summary>
    /// <param name="animalType">動物タイプ</param>
    /// <param name="position">位置</param>
    /// <param name="team">チーム番号</param>
    private void CreateMarker(Param_AnimalInfo.AnimalType animalType, Vector3 position, int team = 0)
    {
        // 生成/位置/親を設定
        GameObject mObj = Instantiate(_charaMarkerPrefab, this.transform);
        mObj.transform.SetParent(this.transform);

        // ローカル座標を設定
        mObj.transform.localPosition = position;

        DebugPlace_CharaMarker marker = mObj.GetComponent<DebugPlace_CharaMarker>();
        if (marker != null) {
            // AnimalTypeから直接画像と名前を取得（名前の不一致問題を回避）
            string name = (_animalInfoList != null) ? _animalInfoList.GetAnimalInfo(animalType).InfoParam.AnimalName : animalType.ToString();
            Sprite image = (_animalInfoList != null) ? _animalInfoList.GetAnimalInfo(animalType).InfoParam.Icon : null;
            
            marker.SetCharaInfo(name, image, team, animalType);

            // マーカーセレクターに登録
            if (_markerSelector != null) {
                _markerSelector.RegisterMarker(marker);
            }
            
            // 生成されたマーカーをリストに追加
            _createdMarkers.Add(marker);
        }
    }

    // ボールを指定ローカル座標に再生成
    public void SpawnBallAt(Vector3 localPosition)
    {
        CreateMarker(Param_AnimalInfo.AnimalType.Ball, localPosition, NEUTRAL_TEAM);
    }
    
    /// <summary>
    /// 特定のチームのマーカーをリセット
    /// </summary>
    public void ResetTeamMarkers(int team)
    {
        // 指定チームのマーカーを削除
        var markersToRemove = new List<DebugPlace_CharaMarker>();
        foreach (var marker in _createdMarkers)
        {
            if (marker != null && marker.GetTeam() == team)
            {
                markersToRemove.Add(marker);
            }
        }
        
        // 削除処理
        foreach (var marker in markersToRemove)
        {
            if (_markerSelector != null)
            {
                _markerSelector.UnregisterMarker(marker);
            }
            if (marker != null && marker.gameObject != null)
            {
                DestroyImmediate(marker.gameObject);
            }
            _createdMarkers.Remove(marker);
        }
        
        // 新しいマーカーを生成
        for (int i = 0; i < MARKER_COUNT; i++)
        {
            Vector3 canvasPosition = GetMarkerPosition(team, i);
            var animalType = (i == 3) ? Param_AnimalInfo.AnimalType.Bear : Param_AnimalInfo.AnimalType.Boar;
            CreateMarker(animalType, canvasPosition, team);
        }
        
        Debug.Log($"チーム{team}のマーカーをリセットしました");
    }
    
    /// <summary>
    /// 生成されたマーカーの数を取得
    /// </summary>
    public int GetCreatedMarkerCount()
    {
        return _createdMarkers.Count;
    }
    
    /// <summary>
    /// 生成されたマーカーのリストを取得
    /// </summary>
    public List<DebugPlace_CharaMarker> GetCreatedMarkers()
    {
        return new List<DebugPlace_CharaMarker>(_createdMarkers);
    }

    private Vector3 GetMarkerPosition(int team, int index)
    {
        float x = (index == 0) ? -150.0f : -450.0f;
        float y = 0.0f;
        
        // キーパー（index==3）は特別配置
        if (index == 3)
        {
            x = -755.0f;
            y = 0.0f;
        }
        else
        {
            switch (index){
                case 0: y = 0.0f; break;
                case 1: y = 150.0f; break;
                case 2: y = -150.0f; break;
                default: y = 0.0f; break;
            }
        }
        // 相手チームはX軸を反転
        if(team >= 1){
            x *= -1.0f;
        }
        return new Vector3(x, y, 0.0f);
    }

    // ゲーム座標(±7,±20)っぽければCanvas座標(±800,±300)へ変換
    private Vector3 ToCanvasPositionIfNeeded(Vector3 pos)
    {
        bool looksLikeGamePos = Mathf.Abs(pos.x) <= 8f && Mathf.Abs(pos.z) <= 21f && Mathf.Approximately(pos.y, 0f);
        if (!looksLikeGamePos)
        {
            return pos;
        }
        float canvasX = (pos.z / 20f) * 800f;
        float canvasY = (-pos.x / 7f) * 300f;
        return new Vector3(canvasX, canvasY, 0f);
    }

    // // キャラクター名に部分一致する最初のインデックスを取得
    // private int GetIndexByNameContains(string keyword)
    // {
    //     if (_charaList == null) return 0;
    //     for (int i = 0; i < _charaList.GetCharaCount(); i++)
    //     {
    //         var avatar = _charaList.GetCharaAvatar(i);
    //         if (avatar != null && avatar.name.Contains(keyword))
    //         {
    //             Debug.Log($"キャラクター名に部分一致する最初のインデックス: {i}, キーワード: {keyword}");
    //             return i;
    //         }
    //     }
    //     return 0; // 見つからない場合は0を返す
    // }
}
