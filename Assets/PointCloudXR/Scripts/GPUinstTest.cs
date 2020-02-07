/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;

public class GPUinstTest : MonoBehaviour
{
    public GameObject point;

	void Start ()
    {
        int limit = 100000;
        float sideLength = Mathf.Sqrt(limit);
		for( int i = 0; i < limit; i++)
        {
            Instantiate( point, new Vector3((i % sideLength) * 0.1f, 0, 0.1f * sideLength), Quaternion.identity);
        }
	}

}
