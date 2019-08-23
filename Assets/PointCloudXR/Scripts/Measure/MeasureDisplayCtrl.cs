/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using TMPro;
using UnityEngine;

public class MeasureDisplayCtrl : MonoBehaviour
{
    private TextMeshPro _Distance;
    public  GameObject _Frame;
    public  GameObject _Canvas;
    public  GameObject _Parent;

    void Start ()
    {
        _Frame.SetActive(false);
        _Distance = transform.Find("Frame/Distance").GetComponent<TextMeshPro>();

        transform.parent = _Parent.transform;
        transform.localPosition = new Vector3(0.0f, 0.01f, 0.006f);
        transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
    }

    public void DisplaySetActive(bool active)
    {
        _Frame.SetActive(active);
    }

    public void DistanceSet(float distance)
    {
        _Distance.text = Math.Round(distance, 0).ToString() + " mm";
    }
}
