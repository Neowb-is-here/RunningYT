using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Moves a spawned key so it reaches its indicator on a target song beat.
    /// </summary>
    public class RhythmBeatSyncedKey : MonoBehaviour
    {
        RhythmKeyIndicator m_Indicator;
        AudioSource m_AudioSource;
        Vector3 m_StartPosition;
        float m_SpawnSongTime;
        float m_HitSongTime;
        float m_ResolveDistance;
        float m_FallbackStartTime;
        bool m_IsResolved;

        public void Initialize(
            RhythmKeyIndicator indicator,
            AudioSource audioSource,
            float spawnSongTime,
            float hitSongTime,
            float resolveDistance)
        {
            m_Indicator = indicator;
            m_AudioSource = audioSource;
            m_StartPosition = transform.position;
            m_SpawnSongTime = spawnSongTime;
            m_HitSongTime = hitSongTime;
            m_ResolveDistance = Mathf.Max(0.0f, resolveDistance);
            m_FallbackStartTime = Time.time - spawnSongTime;
            m_IsResolved = false;

            ConfigurePhysics();
        }

        void Update()
        {
            if (m_IsResolved || m_Indicator == null)
            {
                return;
            }

            float songTime = GetSongTime();
            float travelDuration = Mathf.Max(0.0001f, m_HitSongTime - m_SpawnSongTime);
            float normalizedTime = Mathf.Clamp01((songTime - m_SpawnSongTime) / travelDuration);

            transform.position = Vector3.Lerp(m_StartPosition, m_Indicator.transform.position, normalizedTime);

            if (songTime >= m_HitSongTime || Vector3.Distance(transform.position, m_Indicator.transform.position) <= m_ResolveDistance)
            {
                m_IsResolved = true;
                m_Indicator.ResolveKey(gameObject);
            }
        }

        float GetSongTime()
        {
            if (m_AudioSource != null && m_AudioSource.clip != null)
            {
                return m_AudioSource.time;
            }

            return Time.time - m_FallbackStartTime;
        }

        void ConfigurePhysics()
        {
            Rigidbody keyRigidbody = GetComponent<Rigidbody>();
            if (keyRigidbody == null)
            {
                keyRigidbody = gameObject.AddComponent<Rigidbody>();
            }

            keyRigidbody.isKinematic = true;
            keyRigidbody.useGravity = false;
        }
    }
}
