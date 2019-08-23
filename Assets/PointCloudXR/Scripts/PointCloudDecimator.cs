/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;

public class PointCloudDecimator : MonoBehaviour
{
    public GameObject pointCloud;
    private Material pointCloudMaterial;
    private GameObject mainCamera;
    private Vector4 position;
    private Vector3 startPosition;

    void Start()
    {
        pointCloudMaterial = pointCloud.GetComponent<Renderer>().sharedMaterial;
        position = new Vector4();
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        startPosition = mainCamera.transform.position;
    }

    void FixedUpdate()
    {
        position[0] = mainCamera.transform.position.x;
        position[1] = mainCamera.transform.position.y;
        position[2] = mainCamera.transform.position.z;
        position[3] = 0;
        pointCloudMaterial.SetVector("_UserPos", position);
    }

    void OnApplicationQuit()
    {
        pointCloudMaterial.SetVector("_UserPos", startPosition);
    }
}