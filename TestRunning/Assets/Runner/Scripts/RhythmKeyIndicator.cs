using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Resolves rhythm keys when they reach the indicator trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RhythmKeyIndicator : MonoBehaviour
    {
        const string k_DefaultKeyLayerName = "keys";
        const string k_DefaultKeyLayerNamePascal = "Keys";

        [Header("Detection")]
        [SerializeField]
        LayerMask m_KeyLayerMask;

        [Header("Resolve")]
        [SerializeField]
        bool m_DestroyKeyOnEnter = true;

        [SerializeField]
        GameObject m_DisappearEffectPrefab;

        [SerializeField, Min(0.0f)]
        float m_DisappearEffectLifetime = 2.0f;

        [SerializeField]
        bool m_ParentDisappearEffectToIndicator = true;

        [SerializeField]
        bool m_ForceEffectLocalSimulationSpace = true;

        [Header("Resolve Audio")]
        [SerializeField]
        AudioSource m_ResolveAudioSource;

        [SerializeField]
        AudioClip m_ResolveSound;

        [SerializeField, Min(0.0f)]
        float m_ResolveSoundVolume = 1.0f;

        [SerializeField, Min(0.0f)]
        float m_ResolveSoundStartOffset;

        [SerializeField, Min(0.0f)]
        float m_EarlyResolveSoundDistance = 0.35f;

        [SerializeField, Min(0.0f)]
        float m_ResolvePitchVariance;

        [Header("Impact Shake")]
        [SerializeField]
        bool m_ShakeOnResolve = true;

        [SerializeField, Min(0.0f)]
        float m_ShakeDuration = 0.12f;

        [SerializeField, Min(0.0f)]
        float m_ShakeStrength = 0.06f;

        Coroutine m_ShakeCoroutine;
        Vector3 m_ShakeBaseLocalPosition;
        readonly HashSet<int> m_KeysWithResolveSoundPlayed = new HashSet<int>();

        public float EarlyResolveSoundDistance => m_EarlyResolveSoundDistance;

        void Awake()
        {
            ConfigurePhysics();
            ResolveAudioSource();
        }

        void Reset()
        {
            ConfigurePhysics();
            ResolveAudioSource();
            SetDefaultKeyLayerMaskIfNeeded();
        }

        void OnValidate()
        {
            m_DisappearEffectLifetime = Mathf.Max(0.0f, m_DisappearEffectLifetime);
            m_ResolveSoundVolume = Mathf.Max(0.0f, m_ResolveSoundVolume);
            m_ResolveSoundStartOffset = Mathf.Max(0.0f, m_ResolveSoundStartOffset);
            m_EarlyResolveSoundDistance = Mathf.Max(0.0f, m_EarlyResolveSoundDistance);
            m_ResolvePitchVariance = Mathf.Max(0.0f, m_ResolvePitchVariance);
            m_ShakeDuration = Mathf.Max(0.0f, m_ShakeDuration);
            m_ShakeStrength = Mathf.Max(0.0f, m_ShakeStrength);
            SetDefaultKeyLayerMaskIfNeeded();
        }

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                ConfigurePhysics();
                ResolveAudioSource();
            }
        }

        void OnDisable()
        {
            StopImpactShake();
        }

        void OnTriggerEnter(Collider other)
        {
            GameObject keyObject = GetKeyObject(other);
            if (keyObject == null || !IsInKeyLayer(keyObject))
            {
                return;
            }

            ResolveKey(keyObject);
        }

        public void ResolveKey(GameObject keyObject)
        {
            if (keyObject == null)
            {
                return;
            }

            RhythmKeyMover keyMover = keyObject.GetComponent<RhythmKeyMover>();
            if (keyMover != null && keyMover.IsResolved)
            {
                return;
            }

            SpawnDisappearEffect(keyObject.transform.position, keyObject.transform.rotation);
            PlayResolveSoundOnce(keyObject);
            PlayImpactShake();

            if (!m_DestroyKeyOnEnter)
            {
                return;
            }

            if (keyMover != null)
            {
                keyMover.Resolve();
                return;
            }

            Destroy(keyObject);
        }

        public void PlayEarlyResolveSound(GameObject keyObject)
        {
            if (keyObject == null || m_ResolveSound == null)
            {
                return;
            }

            PlayResolveSoundOnce(keyObject);
        }

        GameObject GetKeyObject(Collider keyCollider)
        {
            if (keyCollider == null)
            {
                return null;
            }

            RhythmKeyMover keyMover = keyCollider.GetComponentInParent<RhythmKeyMover>();
            if (keyMover != null)
            {
                return keyMover.gameObject;
            }

            return keyCollider.attachedRigidbody != null
                ? keyCollider.attachedRigidbody.gameObject
                : keyCollider.gameObject;
        }

        bool IsInKeyLayer(GameObject keyObject)
        {
            if (keyObject == null || m_KeyLayerMask.value == 0)
            {
                return false;
            }

            return (m_KeyLayerMask.value & (1 << keyObject.layer)) != 0;
        }

        void SpawnDisappearEffect(Vector3 position, Quaternion rotation)
        {
            if (m_DisappearEffectPrefab == null)
            {
                return;
            }

            GameObject effect = Instantiate(m_DisappearEffectPrefab, position, rotation);
            if (m_ParentDisappearEffectToIndicator)
            {
                effect.transform.SetParent(transform, true);
            }

            if (m_ForceEffectLocalSimulationSpace)
            {
                SetEffectSimulationSpace(effect, ParticleSystemSimulationSpace.Local);
            }

            if (m_DisappearEffectLifetime > 0.0f)
            {
                Destroy(effect, m_DisappearEffectLifetime);
            }
        }

        void PlayResolveSoundOnce(GameObject keyObject)
        {
            if (keyObject == null)
            {
                PlayResolveSound();
                return;
            }

            int keyId = keyObject.GetInstanceID();
            if (m_KeysWithResolveSoundPlayed.Contains(keyId))
            {
                return;
            }

            m_KeysWithResolveSoundPlayed.Add(keyId);
            PlayResolveSound();
        }

        void PlayResolveSound()
        {
            if (m_ResolveSound == null)
            {
                return;
            }

            ResolveAudioSource();
            if (m_ResolveAudioSource == null)
            {
                AudioSource.PlayClipAtPoint(m_ResolveSound, transform.position, m_ResolveSoundVolume);
                return;
            }

            if (m_ResolveSoundStartOffset > 0.0f)
            {
                PlayResolveSoundWithOffset();
                return;
            }

            float previousPitch = m_ResolveAudioSource.pitch;
            if (m_ResolvePitchVariance > 0.0f)
            {
                m_ResolveAudioSource.pitch = previousPitch + Random.Range(-m_ResolvePitchVariance, m_ResolvePitchVariance);
            }

            m_ResolveAudioSource.PlayOneShot(m_ResolveSound, m_ResolveSoundVolume);
            m_ResolveAudioSource.pitch = previousPitch;
        }

        void PlayResolveSoundWithOffset()
        {
            float clipLength = m_ResolveSound.length;
            float startOffset = Mathf.Clamp(m_ResolveSoundStartOffset, 0.0f, Mathf.Max(0.0f, clipLength - 0.01f));
            float pitch = GetResolveSoundPitch();

            GameObject audioObject = new GameObject($"{m_ResolveSound.name} Offset Audio");
            audioObject.transform.position = transform.position;

            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            CopyAudioSourceSettings(audioSource);
            audioSource.clip = m_ResolveSound;
            audioSource.volume = m_ResolveSoundVolume;
            audioSource.pitch = pitch;
            audioSource.time = startOffset;
            audioSource.Play();

            float remainingLifetime = (clipLength - startOffset) / Mathf.Max(0.01f, Mathf.Abs(pitch));
            Destroy(audioObject, remainingLifetime + 0.1f);
        }

        float GetResolveSoundPitch()
        {
            float pitch = m_ResolveAudioSource != null ? m_ResolveAudioSource.pitch : 1.0f;
            if (m_ResolvePitchVariance > 0.0f)
            {
                pitch += Random.Range(-m_ResolvePitchVariance, m_ResolvePitchVariance);
            }

            return Mathf.Max(0.01f, pitch);
        }

        void CopyAudioSourceSettings(AudioSource targetAudioSource)
        {
            if (targetAudioSource == null)
            {
                return;
            }

            targetAudioSource.playOnAwake = false;

            if (m_ResolveAudioSource == null)
            {
                targetAudioSource.spatialBlend = 0.0f;
                return;
            }

            targetAudioSource.outputAudioMixerGroup = m_ResolveAudioSource.outputAudioMixerGroup;
            targetAudioSource.spatialBlend = m_ResolveAudioSource.spatialBlend;
            targetAudioSource.rolloffMode = m_ResolveAudioSource.rolloffMode;
            targetAudioSource.minDistance = m_ResolveAudioSource.minDistance;
            targetAudioSource.maxDistance = m_ResolveAudioSource.maxDistance;
        }

        void SetEffectSimulationSpace(GameObject effect, ParticleSystemSimulationSpace simulationSpace)
        {
            if (effect == null)
            {
                return;
            }

            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem.MainModule main = particleSystems[i].main;
                main.simulationSpace = simulationSpace;
            }
        }

        void PlayImpactShake()
        {
            if (!m_ShakeOnResolve || m_ShakeDuration <= 0.0f || m_ShakeStrength <= 0.0f)
            {
                return;
            }

            StopImpactShake();
            m_ShakeBaseLocalPosition = transform.localPosition;
            m_ShakeCoroutine = StartCoroutine(ShakeIndicator());
        }

        IEnumerator ShakeIndicator()
        {
            float elapsed = 0.0f;
            while (elapsed < m_ShakeDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / m_ShakeDuration);
                float strength = m_ShakeStrength * (1.0f - normalizedTime);
                Vector2 offset = Random.insideUnitCircle * strength;

                transform.localPosition = m_ShakeBaseLocalPosition + new Vector3(offset.x, offset.y, 0.0f);
                yield return null;
            }

            transform.localPosition = m_ShakeBaseLocalPosition;
            m_ShakeCoroutine = null;
        }

        void StopImpactShake()
        {
            if (m_ShakeCoroutine == null)
            {
                return;
            }

            StopCoroutine(m_ShakeCoroutine);
            m_ShakeCoroutine = null;
            transform.localPosition = m_ShakeBaseLocalPosition;
        }

        void ConfigurePhysics()
        {
            Collider indicatorCollider = GetComponent<Collider>();
            if (indicatorCollider != null)
            {
                indicatorCollider.isTrigger = true;
            }

            Rigidbody indicatorRigidbody = GetComponent<Rigidbody>();
            if (indicatorRigidbody == null)
            {
                indicatorRigidbody = gameObject.AddComponent<Rigidbody>();
            }

            indicatorRigidbody.isKinematic = true;
            indicatorRigidbody.useGravity = false;
        }

        void ResolveAudioSource()
        {
            if (m_ResolveAudioSource == null)
            {
                m_ResolveAudioSource = GetComponent<AudioSource>();
            }

            if (m_ResolveAudioSource != null)
            {
                m_ResolveAudioSource.playOnAwake = false;
            }
        }

        void SetDefaultKeyLayerMaskIfNeeded()
        {
            if (m_KeyLayerMask.value != 0)
            {
                return;
            }

            int keyLayer = LayerMask.NameToLayer(k_DefaultKeyLayerName);
            if (keyLayer == -1)
            {
                keyLayer = LayerMask.NameToLayer(k_DefaultKeyLayerNamePascal);
            }

            if (keyLayer != -1)
            {
                m_KeyLayerMask = 1 << keyLayer;
            }
        }
    }
}
