using System;
using UnityEngine;

public class LookAtCameraOnEnable : MonoBehaviour
{
    void OnEnable()
    {
        var camera = FindObjectOfType<Camera>();
        var r1 = transform.rotation;
        transform.LookAt(camera.transform);
        var r2 = transform.rotation;
        transform.rotation = Quaternion.Euler(r1.x, r2.y + 140, r1.z);
    }
}
