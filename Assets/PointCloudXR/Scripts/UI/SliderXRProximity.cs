/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using UnityEngine;

public class SliderXRProximity : MonoBehaviour
{
    public event EventHandler<EventArgs> OnTriggerEnterEvent;
    public event EventHandler<EventArgs> OnTriggerExitEvent;

    void OnTriggerEnter(Collider other)
    {
        if (other.name.Split('_')[0] == "MenuPointer")
        {
            OnTriggerEnterEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.name.Split('_')[0] == "MenuPointer")
        {
            OnTriggerExitEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
