using System.IO;
using UnityEngine;

public static class ES3PathConfigurator
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ConfigureDefaultPath()
    {
        if (ES3Settings.defaultSettings == null)
        {
            Debug.LogWarning("[ES3PathConfigurator] ES3 default settings are null.");
            return;
        }

        string appName = GetAppNameForSaveFolder();
        string baseFileName = Path.GetFileName(ES3Settings.defaultSettings.path);
        if (string.IsNullOrEmpty(baseFileName))
        {
            baseFileName = "SaveFile.es3";
        }

        string relativePath = Path.Combine(appName, baseFileName);
        ES3Settings.defaultSettings.path = relativePath;

        string absoluteDirectory = Path.Combine(Application.persistentDataPath, appName);
        Directory.CreateDirectory(absoluteDirectory);

        Debug.Log($"[ES3PathConfigurator] ES3 path: {Path.Combine(Application.persistentDataPath, relativePath)}");
    }

    private static string GetAppNameForSaveFolder()
    {
        if (Application.platform != RuntimePlatform.OSXPlayer)
        {
            return Application.productName;
        }

        try
        {
            // コマンドライン先頭は通常 ".../<BundleName>.app/Contents/MacOS/<ExecutableName>"
            string[] args = System.Environment.GetCommandLineArgs();
            if (args != null && args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                string executablePath = Path.GetFullPath(args[0]);
                int appIndex = executablePath.IndexOf(".app/", System.StringComparison.OrdinalIgnoreCase);
                if (appIndex > 0)
                {
                    string appBundlePath = executablePath.Substring(0, appIndex + 4);
                    string appBundleName = Path.GetFileNameWithoutExtension(appBundlePath);
                    if (!string.IsNullOrEmpty(appBundleName))
                    {
                        return appBundleName;
                    }
                }
            }

            // フォールバック: Application.dataPath から推測
            string fallbackBundlePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string fallbackBundleName = Path.GetFileNameWithoutExtension(fallbackBundlePath);
            if (!string.IsNullOrEmpty(fallbackBundleName))
            {
                return fallbackBundleName;
            }
        }
        catch
        {
        }

        return Application.productName;
    }
}
