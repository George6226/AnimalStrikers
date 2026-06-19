using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class TextFileUtility {

	// CSVファイルを読み込む
	public static List<string[]> readCSVFile(string fileName)
	{
		List<string[]> datas = new List<string[]>();

		TextAsset csvFile = Resources.Load(fileName) as TextAsset;
		StringReader reader = new StringReader(csvFile.text);

		while (reader.Peek() > -1)
		{
			// 1行読み込む
			string line = reader.ReadLine();
			datas.Add(line.Split(','));
		}

		return datas;
	}
}
