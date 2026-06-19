using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 言語翻訳
public class LanguageTranslator : MonoBehaviour {

    // エディタ上の言語
    public enum EDITOR_LANGUAGE
    {
        JAPANESE,
        ENGLISH,
    }

    // エディタ上の言語
    [SerializeField] private  EDITOR_LANGUAGE _editorLanguage;
    public EDITOR_LANGUAGE EditorLanguage{
        get { return _editorLanguage; }
    }

    #region Singleton
    // インスタンス
    private static LanguageTranslator _instance;
    public static LanguageTranslator Instance
    {
        get
        {
            // インスタンス
            if (_instance == null)
            {
                _instance = (LanguageTranslator)FindObjectOfType(typeof(LanguageTranslator));

                if (_instance == null)
                {
                    Debug.LogError(typeof(LanguageTranslator) + "is nothing");
                }
            }
            return _instance;
        }
    }
    #endregion Singleton
}
