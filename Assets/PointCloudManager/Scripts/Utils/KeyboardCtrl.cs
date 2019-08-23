/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;

public class KeyboardCtrl : MonoBehaviour
{
    public PointCloudManager _PointCloudManager;

	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            _PointCloudManager.TogglePointClassVisibility(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _PointCloudManager.TogglePointClassVisibility(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _PointCloudManager.TogglePointClassVisibility(2);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _PointCloudManager.TogglePointClassVisibility(3);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Rigidbody[] foundObjects = FindObjectsOfType<Rigidbody>();

            for (int i = 0; i < foundObjects.Length; i++)
            {
                CollisionDetectionMode cdm = foundObjects[i].collisionDetectionMode;
                string obj_name = foundObjects[i].gameObject.name;

                if (cdm == CollisionDetectionMode.ContinuousDynamic)
                {
                    print($"rb {i}: {cdm} {obj_name}");
                }
            }
        }
    }
}
