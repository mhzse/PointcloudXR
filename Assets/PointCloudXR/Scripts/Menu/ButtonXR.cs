/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class ButtonToggleEvent : EventArgs
{
    public string       _Name;
    public bool         _Active;

    public ButtonToggleEvent(string name, bool active)
    {
        this._Name = name;
        this._Active = active;
    }
}

public class ButtonXR : MonoBehaviour
{
    public ButtonXR_base _Base;
    public GameObject _Front;
    public TextMeshPro _Text;
    public GameObject _MenuPointerStop;

    public ColorByParam _ColorBy;

    // Fired when the button is pressed
    public event EventHandler<ButtonToggleEvent> OnToggleEvent; 

    //[Range(0, 1)]
    private float _HoverEmissionStrengthRed = 0;
    //[Range(0, 1)]
    private float _HoverEmissionStrengthGreen = 0;
    //[Range(0, 1)]
    private float _HoverEmissionStrengthBlue = 0;
    //[Range(0, 1)]
    private float _ButtonPressEmissionStrengthRed = 0;
    //[Range(0, 1)]
    private float _ButtonPressEmissionStrengthGreen = 0;
    //[Range(0, 1)]
    private float _ButtonPressEmissionStrengthBlue = 0;

    private bool _Active;

    private Color _ColorActive;
    private Color _ColorInactive;

    public bool _Enabled;

    private bool _Debug = false;

    private bool _PulseGlowOn = false;

    public bool _CustomColor = false;

	void Start ()
    {
        _Base.OnTriggerEnterEvent += BaseTriggerEnterEvent;
        _Base.OnTriggerExitEvent += BaseTriggerExitEvent;
        _Base.OnButtonPressedEvent += OnButtonPressed;

        _HoverEmissionStrengthRed = 0;
        _HoverEmissionStrengthGreen = 0.2f;
        _HoverEmissionStrengthBlue = 0;
        _ButtonPressEmissionStrengthRed = 0;
        _ButtonPressEmissionStrengthGreen = 0.4f;
        _ButtonPressEmissionStrengthBlue = 0;

        _ColorActive = new Color(0, 0.3f, 0);
        _ColorInactive = new Color(0.27f, 0.27f, 0.27f);
        
        SetBaseColorInactive();
    }

    public ButtonXR_base GetBase()
    {
        return _Base;
    }

    public GameObject GetFront()
    {
        return _Front;
    }

    public TextMeshPro GetText()
    {
        return _Text;
    }

    public void Reset()
    {
        _Base.transform.localPosition = new Vector3(_Base.transform.localPosition.x, _Base.transform.localPosition.y, 0);
    }

    public void DebugPrint(string msg)
    {
        if(_Debug)
        {
            Debug.Log(this.GetType().Name + ":" + msg + " [" + gameObject.name + "]\n");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetCurrentMethod()
    {
        System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
        System.Diagnostics.StackFrame sf = st.GetFrame(1);

        return sf.GetMethod().Name;
    }

    public void SetColorActive(Color activeColor)
    {
        DebugPrint(GetCurrentMethod());
        _ColorActive = activeColor;
    }

    public void SetColorInactive(Color inactiveColor)
    {
        DebugPrint(GetCurrentMethod());
        _ColorInactive = inactiveColor;
    }

    public void SetScaleX(float scaleX)
    {
        DebugPrint(GetCurrentMethod());
        _Base.transform.localScale = new Vector3(scaleX, _Base.transform.localScale.y, _Base.transform.localScale.z);
        _Front.transform.localScale = new Vector3(scaleX * 1.07f, _Front.transform.localScale.y, _Front.transform.localScale.z);

        Vector3 boxSize = GetComponent<BoxCollider>().size;
        boxSize.x *= scaleX;
        GetComponent<BoxCollider>().size = boxSize;

        Vector3 boxSizeMenuPointerStopCollider = _MenuPointerStop.GetComponent<BoxCollider>().size;
        boxSizeMenuPointerStopCollider.x *= scaleX;
        _MenuPointerStop.GetComponent<BoxCollider>().size = boxSizeMenuPointerStopCollider;
    }

    public void SetScaleY( float scaleY )
    {
        DebugPrint(GetCurrentMethod());
        _Base.transform.localScale = new Vector3(_Base.transform.localScale.x, scaleY, _Base.transform.localScale.z);
        _Front.transform.localScale = new Vector3(_Front.transform.localScale.x, scaleY, _Front.transform.localScale.z);

        Vector3 boxSize = GetComponent<BoxCollider>().size;
        boxSize.y *= scaleY;
        GetComponent<BoxCollider>().size = boxSize;

        Vector3 boxSizeMenuPointerStopCollider = _MenuPointerStop.GetComponent<BoxCollider>().size;
        boxSizeMenuPointerStopCollider.y *= scaleY;
        _MenuPointerStop.GetComponent<BoxCollider>().size = boxSizeMenuPointerStopCollider;
    }

    public TextMeshPro GetTextMeshPro()
    {
        DebugPrint(GetCurrentMethod());
        return _Text;
    }

    public void SetText(string text)
    {
        DebugPrint(GetCurrentMethod());
        _Text.text = text;
    }

    public void SetActive(bool active)
    {
        DebugPrint(GetCurrentMethod());
        _Active = active;
        _Text.GetComponent<Renderer>().material.DisableKeyword("GLOW_ON");

        if (_Active)
        {
            SetBaseColorActive();
        }
        else
        {
            SetBaseColorInactive();
        }
    }

    public void Activate()
    {
        DebugPrint(GetCurrentMethod());
        _Active = true;

        // Trigger enter
        _Text.GetComponent<Renderer>().material.EnableKeyword("GLOW_ON");
        _Text.GetComponent<Renderer>().material.SetFloat("_GlowPower", 0.1f);
        _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(0f, 1f, 0f));
        _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_HoverEmissionStrengthRed, _HoverEmissionStrengthGreen, _HoverEmissionStrengthBlue));

        // Base trigger enter
        _Text.GetComponent<Renderer>().material.SetFloat("_GlowPower", 0.3f);
        _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(0f, 1f, 0f));
        _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_ButtonPressEmissionStrengthRed, _ButtonPressEmissionStrengthGreen, _ButtonPressEmissionStrengthBlue));

        // On button press
        _Base.GetComponent<Renderer>().material.SetColor("_Color", _ColorActive);
        _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_ButtonPressEmissionStrengthRed, _ButtonPressEmissionStrengthGreen, _ButtonPressEmissionStrengthBlue));
        _Text.color = new Color(0f, 0.6f, 0f);// Color.green;
        _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(0f, 1f, 0f));

        // Base trigger exit
        _Text.GetComponent<Renderer>().material.SetFloat("_GlowPower", 0.1f);
        _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(0f, 1f, 0f));
        _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_HoverEmissionStrengthRed, _HoverEmissionStrengthGreen, _HoverEmissionStrengthBlue));

        // Trigger exit
        _Text.GetComponent<Renderer>().material.DisableKeyword("GLOW_ON");
        _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0f, 0.05f, 0f));
    }

    public bool GetActive()
    {
        DebugPrint(GetCurrentMethod());
        return _Active;
    }

    public void Enable(bool enable)
    {
        DebugPrint(GetCurrentMethod());
        _Enabled = enable;
        _Text.GetComponent<Renderer>().material.DisableKeyword("GLOW_ON");

        if (_Enabled)
        {
            if(_Active)
            {
                SetBaseColorActive();
            }
            else
            {
                SetBaseColorInactive();
            }
        }
        else
        {
            _Active = false;
            SetBaseColorInactive();
        }
    }

    private void SetBaseColorActive()
    {
        DebugPrint(GetCurrentMethod());
        _Text.color = new Color(0f, 0.6f, 0f);
        _Base.GetComponent<Renderer>().material.SetColor("_Color", _ColorActive);
        _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0f, 0.05f, 0f));
    }

    public void SetBaseColorInactive()
    {
        DebugPrint(GetCurrentMethod());

        // TODO: A quick fix fore a more saturated white color on BACK buttons...refactor this
        if (_CustomColor)
        {
            _Text.color = new Color(1f, 1f, 1f);
        }
        else
        {
            _Text.color = new Color(0.5f, 0.5f, 0.5f);
        }
        _Base.GetComponent<Renderer>().material.SetColor("_Color", _ColorInactive);
        _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0.05f, 0.05f, 0.05f));
    }

    void OnTriggerEnter(Collider other)
    {
        DebugPrint(GetCurrentMethod());
        if (other.name.Split('_')[0] == "MenuPointer")
        {
            if (_Enabled)
            {
                _Text.GetComponent<Renderer>().material.EnableKeyword("GLOW_ON");
                _Text.GetComponent<Renderer>().material.SetFloat("_GlowPower", 0.1f);

                if (_Active)
                {
                    _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(0f, 1f, 0f));
                    _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_HoverEmissionStrengthRed, _HoverEmissionStrengthGreen, _HoverEmissionStrengthBlue));
                }
                else
                {
                    _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(1f, 1f, 1f));
                    _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_HoverEmissionStrengthGreen, _HoverEmissionStrengthGreen, _HoverEmissionStrengthGreen));
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        DebugPrint(GetCurrentMethod());
        if (other.name.Split('_')[0] == "MenuPointer")
        {
            if (_Enabled)
            {
                _Text.GetComponent<Renderer>().material.DisableKeyword("GLOW_ON");

                if (_Active)
                {
                    _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0f, 0.05f, 0f));
                }
                else
                {
                    _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0.05f, 0.05f, 0.05f));
                }
            }
        }
    }

    void BaseTriggerEnterEvent(object sender, ButtonXRTriggerEvent e)
    {
        DebugPrint(GetCurrentMethod());
        if (_Enabled)
        {
            _Text.GetComponent<Renderer>().material.SetFloat("_GlowPower", 0.3f);

            if (_Active)
            {
                _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(0f, 1f, 0f));
                _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_ButtonPressEmissionStrengthRed, _ButtonPressEmissionStrengthGreen, _ButtonPressEmissionStrengthBlue));
            }
            else
            {
                _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(1f, 1f, 1f));
                _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_ButtonPressEmissionStrengthGreen, _ButtonPressEmissionStrengthGreen, _ButtonPressEmissionStrengthGreen));
            }
        }
    }

    void BaseTriggerExitEvent(object sender, ButtonXRTriggerEvent e)
    {
        DebugPrint(GetCurrentMethod());
        if (_Enabled)
        {
            _Text.GetComponent<Renderer>().material.SetFloat("_GlowPower", 0.1f);

            if (_Active)
            {
                _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(0f, 1f, 0f));
                _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_HoverEmissionStrengthRed, _HoverEmissionStrengthGreen, _HoverEmissionStrengthBlue));
            }
            else
            {
                _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(1f, 1f, 1f));
                _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_HoverEmissionStrengthGreen, _HoverEmissionStrengthGreen, _HoverEmissionStrengthGreen));
            }
        }
    }

    void OnButtonPressed(object sender, ButtonXRTriggerEvent e)
    {
        DebugPrint(GetCurrentMethod());
        if (_Enabled)
        {
            _Active = !_Active;

            if (_Active)
            {
                _Base.GetComponent<Renderer>().material.SetColor("_Color", _ColorActive);
                _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_ButtonPressEmissionStrengthRed, _ButtonPressEmissionStrengthGreen, _ButtonPressEmissionStrengthBlue));

                _Text.color = new Color(0f, 0.6f, 0f);// Color.green;
                _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(0f, 1f, 0f));
            }
            else
            {
                _Base.GetComponent<Renderer>().material.SetColor("_Color", _ColorInactive);
                _Base.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(_ButtonPressEmissionStrengthGreen, _ButtonPressEmissionStrengthGreen, _ButtonPressEmissionStrengthGreen));

                _Text.color = new Color(0.5f, 0.5f, 0.5f);// Color.white;
                _Text.GetComponent<Renderer>().material.SetColor("_GlowColor", new Color(1f, 1f, 1f));
            }

            OnToggleEvent?.Invoke(this, new ButtonToggleEvent(this.name, _Active));
        }
    }

    public void PulseGlowOn(bool active)
    {
        _PulseGlowOn = active;
    }

    private void Update()
    {
        if (_PulseGlowOn && _Active)
        {
            // Animate _ColorActive
            Color lerpedColor = Color.Lerp(Color.black, Color.green, Mathf.PingPong(Time.time, 1));
            _Base.GetComponent<Renderer>().material.SetColor("_Color", lerpedColor);
        }
    }
}
