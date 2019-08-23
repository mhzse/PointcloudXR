/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using UnityEngine;

public enum StartMode {  IMPORT, LOAD, NONE }
public class StartModeSelect : MonoBehaviour
{
    public event EventHandler<EventArgs> OnStartModeSelectEvent;
    public event EventHandler<EventArgs> OnTransitionDoneEvent;

    private PointCloudManager _PointCloudManager;

    private bool _ScaleUp = false;
    private bool _ScaleDown = false;
    [Range(0.001f, 2f)]
    public float _ScaleUpSpeed = 0.1f;
    [Range(0.001f, 2f)]
    public float _ScaleDownSpeed = 0.1f;
    public float _ScaleMax = 0.5f;
    public float _ScaleMin = 0.5f;

    private float _ScaleXdefault;
    private float _ScaleYdefault;
    private float _ScaleZdefault;

    [Range(0.001f, 200f)]
    public float _RotationSpeed = 0.1f;
    private float _RotationX = 0;
    private float _RotationY = 0;
    private float _RotationZ = 0;

    public bool _RotateClockwise = false;

    public FileManagerXR _FileManagerXR;
    public StartMode _StartMode;

    void Start()
    {
        _ScaleXdefault = transform.localScale.x;
        _ScaleYdefault = transform.localScale.y;
        _ScaleZdefault = transform.localScale.z;
    }

    public void SetPointCloudManager( PointCloudManager pcm )
    {
        _PointCloudManager = pcm;
    }

    void OnTriggerEnter(Collider other)
    {
        if( _FileManagerXR.GetStartMode() == _StartMode )
        {
            SelectMe();
        }
    }

    public void SelectMe()
    {
        _ScaleUp = true;
    }

    private void Update()
    {
        if(_ScaleUp)
        {
            if( transform.localScale.x < (_ScaleXdefault + _ScaleXdefault * _ScaleMax) )
            {
                float scaleX = transform.localScale.x + _ScaleUpSpeed * Time.deltaTime;
                float scaleY = transform.localScale.y + _ScaleUpSpeed * Time.deltaTime;
                float scaleZ = transform.localScale.z + _ScaleUpSpeed * Time.deltaTime;

                transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }
            else
            {
                _ScaleUp = false;
                _ScaleDown = true;
            }
            
        }

        if(_ScaleDown)
        {
            if (transform.localScale.x > (_ScaleXdefault - _ScaleXdefault * (1-_ScaleMin)))
            {
                float scaleX = transform.localScale.x - _ScaleDownSpeed * Time.deltaTime;
                float scaleY = transform.localScale.y - _ScaleDownSpeed * Time.deltaTime * 1.6f;
                float scaleZ = transform.localScale.z - _ScaleDownSpeed * Time.deltaTime;

                transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }
            else
            {
                _ScaleDown = false;
                OnTransitionDoneEvent?.Invoke(this, new PointerClickEventArgs(gameObject.name));
            }
        }

        if(_FileManagerXR.GetStartMode() == _StartMode)
        {
            if(_RotateClockwise)
            {
                _RotationX = transform.localEulerAngles.x;
                _RotationY = transform.localEulerAngles.y + Time.deltaTime * _RotationSpeed;
                _RotationZ = transform.localEulerAngles.z;
            }
            else
            {
                _RotationX = transform.localEulerAngles.x;
                _RotationY = transform.localEulerAngles.y - Time.deltaTime * _RotationSpeed;
                _RotationZ = transform.localEulerAngles.z;
            }

            transform.localEulerAngles = new Vector3(_RotationX, _RotationY, _RotationZ);
        }
    }
}
