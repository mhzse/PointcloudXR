/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;

public class ButtonParametric : MonoBehaviour
{
    public float _ScaleX = 1;
    public bool _SetScaleX = false;

    private GameObject _BaseCornerUpperLeft;
    private GameObject _BaseCornerUpperRight;
    private GameObject _BaseCornerLowerLeft;
    private GameObject _BaseCornerLowerRight;

    private GameObject _BaseSideUpper;
    private GameObject _BaseSideLower;
    private GameObject _BaseSideLeft;
    private GameObject _BaseSideRight;

    private Vector3 _BaseCornerUpperLeftPos;
    private Vector3 _BaseCornerUpperRightPos;
    private Vector3 _BaseCornerLowerLeftPos;
    private Vector3 _BaseCornerLowerRightPos;

    private Vector3 _BaseSideUpperPos;
    private Vector3 _BaseSideLowerPos;
    private Vector3 _BaseSideLeftPos;
    private Vector3 _BaseSideRightPos;

    void Start ()
    {
        _BaseCornerUpperLeft = transform.Find("BaseCornerUpperLeft").gameObject;
        _BaseCornerUpperRight = transform.Find("BaseCornerUpperRight").gameObject;
        _BaseCornerLowerLeft = transform.Find("BaseCornerLowerLeft").gameObject;
        _BaseCornerLowerRight = transform.Find("BaseCornerLowerRight").gameObject;

        _BaseSideUpper = transform.Find("BaseSideUpper").gameObject;
        _BaseSideLower = transform.Find("BaseSideLower").gameObject;
        _BaseSideLeft = transform.Find("BaseSideLeft").gameObject;
        _BaseSideRight = transform.Find("BaseSideRight").gameObject;

        _BaseCornerUpperLeftPos = transform.Find("BaseCornerUpperLeft").localPosition;
        _BaseCornerUpperRightPos = transform.Find("BaseCornerUpperRight").position;
        _BaseCornerLowerLeftPos = transform.Find("BaseCornerLowerLeft").localPosition;
        _BaseCornerLowerRightPos = transform.Find("BaseCornerLowerRight").localPosition;

        _BaseSideUpperPos = transform.Find("BaseSideUpper").localPosition;
        _BaseSideLowerPos = transform.Find("BaseSideLower").localPosition;
        _BaseSideLeftPos = transform.Find("BaseSideLeft").localPosition;
        _BaseSideRightPos = transform.Find("BaseSideRight").localPosition;
    }
	
    private void SetScaleX( float scaleX )
    {
        _BaseSideUpper.transform.localScale = new Vector3(_BaseSideUpper.transform.localScale.x * _ScaleX,
                                                          _BaseSideUpper.transform.localScale.y,
                                                          _BaseSideUpper.transform.localScale.z);

        _BaseCornerUpperRight.transform.localPosition = new Vector3(_BaseCornerUpperRightPos.x * _ScaleX,
                                                               _BaseCornerUpperRight.transform.position.y,
                                                               _BaseCornerUpperRight.transform.position.z);
    }

    void Update ()
    {
        if(_SetScaleX)
        {
            SetScaleX(_ScaleX);
            _SetScaleX = false;
        }

    }
}
