using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 言語差による画像の変更
public class LanguageImageChanger : MonoBehaviour {

    // 日本/英語画像
    [SerializeField] private Sprite _japanImage;
    [SerializeField] private Sprite _englishImage;

    // 画像(UI)
    private Image _image;
    // 画像(Render)
    private SpriteRenderer _render;
    // 言語の種類
    private string _languageKind = "";

    // 初期化
    private void Awake()
    {
        _image = GetComponent<Image>();
        _render = GetComponent<SpriteRenderer>();
    }

    // Use this for initialization
    void Start()
    {
#if UNITY_EDITOR
        // 日本語の場合
        if (LanguageTranslator.Instance.EditorLanguage == LanguageTranslator.EDITOR_LANGUAGE.JAPANESE){
            _languageKind = "Japanese";
        }
        else{
            _languageKind = "English";
        }
#else
        // 日本語の場合
        if (Application.systemLanguage == SystemLanguage.Japanese){
            _languageKind = "Japanese";
        }
        else{
            _languageKind = "English";
        }
#endif
        // 画像変更
        changeImage(_languageKind);
    }

    // 画像の変更
    private void changeImage(string kind)
    {
        // UI画像の場合
        if(_image != null)
        {
            // 日本語の場合
            if(kind.Contains("Japanese"))
            {
                _image.sprite = _japanImage;
            }
            else{
                _image.sprite = _englishImage;
            }
            // 画像サイズを変更
            _image.SetNativeSize();
        }
        // 通常画像の場合
        if (_render != null)
        {
            // 日本語の場合
            if (kind.Contains("Japanese"))
            {
                _render.sprite = _japanImage;
            }
            else
            {
                _render.sprite = _englishImage;
            }
        }
    }
}
