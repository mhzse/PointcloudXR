/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using UnityEngine;
using Valve.VR;

public class FrameCount : MonoBehaviour 
{

    public double FPS;
    public int droppedFramesCount;
    private double lastUpdate;
    public int _GUISizeFactor = 3;


    private void Update()
    {
        FrameCounter();

    }

    private void FrameCounter()
    {
        var compositor = OpenVR.Compositor;

        if (compositor != null)
        {
            var timing = new Compositor_FrameTiming();
            timing.m_nSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Compositor_FrameTiming));
            compositor.GetFrameTiming(ref timing, 0);

            var update = timing.m_flSystemTimeInSeconds;
            if (update > lastUpdate)
            {
                var framerate = (lastUpdate > 0.0f) ? 1.0f / (update - lastUpdate) : 0.0f;
                lastUpdate = update;
                FPS = framerate;
                droppedFramesCount = (int)timing.m_nNumDroppedFrames;
            }
            else
            {
                lastUpdate = update;
            }
        }
    }

    void OnGUI()
    {

        GUIStyle styleLevel = new GUIStyle();
        styleLevel.normal.textColor = Color.gray;
        styleLevel.fontSize = 30;
        styleLevel.alignment = TextAnchor.UpperCenter;

        GUI.Label(new Rect(Screen.width / 2 - 35, 40, 70, 20), Convert.ToInt32(FPS).ToString(), styleLevel);
    }
}
