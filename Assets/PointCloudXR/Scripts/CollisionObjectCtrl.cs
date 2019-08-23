/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;

public class CollisionObjectCtrl : MonoBehaviour
{
    public Transform _EyeCameraTransform;

	void Update ()
    {
        transform.localPosition = new Vector3(_EyeCameraTransform.localPosition.x, 0, _EyeCameraTransform.localPosition.z);
	}
}
