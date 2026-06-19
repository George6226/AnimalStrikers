using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSSetting : MonoBehaviour {
	void Awake () {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        //		Resources.UnloadUnusedAssets ();
    }
}
