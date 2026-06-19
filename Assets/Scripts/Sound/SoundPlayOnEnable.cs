using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayOnEnable : MonoBehaviour
{
    // サウンド名
    [SerializeField] private string _soundName;
    // 秒数
    [SerializeField] private float _waitSec;

    // 表示時
    void OnEnable()
    {
        StartCoroutine(playSoundCoroutine(_waitSec));
    }

    private IEnumerator playSoundCoroutine(float sec)
    {
        yield return new WaitForSeconds(sec);

        SoundManager.Instance.ManagerSE.playSoundEffect(_soundName);
    }
}
