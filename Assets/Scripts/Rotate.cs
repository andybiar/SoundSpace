using System;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    enum Direction { Clockwise, Counterclockwise }

    const float k_MinRotationSpeed = .001f;

    [SerializeField]
    Direction m_Direction = Direction.Clockwise;

    [SerializeField]
    float m_RotationSpeed = 12;

    void Update()
    {
        var r = transform.rotation.eulerAngles;
        var d = m_Direction == Direction.Clockwise ? -1 : 1;
        transform.rotation = Quaternion.Euler(r.x, r.y + Time.deltaTime * m_RotationSpeed * d, r.z);
    }

    void OnValidate()
    {
        if (m_RotationSpeed < k_MinRotationSpeed)
            m_RotationSpeed = k_MinRotationSpeed;
    }
}
