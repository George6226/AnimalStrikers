using System.Text;
using System.IO;
using UnityEngine;
using System;

namespace Game.Goap
{
    public static class DebugLogger
    {
        private static StringBuilder _builder = new StringBuilder();
        private static string _logFilePath;
        private static bool _initialized = false;

        private static void Init()
        {
            if (_initialized) return;
            string dir = Application.dataPath + "/DebugLog";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string nameWithTime = $"GoapDebugLog_{timeStamp}.txt";
            _logFilePath = Path.Combine(dir, nameWithTime);
            _initialized = true;
        }

        public static void Clear()
        {
            Init();
            _builder.Clear();
        }

        public static void Log(string message)
        {
            Init();
            
            // 特定の文字列を含んでいる場合のみログを出力
            if (ShouldLogMessage(message))
            {
                Debug.Log("出力あり:"+message);
                _builder.AppendLine(message);
            }
        }

        private static bool ShouldLogMessage(string message)
        {
            // ログに出力したい特定の文字列をここに追加
            string[] keywords = {
            };

            // キーワードが空の場合は全てのログを出力
            if (keywords.Length == 0)
            {
                return true;
            }

            // いずれかのキーワードが含まれている場合はログを出力
            foreach (string keyword in keywords)
            {
                if (message.Contains(keyword))
                {
                    return true;
                }
            }

            return false; // キーワードが含まれていない場合はログを出力しない
        }

        public static void Save()
        {
            Init();
            try
            {
                File.AppendAllText(_logFilePath, _builder.ToString());
                _builder.Clear();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DebugLogger Save Error: {e.Message}");
            }
        }
    }
} 