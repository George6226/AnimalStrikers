using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class TimeUtility {

    // 基本の日付
    private static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

	// DateTimeからUnixTimeへ変換
	public static long getUnixTime(DateTime dateTime)
	{
		return (long)(dateTime - UnixEpoch).TotalSeconds;
	}

	// UnixTimeからDateTimeへ変換
	public static DateTime getDateTime(long unixTime)
	{
		return UnixEpoch.AddSeconds(unixTime);
	}

    // 現在時刻を取得する
    public static long getNowTime()
    {
        return getUnixTime(System.DateTime.Now);
    }

    // 現在時刻からどれだけ過ぎているかを取得
    public static long getSpendTime(long time)
    {
        // 現在時刻
        long nowTime = getUnixTime(System.DateTime.Now);

        return time - nowTime;
    }

    // 時間の文字列を取得する
    public static string getTimeString(long allSec, string type = "HMS", string separate = ":")
    {
        // 時間/分/秒
        int hour = (int)(allSec / 3600.0f);
        allSec -= hour * 3600;
        int min = (int)(allSec/60.0f);
        allSec -= min * 60;
        int sec = (int)allSec;

        string timeStr = "";
        // 時間を表示
        if(type.Contains("H")){
            if (hour < 10){
                timeStr += "0";
            }
            timeStr += (hour.ToString() + separate);
        }
        // 分を表示
        if(type.Contains("M"))
        {
            if (min < 10)
            {
                timeStr += "0";
            }
            timeStr += (min.ToString() + separate);
        }
        // 秒を表示
        if(type.Contains("S"))
        {
            if (sec < 10)
            {
                timeStr += "0";
            }
            timeStr += sec.ToString();
        }

        return timeStr;
    }
}
