/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using UnityEngine;

public class ButtonXRTriggerEvent : EventArgs
{
    public Collider _Other { get; private set; }

    public ButtonXRTriggerEvent(Collider other)
    {
        this._Other = other;
    }
}

public class ButtonXR_base : MonoBehaviour
{
    public event EventHandler<ButtonXRTriggerEvent> OnTriggerEnterEvent;
    public event EventHandler<ButtonXRTriggerEvent> OnTriggerExitEvent;
    public event EventHandler<ButtonXRTriggerEvent> OnButtonPressedEvent;

    private Vector3 _OriginalLocalPosition;
    private Vector3 _OtherTriggerEnterLocalPosition;

    private bool _ButtonExecuted = false;

    void Start ()
    {
        _OriginalLocalPosition = transform.localPosition;
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.name.Split('_')[0] == "MenuPointer")
        {
            _ButtonExecuted = false;
            _OtherTriggerEnterLocalPosition = transform.InverseTransformPoint(other.transform.position);
            OnTriggerEnterEvent?.Invoke(this, new ButtonXRTriggerEvent(other));
        }

    }
    
    void OnTriggerStay(Collider other)
    {
        if (other.name.Split('_')[0] == "MenuPointer")
        {
            Vector3 otherLocal = transform.InverseTransformPoint(other.transform.position);

            float pushZ = transform.localPosition.z - (_OtherTriggerEnterLocalPosition.z - otherLocal.z);
            float threshold = -0.005f;

            if (pushZ < 0 && pushZ >= threshold)
            {
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, pushZ);
            }

            if (pushZ <= threshold && !_ButtonExecuted)
            {
                _ButtonExecuted = true;
                OnButtonPressedEvent?.Invoke(this, new ButtonXRTriggerEvent(other));
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.name.Split('_')[0] == "MenuPointer")
        {
            _ButtonExecuted = false;
            transform.localPosition = _OriginalLocalPosition;
            OnTriggerExitEvent?.Invoke(this, new ButtonXRTriggerEvent(other));
        }
    }
}
