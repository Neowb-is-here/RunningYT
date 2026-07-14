using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Plays music and spawns rhythm keys so they reach indicators on BPM beats.
    /// </summary>
    public class RhythmBeatSpawner : MonoBehaviour
    {
        [System.Serializable]
        class BeatPatternStep
        {
            public int IndicatorIndex;
            public GameObject KeyPrefab;
            public Transform SpawnPoint;
            public bool OverrideSpawnZ;
            public float SpawnZ;
        }

        [Header("Music")]
        [SerializeField]
        AudioSource m_AudioSource;

        [SerializeField]
        AudioClip m_MusicClip;

        [SerializeField]
        bool m_PlayOnStart = true;

        [SerializeField]
        bool m_PlayMusicWhenStarted = true;

        [SerializeField]
        bool m_RestartMusicOnEnable = true;

        [Header("Beat Timing")]
        [SerializeField, Min(1.0f)]
        float m_Bpm = 120.0f;

        [SerializeField, Min(0)]
        int m_BeatsBeforeHit = 4;

        [SerializeField]
        float m_FirstBeatOffset;

        [SerializeField, Min(0)]
        int m_StartBeatIndex;

        [SerializeField, Min(0)]
        int m_MaxSpawnedBeats;

        [Header("Keys")]
        [SerializeField]
        GameObject[] m_KeyPrefabs;

        [SerializeField]
        Transform[] m_SpawnPoints;

        [SerializeField]
        RhythmKeyIndicator[] m_Indicators;

        [SerializeField]
        Transform m_KeyParent;

        [SerializeField, Min(0.0f)]
        float m_FallbackSpawnDistance = 12.0f;

        [SerializeField]
        Vector3 m_FallbackSpawnDirection = Vector3.forward;

        [SerializeField]
        bool m_OverrideSpawnZ;

        [SerializeField]
        float m_SpawnZ;

        [SerializeField, Min(0.0f)]
        float m_ResolveDistance = 0.05f;

        [Header("Pattern")]
        [SerializeField, Tooltip("Optional detailed pattern. If this has entries, each beat can choose its indicator, prefab, spawn point, and spawn Z.")]
        BeatPatternStep[] m_BeatPattern;

        [SerializeField, Tooltip("Use lane indexes. Example for four indicators: 0, 1, 2, 3.")]
        int[] m_LanePattern;

        [SerializeField]
        bool m_LoopPattern = true;

        int m_NextHitBeatIndex;
        int m_SpawnedBeatCount;
        float m_RuntimeStartTime;
        bool m_HasFinishedPattern;
        bool m_IsRunning;

        float SecondsPerBeat => 60.0f / m_Bpm;

        public bool IsRunning => m_IsRunning;

        void Awake()
        {
            ResolveAudioSource();
        }

        void OnEnable()
        {
            ResetSpawnSchedule();

            if (m_PlayOnStart)
            {
                StartRhythm();
            }
        }

        void OnValidate()
        {
            m_Bpm = Mathf.Max(1.0f, m_Bpm);
            m_BeatsBeforeHit = Mathf.Max(0, m_BeatsBeforeHit);
            m_StartBeatIndex = Mathf.Max(0, m_StartBeatIndex);
            m_MaxSpawnedBeats = Mathf.Max(0, m_MaxSpawnedBeats);
            m_FallbackSpawnDistance = Mathf.Max(0.0f, m_FallbackSpawnDistance);
            m_ResolveDistance = Mathf.Max(0.0f, m_ResolveDistance);

            if (m_FallbackSpawnDirection.sqrMagnitude <= 0.0f)
            {
                m_FallbackSpawnDirection = Vector3.forward;
            }
        }

        void Update()
        {
            if (!m_IsRunning || !CanSpawn())
            {
                return;
            }

            float songTime = GetSongTime();
            float travelTime = SecondsPerBeat * m_BeatsBeforeHit;

            while (CanSpawn() && GetSpawnTimeForBeat(m_NextHitBeatIndex, travelTime) <= songTime)
            {
                if (!SpawnKeyForBeat(m_NextHitBeatIndex, songTime))
                {
                    m_HasFinishedPattern = true;
                    return;
                }

                m_NextHitBeatIndex++;
                m_SpawnedBeatCount++;
            }
        }

        public void StartRhythm()
        {
            ResetSpawnSchedule();
            m_IsRunning = true;

            if (m_PlayMusicWhenStarted)
            {
                PlayMusic();
            }
            else
            {
                m_RuntimeStartTime = Time.time;
            }
        }

        public void StopRhythm()
        {
            m_IsRunning = false;
            StopMusic();
        }

        public void PlayMusic()
        {
            ResolveAudioSource();
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (m_MusicClip != null)
            {
                m_AudioSource.clip = m_MusicClip;
            }

            if (m_AudioSource.clip == null)
            {
                return;
            }

            if (m_RestartMusicOnEnable)
            {
                m_AudioSource.time = 0.0f;
            }

            m_RuntimeStartTime = Time.time;
            m_AudioSource.Play();
        }

        public void StopMusic()
        {
            if (m_AudioSource != null)
            {
                m_AudioSource.Stop();
            }
        }

        void ResolveAudioSource()
        {
            if (m_AudioSource == null)
            {
                m_AudioSource = GetComponent<AudioSource>();
            }

            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        void ResetSpawnSchedule()
        {
            m_NextHitBeatIndex = m_StartBeatIndex + m_BeatsBeforeHit;
            m_SpawnedBeatCount = 0;
            m_RuntimeStartTime = Time.time;
            m_HasFinishedPattern = false;
            m_IsRunning = false;
        }

        bool CanSpawn()
        {
            if (m_HasFinishedPattern)
            {
                return false;
            }

            if (m_Indicators == null || m_Indicators.Length == 0)
            {
                return false;
            }

            if (m_MaxSpawnedBeats > 0 && m_SpawnedBeatCount >= m_MaxSpawnedBeats)
            {
                return false;
            }

            return true;
        }

        float GetSongTime()
        {
            if (m_AudioSource != null && m_AudioSource.clip != null)
            {
                return m_AudioSource.time;
            }

            return Time.time - m_RuntimeStartTime;
        }

        float GetHitTimeForBeat(int beatIndex)
        {
            return m_FirstBeatOffset + beatIndex * SecondsPerBeat;
        }

        float GetSpawnTimeForBeat(int beatIndex, float travelTime)
        {
            return GetHitTimeForBeat(beatIndex) - travelTime;
        }

        bool SpawnKeyForBeat(int beatIndex, float songTime)
        {
            BeatPatternStep step = GetBeatPatternStep(beatIndex);
            if (HasDetailedPattern() && step == null)
            {
                return false;
            }

            int laneIndex = step != null ? Mathf.Clamp(step.IndicatorIndex, 0, m_Indicators.Length - 1) : GetLaneIndex(beatIndex);
            RhythmKeyIndicator indicator = GetIndicator(laneIndex);
            GameObject prefab = GetPrefab(laneIndex, step);
            if (indicator == null || prefab == null)
            {
                return false;
            }

            Vector3 spawnPosition = GetSpawnPosition(laneIndex, indicator.transform, step);
            GameObject keyObject = Instantiate(prefab, spawnPosition, prefab.transform.rotation);
            if (m_KeyParent != null)
            {
                keyObject.transform.SetParent(m_KeyParent);
            }

            RhythmBeatSyncedKey beatKey = keyObject.GetComponent<RhythmBeatSyncedKey>();
            if (beatKey == null)
            {
                beatKey = keyObject.AddComponent<RhythmBeatSyncedKey>();
            }

            float hitTime = GetHitTimeForBeat(beatIndex);
            float spawnTime = Mathf.Min(songTime, hitTime);
            beatKey.Initialize(indicator, m_AudioSource, spawnTime, hitTime, m_ResolveDistance);
            return true;
        }

        BeatPatternStep GetBeatPatternStep(int beatIndex)
        {
            if (!HasDetailedPattern())
            {
                return null;
            }

            int patternIndex = GetPatternIndex(beatIndex, m_BeatPattern.Length);
            if (patternIndex < 0)
            {
                return null;
            }

            return m_BeatPattern[patternIndex];
        }

        bool HasDetailedPattern()
        {
            return m_BeatPattern != null && m_BeatPattern.Length > 0;
        }

        int GetLaneIndex(int beatIndex)
        {
            if (m_LanePattern == null || m_LanePattern.Length == 0)
            {
                return Random.Range(0, m_Indicators.Length);
            }

            int patternIndex = GetPatternIndex(beatIndex, m_LanePattern.Length);
            if (patternIndex < 0)
            {
                return -1;
            }

            return Mathf.Clamp(m_LanePattern[patternIndex], 0, m_Indicators.Length - 1);
        }

        RhythmKeyIndicator GetIndicator(int laneIndex)
        {
            if (laneIndex < 0 || m_Indicators == null || laneIndex >= m_Indicators.Length)
            {
                return null;
            }

            return m_Indicators[laneIndex];
        }

        int GetPatternIndex(int beatIndex, int patternLength)
        {
            if (patternLength <= 0)
            {
                return -1;
            }

            int patternIndex = beatIndex - m_StartBeatIndex - m_BeatsBeforeHit;
            if (m_LoopPattern)
            {
                patternIndex %= patternLength;
            }

            if (patternIndex < 0 || patternIndex >= patternLength)
            {
                return -1;
            }

            return patternIndex;
        }

        GameObject GetPrefab(int laneIndex, BeatPatternStep step)
        {
            if (step != null && step.KeyPrefab != null)
            {
                return step.KeyPrefab;
            }

            if (m_KeyPrefabs == null || m_KeyPrefabs.Length == 0)
            {
                return null;
            }

            if (laneIndex >= 0 && laneIndex < m_KeyPrefabs.Length && m_KeyPrefabs[laneIndex] != null)
            {
                return m_KeyPrefabs[laneIndex];
            }

            for (int i = 0; i < m_KeyPrefabs.Length; i++)
            {
                if (m_KeyPrefabs[i] != null)
                {
                    return m_KeyPrefabs[i];
                }
            }

            return null;
        }

        Vector3 GetSpawnPosition(int laneIndex, Transform indicatorTransform, BeatPatternStep step)
        {
            Vector3 spawnPosition;
            if (step != null && step.SpawnPoint != null)
            {
                spawnPosition = step.SpawnPoint.position;
            }
            else
            {
                spawnPosition = GetDefaultSpawnPosition(laneIndex, indicatorTransform);
            }

            if (step != null && step.OverrideSpawnZ)
            {
                spawnPosition.z = step.SpawnZ;
            }
            else if (m_OverrideSpawnZ)
            {
                spawnPosition.z = m_SpawnZ;
            }

            return spawnPosition;
        }

        Vector3 GetDefaultSpawnPosition(int laneIndex, Transform indicatorTransform)
        {
            if (m_SpawnPoints != null &&
                laneIndex >= 0 &&
                laneIndex < m_SpawnPoints.Length &&
                m_SpawnPoints[laneIndex] != null)
            {
                return m_SpawnPoints[laneIndex].position;
            }

            Vector3 direction = m_FallbackSpawnDirection.normalized;
            return indicatorTransform.position + direction * m_FallbackSpawnDistance;
        }
    }
}
