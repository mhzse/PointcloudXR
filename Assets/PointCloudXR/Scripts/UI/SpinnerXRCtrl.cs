/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;
using TMPro;

public class SpinnerXRCtrl : MonoBehaviour
{
    public SpinnerXR _SpinnerXR;
    public TextMeshPro _Text;

    public void SetProgress( float progress )
    {
        _SpinnerXR.SetProgress(progress);
        _Text.text = (progress * 100).ToString("n0") + "%";
    }

    public void SetActive( bool active )
    {
        if(!active)
        {
            _SpinnerXR.SetProgress(0);
            _Text.text = "";
        }

        _SpinnerXR.gameObject.SetActive(active);
        _Text.gameObject.SetActive(active);
    }
}
