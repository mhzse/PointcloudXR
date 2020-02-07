/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using UnityEngine;
using Valve.VR;

public enum RestrictMode
{
    XZ, Y, none
}

public enum MeasureState
{
    READY, SEARCHING_FIRST, SEARCHING_SECOND, CREATING, DONE, OFF
}

public class MeasureToolCtrl : MonoBehaviour
{
    // bool defaults to false in C#
    public ViveCtrl     _ViveCtrl;
    public GameObject   _PointerPrefab;
    public Material     _PointerMaterial;
    public Material     _LineMaterial;
    public MeasureDisplayCtrl _Display;
    public Color _ReadyStateColor;
    public Color _DoneStateColor;

    public bool _SnapToPoint { get; set; }
    public Vector3 _NearestPoint { get; set; }
    public bool _NearestPointFound { get; set; }
    public bool _CreatingLine { get; set; }
    public bool _FirstPointSet { get; set; }
    public bool _SecondPointSet { get; set; }
    public bool _LineDone { get; set; }
    public Vector3[] _LinePositions { get; set; }
    public GameObject _Line { get; set; }
    public bool _Active { get; set; }
    public RestrictMode _MeasurementLineRestrictMode = RestrictMode.none;

    private GameObject _MeasurePointer;

    private MeasureState _State;

    void Start ()
    {
        _SnapToPoint = false;
        _NearestPoint = Vector3.zero;
        _NearestPointFound = false;
        _CreatingLine = false;
        _FirstPointSet = false;
        _SecondPointSet = false;
        _LineDone = false;
        _LinePositions = new Vector3[2];
        _Active = false;

        // Create a visual startpoint for mesaurement and attach it to the right controller.
        _MeasurePointer = Instantiate(_PointerPrefab);
        _MeasurePointer.transform.parent = _ViveCtrl.GetRightHand().transform;
        _MeasurePointer.transform.localPosition = new Vector3(0.0f, -0.002f, 0.06f);
        _MeasurePointer.transform.localRotation = Quaternion.Euler(-33.0f, 0.0f, 0.0f);
        _MeasurePointer.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        _MeasurePointer.GetComponent<Renderer>().material = _PointerMaterial;
        _MeasurePointer.AddComponent<BoxCollider>();
        _MeasurePointer.GetComponent<BoxCollider>().isTrigger = true;
        _MeasurePointer.name = "MeasurePointer";
        _MeasurePointer.SetActive(false);

        // Initialize the mesaurement line.
        _Line = new GameObject();
        _Line.name = "MeasureLine";
        _Line.AddComponent<LineRenderer>();
        _Line.GetComponent<LineRenderer>().SetPosition(0, Vector3.zero);
        _Line.GetComponent<LineRenderer>().SetPosition(1, Vector3.zero);
        _Line.GetComponent<LineRenderer>().material = _LineMaterial;
        
    }
	
    private void SetState(MeasureState state)
    {
        _State = state;

        if(state == MeasureState.READY)
        {
            _ViveCtrl.MenuButtonRightColorSet(_ReadyStateColor, false);
        }

        if (state == MeasureState.CREATING)
        {
            _ViveCtrl.MenuButtonRightColorSet(_ReadyStateColor, true);
        }

        if (state == MeasureState.SEARCHING_FIRST)
        {
            _ViveCtrl.MenuButtonRightColorSet(_ReadyStateColor, true);
        }

        if (state == MeasureState.SEARCHING_SECOND)
        {
            _ViveCtrl.MenuButtonRightColorSet(_ReadyStateColor, true);
        }

        if (state == MeasureState.DONE)
        {
            _ViveCtrl.MenuButtonRightColorSet(_DoneStateColor, false);
        }

        if (state == MeasureState.OFF)
        {
            _ViveCtrl.MenuButtonRightColorReset();
        }
    }

    public void SetActive(bool active)
    {
        _Active = active;

        if (_Active)
        {
            Reset();
            _MeasurePointer.SetActive(true);
            _Display.DisplaySetActive(true);

            SteamVR_Actions.pointcloudview.MeasureToolToggleState.AddOnChangeListener(Measure, SteamVR_Input_Sources.Any);
            SetState(MeasureState.READY);
        }
        else
        {
            Reset();
            _MeasurePointer.SetActive(false);
            _Display.DisplaySetActive(false);

            SteamVR_Actions.pointcloudview.MeasureToolToggleState.RemoveOnChangeListener(Measure, SteamVR_Input_Sources.Any);
            SetState(MeasureState.OFF);
        }
    }

    public bool GetActive()
    {
        return _Active;
    }

    public void SnapToPointSetActive(bool active)
    {
        _SnapToPoint = active;
    }

    public bool SnapToPointGetActive()
    {
        return _SnapToPoint;
    }

    public void RestrictModeSet(RestrictMode restrictMode)
    {
        _MeasurementLineRestrictMode = restrictMode;
    }

    public RestrictMode RestrictModeGet()
    {
        return _MeasurementLineRestrictMode;
    }

    private void LateUpdate ()
    {
        if (_State == MeasureState.SEARCHING_FIRST && _SnapToPoint)
        {
            LineRenderer line = _Line.GetComponent<LineRenderer>();
            _NearestPoint = _ViveCtrl.GetNearestPointPosition(_MeasurePointer.transform.position);
            _LinePositions[0] = _NearestPoint;
            line.SetPosition(0, _NearestPoint);

            line.startWidth = 0.005f;
            line.endWidth = 0.005f;

            _LinePositions[1] = _MeasurePointer.transform.position;
            line.SetPosition(1, _LinePositions[1]);

            float lineLength = Vector3.Distance(_LinePositions[0], _LinePositions[1]) * 1000;// Length in mm
            _Display.DistanceSet(lineLength);
        }

        if (_State == MeasureState.SEARCHING_SECOND && _SnapToPoint)
        {
            LineRenderer line = _Line.GetComponent<LineRenderer>();
            _NearestPoint = _ViveCtrl.GetNearestPointPosition(_MeasurePointer.transform.position);

            _LinePositions[1] = _NearestPoint;
            line.SetPosition(1, _LinePositions[1]);

            float lineLength = Vector3.Distance(_LinePositions[0], _LinePositions[1]) * 1000;// Length in mm
            _Display.DistanceSet(lineLength);
        }

        if (_State == MeasureState.SEARCHING_SECOND && !_SnapToPoint)
        {
            DrawLine();
        }
    }

    private void DrawLine()
    {
        LineRenderer l = _Line.GetComponent<LineRenderer>();

        if (_MeasurementLineRestrictMode == RestrictMode.none)
        {
            _LinePositions[1] = _MeasurePointer.transform.position;
        }

        if (_MeasurementLineRestrictMode == RestrictMode.XZ)
        {
            _LinePositions[1] = new Vector3(_MeasurePointer.transform.position.x, _LinePositions[0].y, _MeasurePointer.transform.position.z);
        }

        if (_MeasurementLineRestrictMode == RestrictMode.Y)
        {
            _LinePositions[1] = new Vector3(_LinePositions[0].x, _MeasurePointer.transform.position.y, _LinePositions[0].z);
        }

        l.SetPosition(1, _LinePositions[1]);

        float lineLength = Vector3.Distance(_LinePositions[0], _LinePositions[1]) * 1000;// Length in mm
        _Display.DistanceSet(lineLength);
    }

    private void DrawLineSnap()
    {
        LineRenderer line = _Line.GetComponent<LineRenderer>();

        if(_NearestPointFound && _FirstPointSet && !_SecondPointSet)
        {
            line.SetPosition(1, _NearestPoint);
            _NearestPointFound = false;

            _NearestPoint = _ViveCtrl.GetNearestPointPosition(_MeasurePointer.transform.position);
            _LinePositions[1] = _NearestPoint;
            line.SetPosition(1, _NearestPoint);
        }
        
        float lineLength = Vector3.Distance(_LinePositions[0], _LinePositions[1]) * 1000;// Length in mm
        _Display.DistanceSet(lineLength);
    }

    public void Reset() // Enter Ready state
    {
        _NearestPoint = Vector3.zero;
        _FirstPointSet = false;
        _SecondPointSet = false;
        _LinePositions = new Vector3[2];
        _Line.GetComponent<LineRenderer>().SetPosition(0, Vector3.zero);
        _Line.GetComponent<LineRenderer>().SetPosition(1, Vector3.zero);

        _Display.DistanceSet(0f);
    }

    public void Measure(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        LineRenderer line = _Line.GetComponent<LineRenderer>();

        if (_State == MeasureState.READY && _SnapToPoint)
        {
            _State = MeasureState.SEARCHING_FIRST;
            return;
        }
        else if (_State == MeasureState.READY && !_SnapToPoint)
        {
            _State = MeasureState.SEARCHING_FIRST;
        }

        if (_State == MeasureState.SEARCHING_FIRST)
        {
            if (_SnapToPoint)
            {
                _NearestPointFound = false;

                _NearestPoint = _ViveCtrl.GetNearestPointPosition(_MeasurePointer.transform.position);
                _LinePositions[0] = _NearestPoint;
                line.SetPosition(0, _NearestPoint);

                line.startWidth = 0.005f;
                line.endWidth = 0.005f;


                _FirstPointSet = true;
                SetState(MeasureState.SEARCHING_SECOND);
                return;
            }
            else
            {
                _LinePositions[0] = _MeasurePointer.transform.position;
                line.SetPosition(0, _MeasurePointer.transform.position);
                line.startWidth = 0.005f;
                line.endWidth = 0.005f;
                _FirstPointSet = true;
                SetState(MeasureState.SEARCHING_SECOND);
                return;
            }
        }

        if (_State == MeasureState.SEARCHING_SECOND)
        {
            if (_SnapToPoint)
            {
                _NearestPointFound = false; // Reset for next possible line with snap

                _NearestPoint = _ViveCtrl.GetNearestPointPosition(_MeasurePointer.transform.position);

                _LinePositions[1] = _NearestPoint;
                line.SetPosition(1, _NearestPoint);

                _SecondPointSet = true;
                SetState(MeasureState.DONE);
                return;
            }

            _SecondPointSet = true;
            SetState(MeasureState.DONE);
            return;
        }

        if (_State == MeasureState.DONE)
        {
            Reset();
            SetState(MeasureState.READY);
            return;
        }
    }

    void OnNearestPointFoundEvent(object sender, EventArgs e)
    {
        _ViveCtrl.OnNearestPointEventHandlerRemove(OnNearestPointFoundEvent);
        _NearestPoint = _ViveCtrl.NearestPointGet();
        _NearestPointFound = true;
    }
}
