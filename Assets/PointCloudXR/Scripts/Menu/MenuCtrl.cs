/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public enum ExportFormat {  XYZ, LAS };
public enum ColorByParam {  RGB, CLASS, HEIGHT, INTENSITY, POINT_SOURCE_ID, NONE };

public class MenuCtrl : MonoBehaviour
{
    public bool _Activated = false;

    public ViveCtrl          _ViveCtrl;
    public PointCloudManager _PointCloudManager;

    public  GameObject      _MenuPointerPrefab;
    public  Material        _MenuPointerMaterial;
    private GameObject      _Screens;
    public  GameObject      _ToolsScreen;
    public  GameObject      _ToolsMeasureScreen;
    public  GameObject      _ToolsEditScreen;
    public  GameObject      _ToolsEditSelectByClassScreen;
    public  GameObject      _ToolsVisualsScreen;
    public  GameObject      _DebugScreen;
    public  MeasureToolCtrl _MeasureTool;
    public  EditToolCtrl    _EditTool;
    public  StartView       _StartView;

    public ButtonXR _ButtonMeasureToggle;
    public ButtonXR _ButtonMeasureBack;
    public ButtonXR _ButtonMeasureSnap;
    public ButtonXR _ButtonMeasureRestrictXZ;
    public ButtonXR _ButtonMeasureRestrictY;

    public ButtonXR _ButtonEditToggle;
    public ButtonXR _ButtonEditBack;
    public ButtonXR _ButtonDeleteSelected;
    public ButtonXR _ButtonSelectInverse;
    public ButtonXR _ButtonSelectNone;
    public ButtonXR _ButtonSelectByClass;
    public ButtonXR _ButtonSelectByClassBack;

    public  ButtonXR   _ButtonVisualsToggle;
    public  ButtonXR   _ButtonVisualsBack;
    public  ButtonXR   _ButtonAddIntensityToColor;
    public  ButtonXR   _ButtonColorByRGB;
    public  ButtonXR   _ButtonColorByClass;
    public  ButtonXR   _ButtonColorByHight;
    public  ButtonXR   _ButtonColorByIntensity;
    public  ButtonXR   _ButtonColorByPointSourceID;
    public  GameObject _SliderGeometrySize;
    private SliderXR   _SliderGeometrySizeXR;

    public ButtonXR _ButtonSave;
    public ButtonXR _ButtonExport;
    public ButtonXR _ButtonSaveSelected;
    public ButtonXR _ButtonExportSelected;
    public ButtonXR _ButtonExportAsXYZ;
    public ButtonXR _ButtonExportAsLAS;
    public ButtonXR _ButtonStartMeny;
    public ButtonXR _ButtonQuit;

    public  ButtonXR    _ButtonSetUserPosition;
    public  ButtonXR    _ButtonFrustumCulling;

    public  GameObject  _SliderThrottle;
    private SliderXR    _SliderThrottleXR;
    public  TextMeshPro _PointsInCloud;
    public  TextMeshPro _PointsInFrustum;

    private GameObject _MenuPointer;

    private Camera _UserCamera;

    private Transform _RightHandTransform;
    private Transform _LeftHandTransform;

    private EditToolMode _EditToolActionPrev; // Used for setting Action to previous action in OnLeftTrackedControllerTriggerUnClicked
    private ExportFormat _ExportFormat;

    private int    _SaveSelectedIndex  = 0; // Used for naming save file when saving selection, index incremented for each save
    private string _SaveSelectedSuffix = "_selection_";

    private int    _ExportSelectedIndex  = 0;
    private string _ExportSelectedSuffix = "_selection_";

    private List<ButtonXR> _ButtonList;

    private ColorByParam _ColorBy;

    void Start()
    {
        _ColorBy = ColorByParam.NONE;
        _ExportFormat = ExportFormat.LAS;

        _ButtonExportAsLAS.SetActive(true);

        _Screens = transform.Find("Screens").gameObject;

        _UserCamera         = _ViveCtrl.GetVRCamera();
        _RightHandTransform = _ViveCtrl.GetRightHand().transform;
        _LeftHandTransform  = _ViveCtrl.GetLeftHand().transform;

        // Position menu on back of left controller
        transform.parent        = _LeftHandTransform;
        transform.localPosition = new Vector3(0f, -0.06f, -0.09f);
        transform.localRotation = Quaternion.Euler(-90.0f, 0f, 180f);

        GameObject pointerBase = new GameObject();
        pointerBase.transform.parent        = _RightHandTransform;
        pointerBase.transform.localPosition = new Vector3(0.0f, -0.087f, 0.063f);
        pointerBase.transform.localRotation = Quaternion.Euler(60.0f, 0.0f, 0.0f);
        pointerBase.name = "pointerBase";

        _MenuPointer = Instantiate(_MenuPointerPrefab);
        _MenuPointer.name = "MenuPointer_CtrlMeny";
        _MenuPointer.transform.parent = pointerBase.transform;

        _MenuPointer.transform.localScale    = new Vector3(0.01f, 0.01f, 0.03f);
        _MenuPointer.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        _MenuPointer.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

        _MenuPointer.AddComponent<MenuPointerCtrl>();
        _MenuPointer.GetComponent<Renderer>().material     = _MenuPointerMaterial;
        _MenuPointer.AddComponent<BoxCollider>();
        _MenuPointer.GetComponent<BoxCollider>().isTrigger = true;
        _MenuPointer.GetComponent<BoxCollider>().center    = new Vector3(0.0104362f, 0.0007376721f, 0.5f);
        _MenuPointer.GetComponent<BoxCollider>().size      = new Vector3(0.14f, 0.12f, 1f);

        
        _MenuPointer.GetComponent<Renderer>().material.SetColor("_Color", Color.red);

        _MenuPointer.SetActive(false);

        // Listen to click events from the pointer
        _MenuPointer.GetComponent<MenuPointerCtrl>().OnPointerTriggerEnterEvent += OnPointerClick;

        // Register listerners for all buttons
        _ButtonMeasureToggle.OnToggleEvent     += OnButtonMeasureToggleEvent;
        _ButtonMeasureBack.OnToggleEvent       += OnButtonMeasureBackEvent;
        _ButtonMeasureSnap.OnToggleEvent       += OnMeasureSnapToggleEvent;
        _ButtonMeasureRestrictXZ.OnToggleEvent += OnMeasureRestrictXZToggleEvent;
        _ButtonMeasureRestrictY.OnToggleEvent  += OnMeasureRestrictYToggleEvent;

        _ButtonEditToggle.OnToggleEvent     += OnButtonEditToggleEvent;
        _ButtonEditBack.OnToggleEvent       += OnButtonEditBackEvent;
        _ButtonDeleteSelected.OnToggleEvent += OnButtonDeleteSelected;
        _ButtonSelectInverse.OnToggleEvent  += OnButtonSelectInverse;
        _ButtonSelectNone.OnToggleEvent     += OnButtonSelectNone;

        _ButtonVisualsToggle.OnToggleEvent       += OnButtonVisualsToggleEvent;
        _ButtonVisualsBack.OnToggleEvent         += OnButtonVisualsBackEvent;
        _ButtonAddIntensityToColor.OnToggleEvent += OnButtonAddIntensityToColorToggleEvent;

        _ButtonColorByRGB.OnToggleEvent          += OnButtonColorByEvent;
        _ButtonColorByRGB.transform.gameObject.GetComponent<ButtonXR>()._ColorBy = ColorByParam.RGB;

        _ButtonColorByClass.OnToggleEvent += OnButtonColorByEvent;
        _ButtonColorByClass.transform.gameObject.GetComponent<ButtonXR>()._ColorBy = ColorByParam.CLASS;

        _ButtonColorByHight.OnToggleEvent += OnButtonColorByEvent;
        _ButtonColorByHight.transform.gameObject.GetComponent<ButtonXR>()._ColorBy = ColorByParam.HEIGHT;

        _ButtonColorByIntensity.OnToggleEvent += OnButtonColorByEvent;
        _ButtonColorByIntensity.transform.gameObject.GetComponent<ButtonXR>()._ColorBy = ColorByParam.INTENSITY;

        _ButtonColorByPointSourceID.OnToggleEvent += OnButtonColorByEvent;
        _ButtonColorByPointSourceID.transform.gameObject.GetComponent<ButtonXR>()._ColorBy = ColorByParam.POINT_SOURCE_ID;

        _ButtonSave.OnToggleEvent           += OnButtonSaveToggleEvent;
        _ButtonExport.OnToggleEvent         += OnButtonExportToggleEvent;
        _ButtonExport.PulseGlowOn(true);
        _ButtonSaveSelected.OnToggleEvent   += OnButtonSaveSelectedToggleEvent;
        _ButtonExportSelected.OnToggleEvent += OnButtonExportSelectedToggleEvent;
        _ButtonExportAsXYZ.OnToggleEvent    += OnButtonExportAsXYZToggle;
        _ButtonExportAsLAS.OnToggleEvent    += OnButtonExportAsLASToggle;
        _ButtonStartMeny.OnToggleEvent      += OnStartMeny;
        _ButtonQuit.OnToggleEvent           += OnButtonQuit;

        _ButtonSetUserPosition.OnToggleEvent     += OnButtonSetUserPositionToggleEvent;
        _ButtonFrustumCulling.OnToggleEvent      += OnButtonFrustumCullingToggleEvent;

        _PointCloudManager.OnPointCloudLoaded += OnPointCloudLoaded;

        ResetScreens();

        _ButtonList = new List<ButtonXR>();
        _ButtonList.Add(_ButtonMeasureToggle);
        _ButtonList.Add(_ButtonMeasureSnap);
        _ButtonList.Add(_ButtonMeasureRestrictXZ);
        _ButtonList.Add(_ButtonMeasureRestrictY);
        _ButtonList.Add(_ButtonEditToggle);
        _ButtonList.Add(_ButtonDeleteSelected);
        _ButtonList.Add(_ButtonSelectInverse);
        _ButtonList.Add(_ButtonSelectNone);
        _ButtonList.Add(_ButtonSave);
        _ButtonList.Add(_ButtonExport);
        _ButtonList.Add(_ButtonSaveSelected);
        _ButtonList.Add(_ButtonExportSelected);
        _ButtonList.Add(_ButtonExportAsXYZ);
        _ButtonList.Add(_ButtonExportAsLAS);
        _ButtonList.Add(_ButtonStartMeny);
        _ButtonList.Add(_ButtonQuit);
        _ButtonList.Add(_ButtonSetUserPosition);
        _ButtonList.Add(_ButtonColorByIntensity);
        _ButtonList.Add(_ButtonAddIntensityToColor);

        _SliderGeometrySizeXR = _SliderGeometrySize.transform.GetComponentInChildren<SliderXR>();
        _SliderGeometrySizeXR.SetMin(0.001f);
        _SliderGeometrySizeXR.SetMax(0.1f);
        _SliderGeometrySizeXR.OnValueChangeEvent += OnGeometrySizeChanged;

        _SliderThrottleXR = _SliderThrottle.transform.GetComponentInChildren<SliderXR>();
        _SliderThrottleXR.SetMin(1f);
        _SliderThrottleXR.SetMax(6f);
        _SliderThrottleXR.OnValueChangeEvent += OnThrottleChanged;

        EnableButtonsForPointCloud(false);

        MenuUpdate();
        ChildrenSetActive(false);
    }

    private void SetColorBy(ColorByParam color_by)
    {
        _ColorBy = color_by;

        if(_ColorBy == ColorByParam.RGB)
        {
            _PointCloudManager.UserColorActive(false);
        }

        if (_ColorBy == ColorByParam.CLASS)
        {
            _PointCloudManager.ColorByClass();
            _PointCloudManager.UserColorActive(true);
        }

        if (_ColorBy == ColorByParam.HEIGHT)
        {
            _PointCloudManager.ColorByHeight();
            _PointCloudManager.UserColorActive(true);
        }

        if (_ColorBy == ColorByParam.INTENSITY)
        {
            _PointCloudManager.ColorByIntensity();
            _PointCloudManager.UserColorActive(true);
        }

        if (_ColorBy == ColorByParam.POINT_SOURCE_ID)
        {
            _PointCloudManager.ColorByPointsourceID();
            _PointCloudManager.UserColorActive(true);
        }

        if (_ColorBy == ColorByParam.NONE)
        {
            _PointCloudManager.ColorByNone();
            _PointCloudManager.UserColorActive(true);
        }
    }

    public void ResetScreens()
    {
        _ToolsScreen.SetActive(true);
        _ToolsEditScreen.SetActive(false);
        _ToolsMeasureScreen.SetActive(false);
        _ToolsVisualsScreen.SetActive(false);
    }

    // Initialize value of controllers in the menu
    void InitializeControllerValues()
    {
        _SliderGeometrySizeXR.SetValue(_PointCloudManager.GetShaderGeometrySize());
        _SliderThrottleXR.SetValue(_ViveCtrl.GetThrottle());
        _ButtonAddIntensityToColor.SetActive(_PointCloudManager.GetAddIntensityToColor());
        _ButtonFrustumCulling.SetActive(_PointCloudManager._culling_enabled);
    }

    void OnIntensityChanged(object sender, SliderXRValueChangedEvent e)
    {
        _PointCloudManager.SetShaderIntensity(e.value);
    }

    void OnBrightnessChanged(object sender, SliderXRValueChangedEvent e)
    {
        _PointCloudManager.SetShaderBrightness(e.value);
    }

    void OnGeometrySizeChanged(object sender, SliderXRValueChangedEvent e)
    {
        _PointCloudManager.SetShaderGeometrySize(e.value);
    }

    void OnThrottleChanged(object sender, SliderXRValueChangedEvent e)
    {
        _ViveCtrl.SetThrottle(e.value);
    }

    private void OnPointCloudLoaded(object sender, EventArgs e)
    {
        EnableButtonsForPointCloud(true);
    }

    private void EnableButtonsForPointCloud(bool enabled)
    {
        for (int i = 0; i < _ButtonList.Count; i++)
        {
            _ButtonList[i].Enable(enabled);
        }

        _ButtonStartMeny.Reset();
        _ButtonStartMeny.SetActive(false);
        _ButtonQuit.SetActive(false);

        if ( enabled )
        {
            _PointsInCloud.text = _PointCloudManager.GetPointCloudFormat().NumberOfPoints.ToString("N0", CultureInfo.CreateSpecificCulture("sv-SE")) + " points in cloud";
        }
    }

    void OnButtonMeasureToggleEvent(object sender, ButtonToggleEvent e)
    {
        _ButtonMeasureToggle.SetActive(false);
        MeasureSetActive(e._Active);
    }

    void OnButtonMeasureBackEvent(object sender, ButtonToggleEvent e)
    {
        _ButtonMeasureBack.SetActive(false);
        MeasureSetActive(!e._Active);
    }

    void OnMeasureSnapToggleEvent(object sender, ButtonToggleEvent e)
    {
        if (e._Active)
        {
            MeasureSnapSetActive(true);
        }
        else
        {
            MeasureSnapSetActive(false);
        }
    }

    void OnMeasureRestrictXZToggleEvent(object sender, ButtonToggleEvent e)
    {
        if (e._Active)
        {
            if (_MeasureTool.RestrictModeGet() == RestrictMode.Y)
            {
                _ButtonMeasureRestrictY.SetActive(false);
            }

            MeasureRestrictModeSet(RestrictMode.XZ);
        }
        else
        {
            MeasureRestrictModeSet(RestrictMode.none);
        }
    }

    void OnMeasureRestrictYToggleEvent(object sender, ButtonToggleEvent e)
    {
        if (e._Active)
        {
            if (_MeasureTool.RestrictModeGet() == RestrictMode.XZ)
            {
                _ButtonMeasureRestrictXZ.SetActive(false);
            }

            MeasureRestrictModeSet(RestrictMode.Y);
        }
        else
        {
            MeasureRestrictModeSet(RestrictMode.none);
        }
    }

    void OnButtonEditToggleEvent(object sender, ButtonToggleEvent e)
    {
        _ButtonEditToggle.SetActive(false);
        EditSetActive(e._Active);
    }

    void OnButtonEditBackEvent(object sender, ButtonToggleEvent e)
    {
        _ButtonEditBack.SetActive(false);
        EditSetActive(!e._Active);
    }

    void OnButtonSelectByClassBackEvent( object sender, ButtonToggleEvent e )
    {
        _ButtonSelectByClassBack.SetActive(false);
        SelectByClassSetActive(false);
    }

    void OnButtonVisualsToggleEvent(object sender, ButtonToggleEvent e)
    {
        _ButtonVisualsToggle.SetActive(false);
        VisualsSetActive(e._Active);
    }

    void OnButtonVisualsBackEvent(object sender, ButtonToggleEvent e)
    {
        _ButtonVisualsBack.SetActive(false);
        VisualsSetActive(!e._Active);
    }

    void EditSetActive(bool active)
    {
        _ViveCtrl.EditToolSetActive(active);

        _ToolsScreen.SetActive(!active);
        _ToolsEditScreen.SetActive(active);
        MenuUpdate();
    }

    void SelectByClassSetActive( bool active )
    {
        _ViveCtrl.EditToolSetActive(!active);
        _ToolsEditSelectByClassScreen.SetActive(active);
        _ToolsEditScreen.SetActive(!active);
        MenuUpdate();
    }

    void VisualsSetActive(bool active)
    {
        _ToolsScreen.SetActive(!active);
        _ToolsVisualsScreen.SetActive(active);
        MenuUpdate();
    }

    void OnButtonEditEnhancedSelectToggle(object sender, ButtonToggleEvent e)
    {
        if (e._Active)
        {
            _EditTool.ActionSelectedEnhancedViewSetActive(true);
        }
        else
        {
            _EditTool.ActionSelectedEnhancedViewSetActive(false);
        }
    }

    void OnButtonExportAsXYZToggle(object sender, ButtonToggleEvent e)
    {
        if (e._Active)
        {
            _ExportFormat = ExportFormat.XYZ;
            _ButtonExportAsLAS.SetActive(false);
        }
    }

    void OnButtonExportAsLASToggle(object sender, ButtonToggleEvent e)
    {
        if (e._Active)
        {
            _ExportFormat = ExportFormat.LAS;
            _ButtonExportAsXYZ.SetActive(false);
        }
    }

    void OnButtonDeleteSelected(object sender, ButtonToggleEvent e)
    {
        StartCoroutine(SetButtonInactiveDelay(((ButtonXR)sender), 1));
        _PointCloudManager.DeleteSelected();
    }

    void OnButtonSelectInverse(object sender, ButtonToggleEvent e)
    {
        
        StartCoroutine(SetButtonInactiveDelay(((ButtonXR)sender), 1));
        _PointCloudManager.SelectInverse();
    }

    // Test function for UI Button...
    public void SelectInverse()
    {
        _PointCloudManager.SelectInverse();
    }

    void OnButtonSelectNone(object sender, ButtonToggleEvent e)
    {
        StartCoroutine(SetButtonInactiveDelay(((ButtonXR)sender), 1));
        _PointCloudManager.SelectNone();
    }

    IEnumerator SetButtonInactiveDelay(ButtonXR button, float time)
    {
        yield return new WaitForSeconds(time);
        button.SetActive(false);
    }

    void OnButtonUndoDelete(object sender, ButtonToggleEvent e)
    {
        StartCoroutine(SetButtonInactiveDelay(((ButtonXR)sender), 1));
        _PointCloudManager.UndoDelete();
    }

    void OnButtonSaveToggleEvent(object sender, ButtonToggleEvent e)
    {
        if (e._Active && _PointCloudManager.PointCloudIsLoaded())
        {
            // To trigger save pointcoud using threads
            _PointCloudManager.OnPointCloudSaved += OnPointCloudSavedListener;

            _PointCloudManager.OnGPUDataTransfereDone += OnSaveGPUDataTransfereDoneEvent;
            _PointCloudManager.SaveSelectedSetActive(false);
            _PointCloudManager.WriteFilenameSet(_PointCloudManager.ReadFilenameGet());
            _PointCloudManager.TransferGPUDataSimple();
        }
    }
    void OnSaveGPUDataTransfereDoneEvent(object sender, EventArgs e)
    {
        _PointCloudManager.OnGPUDataTransfereDone -= OnSaveGPUDataTransfereDoneEvent;
        _PointCloudManager.SavePointCloudByThread();
    }
    void OnPointCloudSavedListener(object sender, EventArgs e)
    {
        _ButtonSave.SetActive(false);
    }

    void OnButtonExportToggleEvent(object sender, ButtonToggleEvent e)
    {
        if (e._Active && _PointCloudManager.PointCloudIsLoaded())
        {
            _PointCloudManager.OnPointCloudExported += OnPointCloudExportedListener;
            _PointCloudManager.OnGPUDataTransfereDone += OnExportGPUDataTransfereDoneEvent;
            _PointCloudManager.ExportSelectedSetActive(false);
            _PointCloudManager.WriteFilenameSet(_PointCloudManager.ReadFilenameGet());
            _PointCloudManager._UsePointsByteData = true;
            _PointCloudManager.TransferGPUDataSimple();
        }
    }
    void OnExportGPUDataTransfereDoneEvent(object sender, EventArgs e)
    {
        _PointCloudManager.OnGPUDataTransfereDone -= OnExportGPUDataTransfereDoneEvent;

        if (_ExportFormat == ExportFormat.LAS)
        {
            _PointCloudManager._UsePointsByteData = true;
            _PointCloudManager.ExportLAS();
        }
    }
    void OnPointCloudExportedListener(object sender, EventArgs e)
    {
        _PointCloudManager.OnPointCloudExported -= OnPointCloudExportedListener;
        _ButtonExport.SetActive(false);
    }

    void OnButtonSaveSelectedToggleEvent(object sender, ButtonToggleEvent e)
    {
        if (e._Active && _PointCloudManager.PointCloudIsLoaded())
        {
            // To trigger save pointcoud using threads
            _PointCloudManager.OnPointCloudSaved += OnPointCloudSavedSelectedListener;
            _PointCloudManager.OnGPUDataTransfereDone += OnSaveSelectedGPUDataTransfereDoneEvent;
            _PointCloudManager.SaveSelectedSetActive(true);
            _PointCloudManager.WriteFilenameSet(_PointCloudManager.ReadFilenameGet() + _SaveSelectedSuffix + (_SaveSelectedIndex++));
            _PointCloudManager.TransferGPUDataSimple();
        }
    }
    void OnSaveSelectedGPUDataTransfereDoneEvent(object sender, EventArgs e)
    {
        _PointCloudManager.OnGPUDataTransfereDone -= OnSaveSelectedGPUDataTransfereDoneEvent;
        _PointCloudManager.SavePointCloudByThread();
    }
    void OnPointCloudSavedSelectedListener(object sender, EventArgs e)
    {
        _PointCloudManager.OnPointCloudSaved -= OnPointCloudSavedSelectedListener;
        _ButtonSaveSelected.SetActive(false);
    }

    void OnButtonExportSelectedToggleEvent(object sender, ButtonToggleEvent e)
    {
        if (e._Active && _PointCloudManager.PointCloudIsLoaded())
        {
            _PointCloudManager.OnPointCloudExported   += OnPointCloudExportSelectedListener;
            _PointCloudManager.OnGPUDataTransfereDone += OnExportSelectedGPUDataTransfereDoneEvent;
            _PointCloudManager.ExportSelectedSetActive(true);
            _PointCloudManager.WriteFilenameSet(_PointCloudManager.ReadFilenameGet() + _ExportSelectedSuffix + (_ExportSelectedIndex++));
            _PointCloudManager.TransferGPUDataSimple();
        }
    }
    void OnExportSelectedGPUDataTransfereDoneEvent(object sender, EventArgs e)
    {
        _PointCloudManager.OnGPUDataTransfereDone -= OnExportSelectedGPUDataTransfereDoneEvent;
        _PointCloudManager.ExportSelectedLAS();
    }

    void OnPointCloudExportSelectedListener(object sender, EventArgs e)
    {
        _PointCloudManager.OnPointCloudExported -= OnPointCloudExportSelectedListener;
        _ButtonExportSelected.SetActive(false);
    }

    void OnButtonSetUserPositionToggleEvent(object sender, ButtonToggleEvent e)
    {
        StartCoroutine(SetButtonInactiveDelay(((ButtonXR)sender), 1));
        _PointCloudManager.UserStartPoisitionSet();
    }

    void OnButtonFrustumCullingToggleEvent(object sender, ButtonToggleEvent e)
    {
        _PointCloudManager.PointsFrustumCullingActive(e._Active);
    }

    void OnButtonIntensityAsColorToggleEvent(object sender, ButtonToggleEvent e)
    {
        if (e._Active)
        {
            _PointCloudManager.ColorByIntensity();
        }
        _PointCloudManager.UserColorActive(e._Active);
    }

    void OnButtonAddIntensityToColorToggleEvent(object sender, ButtonToggleEvent e)
    {
        _PointCloudManager.SetAddIntensityToColor(e._Active);
    }

    void OnButtonColorByEvent(object sender, ButtonToggleEvent e)
    {
        // Deactivate all other ColorBy* buttons. Change layout to the one used in Edit screen 
        // for mode selection.
        ColorByParam color_by = ((ButtonXR)sender)._ColorBy;

        if (color_by == ColorByParam.RGB)
        {
            if (e._Active)
            {
                SetColorBy(color_by);

                _ButtonColorByClass.SetActive(false);
                _ButtonColorByHight.SetActive(false);
                _ButtonColorByIntensity.SetActive(false);
                _ButtonColorByPointSourceID.SetActive(false);
            }
            else
            {
                SetColorBy(ColorByParam.NONE);
            }
        }

        if (color_by == ColorByParam.CLASS)
        {
            if (e._Active)
            {
                SetColorBy(color_by);

                _ButtonColorByRGB.SetActive(false);
                _ButtonColorByHight.SetActive(false);
                _ButtonColorByIntensity.SetActive(false);
                _ButtonColorByPointSourceID.SetActive(false);
            }
            else
            {
                SetColorBy(ColorByParam.NONE);
            }
        }

        if (color_by == ColorByParam.HEIGHT)
        {
            if (e._Active)
            {
                SetColorBy(color_by);

                _ButtonColorByRGB.SetActive(false);
                _ButtonColorByClass.SetActive(false);
                _ButtonColorByIntensity.SetActive(false);
                _ButtonColorByPointSourceID.SetActive(false);
            }
            else
            {
                SetColorBy(ColorByParam.NONE);
            }
        }

        if (color_by == ColorByParam.INTENSITY)
        {
            if (e._Active)
            {
                SetColorBy(color_by);

                _ButtonColorByRGB.SetActive(false);
                _ButtonColorByClass.SetActive(false);
                _ButtonColorByHight.SetActive(false);
                _ButtonColorByPointSourceID.SetActive(false);
            }
            else
            {
                SetColorBy(ColorByParam.NONE);
            }
        }

        if (color_by == ColorByParam.POINT_SOURCE_ID)
        {
            if (e._Active)
            {
                SetColorBy(color_by);

                _ButtonColorByRGB.SetActive(false);
                _ButtonColorByClass.SetActive(false);
                _ButtonColorByHight.SetActive(false);
                _ButtonColorByIntensity.SetActive(false);
            }
            else
            {
                SetColorBy(ColorByParam.NONE);
            }
        }

    }

    void OnButtonColorByClassEvent(object sender, ButtonToggleEvent e)
    {
        // Deactivate all other ColorBy* buttons. Change layout to the one used in Edit screen 
        // for mode selection.
        if (e._Active)
        {
            _PointCloudManager.ColorByClass();
        }
        _PointCloudManager.UserColorActive(e._Active);
    }

    void OnButtonColorByHightEvent(object sender, ButtonToggleEvent e)
    {
        if (e._Active)
        {
            _PointCloudManager.ColorByHeight();
        }
        _PointCloudManager.UserColorActive(e._Active);
    }


    void OnButtonQuit(object sender, ButtonToggleEvent e)
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    void OnStartMeny(object sender, ButtonToggleEvent e)
    {
        if (!_StartView.GetActive())
        {
            _PointCloudManager.ClearPointCloud();
            _StartView.SetComponentsActive(true);
            EnableButtonsForPointCloud(false);
            MeasureSetActive(false);
            EditSetActive(false);
        }
    }

    private void MenuSetActive(bool active)
    {
        ChildrenSetActive(active);
        _MenuPointer.SetActive(active);
        InitializeControllerValues();
    }

    private bool MenuGetActive()
    {
        if (_MenuPointer.activeSelf && ChildrenGetActive())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Update the components in the menu. It would be more efficient to update each component separately when setting that
    /// specific component. However, this way is easier to maintain. Sometimes maintenance rules over efficiency. 
    /// </summary>
    void MenuUpdate()
    {
        foreach (EditToolMode action in Enum.GetValues(typeof(EditToolMode)))
        {
            if (_ViveCtrl.EditToolGetActive() && _ViveCtrl.EditToolGetAction() == action)
            {
                transform.Find("Screens/Tools_Edit/Text/Edit/Elements/EditAction/EditAction" + action.ToString()).GetComponent<TextMeshPro>().color = Color.green;
            }
            else
            {
                transform.Find("Screens/Tools_Edit/Text/Edit/Elements/EditAction/EditAction" + action.ToString()).GetComponent<TextMeshPro>().color = Color.gray;
            }
        }

        _PointsInFrustum.text = _PointCloudManager.GetPointsInFrustum().ToString("N0", CultureInfo.CreateSpecificCulture("sv-SE")) + " points in frustum";
        _PointsInFrustum.gameObject.SetActive(_PointCloudManager._culling_enabled);
    }

    private void MeasureSnapSetActive(bool active)
    {
        _MeasureTool.SnapToPointSetActive(active);
        MenuUpdate();
    }

    private void MeasureSetActive(bool active)
    {
        _ToolsScreen.SetActive(!active);
        _ToolsMeasureScreen.SetActive(active);
        _MeasureTool.SetActive(active);
        MenuUpdate();
    }

    public void MeasureRestrictModeSet(RestrictMode restrictMode)
    {
        _MeasureTool.RestrictModeSet(restrictMode);
        MenuUpdate();
    }

    void Update()
    {
        // Show menu on backside of left controller if it is facing the camera.
        float alignment = (transform.forward.normalized - _UserCamera.transform.forward.normalized).magnitude;
        if (alignment < 1 && _PointCloudManager.PointCloudIsLoaded() && _Activated)
        {
            MenuSetActive(true);
            MenuUpdate();
        }
        else
        {
            MenuSetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    private void ChildrenSetActive(bool active)
    {
        _Screens.SetActive(active);
    }

    private bool ChildrenGetActive()
    {
        return _Screens.activeSelf;
    }

    private void OnPointerClick(object sender, EventArgs e)
    {
        string name = ((PointerClickEventArgs)e).ClickedName;

        if (name == "MeasureActive")
        {
            if (_MeasureTool.GetActive())
            {
                MeasureSetActive(false);
                _ButtonMeasureSnap.Enable(false);
            }
            else
            {
                MeasureSetActive(true);
                _ButtonMeasureSnap.Enable(true);
            }
        }

        if (name == "SnapOn" && _MeasureTool.GetActive())
        {
            if (_MeasureTool.SnapToPointGetActive())
            {
                MeasureSnapSetActive(false);
            }
            else
            {
                MeasureSnapSetActive(true);
            }
        }

        if (name == "RestrictXZ" && _MeasureTool.GetActive())
        {
            if (_MeasureTool.RestrictModeGet() == RestrictMode.XZ)
            {
                MeasureRestrictModeSet(RestrictMode.none);
            }
            else
            {
                MeasureRestrictModeSet(RestrictMode.XZ);
            }
        }

        if (name == "RestrictY" && _MeasureTool.GetActive())
        {
            if (_MeasureTool.RestrictModeGet() == RestrictMode.Y)
            {
                MeasureRestrictModeSet(RestrictMode.none);
            }
            else
            {
                MeasureRestrictModeSet(RestrictMode.Y);
            }
        }

        if (name == "EditActive")
        {
            if (!_ViveCtrl.EditToolGetActive())
            {
                _ViveCtrl.EditToolSetActive(true);
            }
            else
            {
                _ViveCtrl.EditToolSetActive(false);
            }

            MenuUpdate();
        }

        if (_ViveCtrl.EditToolGetActive())
        {
            foreach (EditToolMode action in Enum.GetValues(typeof(EditToolMode)))
            {
                if (name == "EditAction" + action.ToString())
                {
                    EditToolSetAction(action);
                }
            }
        }
    }

    private void EditToolSetAction(EditToolMode action)
    {
        if (_ViveCtrl.EditToolGetAction() != action)
        {
            _ViveCtrl.EditToolSetMode(action);
        }
        else
        {
            _ViveCtrl.EditToolSetMode(EditToolMode.NONE);
        }

        MenuUpdate();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetCurrentMethod()
    {
        System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
        System.Diagnostics.StackFrame sf = st.GetFrame(1);

        return sf.GetMethod().Name;
    }
}