using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Invert colors post-processing effect
/// </summary>
public class Invert : MonoBehaviour
{
    /// <summary>
    /// Whether to invert the colors or not
    /// </summary>
    [Tooltip("Whether to invert the colors or not")]
    public bool invert = true;
    
    private Shader _invertShader;
    private Material _material;
    
    // Start is called before the first frame update
    void Start()
    {
        //create the material to use
        _invertShader = Shader.Find("Hidden/Invert");
        _material = new Material(_invertShader);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (invert)
        {
            // Copy the source Render Texture to the destination,
            // applying the material along the way.
            Graphics.Blit(src, dest, _material);
        }
    }
}
