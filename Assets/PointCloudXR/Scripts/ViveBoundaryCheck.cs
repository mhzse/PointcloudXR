/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;

public class ViveBoundaryCheck : MonoBehaviour 
{
    void OnCollisionEnter(Collision col)
    {
        Debug.Log("Boundary OnCollisionEnter, col: " + col.gameObject.name);
        if (col.gameObject.name == "Boundary")
        {
            Debug.Log("Boundary collision");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Boundary")
        {
            Debug.Log("Boundary OnTriggerEnter");
        }
        
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "Boundary")
        {
            Debug.Log("Boundary OnTriggerExit");
        }

    }
}
