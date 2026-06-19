using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// デバッグ用のキャラマーカー
public class DebugPlace_CharaMarker : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // 画像
    [SerializeField] private Image _image;
    // 名前
    private string _name;
    // 動物タイプ
    private Param_AnimalInfo.AnimalType _animalType;
    // チーム
    private int _team;
    
    // ボール所持アイコン
    [SerializeField] private Image _possessionIcon;
    // チーム色用の画像（上に被せるオーバーレイなど）
    [SerializeField] private Image _teamColorImage;
    
    // 所持状態
    private bool _hasBall = false;
    
    // 選択状態
    private bool _isSelected = false;
    
    // 選択時の色
    [SerializeField] private Color _selectedColor = Color.yellow;
    // 通常時の色
    [SerializeField] private Color _normalColor = Color.white;
    
    // チームカラー
    [SerializeField] private Color _playerTeamColor = Color.blue;
    [SerializeField] private Color _enemyTeamColor = Color.red;
    [SerializeField] private Color _neutralTeamColor = new Color(1f,1f,1f,0f); // 透明
    
    // 選択イベント
    public System.Action<DebugPlace_CharaMarker> OnMarkerSelected;
    
    // ドラッグイベント
    public System.Action<DebugPlace_CharaMarker, Vector3> OnDragStarted;
    public System.Action<DebugPlace_CharaMarker, Vector3> OnDragMoved;
    public System.Action<DebugPlace_CharaMarker, Vector3> OnDragEnded;
    
    // ドラッグ状態
    private bool _isDragging = false;
    private Vector3 _dragOffset;
    // キーパー用のドラッグ開始時X固定値
    private float _fixedXOnDrag = 0f;
    
    // ドラッグ設定
    [SerializeField] private bool _useLocalPosition = true; // Canvas座標系を使用するか

    // キャラ情報の設定
    public void SetCharaInfo(string name, Sprite image, int team = 0, Param_AnimalInfo.AnimalType animalType = Param_AnimalInfo.AnimalType.None)
    {
        _image.sprite = image;
        _name = name;
        _team = team;
        _animalType = animalType;
        
        // 初期状態の所持アイコンを同期
        UpdatePossessionIconVisibility();
        // チーム色を反映
        UpdateTeamColorOverlay();
    }
    
    // クリックイベント
    public void OnPointerClick(PointerEventData eventData)
    {
        SelectMarker();
    }
    
    // マーカーを選択
    public void SelectMarker()
    {
        _isSelected = true;
        _image.color = _selectedColor;
        OnMarkerSelected?.Invoke(this);
    }
    
    // マーカーの選択を解除
    public void DeselectMarker()
    {
        _isSelected = false;
        _image.color = _normalColor;
    }
    
    // 選択状態を取得
    public bool IsSelected => _isSelected;
    
    // 名前を取得
    public string Name => _name;
    
    // 動物タイプを取得
    public Param_AnimalInfo.AnimalType AnimalType => _animalType;
    
    // 画像を取得
    public Sprite GetCharacterImage()
    {
        return _image != null ? _image.sprite : null;
    }
    
    // ボール所持状態を設定
    public void SetBallPossession(bool hasBall)
    {
        _hasBall = hasBall;
        UpdatePossessionIconVisibility();
    }
    
    private void UpdatePossessionIconVisibility()
    {
        if (_possessionIcon != null)
        {
            _possessionIcon.gameObject.SetActive(_hasBall);
        }
    }
    
    private void UpdateTeamColorOverlay()
    {
        if (_teamColorImage == null) return;
        switch (_team)
        {
            case 0: // Player
                _teamColorImage.color = _playerTeamColor;
                break;
            case 1: // Enemy
                _teamColorImage.color = _enemyTeamColor;
                break;
            default: // Neutral/Unknown
                _teamColorImage.color = _neutralTeamColor;
                break;
        }
    }
    
    // ボール所持状態を取得
    public bool HasBall()
    {
        return _hasBall;
    }
    
    // チームを取得
    public int GetTeam()
    {
        return _team;
    }
    
    // チーム名を取得
    public string GetTeamName()
    {
        switch (_team)
        {
            case 0:
                return "Player";
            case 1:
                return "Enemy";
            case -1:
                return "Neutral";
            default:
                return "Unknown";
        }
    }
    
    // ドラッグ状態を取得
    public bool IsDragging => _isDragging;
    
    // ドラッグ開始
    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        // キーパー（Bear）はX固定のため、開始時Xを保持
        if (!string.IsNullOrEmpty(_name) && _name.Contains("Bear"))
        {
            _fixedXOnDrag = transform.localPosition.x;
        }
        
        if (_useLocalPosition)
        {
            // Canvas座標系でのドラッグ
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out localPoint);
            _dragOffset = transform.localPosition - new Vector3(localPoint.x, localPoint.y, 0);
        }
        else
        {
            // ワールド座標系でのドラッグ
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, transform.position.z));
            _dragOffset = transform.position - mouseWorldPos;
        }
        
        // ドラッグ開始イベントを発火
        OnDragStarted?.Invoke(this, _useLocalPosition ? transform.localPosition : transform.position);
        
        Debug.Log($"ドラッグ開始: {_name}");
    }
    
    // ドラッグ中
    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        
        if (_useLocalPosition)
        {
            // Canvas座標系でのドラッグ
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out localPoint);
            
            Vector3 newLocalPosition = new Vector3(localPoint.x, localPoint.y, 0) + _dragOffset;
            // 移動制限
            bool isKeeper = !string.IsNullOrEmpty(_name) && _name.Contains("Bear");
            float clampedX = isKeeper ? _fixedXOnDrag : Mathf.Clamp(newLocalPosition.x, -800f, 800f);
            float clampedY = isKeeper ? Mathf.Clamp(newLocalPosition.y, -160f, 160f) : Mathf.Clamp(newLocalPosition.y, -300f, 300f);
            Vector3 clampedLocalPosition = new Vector3(clampedX, clampedY, 0);
            transform.localPosition = clampedLocalPosition;
            
            // ドラッグ移動イベントを発火
            OnDragMoved?.Invoke(this, clampedLocalPosition);
        }
        else
        {
            // ワールド座標系でのドラッグ
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, transform.position.z));
            
            Vector3 newPosition = mouseWorldPos + _dragOffset;
            transform.position = newPosition;
            
            // ドラッグ移動イベントを発火
            OnDragMoved?.Invoke(this, newPosition);
        }
    }
    
    // ドラッグ終了
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        
        _isDragging = false;
        
        // ドラッグ終了イベントを発火
        Vector3 finalPosition = _useLocalPosition ? transform.localPosition : transform.position;
        OnDragEnded?.Invoke(this, finalPosition);
        
        Debug.Log($"ドラッグ終了: {_name} at {finalPosition}");
    }
}
