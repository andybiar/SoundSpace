using System;
using Niantic.ARDK.Utilities.Input.Legacy;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PhysicsRaycastOnTouch : MonoBehaviour
{
    Camera m_Camera;

    void OnEnable()
    {
        m_Camera = GetComponent<Camera>();
    }

    void Update()
    {
        if (PlatformAgnosticInput.touchCount <= 0) return;
        var touch = PlatformAgnosticInput.GetTouch(0);
        if (touch.IsTouchOverUIObject()) return;
        
        if (touch.phase == TouchPhase.Began)
        {
            OnTouchScreen(touch);
        }
    }

    void OnTouchScreen(Touch touch)
    {
        Ray ray = m_Camera.ScreenPointToRay(touch.position);
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            var g = hitInfo.collider.gameObject;
            var rock = g.GetComponent<MagicRock>();
            if (rock is null) return;

            rock.Play();
        }
    }
}
