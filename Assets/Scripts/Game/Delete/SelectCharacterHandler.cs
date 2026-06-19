using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 選択されたキャラクタの操作
public class SelectCharacterHandler : MonoBehaviour
{
    //// キャラクタの選択
    //[SerializeField] private CharacterSelector _charaSelect;
    //// スライドパッド
    //[SerializeField] private UI_SlidePad _slidePad;

    //// 定期的Update
    //private void FixedUpdate()
    //{
    //    // キャラクタリストがない OR キャラがいない
    //    if (_charaSelect.SelChara == null){
    //        return;
    //    }
    //    // スライド中でない
    //    if (!_slidePad.SlideNow){
    //        _charaSelect.SelChara.stand();
    //        return;
    //    }

    //    // 移動/角度
    //    _charaSelect.SelChara.move(_slidePad.SlideScale);
    //    _charaSelect.SelChara.rotate(_slidePad.Radian);
    //}

    //// アニメーションの変更
    //public void changeAnime()
    //{
    //    _charaSelect.SelChara.changeAnime();
    //}
}
