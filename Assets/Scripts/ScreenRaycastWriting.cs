using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handwriting with mouse input, raycasting from screen point to surface
/// </summary>
public class ScreenRaycastWriting : Writing
{
    /// <summary>
    /// the main camera
    /// </summary>
    private Camera _mainCamera;
    
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        _mainCamera = Camera.main;
    }

    // Update is called once per frame
    protected override void Update()
    {
        //handle mouse inputs
        if (Input.GetMouseButton(0))
        {
            //raycast from screen to surface
            Ray ray = _mainCamera.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                0));

            if (Physics.Raycast(ray, out var hitInfo, 1000))
            {
                if (hitInfo.collider.gameObject == gameObject)
                    UpdateLine(hitInfo.point,-hitInfo.normal,hitInfo.collider.transform.forward); 
                //hacky (uses writing surface as up reference)
            }

        }

        if (Input.GetMouseButtonUp(0))
        {
            EndLine();
        }

        base.Update();
    }

    
}
