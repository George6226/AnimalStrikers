using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 固定テキストキー
public static class StaticTextKey {

    // テキストのキー
    public enum TEXTKEY
    {
        NONE = 0,
        NEW_PRODUCT_ARRIVAL,
        ZOO_AREA_LEVELUP,
        NEW_ANIMAL_ADD_S,
        NEW_ANIMAL_ADD_M,
        NEW_ANIMAL_ADD_L,
        UFOCATCHER_OPEN_SIZE_M,
        UFOCATCHER_OPEN_SIZE_L,
        BACKYARD,
        IS_ALL_ANIMAL_BACKYARD,
        ALL_ANIMAL_BACKYARD,
        NOT_ENOUGH_MONEY,
        IS_LEVELUP,
        HOW_MANY_BUY,
        PURCHASED_NUM,
        RETRY_ANIMAL_CATCHER,
    }

    // アイテムの切り取りの際に必要なキー
    public enum CONTAIN_KEY{
        COIN,
    }

    // 遊び方タイトル
    public enum TUTORIAL_KEY{
        NONE = 0,
        LETS_PLAY_ANIMAL_CATCHER,
        PLAY_S_SIZE_CATCHER,
        TARGET_ANIMAL_CRANE_MOVE,
        FAILD_TRY_AGAIN,
        CONGRATURATION_BACK_TO_ZOO,
        LETS_BUY_CAGE,
        LETS_SET_ANIMAL_INTO_CAGE,
        CONGRATURATION_SET_ANIMAL,
        VISITOR_ADD_SET_ANIMAL, // 削除
        CAGE_SPACE,
        CAGE_OVER_SPACE, // 削除
        LETS_WALK_ZOO,
        TAP_WALK_PLACE,
        ZOO_LEVELUP_CONTINUE,
        LETS_ENJOY_ZOO,
        LETS_LEVELUP,
        CONGRATURATION_LEVELUP,
        LETS_LEVELUP_CONTINUE,
        OBJECT_DESCRIPTION,
        ACCESSORY_DESCRIPTION,
    }
}
