using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アニマルのアニメーションの状態
public class AnimalAnime_State : MonoBehaviour
{
    // アニメーションの種類
    public enum PLAYER_ANIME_KIND
    {
        NONE = 0,
        STAND,
        ATTACK,
        DAMAGE_B,
        DAMAGE_F,
        HEADING,
        JOY,
        MOVE,
        SHOOT,
        SLIDING,
        SPECIAL,
    }
    // キーパーのアニメーションの種類
    public enum KEEPER_ANIME_KIND
    {
        NONE,
        STAND,
        BALL_CATCH,
        BALL_KICK,
        BALL_ROLL,
        MOVE_L,
        MOVE_R,
        PARRY_JUMP_L,
        PARRY_JUMP_R,
        PARRY_STAND,
    }

    // 現在のアニメーション状態
    private int _animeState = (int)PLAYER_ANIME_KIND.STAND;
    public int AnimeState
    {
        set { _animeState = value; }
        get { return _animeState; }
    }

    // アニメーションをプレイ中か?
    private bool _animePlayNow = false;
    public bool AnimePlayNow
    {
        set { _animePlayNow = value; }
        get { return _animePlayNow; }
    }

    // アニメーション名を取得する
    public string getAnimeName(int kind, bool player)
    {
        return (player) ? getAnimeName((PLAYER_ANIME_KIND)kind) : getAnimeName((KEEPER_ANIME_KIND)kind);
    }

    // アニメーション名の取得(Player)
    public string getAnimeName(PLAYER_ANIME_KIND kind)
    {
        switch(kind)
        {
            case PLAYER_ANIME_KIND.ATTACK:
                return "Attack";
            case PLAYER_ANIME_KIND.DAMAGE_B:
                return "Damage_B";
            case PLAYER_ANIME_KIND.DAMAGE_F:
                return "Damage_F";
            case PLAYER_ANIME_KIND.HEADING:
                return "Heading";
            case PLAYER_ANIME_KIND.JOY:
                return "Joy";
            case PLAYER_ANIME_KIND.MOVE:
                return "Move";
            case PLAYER_ANIME_KIND.SHOOT:
                return "Shoot";
            case PLAYER_ANIME_KIND.SLIDING:
                return "Slinding";
            case PLAYER_ANIME_KIND.STAND:
                return "Stand";
                case PLAYER_ANIME_KIND.SPECIAL:
                return "Special";
        }

        return "None";
    }

    // アニメーション名の取得(Player)
    public string getAnimeName(KEEPER_ANIME_KIND kind)
    {
        switch (kind)
        {
            case KEEPER_ANIME_KIND.BALL_CATCH:
                return "Ball_Catch";
            case KEEPER_ANIME_KIND.BALL_KICK:
                return "Ball_Kick";
            case KEEPER_ANIME_KIND.BALL_ROLL:
                return "Ball_Roll";
            case KEEPER_ANIME_KIND.MOVE_L:
                return "Move_L";
            case KEEPER_ANIME_KIND.MOVE_R:
                return "Move_R";
            case KEEPER_ANIME_KIND.PARRY_JUMP_L:
                return "Parry_Jump_L";
            case KEEPER_ANIME_KIND.PARRY_JUMP_R:
                return "Parry_Jump_R";
            case KEEPER_ANIME_KIND.PARRY_STAND:
                return "Parry_Stand";
            case KEEPER_ANIME_KIND.STAND:
                return "Stand";
        }

        return "None";
    }

    // 移動状態か?
    public bool isMoveState(int kind, bool player)
    {
        if (player)
        {
            if(kind == (int)PLAYER_ANIME_KIND.MOVE)
            {
                return true;
            }
        }
        else
        {
            if(kind == (int)KEEPER_ANIME_KIND.MOVE_L || kind == (int)KEEPER_ANIME_KIND.MOVE_R)
            {
                return true;
            }
        }

        return false;
    }
}
