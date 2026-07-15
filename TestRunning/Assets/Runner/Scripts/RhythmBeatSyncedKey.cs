using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Marks a beat-spawned key that stays in place until an indicator resolves it.
    /// </summary>
    public class RhythmBeatSyncedKey : MonoBehaviour
    {
        RhythmKeyIndicator m_Indicator;
        bool m_HasPlayedEarlyResolveSound;

        public void Initialize(
            RhythmKeyIndicator indicator,
            AudioSource audioSource,
            float spawnSongTime,
            float hitSongTime,
            float resolveDistance)
        {
            m_Indicator = indicator;
            m_HasPlayedEarlyResolveSound = false;
            ConfigurePhysics();
        }

        void Awake()
        {
            ConfigurePhysics();
        }

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                ConfigurePhysics();
            }
        }

        void Update()
        {
            if (m_HasPlayedEarlyResolveSound || m_Indicator == null)
            {
                return;
            }

            float earlySoundDistance = m_Indicator.EarlyResolveSoundDistance;
            if (earlySoundDistance <= 0.0f)
            {
                return;
            }

            float sqrDistance = (transform.position - m_Indicator.transform.position).sqrMagnitude;
            if (sqrDistance > earlySoundDistance * earlySoundDistance)
            {
                return;
            }

            m_HasPlayedEarlyResolveSound = true;
            m_Indicator.PlayEarlyResolveSound(gameObject);
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
