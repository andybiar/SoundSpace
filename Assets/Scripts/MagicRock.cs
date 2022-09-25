using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class MagicRock : MonoBehaviour, IPlayable
{
    [Serializable]
    class Model
    {
        public static readonly int k_EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField]
        bool m_OscillateOnEnable;
        public bool OscillateOnEnable => m_OscillateOnEnable;
        
        [SerializeField]
        Vector2 m_OscillatingIntensities = new Vector2(.32f, .9f);
        public Vector2 OscillatingIntensities => m_OscillatingIntensities;

        [SerializeField]
        float m_MaxIntensity = 1.4f;
        public float MaxIntensity => m_MaxIntensity;

        [SerializeField]
        float m_OscillateTimeSeconds = .95f;
        public float OscillateTimeSeconds => m_OscillateTimeSeconds;

        [SerializeField]
        UnityEvent m_OnFirstPlay;
        public UnityEvent OnFirstPlay => m_OnFirstPlay;

        [SerializeField]
        float m_TimeUntilDecay = 1.5f;
        public float TimeUntilDecay => m_TimeUntilDecay;

        WaitForSeconds m_AwaitDecay;

        public AudioSource AudioSource { get; set; }
        public Renderer Renderer { get; set; }
        public Material Material { get; set; }
        public Color InitialColor { get; set; }

        public WaitForSeconds AwaitDecay
        {
            get
            {
                if (m_AwaitDecay == null) m_AwaitDecay = new WaitForSeconds(TimeUntilDecay);
                return m_AwaitDecay;
            }
        }
    }

    [SerializeField]
    Model m_Model;

    Idle m_Idle;
    Oscillating m_Oscillating;
    Playing m_Playing;
    Decaying m_Decaying;
    IState m_CurrentState;

    GameObject m_meshChild;
    Coroutine m_decayRoutine;

    void Awake()
    {
        m_meshChild = transform.GetChild(0).gameObject;
        m_Model.AudioSource = GetComponent<AudioSource>();
        m_Model.Renderer = m_meshChild.GetComponent<Renderer>();
        m_Model.Material = m_Model.Renderer.sharedMaterial;
        m_Model.InitialColor = m_Model.Material.GetColor(Model.k_EmissionColor);

        m_Idle = new Idle(m_Model);
        m_Oscillating = new Oscillating(m_Model);
        m_Playing = new Playing(m_Model);
        m_Decaying = new Decaying(m_Model);
    }
    
    void Start()
    {
        m_CurrentState = m_Model.OscillateOnEnable ? m_Oscillating : m_Idle;
        m_CurrentState.Enter();
    }

    void OnDestroy()
    {
        m_Model.Material.SetColor(Model.k_EmissionColor, m_Model.InitialColor);
    }

    void Update()
    {
        m_CurrentState.Update();
    }

    public void BeginOscillate()
    {
        if (m_CurrentState == m_Idle)
            m_CurrentState = m_Oscillating;
    }

    public void Play()
    {
        if (m_CurrentState == m_Idle) return;

        if (m_decayRoutine != null)
            StopCoroutine(m_decayRoutine);

        m_CurrentState.Exit();
        m_CurrentState = m_Playing;
        m_CurrentState.Enter();
        
        iTween.ShakePosition(m_meshChild, transform.right * .01f, m_Model.TimeUntilDecay + 2.25f);
        
        m_decayRoutine = StartCoroutine(Decay());
    }

    IEnumerator Decay()
    {
        yield return m_Model.AwaitDecay;
        
        var timeUntilEndOfClip = m_Model.AudioSource.clip.length - m_Model.AudioSource.time;
        m_Decaying.Duration = timeUntilEndOfClip;
        m_CurrentState.Exit();
        m_CurrentState = m_Decaying;
        m_CurrentState.Enter();
        yield return new WaitForSeconds(timeUntilEndOfClip);
        
        m_CurrentState.Exit();
        m_CurrentState = m_Oscillating;
        m_CurrentState.Enter();
    }

    #region States

    interface IState
    {
        void Enter();
        void Update();
        void Exit();
    }

    abstract class BaseState : IState
    {
        protected Model m_model;

        protected BaseState(Model model)
        {
            m_model = model;
        }

        public abstract void Enter();
        public abstract void Update();
        public abstract void Exit();
    }

    class Idle : BaseState
    {
        public Idle(Model model) : base(model) { }

        public override void Enter()
        {
            m_model.Material.SetColor(Model.k_EmissionColor, Color.black * 0);
        }

        public override void Update() { }
        public override void Exit() { }
    }

    class Oscillating : BaseState
    {
        float m_CurrentOscillateTime;
        int m_OscillateDirection;

        public Oscillating(Model model) : base(model) { }

        public override void Enter()
        {
            m_CurrentOscillateTime = 0;
            m_OscillateDirection = 1;
        }

        public override void Update()
        {
            var oscillateTimeSeconds = m_model.OscillateTimeSeconds;
            m_CurrentOscillateTime += Time.deltaTime * m_OscillateDirection;
            if (m_CurrentOscillateTime >= oscillateTimeSeconds)
            {
                m_CurrentOscillateTime = oscillateTimeSeconds;
                m_OscillateDirection = -1;
            }
            else if (m_CurrentOscillateTime <= 0)
            {
                m_CurrentOscillateTime = 0;
                m_OscillateDirection = 1;
            }

            float t = m_CurrentOscillateTime / oscillateTimeSeconds;
            float intensity = Mathf.Lerp(m_model.OscillatingIntensities.x, m_model.OscillatingIntensities.y, t);
            m_model.Material.SetColor(Model.k_EmissionColor, m_model.InitialColor * intensity);
        }

        public override void Exit() { }
    }

    class Playing : BaseState
    {
        bool m_hasFirstPlayed;

        public Playing(Model model) : base(model) { }

        public override void Enter()
        {
            if (!m_hasFirstPlayed)
            {
                m_model.OnFirstPlay?.Invoke();
                m_hasFirstPlayed = true;
            }

            m_model.AudioSource.Play();
            m_model.Material.SetColor(Model.k_EmissionColor, m_model.InitialColor * m_model.MaxIntensity);
        }

        public override void Update() { }

        public override void Exit() { }
    }

    class Decaying : BaseState
    {
        public float Duration;
        float m_InitialTime;
        float m_DecayRange;

        public Decaying(Model model) : base(model) { }

        public override void Enter()
        {
            m_InitialTime = Time.time;
            m_DecayRange = m_model.MaxIntensity - m_model.OscillatingIntensities.x;
        }

        public override void Update()
        {
            float t = Mathf.Clamp((Time.time - m_InitialTime) / Duration, 0, 1) * Mathf.PI / 2;
            float intensity = Mathf.Cos(t) * m_DecayRange + m_model.OscillatingIntensities.x;
            m_model.Material.SetColor(Model.k_EmissionColor, m_model.InitialColor * intensity);
        }

        public override void Exit() { }
    }

    #endregion
}
