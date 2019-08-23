/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using TMPro;
using UnityEngine;

public class SliderXR : MonoBehaviour
{
    public event EventHandler<SliderXRValueChangedEvent> OnValueChangeEvent;

    public GameObject _Ruler;
    public SliderXRSelector _Selector;
    public TextMeshPro _Text;

    public float _MaxValue = 1f;
    public float _MinValue = 0f;

    void Start ()
    {
        _Selector.OnValueChangeEvent += ValueChangedEvent;

    }

    public void SetMin( float min )
    {
        _MinValue = min;
    }

    public void SetMax( float max )
    {
        _MaxValue = max;
    }

    public void SetValue( float value )
    {
        if( value >= _MinValue && value <= _MaxValue )
        {
            float length = _MaxValue - _MinValue;
            float v = value - _MinValue;
            float percent = (v / length) * 100f;
            _Selector.SetPercentage(percent);
            SetText(value.ToString("n3"));
        }
    }

    void ValueChangedEvent(object sender, SliderXRValueChangedEvent e)
    {
        float percent = e.value;
        float length = _MaxValue - _MinValue;
        float newValue = (percent / 100f) * length + _MinValue;

        OnValueChangeEvent?.Invoke(this, new SliderXRValueChangedEvent(newValue));

        SetText(newValue.ToString("n3"));
    }

    private void SetText( string t )
    {
        _Text.text = t;
    }
}
