/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;

public class SpinnerXR : MonoBehaviour
{
    public Material _Material;

    public void SetProgress( float progress )
    {
        _Material.SetFloat("_Progress", progress);
    }

    public void SetActive( bool active )
    {
        SetActive(active);
    }
}
