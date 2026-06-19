//  EnumUtility.cs
//  http://kan-kikuchi.hatenablog.com/entry/EnumUtility
//
//  Created by kan.kikuchi on 2019.04.20.

using System;
using System.Collections.Generic;

/// <summary>
/// enumの便利クラス
/// </summary>
public static class EnumUtility {
  
  //=================================================================================
  //取得
  //=================================================================================
  
  /// <summary>
  /// 項目数を取得
  /// </summary>
  public static int GetTypeNum<T>() where T : struct{
    return Enum.GetValues (typeof(T)).Length;
  }
  
  /// <summary>
  /// 項目をランダムに一つ取得
  /// </summary>
  public static T GetRandom<T>() where T : struct{
    int no = UnityEngine.Random.Range(0, GetTypeNum<T>());
    //int no = new System.Random().Next(0, GetTypeNum<T>()); //UnityEngineを使わない場合
    return NoToType<T>(no);
  }
  
  /// <summary>
  /// 全ての項目が入ったListを取得
  /// </summary>
  public static List<T> GetAllInList<T>() where T : struct{
    var list = new List<T>();
    foreach(T t in Enum.GetValues(typeof(T))){
      list.Add(t);
    }
    list.Sort();
    return list;
  }
  
  //=================================================================================
  //変換
  //=================================================================================
  
  /// <summary>
  /// 入力された文字列と同じ項目を取得
  /// </summary>
  public static T KeyToType<T>(string targetKey) where T : struct{
    return (T)Enum.Parse (typeof(T), targetKey);
  }
  
  /// <summary>
  /// 入力された番号の項目を取得
  /// </summary>
  public static T NoToType<T>(int targetNo) where T : struct{
    return (T)Enum.ToObject (typeof(T), targetNo);
  }

  // 項目から番号を取得する
  public static int TypeToNo<T>(T type) where T : struct{
        int count = 0;
        List<T> list = GetAllInList<T>();
        foreach (T t in list){
            if(t.ToString() == type.ToString()){
                return count;
            }
            count++;
        }

        return -1;
  }
  
  //=================================================================================
  //判定
  //=================================================================================
  
  /// <summary>
  /// 入力された文字列の項目が含まれているか
  /// </summary>
  public static bool ContainsKey<T>(string tagetKey) where T : struct{
    foreach(T t in Enum.GetValues(typeof(T))){
      if(t.ToString() == tagetKey){
        return true;
      }
    }

    return false;
  }
  
  //=================================================================================
  //実行
  //=================================================================================
  
  /// <summary>
  /// 全ての項目に対してデリゲートを実行
  /// </summary>
  public static void ExcuteActionInAllValue<T>(Action<T> action) where T : struct{
    foreach(T t in Enum.GetValues(typeof(T))){
      action (t);
    }
  }
  
}