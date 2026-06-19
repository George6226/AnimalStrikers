using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;
using UnityEditor;

public class OverrideFile : AssetPostprocessor {

	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
	{
		if(Event.current == null || Event.current.type != EventType.DragPerform ){	return; }

		foreach( var asset in importedAssets )
		{
			var rootAsset = ParentFile(asset);
			if( rootAsset == null)
				continue;

			if( EditorUtility.DisplayDialog("override", rootAsset + "を上書きしますか？","上書き", "両方残す" ) )
			{
				File.Copy(asset, rootAsset, true);
				AssetDatabase.DeleteAsset(asset);

				AssetDatabase.ImportAsset(rootAsset);
				AssetDatabase.Refresh();
			}
		}
	}

	static string ParentFile(string name)
	{
		var match = Regex.Match(name, @"(?<item>.*) 1.(?<extension>.*)");
		if(! match.Success )
			return null;

		return string.Format("{0}.{1}", match.Groups["item"] ,match.Groups["extension"]);
	}
}