/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using UnityEngine;

public class SliderXRValueChangedEvent : EventArgs
{
    public float value { get; private set; }

    public SliderXRValueChangedEvent(float val)
    {
        value = val;
    }
}

public class SliderXRSelector : MonoBehaviour
{
    public event EventHandler<SliderXRValueChangedEvent> OnValueChangeEvent;

    private Vector3 _OtherTriggerEnterLocalPosition;
    private float _MinX = -0.029f;
    private float _MaxX = 0.029f;

    public GameObject _SelectorMesh;
    public SliderXRProximity _Proximity;

    private Color _ColorActive = new Color(0, 0.3f, 0);
    private Color _ColorInactive = new Color(0.27f, 0.27f, 0.27f);
    private float _SelectorActiveEmissionStrengthGreen = 0.6f;
    private float _SelectorProximityEmissionStrengthGreen = 0.3f;

    private float _LocalPositionXPrev;
    private float _Length;

    void Start ()
    {
        _Proximity.OnTriggerEnterEvent += ProximityEnterEvent;
        _Proximity.OnTriggerExitEvent += ProximityExitEvent;

        _LocalPositionXPrev = _MaxX + 1;
        _Length = _MaxX * 2;
    }
	
    // 0 - 100
    public void SetPercentage( float percent )
    {
        if( percent >= 0 && percent <= 100 )
        {
            float newPositionX = _Length * (percent / 100f);
            transform.localPosition = new Vector3(_MinX + newPositionX, transform.localPosition.y, transform.localPosition.z);
        }
    }

    public float GetPercentage()
    {
        return ((transform.localPosition.x + _MaxX) / _Length) * 100f;
    }

	void Update ()
    {
		
	}

    void ProximityEnterEvent(object sender, EventArgs e)
    {
        _SelectorMesh.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0, _SelectorProximityEmissionStrengthGreen, 0));
    }

    void ProximityExitEvent(object sender, EventArgs e)
    {
        _SelectorMesh.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0, 0, 0));
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.name.Split('_')[0] == "MenuPointer")
        {
            _OtherTriggerEnterLocalPosition = transform.InverseTransformPoint(other.transform.position);
            _SelectorMesh.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0, _SelectorActiveEmissionStrengthGreen, 0));
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.name.Split('_')[0] == "MenuPointer")
        {
            Vector3 otherLocal = transform.InverseTransformPoint(other.transform.position);
            float pushX = transform.localPosition.x - (_OtherTriggerEnterLocalPosition.x - otherLocal.x);

            if( (pushX >= _MinX) && (pushX <= _MaxX) && (_LocalPositionXPrev != pushX) )
            {
                transform.localPosition = new Vector3(pushX, transform.localPosition.y, transform.localPosition.z);
                _LocalPositionXPrev = pushX;

                OnValueChangeEvent?.Invoke( this, new SliderXRValueChangedEvent(GetPercentage()) ); // TODO: Send new value here...
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.name.Split('_')[0] == "MenuPointer")
        {
            _SelectorMesh.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0, _SelectorProximityEmissionStrengthGreen, 0));
        }
    }
}
