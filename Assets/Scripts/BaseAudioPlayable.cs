using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BaseAudioPlayable : MonoBehaviour, IPlayable
{
    AudioSource m_AudioSource;

    void OnEnable()
    {
        m_AudioSource = GetComponent<AudioSource>();
    }

    public void Play()
    {
        m_AudioSource.Play();
    }
}
