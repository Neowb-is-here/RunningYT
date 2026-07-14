using System.Collections;
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

        [Header("Impact Shake")]
        [SerializeField]
        bool m_ShakeOnResolve = true;

        [SerializeField, Min(0.0f)]
        float m_ShakeDuration = 0.12f;

        [SerializeField, Min(0.0f)]
        float m_ShakeStrength = 0.06f;

        Coroutine m_ShakeCoroutine;
        Vector3 m_ShakeBaseLocalPosition;

        void Awake()
        {
            ConfigurePhysics();
        }

        void Reset()
        {
            ConfigurePhysics();
            SetDefaultKeyLayerMaskIfNeeded();
        }

        void OnValidate()
        {
            m_DisappearEffectLifetime = Mathf.Max(0.0f, m_DisappearEffectLifetime);
            m_ShakeDuration = Mathf.Max(0.0f, m_ShakeDuration);
            m_ShakeStrength = Mathf.Max(0.0f, m_ShakeStrength);
            SetDefaultKeyLayerMaskIfNeeded();
        }

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                ConfigurePhysics();
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
