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

        void Reset()
        {
            Collider indicatorCollider = GetComponent<Collider>();
            if (indicatorCollider != null)
            {
                indicatorCollider.isTrigger = true;
            }

            SetDefaultKeyLayerMaskIfNeeded();
        }

        void OnValidate()
        {
            m_DisappearEffectLifetime = Mathf.Max(0.0f, m_DisappearEffectLifetime);
            SetDefaultKeyLayerMaskIfNeeded();
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
            if (m_DisappearEffectLifetime > 0.0f)
            {
                Destroy(effect, m_DisappearEffectLifetime);
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
