/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;
using Valve.VR;
using System;
using Valve.VR.InteractionSystem;

public class ViveCtrl : MonoBehaviour
{
    public  Player  _Player;
    public  Camera  _VRCamera;
    public  Hand    _LeftHand;
    public  Hand    _RightHand;

    public  GameObject _DirectionArrowPrefab;
    private GameObject _DirectionArrow;

    public bool _EnableMovePlatform = false;

    private Transform        _PlayerHeadTransform;
    public PointCloudManager _PointCloudManager;

    public EditToolCtrl _EditTool;

    private int    _RightCtrlIndex;
    private double _TriggerVal;
    private bool   _IsMoving = false;

                       
    public float _ViveMoveForce = 1;
    public float _MaxSpeed = 1;
    public float _Speed = 0;
    public float _ViveNormalDrag = 1;
    public float _ViveStopDrag = 1;
    [Range(0.00001f,1)]
    public float _ViveMass = 1;
    [Range(0, 10)]
    public float _ViveBreakFactor = 1;

    public Material _MenuButtonRightMaterial;
    public Material _MenuButtonMaterialDefault;

    public Material _TrackpadLeftMaterial;
    public Material _TrackpadMaterialDefault;

    public SteamVR_Action_Single  _throttle_action;
    public SteamVR_Action_Boolean _move_backwards_action;

    void Start()
    {

        // Add rigidbody to Player prefab.
        _Player.gameObject.AddComponent<Rigidbody>();
        Rigidbody rb = _Player.GetComponent<Rigidbody>();
        rb.drag = 0.5f;
        rb.angularDrag = 0.3f;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        _PointCloudManager.OnEditToolCollide += OnEditToolCollideListener;


        _DirectionArrow = Instantiate(_DirectionArrowPrefab);
        _DirectionArrow.transform.parent = _RightHand.transform;
        _DirectionArrow.transform.localEulerAngles = new Vector3(58f, 0f, 0f);
        _DirectionArrow.transform.localPosition = new Vector3(0f, -0.0609f, 0.038f); // TODO: Do not hard code this
        _DirectionArrow.SetActive(false);
        
        _PlayerHeadTransform = _VRCamera.transform;

        // Trigger haptic feedback on EditTool scale and offset events
        if (_EditTool != null)
        {
            _EditTool.OnOffsetAccepted += OnOffsetAcceptedListener;
            _EditTool.OnScaleAccepted  += OnScaleAcceptedListener;
        }

        
    } 

    public void SetPlayerPosition(Vector3 pos)
    {
        _Player.transform.position = pos;
    }

    public Camera GetVRCamera()
    {
        return _VRCamera;
    }
    public PointCloudManager PointCloudManagerGet()
    {
        return _PointCloudManager.GetComponent<PointCloudManager>();
    }

    public void MenuButtonRightColorSet(Color color, bool emissive)
    {
        if(emissive)
        {
            _MenuButtonRightMaterial.SetColor("_ColorTint", color);
            Color rimColor = new Color(color.r * 10, color.g * 10, color.b * 10);
            _MenuButtonRightMaterial.SetColor("_RimColor", rimColor);
            _MenuButtonRightMaterial.SetFloat("_RimPower", 1);
        }
        else
        {
            _MenuButtonRightMaterial.SetColor("_ColorTint", color);
            _MenuButtonRightMaterial.SetColor("_RimColor", Color.black);
            _MenuButtonRightMaterial.SetFloat("_RimPower", 6);
        }
    }

    public void MenuButtonRightColorReset()
    {
        _LeftHand.transform.Find("LeftRenderModel(Clone)/controller(Clone)/button").gameObject.GetComponent<Renderer>().material = _MenuButtonMaterialDefault;
    }

    public void MenuButtonLeftMaterialSet(Material material)
    {
        _LeftHand.transform.Find("LeftRenderModel(Clone)/controller(Clone)/button").gameObject.GetComponent<Renderer>().material = material;
    }

    public void MenuButtonLeftMaterialReset()
    {
        _LeftHand.transform.Find("LeftRenderModel(Clone)/controller(Clone)/button").gameObject.GetComponent<Renderer>().material = _MenuButtonMaterialDefault;
    }

    public void TrackpadLeftMaterialSet(Material material)
    {
        //_LeftController.transform.Find("Model/trackpad").gameObject.GetComponent<Renderer>().material = material;
    }

    public void TrackpadLeftMaterialReset()
    {
        //_LeftController.transform.Find("Model/trackpad").gameObject.GetComponent<Renderer>().material = _TrackpadMaterialDefault;
    }

    public Hand GetRightHand()
    {
        return _RightHand;
    }

    public Hand GetLeftHand()
    {
        return _LeftHand;
    }

    public void OnNearestPointEventHandlerAdd(EventHandler<EventArgs> eh)
    {
        _PointCloudManager.OnNearestPointFound += eh;
    }

    public void OnNearestPointEventHandlerRemove(EventHandler<EventArgs> eh)
    {
        _PointCloudManager.OnNearestPointFound -= eh;
    }

    public Vector3 NearestPointGet()
    {
        return _PointCloudManager.GetNearestPoint();
    }

    public void ShowNearestPoint(bool show)
    {
        _PointCloudManager.ShowNearestPoint(show);
    }

    public Vector3 GetNearestPointPosition(Vector3 position)
    {
        return _PointCloudManager.GetNearestPointPosition(position);
    }

    public Camera EyeCameraGet()
    {
        return _VRCamera;
    }
    public bool EditToolGetActive()
    {
        return _EditTool.GetActive();
    }

    // TODO: Move this code to MenuCtrl. MenuCtrl is injecting behaviour onto _ViveCtrl, follow this design principle. Will result in less
    // code in _ViveCtrl.cs. MenuCtrl can populate a reference to EditTool via _ViveCtrl. Remove everything else...
    public void EditToolSetActive(bool active)
    {
        if(active)
        {
            MenuButtonLeftMaterialSet(_EditTool.GetMaterial());

            SteamVR_Actions.pointcloudview.EditToolToggleMode.AddOnChangeListener(EditToolTogleModeButtonPress,      SteamVR_Input_Sources.Any);


            SteamVR_Actions.pointcloudview.EditToolMoveForward.AddOnUpdateListener(EditToolMoveForwardButtonPress,   SteamVR_Input_Sources.Any);
            SteamVR_Actions.pointcloudview.EditToolMoveBackward.AddOnUpdateListener(EditToolMoveBackwardButtonPress, SteamVR_Input_Sources.Any);
            SteamVR_Actions.pointcloudview.EditToolScaleUp.AddOnUpdateListener(EditToolScaleUpButtonPress,           SteamVR_Input_Sources.Any);
            SteamVR_Actions.pointcloudview.EditToolScaleDown.AddOnUpdateListener(EditToolScaleDownButtonPress,       SteamVR_Input_Sources.Any);
        }
        else
        {
            MenuButtonLeftMaterialReset();

            SteamVR_Actions.pointcloudview.EditToolToggleMode.RemoveOnChangeListener(EditToolTogleModeButtonPress,      SteamVR_Input_Sources.Any);
            SteamVR_Actions.pointcloudview.EditToolMoveForward.RemoveOnUpdateListener(EditToolMoveForwardButtonPress,   SteamVR_Input_Sources.Any);
            SteamVR_Actions.pointcloudview.EditToolMoveBackward.RemoveOnUpdateListener(EditToolMoveBackwardButtonPress, SteamVR_Input_Sources.Any);
            SteamVR_Actions.pointcloudview.EditToolScaleUp.RemoveOnUpdateListener(EditToolScaleUpButtonPress,           SteamVR_Input_Sources.Any);
            SteamVR_Actions.pointcloudview.EditToolScaleDown.RemoveOnUpdateListener(EditToolScaleDownButtonPress,       SteamVR_Input_Sources.Any);
        }

        _EditTool.SetActive(active);
    }

    private void EditToolScaleDownButtonPress(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if (SteamVR_Actions.pointcloudview.EditToolScaleDown.GetState(SteamVR_Input_Sources.Any))
        {
            _EditTool.SetScale(EditToolScaleType.DOWN);
        }
    }

    private void EditToolScaleUpButtonPress(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if (SteamVR_Actions.pointcloudview.EditToolScaleUp.GetState(SteamVR_Input_Sources.Any))
        {
            _EditTool.SetScale(EditToolScaleType.UP);
        }
    }

    private void EditToolMoveBackwardButtonPress(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if (SteamVR_Actions.pointcloudview.EditToolMoveBackward.GetState(SteamVR_Input_Sources.Any))
        {
            _EditTool.SetOffset(EditToolOffsetType.IN);
        }
    }

    private void EditToolMoveForwardButtonPress(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if (SteamVR_Actions.pointcloudview.EditToolMoveForward.GetState(SteamVR_Input_Sources.Any))
        {
            _EditTool.SetOffset(EditToolOffsetType.OUT);
        }
    }

    private void EditToolTogleModeButtonPress(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        EditToolMode mode = _EditTool.GetAction();
        EditToolMode new_mode = EditToolMode.NONE;

        if (mode == EditToolMode.NONE)
        {
            new_mode = EditToolMode.SELECT;
        }

        if (mode == EditToolMode.SELECT)
        {
            new_mode = EditToolMode.DESELECT;
        }

        if (mode == EditToolMode.DESELECT)
        {
            new_mode = EditToolMode.DELETE;
        }

        if (mode == EditToolMode.DELETE)
        {
            new_mode = EditToolMode.NONE;
        }

        EditToolSetMode(new_mode);
    }

    void OnOffsetAcceptedListener(object sender, EventArgs e)
    {
        _LeftHand.TriggerHapticPulse(500);
    }

    void OnScaleAcceptedListener(object sender, EventArgs e)
    {
        _RightHand.TriggerHapticPulse(500);
    }

    public void EditToolSetMode(EditToolMode mode)
    {
        _EditTool.SetMode(mode);

        Material material = new Material(_EditTool.GetMaterial());

        MenuButtonLeftMaterialSet(material);

    }

    public EditToolMode EditToolGetAction()
    {
        return _EditTool.GetAction();
    }

 

    public void UserPositionReset()
    {
        _Player.transform.position = _PointCloudManager.GetUserStartPosition();
    }

    void OnEditToolCollideListener(object sender, EventArgs e)
    {
        _LeftHand.TriggerHapticPulse(500);
    }
    void FixedUpdate()
    {
        _Player.GetComponent<Rigidbody>().mass = _ViveMass;


        // The trigger is noisy, looks like only the first digit is reliable.
        _TriggerVal = 0;
        if(_throttle_action != null && _throttle_action.GetActive(SteamVR_Input_Sources.Any))
        {
            float value = (float)Math.Round(_throttle_action.GetAxis(SteamVR_Input_Sources.Any), 1);
            if (value > 0)
            {
                _TriggerVal = value;
            }
        }
        _Speed = _Player.GetComponent<Rigidbody>().velocity.magnitude;

        if (_TriggerVal > 0 && _EnableMovePlatform)
        {
            _DirectionArrow.SetActive(true);

            if (_Speed < _MaxSpeed)
            {
                Vector3 directionOfForce = new Vector3(60f, 0f, 0f);
                Vector3 directionVector = Quaternion.Euler(directionOfForce) * Vector3.forward;

                if (_move_backwards_action != null && _move_backwards_action.GetActive(SteamVR_Input_Sources.Any))
                {
                    if(_move_backwards_action.GetState(SteamVR_Input_Sources.Any))
                    {
                        directionVector = Quaternion.Euler(directionOfForce) * Vector3.back;
                        _DirectionArrow.transform.localEulerAngles = new Vector3(-58f, 180f, 0f);
                        _DirectionArrow.transform.localPosition = new Vector3(0f, 0f, 0f);
                    }
                    else
                    {
                        _DirectionArrow.transform.localEulerAngles = new Vector3(58f, 0f, 0f);
                        _DirectionArrow.transform.localPosition = new Vector3(0f, -0.0609f, 0.038f);
                    }
                }

                Vector3 forceDirection = _RightHand.transform.rotation * directionVector * _ViveMoveForce * (float)_TriggerVal;
                
                _Player.GetComponent<Rigidbody>().drag = _ViveNormalDrag;
                _Player.GetComponent<Rigidbody>().AddForce(forceDirection, ForceMode.Acceleration);

                _Player.GetComponent<Rigidbody>().maxAngularVelocity = 2f;
            }
            else
            {
                float brakeSpeed = _Speed - _MaxSpeed;
                Vector3 normalisedVelocity = _Player.GetComponent<Rigidbody>().velocity.normalized;
                Vector3 brakeVelocity = normalisedVelocity * brakeSpeed * _ViveBreakFactor;
                _Player.GetComponent<Rigidbody>().AddForce(-brakeVelocity);
            }
        }
        else
        {
            _DirectionArrow.SetActive(false);
            _Player.GetComponent<Rigidbody>().drag = _ViveStopDrag;
        }

    }

    public void SetThrottle(float throttle)
    {
        _MaxSpeed      = throttle;
        _ViveMoveForce = throttle;
    }

    public float GetThrottle()
    {
        return _MaxSpeed;
    }
}
