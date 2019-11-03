using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handwriting with a specified pen tip collider potentially manipulated by e.g. VR hand controllers
/// </summary>
[RequireComponent(typeof(Collider))]
public class PenTipTriggerWriting : Writing
{
    /// <summary>
    /// Index of layer which specifies that an object is a pen tip
    /// </summary>
    [Tooltip("Index of layer which specifies that an object is a pen tip")]
    public int penTipLayerIndex;

    /// <summary>
    /// Update current line based on collision event, called on both collision enter and stay
    /// </summary>
    /// <param name="other">collision data from event</param>
    void UpdateOnCollision(Collision other)
    {
        if (other.collider.gameObject.layer == penTipLayerIndex)
        {
            ContactPoint cp = other.contacts[0];
            Plane myPlane = new Plane(transform.up,transform.position); //plane representation of this surface
            //do a plane cast on contact point for more consistent results
            if (myPlane.Raycast(new Ray(cp.point, transform.up), out var planeProjectEnter))
            {
                Vector3 contactPoint = cp.point + transform.up * planeProjectEnter;
                UpdateLine(contactPoint,
                    -transform.up,
                    transform.forward);
            }
        }
    }
    
    private void OnCollisionEnter(Collision other)
    {
        UpdateOnCollision(other);
    }

    private void OnCollisionStay(Collision other)
    {
        UpdateOnCollision(other);
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.collider.gameObject.layer == penTipLayerIndex)
        {
            //end the current line
            EndLine();
        }
    }
}
