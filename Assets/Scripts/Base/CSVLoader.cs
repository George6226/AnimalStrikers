using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// CSVを読み込む
public abstract class CSVLoader : MonoBehaviour {

    // 読み込み数
    protected int _loadCount = 0;

    // Use this for initialization
    void Start()
    {
        // CSVファイルの読み込み
        //loadCSVData();
    }

    // CSVファイルの読み込み
    public void loadCSVData(int version)
    {
        // パス
        string path = getPath();
#if UNITY_EDITOR
        // 日本語以外の場合
        if (LanguageTranslator.Instance.EditorLanguage != LanguageTranslator.EDITOR_LANGUAGE.JAPANESE){
            path += "eng";
        }
#else
        // 日本語以外の場合
        if (Application.systemLanguage != SystemLanguage.Japanese){
            path += "eng";
        }
#endif
        Debug.Log("path:" + path);

        // ギャラリー情報リスト
        List<string[]> galleryInfoList = TextFileUtility.readCSVFile(path);

        // セーブデータのようい
        readySaveData(galleryInfoList.Count);

        // 初めの1行を飛ばす
        for (int i = 1; i < galleryInfoList.Count; i++)
        {
            // アイテム情報(行)/アイテムプレハブ取得
            string[] line = galleryInfoList[i];
            // テキストの更新
            updateListData(i, line);
        }
        // セーブデータの更新
        updateSaveData();
    }

    // パスを取得する
    protected abstract string getPath();
    // リストにデータを設定する
    protected abstract void updateListData(int index, string[] lines);
    // セーブデータの用意
    protected virtual void readySaveData(int count){
    }
    // セーブデータの更新
    protected virtual void updateSaveData(){
    }

    // 説明データの加工
    protected string changeDescriptionData(string desc)
    {
        string data = "";
        string[] spDesc = desc.Split('&');

        for (int i = 0; i < spDesc.Length; i++)
        {
            data += spDesc[i];

            if (i != spDesc.Length - 1)
            {
                data += "\n";
            }
        }

        return data;
    }
}
