using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Marks a beat-spawned key that stays in place until an indicator resolves it.
    /// </summary>
    public class RhythmBeatSyncedKey : MonoBehaviour
    {
        public void Initialize(
            RhythmKeyIndicator indicator,
            AudioSource audioSource,
            float spawnSongTime,
            float hitSongTime,
            float resolveDistance)
        {
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
