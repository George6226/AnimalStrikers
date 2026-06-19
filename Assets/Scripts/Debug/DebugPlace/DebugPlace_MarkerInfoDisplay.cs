using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// マーカー情報表示クラス
public class DebugPlace_MarkerInfoDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _infoPanel; // 情報パネル
    [SerializeField] private TextMeshProUGUI _nameText; // 名前テキスト
    [SerializeField] private Image _characterImage; // キャラクター画像
    [SerializeField] private TextMeshProUGUI _teamText; // チームテキスト
    [SerializeField] private TextMeshProUGUI _positionText; // 位置テキスト
    [SerializeField] private TextMeshProUGUI _statusText; // ステータステキスト
    
    [Header("Settings")]
    [SerializeField] private bool _showTeam = true; // チームを表示するか
    [SerializeField] private bool _showPosition = true; // 位置を表示するか
    [SerializeField] private bool _showStatus = true; // ステータスを表示するか
    [SerializeField] private bool _autoHideOnDeselect = true; // 選択解除時に自動非表示にするか
    
    // マーカーセレクター
    [SerializeField] private DebugPlace_MarkerSelector _markerSelector;
    [SerializeField] private DebugPlace_CharaMarkerCreator _markerCreator; // ボール再生成用
    [SerializeField] private Button _releaseBallButton; // 解除ボタン
    [SerializeField] private Button _changeCharacterButton; // キャラクタ変更ボタン
    
    // 現在表示中のマーカー
    private DebugPlace_CharaMarker _currentDisplayedMarker;
    
    void Start()
    {
        // 初期状態では非表示
        if (_infoPanel != null)
        {
            _infoPanel.SetActive(false);
        }
        
        // 解除ボタンの初期設定
        if (_releaseBallButton != null)
        {
            _releaseBallButton.onClick.RemoveAllListeners();
            _releaseBallButton.onClick.AddListener(OnReleaseBallClicked);
            _releaseBallButton.gameObject.SetActive(false);
        }
        
        // キャラクタ変更ボタンの初期設定
        if (_changeCharacterButton != null)
        {
            _changeCharacterButton.onClick.RemoveAllListeners();
            _changeCharacterButton.onClick.AddListener(OnChangeCharacterClicked);
            _changeCharacterButton.gameObject.SetActive(false);
        }
        
        // マーカーセレクターのイベントを登録
        if (_markerSelector != null)
        {
            _markerSelector.OnSelectionChanged += OnMarkerSelectionChanged;
            _markerSelector.OnMarkerDragStarted += OnMarkerDragStarted;
            _markerSelector.OnMarkerDragMoved += OnMarkerDragMoved;
            _markerSelector.OnMarkerDragEnded += OnMarkerDragEnded;
        }
    }
    
    void OnDestroy()
    {
        // イベントの登録解除
        if (_markerSelector != null)
        {
            _markerSelector.OnSelectionChanged -= OnMarkerSelectionChanged;
            _markerSelector.OnMarkerDragStarted -= OnMarkerDragStarted;
            _markerSelector.OnMarkerDragMoved -= OnMarkerDragMoved;
            _markerSelector.OnMarkerDragEnded -= OnMarkerDragEnded;
        }
    }
    
    /// <summary>
    /// マーカー選択変更時の処理
    /// </summary>
    private void OnMarkerSelectionChanged(DebugPlace_CharaMarker marker)
    {
        if (marker != null)
        {
            ShowMarkerInfo(marker);
        }
        else if (_autoHideOnDeselect)
        {
            HideInfo();
        }
    }
    
    /// <summary>
    /// マーカードラッグ開始時の処理
    /// </summary>
    private void OnMarkerDragStarted(DebugPlace_CharaMarker marker, Vector3 position)
    {
        // 現在表示中のマーカーがドラッグ開始された場合、ステータスを更新
        if (_currentDisplayedMarker == marker && _showStatus)
        {
            UpdateStatusText();
        }
    }
    
    /// <summary>
    /// マーカードラッグ移動時の処理
    /// </summary>
    private void OnMarkerDragMoved(DebugPlace_CharaMarker marker, Vector3 position)
    {
        // 現在表示中のマーカーがドラッグされた場合、位置情報を更新
        if (_currentDisplayedMarker == marker && _showPosition)
        {
            UpdatePositionText(position);
        }
    }
    
    /// <summary>
    /// マーカードラッグ終了時の処理
    /// </summary>
    private void OnMarkerDragEnded(DebugPlace_CharaMarker marker, Vector3 position)
    {
        // 現在表示中のマーカーがドラッグ終了された場合、ステータスを更新
        if (_currentDisplayedMarker == marker && _showStatus)
        {
            UpdateStatusText();
        }
        
        UpdateReleaseButtonVisibility();
    }
    
    private void OnReleaseBallClicked()
    {
        if (_currentDisplayedMarker == null) return;
        if (_markerSelector == null || _markerCreator == null) return;
        
        // 所持解除
        _currentDisplayedMarker.SetBallPossession(false);
        _markerSelector.SetBallOwner(null);
        
        // (0,0) にボール再生成（Canvasローカル）
        _markerCreator.SpawnBallAt(Vector3.zero);
        
        UpdateReleaseButtonVisibility();
        UpdateStatusText();
    }
    
    /// <summary>
    /// キャラクタ変更ボタンクリック時の処理
    /// BallかBearになった場合は飛ばして次に変更する
    /// </summary>
    private void OnChangeCharacterClicked()
    {
        Debug.Log("[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: 開始");
        
        if (_markerSelector == null)
        {
            Debug.LogWarning("[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: _markerSelectorがnullです");
            return;
        }
        
        if (_currentDisplayedMarker == null)
        {
            Debug.LogWarning("[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: _currentDisplayedMarkerがnullです");
            return;
        }
        
        string currentName = _currentDisplayedMarker.Name;
        Debug.Log($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: 現在のキャラクタ名 = {currentName}");
        
        // すでにBallかBearの場合は何もしない
        if (IsBallOrBear(_currentDisplayedMarker))
        {
            Debug.Log($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: 現在のキャラクタがBallかBearのため、処理をスキップします");
            return;
        }
        
        // 最大試行回数（無限ループ防止）
        int maxAttempts = 10;
        int attempts = 0;
        
        Debug.Log($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: キャラクタ変更を開始（最大{maxAttempts}回まで試行）");
        
        while (attempts < maxAttempts)
        {
            attempts++;
            Debug.Log($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: 試行{attempts}回目");
            
            // 変更前のキャラクタ名を記録
            string beforeName = _currentDisplayedMarker != null ? _currentDisplayedMarker.Name : "null";
            Debug.Log($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: 変更前のキャラクタ名 = {beforeName}");
            
            // キャラクタ変更を実行
            bool success = _markerSelector.ChangeSelectedMarkerCharacter();
            Debug.Log($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: ChangeSelectedMarkerCharacter()の結果 = {success}");
            
            if (!success)
            {
                // 変更に失敗した場合は終了
                Debug.Log($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: キャラクタ変更に失敗したため、処理を終了します");
                break;
            }
            
            // 変更後のキャラクタ名を確認
            string afterName = _currentDisplayedMarker != null ? _currentDisplayedMarker.Name : "null";
            Debug.Log($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: 変更後のキャラクタ名 = {afterName}");
            
            // 変更後のキャラクタがBallかBearでない場合は終了
            if (_currentDisplayedMarker != null && !IsBallOrBear(_currentDisplayedMarker))
            {
                Debug.Log($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: 変更後のキャラクタがBall/Bearではないため、処理を終了します");
                break;
            }
            
            // BallかBearになった場合は次のキャラクタに変更を試みる
            Debug.Log($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: 変更後のキャラクタがBall/Bearのため、次のキャラクタに変更を試みます");
        }
        
        if (attempts >= maxAttempts)
        {
            Debug.LogWarning($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: 最大試行回数({maxAttempts}回)に達しました");
        }
        
        // 変更が成功した場合、現在のキャラクタ情報を更新
        if (_currentDisplayedMarker != null)
        {
            string finalName = _currentDisplayedMarker.Name;
            Debug.Log($"[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: 最終的なキャラクタ名 = {finalName}");
            ShowMarkerInfo(_currentDisplayedMarker);
        }
        
        Debug.Log("[DebugPlace_MarkerInfoDisplay] OnChangeCharacterClicked: 終了");
    }
    
    /// <summary>
    /// BallかBearかどうかを判定（AnimalTypeを使用）
    /// </summary>
    private bool IsBallOrBear(DebugPlace_CharaMarker marker)
    {
        if (marker == null)
        {
            Debug.Log("[DebugPlace_MarkerInfoDisplay] IsBallOrBear: markerがnullです");
            return false;
        }
        
        Param_AnimalInfo.AnimalType animalType = marker.AnimalType;
        bool isBall = animalType == Param_AnimalInfo.AnimalType.Ball;
        bool isBear = animalType == Param_AnimalInfo.AnimalType.Bear;
        bool result = isBall || isBear;
        
        Debug.Log($"[DebugPlace_MarkerInfoDisplay] IsBallOrBear: marker.Name = {marker.Name}, marker.AnimalType = {animalType}, isBall = {isBall}, isBear = {isBear}, result = {result}");
        
        return result;
    }
    
    /// <summary>
    /// キャラクタ変更ボタンの表示状態を更新
    /// BallかBearの場合は非表示
    /// </summary>
    private void UpdateChangeCharacterButtonVisibility()
    {
        if (_changeCharacterButton == null)
        {
            Debug.Log("[DebugPlace_MarkerInfoDisplay] UpdateChangeCharacterButtonVisibility: _changeCharacterButtonがnullです");
            return;
        }
        
        if (_currentDisplayedMarker == null)
        {
            Debug.Log("[DebugPlace_MarkerInfoDisplay] UpdateChangeCharacterButtonVisibility: _currentDisplayedMarkerがnullです。ボタンを非表示にします");
            _changeCharacterButton.gameObject.SetActive(false);
            return;
        }
        
        bool isBallOrBear = IsBallOrBear(_currentDisplayedMarker);
        bool shouldShow = !isBallOrBear;
        
        Debug.Log($"[DebugPlace_MarkerInfoDisplay] UpdateChangeCharacterButtonVisibility: marker.Name = {_currentDisplayedMarker.Name}, isBallOrBear = {isBallOrBear}, shouldShow = {shouldShow}");
        
        _changeCharacterButton.gameObject.SetActive(shouldShow);
    }
    
    private void UpdateReleaseButtonVisibility()
    {
        if (_releaseBallButton == null)
        {
            return;
        }
        bool show = _currentDisplayedMarker != null && _currentDisplayedMarker.HasBall();
        _releaseBallButton.gameObject.SetActive(show);
    }
    
    /// <summary>
    /// マーカー情報を表示
    /// </summary>
    public void ShowMarkerInfo(DebugPlace_CharaMarker marker)
    {
        if (marker == null) return;
        
        _currentDisplayedMarker = marker;
        
        // パネルを表示
        if (_infoPanel != null)
        {
            _infoPanel.SetActive(true);
        }
        
        // 名前を設定
        if (_nameText != null)
        {
            _nameText.text = $"名前: {marker.Name}";
        }
        
        // 画像を設定
        if (_characterImage != null)
        {
            Sprite characterSprite = marker.GetCharacterImage();
            if (characterSprite != null)
            {
                _characterImage.sprite = characterSprite;
                _characterImage.gameObject.SetActive(true);
            }
            else
            {
                _characterImage.gameObject.SetActive(false);
            }
        }
        
        // チーム情報を設定
        if (_showTeam && _teamText != null)
        {
            string teamName = marker.GetTeamName();
            _teamText.text = $"チーム: {teamName}";
        }
        
        // 位置情報を設定
        if (_showPosition && _positionText != null)
        {
            Vector3 position = marker.transform.localPosition;
            _positionText.text = $"位置: ({position.x:F1}, {position.y:F1}, {position.z:F1})";
        }
        
        // ステータス情報を設定
        if (_showStatus && _statusText != null)
        {
            string status = marker.IsSelected ? "選択中" : "未選択";
            if (marker.IsDragging)
            {
                status += " (ドラッグ中)";
            }
            if (marker.HasBall())
            {
                status += " (所持中)";
            }
            _statusText.text = $"ステータス: {status}";
        }
        
        UpdateReleaseButtonVisibility();
        UpdateChangeCharacterButtonVisibility(); // キャラクタ変更ボタンの表示状態を更新
        
        Debug.Log($"マーカー情報を表示: {marker.Name}");
    }
    
    /// <summary>
    /// 位置テキストを更新
    /// </summary>
    private void UpdatePositionText(Vector3 position)
    {
        if (_positionText != null)
        {
            _positionText.text = $"位置: ({position.x:F1}, {position.y:F1}, {position.z:F1})";
        }
    }
    
    /// <summary>
    /// ステータステキストを更新
    /// </summary>
    private void UpdateStatusText()
    {
        if (_currentDisplayedMarker != null && _statusText != null)
        {
            string status = _currentDisplayedMarker.IsSelected ? "選択中" : "未選択";
            if (_currentDisplayedMarker.IsDragging)
            {
                status += " (ドラッグ中)";
            }
            if (_currentDisplayedMarker.HasBall())
            {
                status += " (所持中)";
            }
            _statusText.text = $"ステータス: {status}";
        }
    }
    
    /// <summary>
    /// 情報表示を非表示にする
    /// </summary>
    public void HideInfo()
    {
        if (_infoPanel != null)
        {
            _infoPanel.SetActive(false);
        }
        _currentDisplayedMarker = null;
    }
    
    /// <summary>
    /// 情報表示を強制的に表示/非表示を切り替え
    /// </summary>
    public void ToggleInfo()
    {
        if (_infoPanel != null)
        {
            bool isActive = _infoPanel.activeSelf;
            _infoPanel.SetActive(!isActive);
            
            if (!isActive && _currentDisplayedMarker != null)
            {
                // 再表示時に情報を更新
                ShowMarkerInfo(_currentDisplayedMarker);
            }
        }
    }
    
    /// <summary>
    /// 現在表示中のマーカーを取得
    /// </summary>
    public DebugPlace_CharaMarker GetCurrentDisplayedMarker()
    {
        return _currentDisplayedMarker;
    }
    
    /// <summary>
    /// 情報が表示されているかチェック
    /// </summary>
    public bool IsInfoDisplayed()
    {
        return _infoPanel != null && _infoPanel.activeSelf;
    }
    
    /// <summary>
    /// 設定を更新
    /// </summary>
    public void UpdateSettings(bool showTeam, bool showPosition, bool showStatus, bool autoHide)
    {
        _showTeam = showTeam;
        _showPosition = showPosition;
        _showStatus = showStatus;
        _autoHideOnDeselect = autoHide;
        
        // 現在表示中の情報を更新
        if (_currentDisplayedMarker != null)
        {
            ShowMarkerInfo(_currentDisplayedMarker);
        }
    }
} 