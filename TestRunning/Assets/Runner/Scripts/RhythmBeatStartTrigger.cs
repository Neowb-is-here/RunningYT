using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Starts a RhythmBeatSpawner when the player enters this trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RhythmBeatStartTrigger : MonoBehaviour
    {
        const string k_PlayerTag = "Player";

        [SerializeField]
        RhythmBeatSpawner m_RhythmBeatSpawner;

        [SerializeField]
        string m_TriggerTag = k_PlayerTag;

        [SerializeField]
        bool m_StartOnlyOnce = true;

        [SerializeField]
        bool m_RestartIfAlreadyRunning;

        bool m_HasStarted;

        void Reset()
        {
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (m_StartOnlyOnce && m_HasStarted)
            {
                return;
            }

            if (!string.IsNullOrEmpty(m_TriggerTag) && !other.CompareTag(m_TriggerTag))
            {
                return;
            }

            if (m_RhythmBeatSpawner == null)
            {
                return;
            }

            if (!m_RestartIfAlreadyRunning && m_RhythmBeatSpawner.IsRunning)
            {
                return;
            }

            m_HasStarted = true;
            m_RhythmBeatSpawner.StartRhythm();
        }
    }
}
