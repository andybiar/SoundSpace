using System;
using UnityEngine;

public class OscillateHeight : MonoBehaviour
{
    const float k_MinOscillateDistance = .0001f;
    const float k_MinOscillateTime = .016f;
    
    [SerializeField]
    float m_OscillateDistance = .5f;

    [SerializeField]
    float m_OscillateTimeSeconds = 1;

    float m_CurrentOscillateTime;
    int m_Direction = 1;
    Vector3 m_InitialPosition;
    Vector3 m_MaxPosition;

    void Start()
    {
        var p = transform.position;
        m_InitialPosition = p;
        m_MaxPosition = new Vector3(p.x, p.y + m_OscillateDistance, p.z);
    }

    void Update()
    {
        m_CurrentOscillateTime += Time.deltaTime * m_Direction;
        if (m_CurrentOscillateTime >= m_OscillateTimeSeconds)
        {
            m_Direction = -1;
            m_CurrentOscillateTime = m_OscillateTimeSeconds;
            transform.position = m_MaxPosition;
        }
        else if (m_CurrentOscillateTime <= 0)
        {
            m_Direction = 1;
            m_CurrentOscillateTime = 0;
            transform.position = m_InitialPosition;
        }
        else
        {
            var t = m_CurrentOscillateTime / m_OscillateTimeSeconds;
            transform.position = Vector3.Lerp(m_InitialPosition, m_MaxPosition, t);
        }
    }

    void OnValidate()
    {
        if (m_OscillateDistance < k_MinOscillateDistance)
            m_OscillateDistance = k_MinOscillateDistance;

        if (m_OscillateTimeSeconds < k_MinOscillateTime)
            m_OscillateDistance = k_MinOscillateTime;
    }
}
