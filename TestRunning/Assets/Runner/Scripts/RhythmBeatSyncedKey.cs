using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Marks a beat-spawned key that stays in place until an indicator resolves it.
    /// </summary>
    public class RhythmBeatSyncedKey : MonoBehaviour
    {
        RhythmKeyIndicator m_Indicator;
        Vector3 m_PreviousIndicatorPosition;
        float m_ResolveDistance;
        bool m_HasPlayedEarlyResolveSound;
        bool m_HasPreviousIndicatorPosition;
        bool m_IsResolved;

        public void Initialize(
            RhythmKeyIndicator indicator,
            AudioSource audioSource,
            float spawnSongTime,
            float hitSongTime,
            float resolveDistance)
        {
            m_Indicator = indicator;
            m_ResolveDistance = Mathf.Max(0.0f, resolveDistance);
            m_HasPlayedEarlyResolveSound = false;
            m_IsResolved = false;
            InitializePreviousIndicatorPosition();
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
            if (m_IsResolved || m_Indicator == null)
            {
                return;
            }

            UpdateEarlyResolveSound();
            UpdateResolveFallback();
        }

        void UpdateEarlyResolveSound()
        {
            if (m_HasPlayedEarlyResolveSound)
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

        void UpdateResolveFallback()
        {
            if (m_ResolveDistance <= 0.0f)
            {
                return;
            }

            Vector3 toKey = transform.position - m_Indicator.transform.position;
            float sqrResolveDistance = m_ResolveDistance * m_ResolveDistance;

            bool isCloseEnough = toKey.sqrMagnitude <= sqrResolveDistance;
            bool pathPassedCloseEnough = m_HasPreviousIndicatorPosition &&
                GetSqrDistanceToIndicatorMovementPath() <= sqrResolveDistance;

            m_PreviousIndicatorPosition = m_Indicator.transform.position;
            m_HasPreviousIndicatorPosition = true;

            if (!isCloseEnough && !pathPassedCloseEnough)
            {
                return;
            }

            m_IsResolved = true;
            m_Indicator.ResolveKey(gameObject);
        }

        float GetSqrDistanceToIndicatorMovementPath()
        {
            Vector3 currentIndicatorPosition = m_Indicator.transform.position;
            Vector3 segment = currentIndicatorPosition - m_PreviousIndicatorPosition;
            float segmentLengthSqr = segment.sqrMagnitude;

            if (segmentLengthSqr <= 0.0001f)
            {
                return (transform.position - currentIndicatorPosition).sqrMagnitude;
            }

            float segmentTime = Vector3.Dot(transform.position - m_PreviousIndicatorPosition, segment) / segmentLengthSqr;
            segmentTime = Mathf.Clamp01(segmentTime);

            Vector3 closestPoint = m_PreviousIndicatorPosition + segment * segmentTime;
            return (transform.position - closestPoint).sqrMagnitude;
        }

        void InitializePreviousIndicatorPosition()
        {
            if (m_Indicator == null)
            {
                m_HasPreviousIndicatorPosition = false;
                return;
            }

            m_PreviousIndicatorPosition = m_Indicator.transform.position;
            m_HasPreviousIndicatorPosition = true;
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
