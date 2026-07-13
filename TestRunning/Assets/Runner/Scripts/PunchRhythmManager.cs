using System.Collections.Generic;
using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Standalone mini-game manager for lane-based punch notes.
    /// </summary>
    public class PunchRhythmManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        Transform m_HitZoneTransform;

        [SerializeField]
        Transform m_NoteParent;

        [SerializeField]
        GameObject[] m_ShapePrefabs;

        [Header("Lanes")]
        [SerializeField, Min(0.1f)]
        float m_LaneSpacing = 2.03f;

        [SerializeField]
        float m_NoteHeight = 1.0f;

        [SerializeField]
        float m_MiddleLaneHeightOffset = 1.05f;

        [SerializeField]
        float m_HitOffsetZ;

        [SerializeField, Min(0.1f)]
        float m_HitWindow = 1.0f;

        [Header("Timing")]
        [SerializeField]
        bool m_AutoSpawn = true;

        [SerializeField, Min(0.1f)]
        float m_SpawnInterval = 1.0f;

        [SerializeField, Min(0.1f)]
        float m_SpawnDistance = 18.0f;

        [SerializeField, Min(0.1f)]
        float m_NoteSpeed = 12.0f;

        [SerializeField, Min(0.1f)]
        float m_MissDistance = 2.0f;

        [SerializeField, Min(1)]
        int m_MaxActiveNotes = 12;

        [Header("Floating Motion")]
        [SerializeField]
        bool m_EnableFloatingMotion = true;

        [SerializeField]
        Vector3 m_RotationSpeed = new Vector3(25.0f, 60.0f, 35.0f);

        [SerializeField, Min(0.0f)]
        float m_RotationSpeedVariance = 20.0f;

        [SerializeField, Min(0.0f)]
        float m_FloatAmplitude = 0.15f;

        [SerializeField, Min(0.0f)]
        float m_FloatFrequency = 2.0f;

        [Header("Disappear Effect")]
        [SerializeField]
        GameObject m_DisappearEffectPrefab;

        [SerializeField]
        bool m_ParentEffectToNoteParent;

        [SerializeField, Min(0.0f)]
        float m_DisappearEffectLifetime = 2.0f;

        [Header("Pattern")]
        [SerializeField, Tooltip("Use 0 = left, 1 = center, 2 = right. Leave empty for random lanes.")]
        int[] m_LanePattern;

        [SerializeField]
        bool m_LoopPattern = true;

        [Header("Debug")]
        [SerializeField]
        bool m_DrawDebug = true;

        readonly List<PunchRhythmNote> m_ActiveNotes = new List<PunchRhythmNote>();
        float m_NextSpawnTime;
        int m_PatternIndex;
        int m_HitCount;
        int m_MissCount;

        public int HitCount => m_HitCount;
        public int MissCount => m_MissCount;

        Transform HitZoneTransform => m_HitZoneTransform != null ? m_HitZoneTransform : transform;

        void OnValidate()
        {
            m_LaneSpacing = Mathf.Max(0.1f, m_LaneSpacing);
            m_HitWindow = Mathf.Max(0.1f, m_HitWindow);
            m_SpawnInterval = Mathf.Max(0.1f, m_SpawnInterval);
            m_SpawnDistance = Mathf.Max(0.1f, m_SpawnDistance);
            m_NoteSpeed = Mathf.Max(0.1f, m_NoteSpeed);
            m_MissDistance = Mathf.Max(0.1f, m_MissDistance);
            m_MaxActiveNotes = Mathf.Max(1, m_MaxActiveNotes);
            m_RotationSpeedVariance = Mathf.Max(0.0f, m_RotationSpeedVariance);
            m_FloatAmplitude = Mathf.Max(0.0f, m_FloatAmplitude);
            m_FloatFrequency = Mathf.Max(0.0f, m_FloatFrequency);
            m_DisappearEffectLifetime = Mathf.Max(0.0f, m_DisappearEffectLifetime);
        }

        void Update()
        {
            m_ActiveNotes.RemoveAll(note => note == null || note.IsResolved);

            if (!m_AutoSpawn)
            {
                return;
            }

            if (Time.time >= m_NextSpawnTime && m_ActiveNotes.Count < m_MaxActiveNotes)
            {
                if (SpawnNextAutoNote())
                {
                    m_NextSpawnTime = Time.time + m_SpawnInterval;
                }
                else
                {
                    m_AutoSpawn = false;
                }
            }
        }

        public void SpawnRandomNote()
        {
            SpawnNote(Random.Range(0, 3));
        }

        bool SpawnNextAutoNote()
        {
            if (m_LanePattern == null || m_LanePattern.Length == 0)
            {
                SpawnRandomNote();
                return true;
            }

            if (m_PatternIndex >= m_LanePattern.Length)
            {
                if (!m_LoopPattern)
                {
                    return false;
                }

                m_PatternIndex = 0;
            }

            SpawnNote(m_LanePattern[m_PatternIndex]);
            m_PatternIndex++;
            return true;
        }

        public void SpawnNote(int laneIndex)
        {
            laneIndex = Mathf.Clamp(laneIndex, 0, 2);

            Vector3 spawnPosition = GetLanePosition(laneIndex, GetHitZ() + m_SpawnDistance);
            GameObject noteObject = CreateNoteObject(laneIndex, spawnPosition);
            if (noteObject == null)
            {
                return;
            }

            if (m_NoteParent != null)
            {
                noteObject.transform.SetParent(m_NoteParent);
            }

            PunchRhythmNote note = noteObject.GetComponent<PunchRhythmNote>();
            if (note == null)
            {
                note = noteObject.AddComponent<PunchRhythmNote>();
            }

            note.Initialize(this, laneIndex);
            m_ActiveNotes.Add(note);
        }

        GameObject CreateNoteObject(int laneIndex, Vector3 position)
        {
            GameObject prefab = GetPrefabForLane(laneIndex);
            if (prefab != null)
            {
                return Instantiate(prefab, position, Quaternion.identity);
            }

            PrimitiveType primitiveType = laneIndex == 0
                ? PrimitiveType.Cube
                : laneIndex == 1
                    ? PrimitiveType.Sphere
                    : PrimitiveType.Capsule;

            GameObject noteObject = GameObject.CreatePrimitive(primitiveType);
            noteObject.name = $"Punch Note Lane {laneIndex}";
            noteObject.transform.position = position;
            noteObject.transform.localScale = Vector3.one * 0.75f;

            Collider noteCollider = noteObject.GetComponent<Collider>();
            if (noteCollider != null)
            {
                noteCollider.isTrigger = true;
            }

            Renderer noteRenderer = noteObject.GetComponent<Renderer>();
            if (noteRenderer != null)
            {
                noteRenderer.material.color = GetLaneColor(laneIndex);
            }

            return noteObject;
        }

        GameObject GetPrefabForLane(int laneIndex)
        {
            if (m_ShapePrefabs == null || m_ShapePrefabs.Length == 0)
            {
                return null;
            }

            if (laneIndex < m_ShapePrefabs.Length && m_ShapePrefabs[laneIndex] != null)
            {
                return m_ShapePrefabs[laneIndex];
            }

            for (int i = 0; i < m_ShapePrefabs.Length; i++)
            {
                if (m_ShapePrefabs[i] != null)
                {
                    return m_ShapePrefabs[i];
                }
            }

            return null;
        }

        Color GetLaneColor(int laneIndex)
        {
            switch (laneIndex)
            {
                case 0:
                    return Color.red;
                case 2:
                    return Color.cyan;
                default:
                    return Color.yellow;
            }
        }

        public void UpdateNote(PunchRhythmNote note, float deltaTime)
        {
            if (note == null || note.IsResolved)
            {
                return;
            }

            Vector3 position = note.transform.position;
            position.x = GetLaneXPosition(note.LaneIndex);
            position.z -= m_NoteSpeed * deltaTime;
            note.transform.position = position;

            float distanceToHitZone = Mathf.Abs(position.z - GetHitZ());
            if (distanceToHitZone <= m_HitWindow)
            {
                ResolveNote(note, true);
            }
            else if (position.z < GetHitZ() - m_MissDistance)
            {
                ResolveNote(note, false);
            }
        }

        void ResolveNote(PunchRhythmNote note, bool wasHit)
        {
            if (note == null || note.IsResolved)
            {
                return;
            }

            note.Resolve();
            m_ActiveNotes.Remove(note);

            if (wasHit)
            {
                m_HitCount++;
            }
            else
            {
                m_MissCount++;
            }

            SpawnDisappearEffect(note);
            Destroy(note.gameObject);
        }

        void SpawnDisappearEffect(PunchRhythmNote note)
        {
            if (m_DisappearEffectPrefab == null || note == null)
            {
                return;
            }

            GameObject effect = Instantiate(m_DisappearEffectPrefab, note.transform.position, note.transform.rotation);
            if (m_ParentEffectToNoteParent && m_NoteParent != null)
            {
                effect.transform.SetParent(m_NoteParent);
            }

            if (m_DisappearEffectLifetime > 0.0f)
            {
                Destroy(effect, m_DisappearEffectLifetime);
            }
        }

        public bool EnableFloatingMotion => m_EnableFloatingMotion;

        public float FloatAmplitude => m_FloatAmplitude;

        public float FloatFrequency => m_FloatFrequency;

        public Vector3 GetRandomRotationSpeed()
        {
            if (!m_EnableFloatingMotion)
            {
                return Vector3.zero;
            }

            return m_RotationSpeed + Random.insideUnitSphere * m_RotationSpeedVariance;
        }

        Vector3 GetLanePosition(int laneIndex, float zPosition)
        {
            Vector3 hitZonePosition = HitZoneTransform.position;
            float yPosition = hitZonePosition.y + m_NoteHeight + GetLaneHeightOffset(laneIndex);
            return new Vector3(GetLaneXPosition(laneIndex), yPosition, zPosition);
        }

        float GetLaneHeightOffset(int laneIndex)
        {
            return laneIndex == 1 ? m_MiddleLaneHeightOffset : 0.0f;
        }

        float GetLaneXPosition(int laneIndex)
        {
            float centerX = HitZoneTransform.position.x;
            switch (laneIndex)
            {
                case 0:
                    return centerX - m_LaneSpacing;
                case 2:
                    return centerX + m_LaneSpacing;
                default:
                    return centerX;
            }
        }

        float GetHitZ()
        {
            return HitZoneTransform.position.z + m_HitOffsetZ;
        }

        void OnDrawGizmos()
        {
            if (!m_DrawDebug)
            {
                return;
            }

            float hitZ = GetHitZ();
            float spawnZ = hitZ + m_SpawnDistance;

            for (int lane = 0; lane < 3; lane++)
            {
                Vector3 hitPosition = GetLanePosition(lane, hitZ);
                Vector3 spawnPosition = GetLanePosition(lane, spawnZ);

                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(hitPosition, new Vector3(0.75f, 0.75f, m_HitWindow * 2.0f));

                Gizmos.color = Color.gray;
                Gizmos.DrawLine(hitPosition, spawnPosition);
            }
        }
    }
}
