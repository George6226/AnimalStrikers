using UnityEngine;

/// <summary>
/// コインの表示を行うクラス
/// NumberTextCreator を使ってコイン画像を右寄せで表示する
/// </summary>
public class UI_CoinView : MonoBehaviour
{
    [Header("表示先の親オブジェクト")]
    [SerializeField] private RectTransform _parentTransform;

    [Header("NumberTextCreator で使用するファイル名（例：number_menu_coin）")]
    [SerializeField] private string _numberFileName = "number_menu_coin";

    /// <summary>現在表示している数字オブジェクトの親</summary>
    private GameObject _currentNumberParent;

    private void OnEnable()
    {
        if (_parentTransform == null)
        {
            _parentTransform = GetComponent<RectTransform>();
        }

        // CoinManager のイベントに登録
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinChanged += OnCoinChanged;
        }

        // 有効化時に一度更新
        UpdateView();
    }

    private void OnDisable()
    {
        // イベント登録解除
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinChanged -= OnCoinChanged;
        }
    }

    /// <summary>
    /// CoinManager からコイン変更通知を受け取ったとき
    /// </summary>
    /// <param name="coin"></param>
    private void OnCoinChanged(int coin)
    {
        UpdateView();
    }

    /// <summary>
    /// コイン表示を最新の値に更新する
    /// </summary>
    public void UpdateView()
    {
        if (CoinManager.Instance == null)
        {
            Debug.LogWarning("CoinManager.Instance が存在しないため、コイン表示を更新できません。");
            return;
        }

        int coin = CoinManager.Instance.CurrentCoin;

        // 以前の表示を削除
        if (_currentNumberParent != null)
        {
            Destroy(_currentNumberParent);
            _currentNumberParent = null;
        }

        // 表示情報の設定（右寄せ）
        NumberTextInfo info = new NumberTextInfo();
        info.TextAlign = NumberTextInfo.TEXT_ALIGN.ALIGN_LEFT;

        // 数字画像を生成
        _currentNumberParent = NumberTextCreator.Instance.createNumberImageObjectList(
            _numberFileName,
            coin.ToString(),
            _parentTransform != null ? _parentTransform.gameObject : this.gameObject,
            info
        );
    }
}


