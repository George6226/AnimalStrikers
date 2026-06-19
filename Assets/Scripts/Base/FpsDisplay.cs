using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FpsDisplay : MonoBehaviour
{
    public bool _isDisplay;
    // for ui.
    private int screenLongSide;
    private Rect boxRect;
    private GUIStyle style = new GUIStyle();

    // for fps calculation.
    private int frameCount;
    private float elapsedTime;
    private double frameRate;

    /// <summary>
    /// Initialization
    /// </summary>
    private void Awake()
    {
        if (_isDisplay == false) return;

        DontDestroyOnLoad(gameObject);
        UpdateUISize();
    }

    /// <summary>
    /// Monitor changes in resolution and calcurate FPS
    /// </summary>
    private void Update()
    {
        if (_isDisplay == false) return;

        // FPS calculation
        frameCount++;
        elapsedTime += Time.deltaTime;
        if (elapsedTime > 0.5f)
        {
            frameRate = System.Math.Round(frameCount / elapsedTime, 1, System.MidpointRounding.AwayFromZero);
            frameCount = 0;
            elapsedTime = 0;

            // Update the UI size if the resolution has changed
            if (screenLongSide != Mathf.Max(Screen.width, Screen.height))
            {
                UpdateUISize();
            }
        }
    }

    /// <summary>
    /// Resize the UI according to the screen resolution
    /// </summary>
    private void UpdateUISize()
    {
        if (_isDisplay == false) return;

        screenLongSide = Mathf.Max(Screen.width, Screen.height);
        var rectLongSide = screenLongSide / 10;
        boxRect = new Rect(1, 1, rectLongSide, rectLongSide / 3);
        style.fontSize = (int)(screenLongSide / 36.8);
        style.normal.textColor = Color.white;
    }

    /// <summary>
    /// Display FPS
    /// </summary>
    private void OnGUI()
    {
        if (_isDisplay == false) return;

        GUI.Box(boxRect, "");
        GUI.Label(boxRect, " " + frameRate + "fps", style);
    }
}
