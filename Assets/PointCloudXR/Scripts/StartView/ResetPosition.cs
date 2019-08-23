/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;

public class ResetPosition : MonoBehaviour
{
    public GameObject _EyeCamera;
    private Transform _EyeTransform;

	void Start ()
    {
        _EyeTransform = _EyeCamera.transform;
    }
	
    private void SetPosition()
    {

    }

	void Update ()
    {
		if(Input.GetKeyDown(KeyCode.R))
        {
            transform.position = _EyeTransform.position + _EyeTransform.forward * 0.5f;
            transform.LookAt(_EyeTransform);
        }
    }
}
