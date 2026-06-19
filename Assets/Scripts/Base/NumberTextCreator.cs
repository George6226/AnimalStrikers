using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NumberTextCreator : MonoBehaviour {

	// 親プレハブ
	[SerializeField] private GameObject _parentPrefab;
	// プレハブ
	[SerializeField] private GameObject _numPrefab;

	#region Singleton
	// インスタンス
	private static NumberTextCreator _instance;
	public static NumberTextCreator Instance {
		get {
			// インスタンス
			if (_instance == null) {
				_instance = (NumberTextCreator)FindObjectOfType (typeof(NumberTextCreator));

				if (_instance == null) {
					Debug.LogError (typeof(NumberTextCreator) + "is nothing");
				}
			}
			return _instance;
		}
	}
	#endregion Singleton

	[System.Serializable]
	// 数字テキストデータ
	public class NumberTextData{
		// 画像リストデータ
		[SerializeField] private List<Sprite> _spriteListData;
		public List<Sprite> SpriteListData{
			get{ return _spriteListData;}
		}
		// 文字データ
		[SerializeField] private string _strData;
		public string StrData{
			get{ return _strData;}
		}
	}

	// 数字画像のリスト
	[SerializeField] private List<NumberTextData> _numberSpList = new List<NumberTextData>();

	// 全体の長さ
	private float _allLength;
	public float AllLength{
		get{ return _allLength;}
	}

	// 数字画像オブジェクトのリストを生成する
	public GameObject createNumberImageObjectList(string fileName, string numData, GameObject parent = null, NumberTextInfo info = null)
	{
		// 親
		if (parent == null){
			parent = this.gameObject;
		}

		// なければ初期化
		if (info == null) {
			info = new NumberTextInfo ();
		}

		NumberTextData ntData = selectNumberTextData (fileName);
		if (ntData == null) {
			Debug.LogError ("数字画像を作る際のファイルがない!:"+fileName);
		}

		// 親
		GameObject newParent = Instantiate (_parentPrefab,parent.transform);
		newParent.transform.SetParent (parent.transform,false);


		// オブジェクトリストの生成
		List<GameObject> list = createNumberObjects(ntData,numData,info);
		// 全体の長さを計算
		_allLength = calcAllLength(list,info.WordSpace);
		// 位置を変更
		list = changePosition(list,info.WordSpace);

		// 親を設定
		for (int i = 0; i < list.Count; i++) {
			list[i].transform.SetParent (newParent.transform,false);
		}

		// 親の位置を調整
		Vector2 pos = newParent.transform.localPosition;
		// 中央揃えならば
		if (info.TextAlign == NumberTextInfo.TEXT_ALIGN.ALIGN_CENTER) {
			pos.x -= AllLength / 2.0f;
			newParent.transform.localPosition = pos;
		}
		// 右揃えならば
		else if (info.TextAlign == NumberTextInfo.TEXT_ALIGN.ALIGN_RIGHT) {
			pos.x -= AllLength;
			newParent.transform.localPosition = pos;

            //Debug.Log("right pos:" + pos + " allLength:" + AllLength);
        }

		// 親を返す
		return newParent;
	}

	// 数字画像データの選択
	private NumberTextData selectNumberTextData(string fileName)
	{
		for (int i = 0; i < _numberSpList.Count; i++) 
		{
			// 画像リスト取得/なければ次へ
			List<Sprite> spList = _numberSpList [i].SpriteListData;

			if (spList.Count <= 0) {
				continue;
			}

			// ファイル名に含まれていたら
			if (spList [0].name.Contains (fileName)) {
				return _numberSpList [i];
			}
		}

		return null;
	}

	// 数字オブジェクト(s)の生成
	private List<GameObject> createNumberObjects(NumberTextData ntData, string numData, NumberTextInfo info)
	{
		// オブジェクトリスト
		List<GameObject> list = new List<GameObject> ();

		// 作成する文字データ
		for (int i = 0; i < numData.Length; i++) {
			// 参照する文字データ
			for (int j = 0; j < ntData.StrData.Length; j++) 
			{
				// 同じ文字ならば
				if (numData [i] == ntData.StrData [j]) 
				{
					// オブジェクトの生成/画像の設定
					GameObject obj = Instantiate (_numPrefab);
					Sprite sp = ntData.SpriteListData [j];
					obj.GetComponent<Image> ().sprite = sp;
					obj.GetComponent<Image>().color = info.TextColor;
					obj.GetComponent<RectTransform> ().sizeDelta = new Vector2 (sp.rect.size.x * info.Scale.x,
						sp.rect.size.y * info.Scale.y);
					list.Add (obj);
					break;
				}
			}
		}

		return list;
	}

	// 全体の長さを計算する
	private float calcAllLength(List<GameObject> list, float space)
	{
		float length = 0.0f;
		for (int i = 0; i < list.Count; i++) {
			RectTransform rect = list [i].GetComponent<RectTransform> ();
			// 横を追加
			length += rect.sizeDelta.x;

			// 最後でない -> 空白を追加
			if (i != list.Count - 1) {
				length += space;
			}
		}

		return length;
	}

	// 位置を変更
	private List<GameObject> changePosition(List<GameObject> list, float space)
	{
		// 何もない
		if (list.Count <= 0) {
			return list;
		}

		// 初めの位置を調整
		RectTransform firstRect = list [0].GetComponent<RectTransform> ();
		firstRect.localPosition = new Vector2 (firstRect.sizeDelta.x / 2.0f, 0.0f);

		// 次の画像から
		for (int i = 1; i < list.Count; i++) 
		{
			RectTransform rect = list [i].GetComponent<RectTransform> ();
			RectTransform Oldrect = list [i-1].GetComponent<RectTransform> ();

			// Xの位置を計算
			float x = Oldrect.localPosition.x + rect.sizeDelta.x/2.0f + Oldrect.sizeDelta.x/2.0f + space;
			// 位置を変更
			rect.localPosition = new Vector2 (x, 0.0f);
		}

		return list;
	}
}


