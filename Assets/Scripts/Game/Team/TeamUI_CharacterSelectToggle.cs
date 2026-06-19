using UnityEngine;
using UnityEngine.UI;

// チーム編成のキャラクタ選択トグルUI
public class TeamUI_CharacterSelectToggle : MonoBehaviour
{
    [SerializeField] private Image _onCharacterImage;
    [SerializeField] private Image _lockImage;

    void OnEnable()
    {
        if(_lockImage != null)
        {
            _lockImage.gameObject.SetActive(true);
        }
    }

    public void changeToggle(bool isOn)
    {
        _onCharacterImage.gameObject.SetActive(isOn);
    }
}
