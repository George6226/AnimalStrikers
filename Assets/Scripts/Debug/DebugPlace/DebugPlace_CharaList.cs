using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// デバッグ用のキャラリスト
public class DebugPlace_CharaList : ScriptableObject
{
    // キャラリスト
    [SerializeField] private List<CharaInfo> _charaInfos = new List<CharaInfo>();

    [System.Serializable]
    public struct CharaInfo
    {
        // キャラ名
        [SerializeField] private string _name;
        public string CharaName
        {
            get
            {
                return _name;
            }
        }

        // キャラ画像
        [SerializeField] private Sprite _image;
        public Sprite CharaImage
        {
            get
            {
                return _image;
            }
        }

        // Avatar参照（PhotonAvatarContainerChild）
        [SerializeField] private PhotonAvatarContainerChild _avatar;
        public PhotonAvatarContainerChild CharaAvatar
        {
            get
            {
                return _avatar;
            }
        }
    }

    // キャラリストの数を取得
    public int GetCharaCount()
    {
        return _charaInfos.Count;
    }

    // キャラリストを取得
    public Sprite GetCharaImage(int index)
    {
        if (index < 0 || index >= _charaInfos.Count)
        {
            return null;
        }
        return GetCharaInfo(index)?.CharaImage;
    }

    // キャラ名から画像を取得
    public Sprite GetCharaImageByName(string name)
    {
        foreach (var charaInfo in _charaInfos)
        {
            if (charaInfo.CharaName == name)
            {
                return charaInfo.CharaImage;
            }
        }
        return null;
    }

    // キャラのAvatarを取得
    public PhotonAvatarContainerChild GetCharaAvatar(int index)
    {
        if (index < 0 || index >= _charaInfos.Count)
        {
            return null;
        }
        return GetCharaInfo(index)?.CharaAvatar;
    }

    // キャラ名からAvatarを取得
    public PhotonAvatarContainerChild GetCharaAvatarByName(string name)
    {
        foreach (var charaInfo in _charaInfos)
        {
            if (charaInfo.CharaName == name)
            {
                return charaInfo.CharaAvatar;
            }
        }
        return null;
    }

    // キャラ名の取得
    public string GetCharaName(int index)
    {
        if (index < 0 || index >= _charaInfos.Count)
        {
            return "";
        }
        return GetCharaInfo(index)?.CharaName;
    }

    // キャラ情報の取得
    private CharaInfo? GetCharaInfo(int index)
    {
        if (index < 0 || index >= _charaInfos.Count)
        {
            return null;
        }
        return _charaInfos[index];
    }
}
