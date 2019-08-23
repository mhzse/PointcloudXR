/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public enum EditToolMode { DELETE, SELECT, DESELECT, NONE };
public enum EditToolScaleType { UP = 1, DOWN = -1, NONE = 0 };
public enum EditToolOffsetType { OUT = 1, IN = -1, NONE = 0 };

public class EditToolCtrl : MonoBehaviour
{
    public GameObject        _Parent;
    public float _ParentOffsetZ;
    public PointCloudManager _point_cloud_manager;

    [Range(0, 1)]
    public float     _SpeedDistanceFactor = 0.5f;
    public Material  _Material;
    public Color     _DeleteColor;
    public Color     _DeleteRimColor;
    public Color     _SelectColor;
    public Color     _SelectRimColor;
    public Color     _DeselectColor;
    public Color     _DeselectRimColor;
    public Color     _InactiveColor;
    public float     _Offset;
    public float     _OffsetMin = 0.0f;
    public float     _OffsetMinDynamic; // To constrain offset distance to half _Geometry scale, preventing _Geometry to swallow _Parent
    public float     _OffsetMax;
    public float     _Scale;
    public float     _ScaleMin = 0.05f;
    public float     _ScaleMax = 5.0f;
    public float     _ScaleMaxDynamic; // To constrain scale to twice offset distance, preventing _Geometry to swallow _Parent
    [Range(0, 0.5f)]
    public float     _SpeedScaleFactor = 0.1f;
    public bool      _IsActive = false;
    public Color     _SelectedPointColor;
    
    private EditToolScaleType  _ScaleType;
    private EditToolOffsetType _OffsetType;

    public event EventHandler<EventArgs> OnOffsetAccepted;
    public event EventHandler<EventArgs> OnScaleAccepted;

    public bool _ActionSelectedEnhancedView = false;

    private EditToolMode _mode;
    public ComputeShader _edit_tool_compute;

    void Start()
    {
        SetActive(false);
        transform.parent = _Parent.transform;
        SetParentOffsetZ(_ParentOffsetZ);
        
        gameObject.GetComponent<Renderer>().material = _Material;
        
        _Scale = _ScaleMin;
        _ScaleType = EditToolScaleType.NONE;

        _OffsetMin = _ScaleMin / 2 + _ParentOffsetZ;
        _Offset = _OffsetMin;
        _OffsetType = EditToolOffsetType.NONE;

        SetMode(EditToolMode.NONE);

        SetOffsetAndScaleDynamic();
        SelectedPointColorSet(_SelectedPointColor);
    }

    public void SelectedPointColorSet(Color pointColor)
    {
        _point_cloud_manager.SelectedPointColorSet(pointColor);
    }

    public void ActionSelectedEnhancedViewSetActive(bool active)
    {
        _ActionSelectedEnhancedView = active;
    }

    public void SetParentOffsetZ(float zOffset)
    {
        _ParentOffsetZ = zOffset;
        transform.localPosition = new Vector3(0f, 0f, zOffset + _ParentOffsetZ);
        _ScaleMaxDynamic = zOffset*2;

        if(_ScaleMaxDynamic < _ScaleMin)
        {
            _ScaleMaxDynamic = _ScaleMin;
        }
    }

    public float GetParentOffsetZ()
    {
        return _ParentOffsetZ;
    }

    private void SetOffsetAndScaleDynamic()
    {
        _ScaleMaxDynamic = _ScaleMin + (_Offset - _OffsetMin) * 2;
        _OffsetMinDynamic = _Scale / 2 + _ParentOffsetZ;
    }
    public void SetScaleType(EditToolScaleType scaleType)
    {
        _ScaleType = scaleType;
    }

    public void SetOffsetType(EditToolOffsetType offsetType)
    {
        _OffsetType = offsetType;
    }

    public void SetOffsetAbs(float offset)
    {
        _Offset = offset;

        // Must be greater than the dynamic minimum offset (that depends on current value of _Scale)
        _Offset = _Offset >= _OffsetMinDynamic ? _Offset : _OffsetMinDynamic;

        // Must be greater than minimum distance
        _Offset = _Offset >= _OffsetMin ? _Offset : _OffsetMin;
        
        // And less than maximun offset
        _Offset = _Offset <= _OffsetMax ? _Offset : _OffsetMax;

        if (transform.localPosition.z < _OffsetMin)
        {
            transform.localPosition = new Vector3(0.0f, 0.0f, _OffsetMin);
        }

        var offsetValue = 1.0f * Time.fixedDeltaTime * (int)_OffsetType;

        float newEditToolOffset = _Offset + offsetValue;

        float speedBoost = (newEditToolOffset / _OffsetMax) * (int)_OffsetType * _SpeedDistanceFactor;
        newEditToolOffset += speedBoost;

        if (newEditToolOffset >= _OffsetMin && newEditToolOffset <= _OffsetMax)
        {
            if (transform.localPosition.z >= _OffsetMin && transform.localPosition.z <= _OffsetMax)
            {
                if ((newEditToolOffset >= _OffsetMin))
                {
                    _Offset = newEditToolOffset;
                    transform.localPosition = new Vector3(0.0f, 0.0f, newEditToolOffset);
                }
            }
        }
        else if (transform.localPosition.z < _OffsetMin)
        {
            transform.localPosition = new Vector3(0.0f, 0.0f, _OffsetMin);
            _Offset = _OffsetMin;
        }
        else if (transform.localPosition.z > _OffsetMax)
        {
            transform.localPosition = new Vector3(0.0f, 0.0f, _OffsetMax);
            _Offset = _OffsetMax;
        }

        _point_cloud_manager.EditToolSetPosition(transform.position);
    }

    public void SetOffset(EditToolOffsetType offsetType)
    {
        var offsetValue = 1.0f * Time.fixedDeltaTime * (int)offsetType;
        float newEditToolOffset = _Offset + offsetValue;
        float speedBoost = (newEditToolOffset / _OffsetMax) * (int)offsetType * _SpeedDistanceFactor;
        newEditToolOffset += speedBoost;
        float newEditToolOffset_original = newEditToolOffset;

        // Must be greater than the dynamic minimum offset (that depends on current value of _Scale)
        newEditToolOffset = newEditToolOffset >= _OffsetMinDynamic ? newEditToolOffset : _OffsetMinDynamic;

        // Must be greater than minimum distance
        newEditToolOffset = newEditToolOffset >= _OffsetMin ? newEditToolOffset : _OffsetMin;

        // And less than maximun offset
        newEditToolOffset = newEditToolOffset <= _OffsetMax ? newEditToolOffset : _OffsetMax;
        
        if (transform.localPosition.z < _OffsetMin)
        {
            transform.localPosition = new Vector3(0.0f, 0.0f, _OffsetMin);
        }
        
        if (newEditToolOffset >= _OffsetMin && newEditToolOffset <= _OffsetMax)
        {
            if (transform.localPosition.z >= _OffsetMin && transform.localPosition.z <= _OffsetMax)
            {
                if (newEditToolOffset >= _OffsetMin)
                {
                    transform.localPosition = new Vector3(0.0f, 0.0f, newEditToolOffset);
                }
            }
        }
        else if (transform.localPosition.z < _OffsetMin)
        {
            transform.localPosition = new Vector3(0.0f, 0.0f, _OffsetMin);
            newEditToolOffset = _OffsetMin;
        }
        else if (transform.localPosition.z > _OffsetMax)
        {
            transform.localPosition = new Vector3(0.0f, 0.0f, _OffsetMax);
            newEditToolOffset = _OffsetMax;
        }

        if(newEditToolOffset_original == newEditToolOffset)
        {
            // Trigger Offset accepted event
            OnOffsetAccepted?.Invoke(this, EventArgs.Empty);
        }

        _Offset = newEditToolOffset;
        _point_cloud_manager.EditToolSetPosition(transform.position);
    }

    public float GetOffset()
    {
        return _Offset;
    }

    public void SetScale(float scale)
    {
        _Scale = scale;

        // Must be less than the dynamic maximun scale (that depends on current value for _Offset)
        _Scale = _Scale <= _ScaleMaxDynamic ? _Scale : _ScaleMaxDynamic;

        // And greater than minimum scale
        _Scale = _Scale >= _ScaleMin ? _Scale : _ScaleMin; // _ScaleMaxDynamic

        // And less than maximum scale
        _Scale = _Scale <= _ScaleMax ? _Scale : _ScaleMax;
        

        var scaleValue = 0.4f * Time.fixedDeltaTime * (int)_ScaleType;

        float scaleBoost = (scaleValue / _ScaleMax) * _SpeedScaleFactor;
        scaleValue += scaleBoost;

        if ((int)_ScaleType == 1 && ((_Scale + scaleValue) <= _ScaleMax))
        {
            if (_Scale + scaleValue < (_Offset * 2))
            {
                _Scale = _Scale + scaleValue;
                transform.localScale = new Vector3(_Scale, _Scale, _Scale);
            }
        }
        else
        {
            if (_Scale + scaleValue >= _ScaleMin && ((_Scale + scaleValue) <= _ScaleMax))
            {
                _Scale = _Scale + scaleValue;
                transform.localScale = new Vector3(_Scale, _Scale, _Scale);
            }
        }
        
        _point_cloud_manager.EditToolSetRadius(_Scale / 2);
    }

    public void SetScale(EditToolScaleType scaleType)
    {
        var   scaleValue = _Scale + 0.4f * Time.fixedDeltaTime * (int)scaleType;
        float scaleBoost = (scaleValue / _ScaleMax) * _SpeedScaleFactor * (int)scaleType;

        scaleValue += scaleBoost;
        float scaleValue_original = scaleValue;

        // Must be less than the dynamic maximun scale (that depends on current value of _Offset)
        scaleValue = scaleValue <= _ScaleMaxDynamic ? scaleValue : _ScaleMaxDynamic;

        // And greater than minimum scale
        scaleValue = scaleValue >= _ScaleMin ? scaleValue : _ScaleMin;

        // And less than maximum scale
        scaleValue = scaleValue <= _ScaleMax ? scaleValue : _ScaleMax;

        transform.localScale = new Vector3(scaleValue, scaleValue, scaleValue);

        if (scaleValue_original == scaleValue)
        {
            // Trigger Scale accepted event
            OnScaleAccepted?.Invoke(this, EventArgs.Empty);
        }

        _Scale = scaleValue;
        _point_cloud_manager.EditToolSetRadius(scaleValue / 2);
    }

    public float GetScale()
    {
        return _Scale;
    }

    public void Deactivate()
    {
        _IsActive = false;
        GetComponent<MeshRenderer>().enabled = false;

        _point_cloud_manager.EditToolSetActive(false);
    }

    public void SetActive(bool active)
    {
        if (active && !_IsActive) // Activate and reset
        {
            _IsActive = true;

            if(_Parent != null)
            {
                transform.parent = _Parent.transform;
            }

            GetComponent<MeshRenderer>().enabled = true;
            ResetEditTool();
        }

        if(!active)
        {
            _IsActive = false;
            GetComponent<MeshRenderer>().enabled = false;

            _point_cloud_manager.EditToolSetActive(false);
        }
    }

    public bool GetActive()
    {
        return _IsActive;
    }

    public Material GetMaterial()
    {
        return _Material;
    }

    public void ResetEditTool()
    {
        _Offset = _OffsetMin;
        transform.localScale = new Vector3(_ScaleMin, _ScaleMin, _ScaleMin);
        transform.localPosition = new Vector3(0.0f, 0.0f, _OffsetMin);

        SetScale(_ScaleMin);
        _point_cloud_manager.EditToolSetActive(_IsActive);
        SetMode(EditToolMode.NONE);
        UpdateMaterialColors();
    }

    public void SetMode(EditToolMode mode)
    {
        _mode = mode;

        Color[] mode_colors = GetModeColors(_mode);
        
        _Material.SetColor("_ColorTint", mode_colors[0]);
        _Material.SetColor("_RimColor",  mode_colors[1]);
        _Material.SetFloat("_RimPower", 6);

        // Update shader to current EditToolMode
        _point_cloud_manager.EditToolSetMode(mode);
    }

    public EditToolMode GetAction()
    {
        return _point_cloud_manager.EditToolGetAction();
    }

    public void UpdateMaterialColors()
    {
        EditToolMode editToolMode = _point_cloud_manager.GetEditToolMode();
        Color[] actionColors = GetModeColors(editToolMode);

        _Material.SetColor("_ColorTint", actionColors[0]);
        _Material.SetColor("_RimColor", actionColors[1]);
    }

    public Color[] GetModeColors(EditToolMode mode)
    {
        Color[] mode_colors = new Color[2];

        if (mode == EditToolMode.SELECT)
        {
            mode_colors[0] = _SelectColor;
            mode_colors[1] = _SelectRimColor;
        }

        if (mode == EditToolMode.DESELECT)
        {
            mode_colors[0] = _DeselectColor;
            mode_colors[1] = _DeselectRimColor;
        }

        if (mode == EditToolMode.DELETE)
        {
            mode_colors[0] = _DeleteColor;
            mode_colors[1] = _DeleteRimColor;
        }

        if (mode == EditToolMode.NONE)
        {
            mode_colors[0] = _InactiveColor;
            mode_colors[1] = _InactiveColor;
        }

        return mode_colors;
    }

    // Call this every frame as long as Edit tool is in select mode.
    public void SelectPoints()
    {
        int num_groups = Mathf.CeilToInt(_point_cloud_manager.GetPointsInCloud() / (float)_point_cloud_manager.GetComputeShaderPointsBufferThreads());
        int kernel = _edit_tool_compute.FindKernel("SelectPoints");

        _edit_tool_compute.SetBuffer(kernel, "_points_buffer", _point_cloud_manager.GetComputeBufferPoints());
        _edit_tool_compute.SetBuffer(kernel, "_comunication_buffer", _point_cloud_manager.GetComputeBufferCom());

        _edit_tool_compute.SetVector("_tool_position", transform.position);
        _edit_tool_compute.SetFloat("_tool_radius", transform.localScale.x / 2);

        _edit_tool_compute.Dispatch(kernel, num_groups, 1, 1);
    }

    // Call this every frame as long as Edit tool is in deselect mode.
    public void DeselectPoints()
    {
        int num_groups = Mathf.CeilToInt(_point_cloud_manager.GetPointsInCloud() / (float)_point_cloud_manager.GetComputeShaderPointsBufferThreads());
        int kernel = _edit_tool_compute.FindKernel("DeselectPoints");

        _edit_tool_compute.SetBuffer(kernel, "_points_buffer", _point_cloud_manager.GetComputeBufferPoints());
        _edit_tool_compute.SetBuffer(kernel, "_comunication_buffer", _point_cloud_manager.GetComputeBufferCom());

        _edit_tool_compute.SetVector("_tool_position", transform.position);
        _edit_tool_compute.SetFloat("_tool_radius", transform.localScale.x / 2);

        _edit_tool_compute.Dispatch(kernel, num_groups, 1, 1);
    }

    // Call this every frame as long as Edit tool is in deselect mode.
    public void DeletePoints()
    {
        int num_groups = Mathf.CeilToInt(_point_cloud_manager.GetPointsInCloud() / (float)_point_cloud_manager.GetComputeShaderPointsBufferThreads());
        int kernel = _edit_tool_compute.FindKernel("DeletePoints");

        _edit_tool_compute.SetBuffer(kernel, "_points_buffer", _point_cloud_manager.GetComputeBufferPoints());
        _edit_tool_compute.SetBuffer(kernel, "_comunication_buffer", _point_cloud_manager.GetComputeBufferCom());

        _edit_tool_compute.SetVector("_tool_position", transform.position);
        _edit_tool_compute.SetFloat("_tool_radius", transform.localScale.x / 2);

        _edit_tool_compute.Dispatch(kernel, num_groups, 1, 1);
    }

    void Update()
    {
        if(_IsActive)
        {
            // Set dynamic scale and offset limits to prevent _Geometry from swollowing _Parent.
            SetOffsetAndScaleDynamic();
            
            // Update position in PCManager
            _point_cloud_manager.EditToolSetPosition(transform.position);

            // Update scale in PCManager, scale is uniform in all dimensions (it's a sphere).
            _point_cloud_manager.EditToolSetRadius(transform.localScale.x / 2);

            // Make sure color reflects current action
            UpdateMaterialColors();

            if (_mode == EditToolMode.SELECT)
            {
                SelectPoints();
            }

            if (_mode == EditToolMode.DESELECT)
            {
                DeselectPoints();
            }

            if (_mode == EditToolMode.DELETE)
            {
                DeletePoints();
            }
        }
    }
}
