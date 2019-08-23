/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using UnityEngine;

public class PointerClickEventArgs : EventArgs
{
    public string ClickedName { get; private set; }

    public PointerClickEventArgs(string name)
    {
        this.ClickedName = name;
    }
}

public class MenuPointerCtrl : MonoBehaviour
{
    public bool _Debug;
    public event EventHandler<EventArgs> OnPointerTriggerEnterEvent;
    private Vector3 _OriginalLocalPosition;
    private Vector3 _OtherTriggerEnterGlobalPosition;
    private Vector3 _PointerGlobalPositionOnTriggerEnter;

    private GameObject _ColliderObject;
    public string _MenuPointerStopperName = "MenuPointerStop";

    private GameObject _DebugLine;
    private GameObject _PlanePointSphere;
    private GameObject _ParentPointSphere;

    private float _DistanceToPlaneOnTriggerEnter = 0;

    void Start ()
    {
        _Debug = false;

        _OriginalLocalPosition = transform.localPosition;
        _ColliderObject = new GameObject
        {
            name = "I_am_no_one"
        };

        // Must start with debug on for this to work
        if (_Debug)
        {
            _DebugLine = new GameObject
            {
                name = "MeasureLine"
            };
            _DebugLine.AddComponent<LineRenderer>();
            _DebugLine.GetComponent<LineRenderer>().SetPosition(0, Vector3.zero);
            _DebugLine.GetComponent<LineRenderer>().SetPosition(1, Vector3.zero);

            _PlanePointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _PlanePointSphere.name = "_PlanePoint";
            _PlanePointSphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            Material planePointMaterial = new Material(Shader.Find("Standard"));
            planePointMaterial.SetColor("_Color", Color.green);
            _PlanePointSphere.GetComponent<Renderer>().material = planePointMaterial;
            _PlanePointSphere.SetActive(false);

            _ParentPointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _ParentPointSphere.name = "_ParentPoint";
            _ParentPointSphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            Material parentPointMaterial = new Material(Shader.Find("Standard"));
            parentPointMaterial.SetColor("_Color", Color.red);
            _ParentPointSphere.GetComponent<Renderer>().material = parentPointMaterial;
            _ParentPointSphere.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.name == _MenuPointerStopperName)
        {
            Vector3 v1 = transform.parent.position - other.transform.position;
            Vector3 v2 = Vector3.ProjectOnPlane(v1, other.transform.forward);
            _DistanceToPlaneOnTriggerEnter = (transform.parent.position - (other.transform.position + v2)).magnitude;

            _OtherTriggerEnterGlobalPosition = other.transform.position;
            _PointerGlobalPositionOnTriggerEnter = transform.position;
            _ColliderObject = other.gameObject;
        }

        OnPointerTriggerEnterEvent?.Invoke(this, new PointerClickEventArgs(other.gameObject.name));
    }

    void OnTriggerStay(Collider other)
    {
        if (other.name == _MenuPointerStopperName)
        {
            Vector3 v1 = transform.parent.position - other.transform.position;
            Vector3 v2 = Vector3.ProjectOnPlane(v1, other.transform.forward);
            float distanceToPlane = (transform.parent.position - (other.transform.position + v2)).magnitude;
            if ( distanceToPlane <= _DistanceToPlaneOnTriggerEnter )
            {
                transform.position = other.transform.position + (_PointerGlobalPositionOnTriggerEnter - _OtherTriggerEnterGlobalPosition);
                transform.position = transform.position + transform.forward * 0.001f; // Push it a bit into the collider so that it stays put
            }

            if (_Debug)
            {
                _PlanePointSphere.SetActive(true);
                _PlanePointSphere.transform.position = other.transform.position + v2;
                _ParentPointSphere.SetActive(true);
                _ParentPointSphere.transform.position = transform.parent.position;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {

        Vector3 v1 = transform.parent.position - other.transform.position;
        Vector3 v2 = Vector3.ProjectOnPlane(v1, other.transform.forward);
        float distanceToPlane = (transform.parent.position - (other.transform.position + v2)).magnitude;
        if (other.name == _MenuPointerStopperName && distanceToPlane > _DistanceToPlaneOnTriggerEnter)
        {
            transform.localPosition = new Vector3(0f, 0f, 0f);

            if (_Debug)
            {
                _DebugLine.GetComponent<LineRenderer>().SetPosition(0, Vector3.zero);
                _DebugLine.GetComponent<LineRenderer>().SetPosition(1, Vector3.zero);
            }
        }

        if (_Debug)
        {
            _PlanePointSphere.SetActive(false);
            _ParentPointSphere.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (_ColliderObject != null)
        {
            if (_ColliderObject.name == _MenuPointerStopperName)
            {
                Vector3 v1 = transform.parent.position - _ColliderObject.transform.position;
                Vector3 v2 = Vector3.ProjectOnPlane(v1, _ColliderObject.transform.forward);
                float distanceToPlane = (transform.parent.position - (_ColliderObject.transform.position + v2)).magnitude;

                if (distanceToPlane > _DistanceToPlaneOnTriggerEnter)
                {
                    transform.localPosition = new Vector3(0f, 0f, 0f);
                }

                if (_Debug)
                {
                    _DebugLine.GetComponent<LineRenderer>().SetPosition(0, _ColliderObject.transform.position + v2);
                    _DebugLine.GetComponent<LineRenderer>().SetPosition(1, transform.parent.position);
                    _DebugLine.GetComponent<LineRenderer>().startWidth = 0.001f;
                    _DebugLine.GetComponent<LineRenderer>().endWidth = 0.001f;
                }
            }
        }
    }
}
