using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Moves a rhythm key toward a static indicator/player target.
    /// </summary>
    public class RhythmKeyMover : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        Transform m_TargetIndicator;

        [SerializeField, Min(0.0f)]
        float m_MoveSpeed = 8.0f;

        [SerializeField, Min(0.0f)]
        float m_AutoResolveDistance = 0.05f;

        [Header("Floating Motion")]
        [SerializeField]
        bool m_EnableFloatingMotion = true;

        [SerializeField]
        Vector3 m_RotationSpeed = new Vector3(25.0f, 60.0f, 35.0f);

        [SerializeField, Min(0.0f)]
        float m_FloatAmplitude = 0.1f;

        [SerializeField, Min(0.0f)]
        float m_FloatFrequency = 2.0f;

        bool m_IsResolved;
        float m_BaseY;
        float m_FloatPhase;

        public bool IsResolved => m_IsResolved;

        void Awake()
        {
            m_BaseY = transform.position.y;
            m_FloatPhase = Random.Range(0.0f, Mathf.PI * 2.0f);
        }

        void OnValidate()
        {
            m_MoveSpeed = Mathf.Max(0.0f, m_MoveSpeed);
            m_AutoResolveDistance = Mathf.Max(0.0f, m_AutoResolveDistance);
            m_FloatAmplitude = Mathf.Max(0.0f, m_FloatAmplitude);
            m_FloatFrequency = Mathf.Max(0.0f, m_FloatFrequency);
        }

        void Update()
        {
            if (m_IsResolved || m_TargetIndicator == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            MoveToTarget(deltaTime);
            UpdateFloatingMotion(deltaTime);
            ResolveIfReachedTarget();
        }

        public void SetTarget(Transform targetIndicator)
        {
            m_TargetIndicator = targetIndicator;
        }

        public void Resolve()
        {
            if (m_IsResolved)
            {
                return;
            }

            m_IsResolved = true;
            Destroy(gameObject);
        }

        void MoveToTarget(float deltaTime)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                m_TargetIndicator.position,
                m_MoveSpeed * deltaTime);
        }

        void UpdateFloatingMotion(float deltaTime)
        {
            if (!m_EnableFloatingMotion)
            {
                return;
            }

            transform.Rotate(m_RotationSpeed * deltaTime, Space.Self);

            if (m_FloatAmplitude <= 0.0f || m_FloatFrequency <= 0.0f)
            {
                return;
            }

            Vector3 position = transform.position;
            position.y = m_BaseY + Mathf.Sin(Time.time * m_FloatFrequency + m_FloatPhase) * m_FloatAmplitude;
            transform.position = position;
        }

        void ResolveIfReachedTarget()
        {
            if (m_AutoResolveDistance <= 0.0f)
            {
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, m_TargetIndicator.position);
            if (distanceToTarget > m_AutoResolveDistance)
            {
                return;
            }

            RhythmKeyIndicator indicator = m_TargetIndicator.GetComponent<RhythmKeyIndicator>();
            if (indicator != null)
            {
                indicator.ResolveKey(gameObject);
                return;
            }

            Resolve();
        }
    }
}
