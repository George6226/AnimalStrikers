using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 初期化データ
public class InitData : MonoBehaviour {


    // ロード終了したか?
    private bool _loadEnd = false;
    public bool LoadEnd{
        get { return _loadEnd; }
    }

    // 自分自身の初期化
    void Awake()
	{
		// バージョン取得
		int version = PlayerPrefs.GetInt (DataKey.DATAKEY_INT_GAME_VERSION, 0);
        // ゲームデータの初期化
        initGameData (version);
		// 毎回初期化するデータ
		alwaysInitData ();
	}

	// 他の初期化
	void Start()
	{
        // 初めのサウンドを鳴らす
        //float volume = ES3.Load<float>(DataKey.DATAKEY_GAME_INFO + DataKey.TAG_FLOAT_SOUND, -80.0f);
        //float volume = ES3.Load<float>(DataKey.DATAKEY_GAME_INFO + DataKey.TAG_FLOAT_SOUND, 0.0f);
        //SoundManager.Instance.SetMasterVolume(volume);

        _loadEnd = true;
    }

	// ゲームデータの初期化
	public void initGameData(int version)
	{
        if(version < 100)
        {
            initData_Sprit.InitSpritData(version);
            version = 100;

            // コインの初期化
            ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.INT_COIN, 0);

            // チーム編成の初期化（Lion, Gorilla, Boar）- PLAYER側に保存
            List<Param_AnimalInfo.AnimalType> initialTeamFormation = new List<Param_AnimalInfo.AnimalType>
            {
                Param_AnimalInfo.AnimalType.Lion,
                Param_AnimalInfo.AnimalType.Gorilla,
                Param_AnimalInfo.AnimalType.Boar,
                Param_AnimalInfo.AnimalType.Bear,
            };
            ES3.Save(DataKey.DATAKEY_GAME_INFO + DataKey.ARRAY_INT_TEAM_FORMATION_PLAYER, initialTeamFormation);
        }
        // バージョンを保存
        PlayerPrefs.SetInt(DataKey.DATAKEY_INT_GAME_VERSION, version);
    }

    // 毎回初期化するデータ
    private void alwaysInitData()
    {
    }
}
