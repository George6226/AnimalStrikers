using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StringCommaUtility
{
    // 数字にコンマをつけて返す
    public static string numberAddCommaToString(int num)
    {
        string str = "";
        int dummyNum = num;

        while (dummyNum >= 1000)
        {
            // 上の桁/下三桁
            int md = dummyNum / 1000;
            int down = dummyNum - md * 1000;

            string downStr = "" + down;
            // 100未満の数
            if(down < 100){
                downStr = "0" + downStr;
            }
            // 10未満の数
            if(down < 10){
                downStr = "0" + downStr;
            }

            str = ","+downStr + str;

            // 上三桁に変更
            dummyNum = md;
        }

        // 先頭にくっつける
        str = dummyNum + str;

        // TODO:例:1234 => 1,234
        // TODO:例:1001 => 1,001
        // TODO:例:1000098 => 1,000,098
        // TODO:例:90 => 90

        return str;
    }
}
