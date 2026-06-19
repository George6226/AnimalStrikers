using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class  StringExt
{
    // 文字列が大文字か?
    public static bool IsUpper(this string self)
    {
        for (int i = 0; i < self.Length; i++)
        {
            // a-z or A-Z ではない
            if (!((self[i] >= 'A' && self[i] <= 'Z') || (self[i] >= 'a' && self[i] <= 'z'))){
                continue;
            }

            // アルファベットが小文字化?
            if (char.IsLower(self[i]))
            {
                return false;
            }
        }

        return true;
    }

    // 文字列が小文字か?
    public static bool IsLower(this string self)
    {
        for(int i=0; i<self.Length; i++)
        {
            // a-z or A-Z ではない
            if (!((self[i] >= 'A' && self[i] <= 'Z') || (self[i] >= 'a' && self[i] <= 'z')))
            {
                continue;
            }

            if (char.IsUpper(self[i]))
            {
                return false;
            }
        }

        return true;
    }

    // 数字か?
    public static bool IsNumber(this string self)
    {
        for(int i=0; i<self.Length; i++)
        {
            // 数字じゃない
            if (!char.IsDigit(self[i]))
            {
                return false;
            }
        }

        return true;
    }
}
