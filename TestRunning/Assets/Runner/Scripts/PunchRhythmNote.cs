using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// A moving punch note controlled by PunchRhythmManager.
    /// </summary>
    public class PunchRhythmNote : MonoBehaviour
    {
        PunchRhythmManager m_Manager;
        int m_LaneIndex;
        bool m_IsResolved;
        bool m_EnableFloatingMotion;
        float m_BaseY;
        float m_FloatAmplitude;
        float m_FloatFrequency;
        float m_FloatPhase;
        Vector3 m_RotationSpeed;

        public int LaneIndex => m_LaneIndex;
        public bool IsResolved => m_IsResolved;

        public void Initialize(PunchRhythmManager manager, int laneIndex)
        {
            m_Manager = manager;
            m_LaneIndex = laneIndex;
            m_IsResolved = false;
            m_BaseY = transform.position.y;
            m_EnableFloatingMotion = manager != null && manager.EnableFloatingMotion;
            m_FloatAmplitude = manager != null ? manager.FloatAmplitude : 0.0f;
            m_FloatFrequency = manager != null ? manager.FloatFrequency : 0.0f;
            m_FloatPhase = Random.Range(0.0f, Mathf.PI * 2.0f);
            m_RotationSpeed = manager != null ? manager.GetRandomRotationSpeed() : Vector3.zero;
        }

        void Update()
        {
            if (m_Manager != null && !m_IsResolved)
            {
                float deltaTime = Time.deltaTime;
                m_Manager.UpdateNote(this, deltaTime);

                if (!m_IsResolved)
                {
                    UpdateFloatingMotion(deltaTime);
                }
            }
        }

        public void Resolve()
        {
            m_IsResolved = true;
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
    }
}
