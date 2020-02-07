using UnityEngine;

public class ActivateMultiDisplay : MonoBehaviour {


	void Start ()
    {
        Debug.Log("displays connected: " + Display.displays.Length);
    }
}
