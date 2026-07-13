using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Adds speed to the player when touched.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SpeedPickup : Spawnable
    {
        const string k_PlayerTag = "Player";

        [SerializeField, Min(0.0f)]
        float m_SpeedAmount = 5.0f;

        [SerializeField]
        bool m_ApplyInstantly = true;

        [SerializeField]
        bool m_HideAfterPickup = true;

        [SerializeField]
        bool m_DisableCollidersAfterPickup = true;

        bool m_Collected;
        Renderer[] m_Renderers;
        Collider[] m_Colliders;

        public override void ResetSpawnable()
        {
            m_Collected = false;
            SetRenderersEnabled(true);
            SetCollidersEnabled(true);
        }

        void OnValidate()
        {
            m_SpeedAmount = Mathf.Max(0.0f, m_SpeedAmount);
        }

        protected override void Awake()
        {
            base.Awake();

            m_Renderers = GetComponentsInChildren<Renderer>();
            m_Colliders = GetComponentsInChildren<Collider>();
        }

        void OnTriggerEnter(Collider col)
        {
            if (!m_Collected && col.CompareTag(k_PlayerTag))
            {
                Collect();
            }
        }

        void Collect()
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.AdjustSpeed(m_SpeedAmount, m_ApplyInstantly);
            }

            m_Collected = true;

            if (m_HideAfterPickup)
            {
                SetRenderersEnabled(false);
            }

            if (m_DisableCollidersAfterPickup)
            {
                SetCollidersEnabled(false);
            }
        }

        void SetRenderersEnabled(bool enabled)
        {
            if (m_Renderers == null)
            {
                m_Renderers = GetComponentsInChildren<Renderer>();
            }

            for (int i = 0; i < m_Renderers.Length; i++)
            {
                if (m_Renderers[i] != null)
                {
                    m_Renderers[i].enabled = enabled;
                }
            }
        }

        void SetCollidersEnabled(bool enabled)
        {
            if (m_Colliders == null)
            {
                m_Colliders = GetComponentsInChildren<Collider>();
            }

            for (int i = 0; i < m_Colliders.Length; i++)
            {
                if (m_Colliders[i] != null)
                {
                    m_Colliders[i].enabled = enabled;
                }
            }
        }
    }
}
