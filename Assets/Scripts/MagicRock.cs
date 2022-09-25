using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(MeshRenderer))]
public class MagicRock : MonoBehaviour, IPlayable
{
    enum State { Idle, Oscillating, Playing }

    static readonly int k_EmissionColor = Shader.PropertyToID("_EmissionColor");

    [SerializeField]
    bool m_OscillateOnEnable;

    [SerializeField]
    Vector2 m_OscillatingIntensities = new Vector2(.2f, .7f);

    [SerializeField]
    float m_MaxIntensity = 1.4f;

    [SerializeField]
    float m_OscillateTimeSeconds = 1.5f;

    [SerializeField]
    UnityEvent m_OnFirstPlay;

    [SerializeField]
    float m_RattleMax = 5;

    [SerializeField]
    Vector2 m_TimeRangeBetweenRattles = new Vector2(.05f, .15f);

    AudioSource m_AudioSource;
    Renderer m_Renderer;
    Material m_Material;
    Color m_InitialColor;
    float m_CurrentOscillateTime;
    int m_OscillateDirection = 1;
    State m_CurrentState = State.Idle;
    bool m_hasFirstPlayed;
    
    float m_CurrentRattleTime;
    float m_CurrentMaxRattleTime;
    float m_CurrentRattleStart;
    float m_CurrentRattleEnd;

    void OnEnable()
    {
        m_AudioSource = GetComponent<AudioSource>();
        m_Renderer = GetComponent<Renderer>();
        m_Material = m_Renderer.sharedMaterial;
        m_InitialColor = m_Material.GetColor(k_EmissionColor);
        if (!m_OscillateOnEnable)
            m_Material.SetColor(k_EmissionColor, m_InitialColor * 0);
        else
            m_CurrentState = State.Oscillating;
    }

    void OnDisable()
    {
        m_Material.SetColor(k_EmissionColor, m_InitialColor);
    }

    void Update()
    {
        if (m_CurrentState is not State.Oscillating)
            return;

        Oscillate();
    }

    void Oscillate()
    {
        m_CurrentOscillateTime += Time.deltaTime * m_OscillateDirection;
        if (m_CurrentOscillateTime >= m_OscillateTimeSeconds)
        {
            m_CurrentOscillateTime = m_OscillateTimeSeconds;
            m_OscillateDirection = -1;
        }
        else if (m_CurrentOscillateTime <= 0)
        {
            m_CurrentOscillateTime = 0;
            m_OscillateDirection = 1;
        }

        float t = m_CurrentOscillateTime / m_OscillateTimeSeconds;
        float intensity = Mathf.Lerp(m_OscillatingIntensities.x, m_OscillatingIntensities.y, t);
        m_Material.SetColor(k_EmissionColor, m_InitialColor * intensity);
    }

    public void BeginOscillate()
    {
        m_OscillateOnEnable = true;
        if (m_CurrentState is State.Idle)
            m_CurrentState = State.Oscillating;
    }

    public void Play()
    {
        if (m_CurrentState is State.Idle) return;
        StartCoroutine(Playing());
    }

    IEnumerator Playing()
    {
        if (!m_hasFirstPlayed)
            m_OnFirstPlay.Invoke();

        m_CurrentState = State.Playing;
        m_AudioSource.Play();
        m_Material.SetColor(k_EmissionColor, m_InitialColor * m_MaxIntensity);
        yield return null;

        while (m_CurrentState is State.Playing && m_AudioSource.isPlaying)
        {
            /*
            m_CurrentRattleTime += Time.deltaTime;
            if (m_CurrentRattleTime > m_CurrentMaxRattleTime)
                SetNewRattle();

            var t = m_CurrentRattleTime / m_CurrentMaxRattleTime;
            float intensity = Mathf.Lerp(m_CurrentRattleStart, m_CurrentRattleEnd, t);
            m_Material.SetColor(k_EmissionColor, m_InitialColor * intensity);*/
            yield return null;
        }

        OnSoundFinishedPlaying();
    }

    void SetNewRattle()
    {
        m_CurrentRattleStart = m_CurrentRattleEnd;
        m_CurrentRattleEnd = m_MaxIntensity + Random.Range(0, m_RattleMax) * Random.Range(0, 2) * 2 - 1;
        m_CurrentMaxRattleTime = Random.Range(m_TimeRangeBetweenRattles.x, m_TimeRangeBetweenRattles.y);
        m_CurrentRattleTime = 0;
    }
    void OnSoundFinishedPlaying()
    {
        if (m_OscillateOnEnable)
            m_CurrentState = State.Oscillating;
        else
            m_CurrentState = State.Idle;
    }
}
