/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;

[ExecuteInEditMode]
public class InvertColors : MonoBehaviour
{
    public Material material;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, material);
    }
}
