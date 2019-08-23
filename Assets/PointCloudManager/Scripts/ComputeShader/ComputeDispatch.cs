using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeDispatch : MonoBehaviour
{
    public ComputeShader _ComputeShader;
    public RenderTexture _Result;

    private Renderer _Renderer;

    void Start ()
    {
        // RunShader();
        _Renderer = GetComponent<Renderer>();
    }


    void RunShader()
    {
        int kernelHandle = _ComputeShader.FindKernel("CSMain");

        _Result = new RenderTexture(256, 256, 24);
        _Result.enableRandomWrite = true;
        _Result.Create();

        _ComputeShader.SetTexture(kernelHandle, "Result", _Result);

        _ComputeShader.Dispatch(kernelHandle, 256 / 8, 256 / 8, 1);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RunShader();
            _Renderer.material.SetTexture("_MainTex",_Result);
        }
    }
}
