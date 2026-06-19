using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// マーカー選択管理クラス
public class DebugPlace_MarkerSelector : MonoBehaviour
{
    // 現在選択されているマーカー
    private DebugPlace_CharaMarker _selectedMarker;
    
    // 選択変更イベント
    public System.Action<DebugPlace_CharaMarker> OnSelectionChanged;
    
    // ドラッグイベント
    public System.Action<DebugPlace_CharaMarker, Vector3> OnMarkerDragStarted;
    public System.Action<DebugPlace_CharaMarker, Vector3> OnMarkerDragMoved;
    public System.Action<DebugPlace_CharaMarker, Vector3> OnMarkerDragEnded;
    
    // ドラッグイベントの内部処理
    private void HandleMarkerDragStarted(DebugPlace_CharaMarker marker, Vector3 position)
    {
        OnMarkerDragStarted?.Invoke(marker, position);
    }
    
    private void HandleMarkerDragMoved(DebugPlace_CharaMarker marker, Vector3 position)
    {
        OnMarkerDragMoved?.Invoke(marker, position);
    }
    
    private void HandleMarkerDragEnded(DebugPlace_CharaMarker marker, Vector3 position)
    {
        OnMarkerDragEnded?.Invoke(marker, position);
        
        // ボールドラッグ終了時の処理：重なり判定
        if (_ballMarker != null && marker == _ballMarker)
        {
            TryAssignBallToOverlappedCharacter(position);
        }
    }
    
    // ボールを重なっているキャラに割り当て
    private void TryAssignBallToOverlappedCharacter(Vector3 ballPosition)
    {
        DebugPlace_CharaMarker nearest = null;
        float nearestSqr = float.MaxValue;
        const float maxSnapDistance = 80f; // Canvasローカル座標の想定距離
        
        foreach (var m in _markers)
        {
            if (m == _ballMarker) continue; // 自分（ボール）は無視
            if (m.GetTeam() == -1) continue; // 他の中立も無視
            
            // 2D距離で判定（Canvasローカル想定）
            Vector3 delta = (m.transform.localPosition - ballPosition);
            delta.z = 0;
            float sqr = delta.sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = m;
            }
        }
        
        if (nearest != null && nearestSqr <= maxSnapDistance * maxSnapDistance)
        {
            // 所持者を更新
            SetBallOwner(nearest);
            
            // 所持UIを更新
            nearest.SetBallPossession(true);
            
            // スナップ：ボールをキャラ上に移動（即時削除前のフェイルセーフ）
            _ballMarker.transform.localPosition = nearest.transform.localPosition;
            
            // ボールマーカーの削除と参照解除
            var ballGo = _ballMarker.gameObject;
            _markers.Remove(_ballMarker);
            UnregisterMarker(_ballMarker);
            _ballMarker = null;
            if (ballGo != null)
            {
                GameObject.Destroy(ballGo);
            }
        }
    }
    
    public void SetBallOwner(DebugPlace_CharaMarker owner)
    {
        _ballOwner = owner;
        OnBallOwnerChanged?.Invoke(owner);
        Debug.Log($"ボール所持者: {owner?.Name ?? "なし"}");
    }
    
    public DebugPlace_CharaMarker GetBallOwner()
    {
        return _ballOwner;
    }
    
    // マーカーリスト
    private List<DebugPlace_CharaMarker> _markers = new List<DebugPlace_CharaMarker>();
    
    // ボール所持管理
    private DebugPlace_CharaMarker _ballOwner;
    private DebugPlace_CharaMarker _ballMarker;
    public System.Action<DebugPlace_CharaMarker> OnBallOwnerChanged;
    
    // キャラクタ変更用
    [SerializeField] private ParamList_AnimalInfo _charaList; // キャラクタリスト
    
    // マーカーを登録
    public void RegisterMarker(DebugPlace_CharaMarker marker)
    {
        if (!_markers.Contains(marker))
        {
            _markers.Add(marker);
            marker.OnMarkerSelected += OnMarkerSelected;
            
            // ドラッグイベントを登録
            marker.OnDragStarted += HandleMarkerDragStarted;
            marker.OnDragMoved += HandleMarkerDragMoved;
            marker.OnDragEnded += HandleMarkerDragEnded;
            
            // ボールマーカーを特定（チーム-1）
            if (marker.GetTeam() == -1)
            {
                _ballMarker = marker;
            }
        }
    }
    
    // マーカーを登録解除
    public void UnregisterMarker(DebugPlace_CharaMarker marker)
    {
        if (_markers.Contains(marker))
        {
            _markers.Remove(marker);
            marker.OnMarkerSelected -= OnMarkerSelected;
            
            // ドラッグイベントを登録解除
            marker.OnDragStarted -= HandleMarkerDragStarted;
            marker.OnDragMoved -= HandleMarkerDragMoved;
            marker.OnDragEnded -= HandleMarkerDragEnded;
            
            // 選択解除されたマーカーが現在選択中のマーカーだった場合
            if (_selectedMarker == marker)
            {
                _selectedMarker = null;
                OnSelectionChanged?.Invoke(null);
            }
        }
    }
    
    // マーカー選択時の処理
    private void OnMarkerSelected(DebugPlace_CharaMarker marker)
    {
        // 前の選択を解除
        if (_selectedMarker != null && _selectedMarker != marker)
        {
            _selectedMarker.DeselectMarker();
        }
        
        // 新しい選択を設定
        _selectedMarker = marker;
        OnSelectionChanged?.Invoke(marker);
        
        Debug.Log($"マーカーが選択されました: {marker.Name}");
    }
    
    // 現在選択されているマーカーを取得
    public DebugPlace_CharaMarker GetSelectedMarker()
    {
        return _selectedMarker;
    }
    
    // 選択を解除
    public void ClearSelection()
    {
        if (_selectedMarker != null)
        {
            _selectedMarker.DeselectMarker();
            _selectedMarker = null;
            OnSelectionChanged?.Invoke(null);
        }
    }
    
    // 特定のマーカーを選択
    public void SelectMarker(DebugPlace_CharaMarker marker)
    {
        if (_markers.Contains(marker))
        {
            marker.SelectMarker();
        }
    }
    
    // 名前でマーカーを検索して選択
    public void SelectMarkerByName(string name)
    {
        foreach (var marker in _markers)
        {
            if (marker.Name == name)
            {
                SelectMarker(marker);
                return;
            }
        }
    }
    
    // 登録されているマーカー数を取得
    public int GetMarkerCount()
    {
        return _markers.Count;
    }
    
    // 全マーカーを取得
    public List<DebugPlace_CharaMarker> GetAllMarkers()
    {
        return new List<DebugPlace_CharaMarker>(_markers);
    }
    
    // ドラッグ中のマーカーを取得
    public DebugPlace_CharaMarker GetDraggingMarker()
    {
        foreach (var marker in _markers)
        {
            if (marker.IsDragging)
            {
                return marker;
            }
        }
        return null;
    }
    
    // ドラッグ中のマーカーがあるかチェック
    public bool IsAnyMarkerDragging()
    {
        return GetDraggingMarker() != null;
    }

    /// <summary>
    /// 選択されているマーカーのキャラクタを変更する
    /// </summary>
    /// <returns>変更が成功した場合はtrue、失敗した場合はfalse</returns>
    public bool ChangeSelectedMarkerCharacter()
    {
        // 選択されているマーカーがない場合は何もしない
        if (_selectedMarker == null || _charaList == null) return false;
        
        // ボールは変更対象外
        if (_selectedMarker.Name == "Ball") return false;
        
        // 次のキャラクタを取得（ボール/Bearを除外）
        string nextCharacterName = GetNextCharacterName(_selectedMarker.Name);
        if (string.IsNullOrEmpty(nextCharacterName)) return false;

        Param_AnimalInfo.AnimalType nextAnimalType = GetAnimalTypeByName(nextCharacterName);
        if (nextAnimalType == Param_AnimalInfo.AnimalType.None) return false;
        if (!TryGetAnimalInfo(nextAnimalType, out Param_AnimalInfo nextInfo)) return false;

        Sprite nextCharacterImage = nextInfo.InfoParam.Icon;
        _selectedMarker.SetCharaInfo(nextCharacterName, nextCharacterImage, _selectedMarker.GetTeam(), nextAnimalType);

        Debug.Log($"[DebugPlace_MarkerSelector] ChangeSelectedMarkerCharacter: nextCharacterName = {nextCharacterName}, nextAnimalType = {nextAnimalType}");
        Debug.Log($"選択されたマーカーのキャラクタを変更しました: {nextCharacterName}");
        return true;
    }
    
    /// <summary>
    /// 次のキャラクタ名を取得（ボール/Bearを除外）
    /// </summary>
    private string GetNextCharacterName(string currentName)
    {
        if (_charaList == null) return "";

        Param_AnimalInfo.AnimalType[] allTypes =
            (Param_AnimalInfo.AnimalType[])System.Enum.GetValues(typeof(Param_AnimalInfo.AnimalType));
        if (allTypes.Length == 0) return "";

        // 現在のキャラクタに対応する enum の並び順インデックスを特定
        int currentEnumIndex = -1;
        for (int i = 0; i < allTypes.Length; i++)
        {
            Param_AnimalInfo.AnimalType type = allTypes[i];
            if (!IsSelectableType(type)) continue;
            if (!TryGetAnimalInfo(type, out Param_AnimalInfo info)) continue;

            if (info.InfoParam.AnimalName == currentName)
            {
                currentEnumIndex = i;
                break;
            }
        }

        // 次のキャラクタを探す（None/Ball/Bear を除外）
        int startIndex = currentEnumIndex >= 0 ? (currentEnumIndex + 1) % allTypes.Length : 0;
        for (int i = 0; i < allTypes.Length; i++)
        {
            int index = (startIndex + i) % allTypes.Length;
            Param_AnimalInfo.AnimalType type = allTypes[index];
            if (!IsSelectableType(type)) continue;
            if (!TryGetAnimalInfo(type, out Param_AnimalInfo info)) continue;
            return info.InfoParam.AnimalName;
        }

        return "";
    }
    
    /// <summary>
    /// 名前からAnimalTypeを取得（_charaListから）
    /// </summary>
    private Param_AnimalInfo.AnimalType GetAnimalTypeByName(string characterName)
    {
        if (_charaList == null || string.IsNullOrEmpty(characterName))
        {
            Debug.LogWarning($"[DebugPlace_MarkerSelector] GetAnimalTypeByName: _charaListがnullまたはcharacterNameが空です。characterName = {characterName}");
            return Param_AnimalInfo.AnimalType.None;
        }

        foreach (Param_AnimalInfo.AnimalType type in System.Enum.GetValues(typeof(Param_AnimalInfo.AnimalType)))
        {
            if (!TryGetAnimalInfo(type, out Param_AnimalInfo info)) continue;
            if (info.InfoParam.AnimalName != characterName) continue;

            Debug.Log($"[DebugPlace_MarkerSelector] GetAnimalTypeByName: characterName = {characterName}, AnimalType = {type}");
            return type;
        }

        Debug.LogWarning($"[DebugPlace_MarkerSelector] GetAnimalTypeByName: characterName = {characterName} に対応するAnimalTypeが見つかりませんでした");
        return Param_AnimalInfo.AnimalType.None;
    }

    private static bool IsSelectableType(Param_AnimalInfo.AnimalType type)
    {
        return type != Param_AnimalInfo.AnimalType.None
            && type != Param_AnimalInfo.AnimalType.Ball
            && type != Param_AnimalInfo.AnimalType.Bear;
    }

    private bool TryGetAnimalInfo(Param_AnimalInfo.AnimalType type, out Param_AnimalInfo info)
    {
        info = null;
        if (_charaList == null || type == Param_AnimalInfo.AnimalType.None)
        {
            return false;
        }

        try
        {
            info = _charaList.GetAnimalInfo(type);
            return info != null;
        }
        catch
        {
            return false;
        }
    }
} 