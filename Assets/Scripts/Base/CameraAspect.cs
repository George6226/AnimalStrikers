using UnityEngine;
using System.Collections;

public class CameraAspect : MonoBehaviour
{
	// 画面サイズ
	[SerializeField] public float _sizeH = 2208;
	[SerializeField] public float _sizeW = 1242;

	void Awake () {
		Camera cam = gameObject.GetComponent<Camera>();
		float baseAspect = _sizeH/_sizeW;
		float nowAspect = (float)Screen.height/(float)Screen.width;
		float changeAspect;

		if(baseAspect>nowAspect){   
			changeAspect = nowAspect/baseAspect;
			cam.rect=new Rect((1-changeAspect)*0.5f,0,changeAspect,1);
		}else{
			changeAspect = baseAspect/nowAspect;
			cam.rect=new Rect(0,(1-changeAspect)*0.5f,1,changeAspect);
		}

		//Debug.Log ("ChangeAsp:" + changeAspect + " base:" + baseAspect + " now:" + nowAspect);

//		Destroy(this);
	}
}