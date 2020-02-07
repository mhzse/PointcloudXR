using System;
using System.IO;
using UnityEngine;
using Valve.VR.InteractionSystem;
using Valve.VR;

public class StartView : MonoBehaviour
{
    public  MenuCtrl          _MenuCtrl; // Need to reset screen visibility...
    public  ViveCtrl          _ViveCtrl;
    private PointCloudManager _PointCloudManager;
    public  SpinnerXRCtrl     _SpinnerCtrl;

    public GameObject _ColliderPrefab;

    public StartModeSelect _ImportStart;
    public StartModeSelect _LoadStart;

    public FileManagerXR _FileManager;

    private Hand _LeftHand;
    private Hand _RightController;

    private float _Progress = 0;

    private bool _ProgressUpdate = false;
    private bool _Active = false;

    public GameObject  _Title;

    private Camera     _VRCamera; 
    public  GameObject _Selectors;
    public  GameObject _Spotlight;

    public SteamVR_ActionSet _action_set_start_view;
    private bool _disable_other_action_sets = false;

    public SteamVR_ActionSet _action_set_pointcloud_view;

    void Start ()
    {
        _PointCloudManager = _ViveCtrl.PointCloudManagerGet();
        _VRCamera = _ViveCtrl.GetVRCamera();

        _LeftHand = _ViveCtrl.GetLeftHand();
        _RightController = _ViveCtrl.GetRightHand();

        GameObject leftCollider = Instantiate(_ColliderPrefab);
        leftCollider.name = "StartView_LeftCollider";
        leftCollider.transform.parent = _LeftHand.transform;
        leftCollider.transform.localPosition = new Vector3(-0.0015f, -0.0372f, 0.0073f);
        leftCollider.transform.localEulerAngles = new Vector3(57.497f, 19.942f, 12.817f);

        GameObject rightCollider = Instantiate(_ColliderPrefab);
        rightCollider.name = "StartView_RightCollider";
        rightCollider.transform.parent = _RightController.transform;
        rightCollider.transform.localPosition = new Vector3(-0.0015f, -0.0372f, 0.0073f);
        rightCollider.transform.localEulerAngles = new Vector3(57.497f, 19.942f, 12.817f);

        transform.localPosition = new Vector3(0f, 1.5f, 0f);
        transform.localEulerAngles = new Vector3(0.0f, 0f, 0.0f);

        _ImportStart.OnTransitionDoneEvent += OnTransitionDone;
        _LoadStart.OnTransitionDoneEvent += OnTransitionDone;

        _ImportStart.SetPointCloudManager(_PointCloudManager);
        _LoadStart.SetPointCloudManager(_PointCloudManager);

        _PointCloudManager.OnProgress += OnProgressEvent;
        _PointCloudManager.OnGPUSetDataDone += OnLoadComplete;

        _SpinnerCtrl.SetActive(false);

        SetComponentsActive(true);
    }

    private void ActionSetStartViewEnabled(bool enabled)
    {
        if(_action_set_start_view != null)
        {
            if (enabled)
            {
                _action_set_start_view.Activate();
            }
            else
            {
                _action_set_start_view.Deactivate();
            }
        }
    }

    private void ActionSetPointcloudViewEnabled(bool enabled)
    {
        if (_action_set_pointcloud_view != null)
        {
            if (enabled)
            {
                _action_set_pointcloud_view.Activate();
            }
            else
            {
                _action_set_pointcloud_view.Deactivate();
            }
        }
    }

    private void OnTransitionDone(object sender, EventArgs e)
    {
        string name = ((PointerClickEventArgs)e).ClickedName; 
        
        if (name == "Import")
        {
            try
            {
                _PointCloudManager.StartImportLAS();
                _Progress = 0;
                SetComponentsActive(false);
                _SpinnerCtrl.SetActive(true);
            }
            catch(FileNotFoundException ex)
            {
                print(ex.ToString());
            }
        }

        if (name == "Load")
        {
            _Progress = 0;
            SetComponentsActive(false);
            _PointCloudManager.StartLoadFile();
            _SpinnerCtrl.SetActive(true);
        }

        _MenuCtrl.ResetScreens();
    }

    void OnProgressEvent(object sender, EventArgs e)
    {
        _Progress = _PointCloudManager.ProgressGet();
        _ProgressUpdate = true;
    }

    void OnLoadComplete(object sender, EventArgs e)
    {
        _SpinnerCtrl.SetActive(false);
        ActionSetPointcloudViewEnabled(true);
    }

    public void SetComponentsActive( bool active )
    {
        ActionSetStartViewEnabled(active);

        if(active)
        {
            ActionSetPointcloudViewEnabled(false);
        }

        _ViveCtrl.SetPlayerPosition(Vector3.zero);

        _Selectors.SetActive(active);
        _ImportStart.transform.localScale = new Vector3(0.03f, 0.05f, 0.03f);
        _LoadStart.transform.localScale = new Vector3(0.03f, 0.05f, 0.03f);
        _Spotlight.SetActive(active);
        _FileManager.SetComponentsActive(active);

        _Title.SetActive(active);
        _Active = active;

        if(active)
        {
            //SteamVR_Input.
            SteamVR_Actions.startview.resetposition.AddOnChangeListener(ResetStartViewPosition, SteamVR_Input_Sources.Any);
        }
        else
        {
            SteamVR_Actions.startview.resetposition.RemoveOnChangeListener(ResetStartViewPosition, SteamVR_Input_Sources.Any);
        }
    }

    private void ResetStartViewPosition(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        transform.position = _VRCamera.transform.position + _VRCamera.transform.forward * 0.5f;
        transform.LookAt(_VRCamera.transform);

        transform.position = new Vector3(transform.position.x, _VRCamera.transform.position.y, transform.position.z);

        Vector3 localEuler = transform.localEulerAngles;
        localEuler.x = 0;
        localEuler.y = transform.localEulerAngles.y - 90;
        transform.localEulerAngles = localEuler;

        print("ResetPosition");
    }

    public bool GetActive()
    {
        return _Active;
    }

}
