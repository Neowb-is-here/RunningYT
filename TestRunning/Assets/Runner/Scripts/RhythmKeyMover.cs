using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Marks a rhythm key that stays in place until a RhythmKeyIndicator resolves it.
    /// </summary>
    public class RhythmKeyMover : MonoBehaviour
    {
        bool m_IsResolved;

        public bool IsResolved => m_IsResolved;

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

        /// <summary>
        /// Kept for older scene references. Keys no longer chase a target.
        /// </summary>
        public void SetTarget(Transform targetIndicator)
        {
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
