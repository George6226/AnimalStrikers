using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Team_CharacterSelectButton : MonoBehaviour
{
    // 配列内のインデックス
    [SerializeField] private int index;
    // 動物の種類
    [SerializeField] private Param_AnimalInfo.AnimalType animalType;
    // 動物キャラのパラメータ定義
    [SerializeField] private ParamList_AnimalInfo paramAnimalInfoList;
    // アイコン表示用のImageコンポーネント
    [SerializeField] private Image iconImage;
    // トグルマネージャーへの参照
    [SerializeField] private TeamUI_CharacterSelectTogglesManager togglesManager;
    // 決定ボタンへの参照
    [SerializeField] private TeamUI_CharacterDecideButton decideButton;
    // ボタンコンポーネント
    private Button button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadTeamFormation();
        UpdateIcon();
        SetupButton();
    }

    /// <summary>
    /// チーム編成データを読み込んで反映する
    /// </summary>
    private void LoadTeamFormation()
    {
        // デフォルト値（Lion, Gorilla, Boar, Bear）
        List<Param_AnimalInfo.AnimalType> defaultFormation = new List<Param_AnimalInfo.AnimalType>
        {
            Param_AnimalInfo.AnimalType.Lion,
            Param_AnimalInfo.AnimalType.Gorilla,
            Param_AnimalInfo.AnimalType.Boar,
            Param_AnimalInfo.AnimalType.Bear,
        };

        // チーム編成データを読み込む（PLAYER側を使用）
        List<Param_AnimalInfo.AnimalType> teamFormation = ES3.Load<List<Param_AnimalInfo.AnimalType>>(DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_PLAYER, defaultFormation);

        Debug.Log($"[Team_CharacterSelectButton] チーム編成を読み込みました。teamFormation: [{string.Join(", ", teamFormation)}]");

        // インデックスが有効な範囲内かチェック
        if (index >= 0 && index < teamFormation.Count)
        {
            // AnimalTypeを直接取得
            animalType = teamFormation[index];
            Debug.Log($"[Team_CharacterSelectButton] チーム編成を読み込みました。index: {index}, animalType: {animalType}");
        }
        else
        {
            Debug.LogWarning($"[Team_CharacterSelectButton] インデックス {index} が範囲外です。リストの長さ: {teamFormation.Count}");
        }
    }

    /// <summary>
    /// ボタンのクリックイベントを設定
    /// </summary>
    private void SetupButton()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogWarning("[Team_CharacterSelectButton] Button コンポーネントが見つかりません。");
        }
    }

    /// <summary>
    /// ボタンがクリックされた時の処理
    /// </summary>
    private void OnButtonClicked()
    {
        if (togglesManager != null)
        {
            togglesManager.OnCharacterSelected(animalType);
        }
        else
        {
            Debug.LogWarning("[Team_CharacterSelectButton] togglesManager が設定されていません。");
        }

        // 決定ボタンに選択された動物タイプを渡す
        if (decideButton != null)
        {
            decideButton.SetSelectedAnimalType(animalType);
            decideButton.SetCallbackTarget(this);
        }
    }

    /// <summary>
    /// 動物タイプを更新してアイコンを更新する
    /// </summary>
    /// <param name="newAnimalType">新しい動物タイプ</param>
    public void UpdateAnimalType(Param_AnimalInfo.AnimalType newAnimalType)
    {
        animalType = newAnimalType;
        UpdateIcon();
        SaveTeamFormation();
        Debug.Log($"[Team_CharacterSelectButton] 動物タイプを更新しました: {newAnimalType}");
    }

    /// <summary>
    /// チーム編成データを保存する
    /// </summary>
    private void SaveTeamFormation()
    {
        // デフォルト値（Lion, Gorilla, Boar）
        List<Param_AnimalInfo.AnimalType> defaultFormation = new List<Param_AnimalInfo.AnimalType>
        {
            Param_AnimalInfo.AnimalType.Lion,
            Param_AnimalInfo.AnimalType.Gorilla,
            Param_AnimalInfo.AnimalType.Boar,
            Param_AnimalInfo.AnimalType.Bear,
        };

        // 現在のチーム編成データを読み込む（PLAYER側を使用）
        List<Param_AnimalInfo.AnimalType> teamFormation = ES3.Load<List<Param_AnimalInfo.AnimalType>>(DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_PLAYER, defaultFormation);

        Debug.Log($"[Team_CharacterSelectButton] 保存前のチーム編成: [{string.Join(", ", teamFormation)}]");

        // インデックスが有効な範囲内かチェック
        if (index >= 0 && index < teamFormation.Count)
        {
            Debug.Log($"[Team_CharacterSelectButton] index {index} の animalType を {teamFormation[index]} から {animalType} に変更します。");
       
            // indexの位置の値を新しいanimalTypeに更新
            teamFormation[index] = animalType;

            // 更新したリストを保存（PLAYER側に保存）
            ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_PLAYER, teamFormation);
            Debug.Log($"[Team_CharacterSelectButton] チーム編成を保存しました。index: {index}, animalType: {animalType}");

            // 保存されたチーム編成をデバッグ表示
            var savedFormation = ES3.Load<List<Param_AnimalInfo.AnimalType>>(DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_PLAYER);
            Debug.Log($"[Team_CharacterSelectButton] 保存後のチーム編成: [{string.Join(", ", savedFormation)}]");
 
        }
        else
        {
            Debug.LogWarning($"[Team_CharacterSelectButton] インデックス {index} が範囲外です。リストの長さ: {teamFormation.Count}");
        }
    }

    /// <summary>
    /// 動物タイプに応じてアイコンを更新する
    /// </summary>
    private void UpdateIcon()
    {
        if (paramAnimalInfoList == null)
        {
            Debug.LogWarning("[Team_CharacterSelectButton] paramAnimalInfoList が設定されていません。");
            return;
        }

        if (iconImage == null)
        {
            Debug.LogWarning("[Team_CharacterSelectButton] iconImage が設定されていません。");
            return;
        }

        Param_AnimalInfo animalInfo;
        try
        {
            animalInfo = paramAnimalInfoList.GetAnimalInfo(animalType);
        }
        catch
        {
            Debug.LogWarning($"[Team_CharacterSelectButton] {animalType} の Param_AnimalInfo が見つかりません。");
            return;
        }

        if (animalInfo != null && animalInfo.InfoParam.Icon != null)
        {
            iconImage.sprite = animalInfo.InfoParam.Icon;
        }
        else
        {
            Debug.LogWarning($"[Team_CharacterSelectButton] {animalType} のアイコンが設定されていません。");
        }
    }
}
