using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Plays music and spawns static rhythm keys on BPM beats.
    /// </summary>
    public class RhythmBeatSpawner : MonoBehaviour
    {
        const string k_DefaultKeyLayerName = "keys";
        const string k_DefaultKeyLayerNamePascal = "Keys";

        [System.Serializable]
        class BeatPatternStep
        {
            public int IndicatorIndex;
            public GameObject KeyPrefab;
            public Transform SpawnPoint;
            public bool OverrideSpawnZ;
            public float SpawnZ;
            [Min(1)]
            public int KeyCount = 1;
            public Vector3 CopyOffset;
        }

        [Header("Music")]
        [SerializeField]
        AudioSource m_AudioSource;

        [SerializeField]
        AudioClip m_MusicClip;

        [SerializeField, Min(0.0f)]
        float m_MusicVolume = 1.0f;

        [SerializeField]
        bool m_PlayOnStart = true;

        [SerializeField]
        bool m_PlayMusicWhenStarted = true;

        [SerializeField]
        bool m_RestartMusicOnEnable = true;

        [Header("Start Sync")]
        [SerializeField]
        bool m_UseScheduledMusicStart = true;

        [SerializeField, Min(0.0f), Tooltip("Seconds to wait before scheduled music playback starts. Gives the scene time to settle before timing begins.")]
        float m_MusicStartDelay = 0.5f;

        [SerializeField, Tooltip("If a Rhythm Player Mover is assigned, hold movement until the scheduled music start time.")]
        bool m_SchedulePlayerMovementWithMusic = true;

        [SerializeField, Tooltip("Wait briefly before scheduling the music so scene rendering, shader, and occlusion warm-up work happens before rhythm timing starts.")]
        bool m_WaitForSceneWarmup = true;

        [SerializeField, Min(0)]
        int m_WarmupFrames = 4;

        [SerializeField, Min(0.0f)]
        float m_WarmupSeconds = 0.25f;

        [SerializeField, Tooltip("Optional heavier startup warm-up. Enable only if the first notes still hitch because it can pause before music starts.")]
        bool m_WarmupAllShadersBeforeStart;

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

        [SerializeField, Tooltip("Leave spawned keys in world space so they do not inherit movement from a parent that follows the player.")]
        bool m_KeepSpawnedKeysWorldLocked = true;

        [SerializeField, Min(0.0f)]
        float m_FallbackSpawnDistance = 12.0f;

        [SerializeField]
        Vector3 m_FallbackSpawnDirection = Vector3.forward;

        [SerializeField]
        bool m_OverrideSpawnZ;

        [SerializeField]
        float m_SpawnZ;

        [SerializeField, Min(0.0f), Tooltip("Backup resolve radius for skipped trigger frames. Keep this small, usually 0.05 to 0.2.")]
        float m_ResolveDistance = 0.05f;

        [Header("Pre Spawn")]
        [SerializeField, Tooltip("Spawn the full configured pattern when rhythm starts, then leave the keys static on the track.")]
        bool m_PreSpawnOnStart;

        [SerializeField, Min(0), Tooltip("How many beat rows to pre-spawn. Use 0 to use the active pattern length.")]
        int m_PreSpawnBeatCount;

        [SerializeField, Min(0.0f), Tooltip("Manual distance from each indicator to the first pre-spawned beat row. Used when beat timing layout is disabled.")]
        float m_PreSpawnStartDistance = 12.0f;

        [SerializeField, Min(0.01f), Tooltip("Manual distance between pre-spawned beat rows. Used when beat timing layout is disabled.")]
        float m_PreSpawnBeatSpacing = 2.0f;

        [FormerlySerializedAs("m_UseBpmForPreSpawnSpacing")]
        [SerializeField, Tooltip("Place pre-spawned rows from Beat Timing and forward speed. This makes BPM, Beats Before Hit, First Beat Offset, and Start Beat Index affect the layout.")]
        bool m_UseBeatTimingForPreSpawnLayout = true;

        [SerializeField, Tooltip("Optional player mover used to read the actual forward speed for beat-timed pre-spawn layout.")]
        RhythmPlayerMover m_RhythmPlayerMover;

        [SerializeField, Min(0.0f), Tooltip("Used for beat-timed pre-spawn layout when no Rhythm Player Mover is assigned.")]
        float m_PreSpawnForwardSpeed = 6.0f;

        [SerializeField, Min(1), Tooltip("How many copies to spawn on the same beat when a detailed pattern step does not override it.")]
        int m_DefaultKeyCount = 1;

        [SerializeField, Tooltip("Offset applied between copies spawned on the same beat. Use 0.6 on X to match the indicator spacing.")]
        Vector3 m_DefaultCopyOffset = new Vector3(0.6f, 0.0f, 0.0f);

        [Header("Pattern")]
        [SerializeField, Tooltip("Optional chord pattern. Use one digit per indicator: 1001 spawns first and last lanes, 0110 spawns the two middle lanes.")]
        string[] m_ChordPattern;

        [SerializeField, Tooltip("Optional detailed pattern. If this has entries, each beat can choose its indicator, prefab, spawn point, and spawn Z.")]
        BeatPatternStep[] m_BeatPattern;

        [SerializeField, Tooltip("Use lane indexes. Example for four indicators: 0, 1, 2, 3.")]
        int[] m_LanePattern;

        [SerializeField]
        bool m_LoopPattern = true;

        [Header("Scene Preview")]
        [SerializeField, Tooltip("Draw the configured pattern in the Scene view while this spawner is selected.")]
        bool m_DrawPatternPreview = true;

        [SerializeField, Min(0.01f)]
        float m_PreviewMarkerSize = 0.25f;

        [SerializeField]
        Color m_PreviewMarkerColor = new Color(0.0f, 0.85f, 1.0f, 0.85f);

        int m_NextHitBeatIndex;
        int m_SpawnedBeatCount;
        float m_RuntimeStartTime;
        float m_ScheduledSongTimeOffset;
        double m_ScheduledStartDspTime;
        Coroutine m_StartRhythmCoroutine;
        bool m_UseDspSongTime;
        bool m_HasFinishedPattern;
        bool m_IsRunning;

        float SecondsPerBeat => 60.0f / m_Bpm;

        public bool IsRunning => m_IsRunning;

        void Awake()
        {
            ResolveAudioSource();
            ResolveRhythmPlayerMover();
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
            m_MusicVolume = Mathf.Max(0.0f, m_MusicVolume);
            m_MusicStartDelay = Mathf.Max(0.0f, m_MusicStartDelay);
            m_WarmupFrames = Mathf.Max(0, m_WarmupFrames);
            m_WarmupSeconds = Mathf.Max(0.0f, m_WarmupSeconds);
            m_BeatsBeforeHit = Mathf.Max(0, m_BeatsBeforeHit);
            m_StartBeatIndex = Mathf.Max(0, m_StartBeatIndex);
            m_MaxSpawnedBeats = Mathf.Max(0, m_MaxSpawnedBeats);
            m_FallbackSpawnDistance = Mathf.Max(0.0f, m_FallbackSpawnDistance);
            m_ResolveDistance = Mathf.Max(0.0f, m_ResolveDistance);
            m_PreSpawnBeatCount = Mathf.Max(0, m_PreSpawnBeatCount);
            m_PreSpawnStartDistance = Mathf.Max(0.0f, m_PreSpawnStartDistance);
            m_PreSpawnBeatSpacing = Mathf.Max(0.01f, m_PreSpawnBeatSpacing);
            m_PreSpawnForwardSpeed = Mathf.Max(0.0f, m_PreSpawnForwardSpeed);
            m_DefaultKeyCount = Mathf.Max(1, m_DefaultKeyCount);
            m_PreviewMarkerSize = Mathf.Max(0.01f, m_PreviewMarkerSize);

            if (m_FallbackSpawnDirection.sqrMagnitude <= 0.0f)
            {
                m_FallbackSpawnDirection = Vector3.forward;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (Application.isPlaying)
            {
                return;
            }

            DrawPatternPreview();
        }

        void OnDisable()
        {
            StopPendingStart();
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
            StopPendingStart();
            ResolveRhythmPlayerMover();

            if (Application.isPlaying && ShouldWaitForWarmup())
            {
                StopPlayerMovement();
                m_StartRhythmCoroutine = StartCoroutine(StartRhythmAfterWarmup());
                return;
            }

            StartRhythmNow();
        }

        IEnumerator StartRhythmAfterWarmup()
        {
            if (m_WarmupAllShadersBeforeStart)
            {
                Shader.WarmupAllShaders();
            }

            int waitedFrames = 0;
            float startedAt = Time.realtimeSinceStartup;

            while (waitedFrames < m_WarmupFrames || Time.realtimeSinceStartup - startedAt < m_WarmupSeconds)
            {
                waitedFrames++;
                yield return null;
            }

            m_StartRhythmCoroutine = null;
            StartRhythmNow();
        }

        bool ShouldWaitForWarmup()
        {
            return m_WaitForSceneWarmup &&
                (m_WarmupFrames > 0 || m_WarmupSeconds > 0.0f || m_WarmupAllShadersBeforeStart);
        }

        void StopPendingStart()
        {
            if (m_StartRhythmCoroutine == null)
            {
                return;
            }

            StopCoroutine(m_StartRhythmCoroutine);
            m_StartRhythmCoroutine = null;
        }

        void StartRhythmNow()
        {
            ResolveRhythmPlayerMover();
            ResetSpawnSchedule();
            m_IsRunning = true;

            if (m_PreSpawnOnStart)
            {
                PreSpawnKeys();
                m_HasFinishedPattern = true;
            }

            if (m_PlayMusicWhenStarted)
            {
                PlayMusic();
            }
            else
            {
                m_RuntimeStartTime = Time.time;
                m_UseDspSongTime = false;
                StartPlayerMovementNow();
            }
        }

        public void StopRhythm()
        {
            StopPendingStart();
            m_IsRunning = false;
            StopMusic();
            StopPlayerMovement();
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
                m_RuntimeStartTime = Time.time;
                m_UseDspSongTime = false;
                StartPlayerMovementNow();
                return;
            }

            m_AudioSource.volume = m_MusicVolume;

            if (m_RestartMusicOnEnable)
            {
                m_AudioSource.time = 0.0f;
            }

            m_ScheduledSongTimeOffset = m_AudioSource.time;

            if (m_UseScheduledMusicStart)
            {
                m_ScheduledStartDspTime = AudioSettings.dspTime + m_MusicStartDelay;
                m_RuntimeStartTime = Time.time + m_MusicStartDelay - m_ScheduledSongTimeOffset;
                m_UseDspSongTime = true;

                m_AudioSource.PlayScheduled(m_ScheduledStartDspTime);
                SchedulePlayerMovement(m_ScheduledStartDspTime);
                return;
            }

            m_RuntimeStartTime = Time.time - m_ScheduledSongTimeOffset;
            m_UseDspSongTime = false;
            m_AudioSource.Play();
            StartPlayerMovementNow();
        }

        public void StopMusic()
        {
            if (m_AudioSource != null)
            {
                m_AudioSource.Stop();
            }

            m_UseDspSongTime = false;
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

        void ResolveRhythmPlayerMover()
        {
            if (m_RhythmPlayerMover != null)
            {
                return;
            }

            m_RhythmPlayerMover = FindObjectOfType<RhythmPlayerMover>();
        }

        void ResetSpawnSchedule()
        {
            m_NextHitBeatIndex = m_StartBeatIndex + m_BeatsBeforeHit;
            m_SpawnedBeatCount = 0;
            m_RuntimeStartTime = Time.time;
            m_ScheduledSongTimeOffset = 0.0f;
            m_ScheduledStartDspTime = 0.0;
            m_UseDspSongTime = false;
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
            if (m_UseDspSongTime)
            {
                float dspSongTime = m_ScheduledSongTimeOffset + (float)(AudioSettings.dspTime - m_ScheduledStartDspTime);
                return Mathf.Max(0.0f, dspSongTime);
            }

            if (m_AudioSource != null && m_AudioSource.clip != null && m_AudioSource.isPlaying)
            {
                return m_AudioSource.time;
            }

            return Time.time - m_RuntimeStartTime;
        }

        void SchedulePlayerMovement(double startDspTime)
        {
            if (!m_SchedulePlayerMovementWithMusic || m_RhythmPlayerMover == null)
            {
                return;
            }

            m_RhythmPlayerMover.ScheduleMovementStart(startDspTime);
        }

        void StartPlayerMovementNow()
        {
            if (!m_SchedulePlayerMovementWithMusic || m_RhythmPlayerMover == null)
            {
                return;
            }

            m_RhythmPlayerMover.StartMovement();
        }

        void StopPlayerMovement()
        {
            if (!m_SchedulePlayerMovementWithMusic || m_RhythmPlayerMover == null)
            {
                return;
            }

            m_RhythmPlayerMover.StopMovement();
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
            if (HasChordPattern())
            {
                return SpawnChordForBeat(beatIndex, songTime, 0.0f);
            }

            BeatPatternStep step = GetBeatPatternStep(beatIndex);
            if (HasDetailedPattern() && step == null)
            {
                return false;
            }

            int laneIndex = step != null ? Mathf.Clamp(step.IndicatorIndex, 0, m_Indicators.Length - 1) : GetLaneIndex(beatIndex);
            return SpawnKeysInLane(laneIndex, step, songTime, beatIndex, 0.0f);
        }

        bool SpawnChordForBeat(int beatIndex, float songTime, float forwardOffset)
        {
            string chord = GetChordPatternStep(beatIndex);
            if (chord == null)
            {
                return false;
            }

            for (int i = 0; i < chord.Length && i < m_Indicators.Length; i++)
            {
                if (IsChordLaneEnabled(chord[i]) && !SpawnKeysInLane(i, null, songTime, beatIndex, forwardOffset))
                {
                    return false;
                }
            }

            return true;
        }

        bool SpawnKeysInLane(int laneIndex, BeatPatternStep step, float songTime, int beatIndex, float forwardOffset)
        {
            RhythmKeyIndicator indicator = GetIndicator(laneIndex);
            GameObject prefab = GetPrefab(laneIndex, step);
            if (indicator == null || prefab == null)
            {
                return false;
            }

            Vector3 spawnPosition = GetSpawnPosition(laneIndex, indicator.transform, step, forwardOffset);
            int keyCount = GetKeyCount(step);
            Vector3 copyOffset = GetCopyOffset(step);

            for (int i = 0; i < keyCount; i++)
            {
                SpawnKeyObject(prefab, indicator, spawnPosition + copyOffset * i, songTime, beatIndex);
            }

            return true;
        }

        string GetChordPatternStep(int beatIndex)
        {
            if (!HasChordPattern())
            {
                return null;
            }

            int patternIndex = GetPatternIndex(beatIndex, m_ChordPattern.Length);
            if (patternIndex < 0)
            {
                return null;
            }

            return m_ChordPattern[patternIndex];
        }

        bool IsChordLaneEnabled(char laneValue)
        {
            return laneValue == '1' || laneValue == 'x' || laneValue == 'X';
        }

        void PreSpawnKeys()
        {
            int beatCount = GetPreSpawnBeatCount();
            if (beatCount <= 0)
            {
                return;
            }

            for (int i = 0; i < beatCount; i++)
            {
                int beatIndex = m_StartBeatIndex + m_BeatsBeforeHit + i;
                float forwardOffset = GetPreSpawnForwardOffset(beatIndex);

                if (!SpawnPreSpawnBeat(beatIndex, forwardOffset))
                {
                    m_HasFinishedPattern = true;
                    return;
                }

                m_SpawnedBeatCount++;
            }
        }

        bool SpawnPreSpawnBeat(int beatIndex, float forwardOffset)
        {
            if (HasChordPattern())
            {
                return SpawnChordForBeat(beatIndex, 0.0f, forwardOffset);
            }

            BeatPatternStep step = GetBeatPatternStep(beatIndex);
            if (HasDetailedPattern() && step == null)
            {
                return false;
            }

            int laneIndex = step != null ? Mathf.Clamp(step.IndicatorIndex, 0, m_Indicators.Length - 1) : GetLaneIndex(beatIndex);
            return SpawnKeysInLane(laneIndex, step, 0.0f, beatIndex, forwardOffset);
        }

        void DrawPatternPreview()
        {
            if (!m_DrawPatternPreview || m_Indicators == null || m_Indicators.Length == 0)
            {
                return;
            }

            int beatCount = GetPreSpawnBeatCount();
            if (beatCount <= 0)
            {
                return;
            }

            Color previousColor = Gizmos.color;
            Gizmos.color = m_PreviewMarkerColor;

            for (int i = 0; i < beatCount; i++)
            {
                int beatIndex = m_StartBeatIndex + m_BeatsBeforeHit + i;
                float forwardOffset = GetPreSpawnForwardOffset(beatIndex);
                DrawPreviewBeat(beatIndex, forwardOffset);
            }

            Gizmos.color = previousColor;
        }

        void DrawPreviewBeat(int beatIndex, float forwardOffset)
        {
            if (HasChordPattern())
            {
                string chord = GetChordPatternStep(beatIndex);
                if (string.IsNullOrEmpty(chord))
                {
                    return;
                }

                for (int i = 0; i < chord.Length && i < m_Indicators.Length; i++)
                {
                    if (IsChordLaneEnabled(chord[i]))
                    {
                        DrawPreviewKeysInLane(i, null, forwardOffset);
                    }
                }

                return;
            }

            BeatPatternStep step = GetBeatPatternStep(beatIndex);
            if (HasDetailedPattern())
            {
                if (step == null)
                {
                    return;
                }

                int laneIndex = Mathf.Clamp(step.IndicatorIndex, 0, m_Indicators.Length - 1);
                DrawPreviewKeysInLane(laneIndex, step, forwardOffset);
                return;
            }

            if (m_LanePattern == null || m_LanePattern.Length == 0)
            {
                return;
            }

            DrawPreviewKeysInLane(GetLaneIndex(beatIndex), null, forwardOffset);
        }

        void DrawPreviewKeysInLane(int laneIndex, BeatPatternStep step, float forwardOffset)
        {
            RhythmKeyIndicator indicator = GetIndicator(laneIndex);
            if (indicator == null)
            {
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition(laneIndex, indicator.transform, step, forwardOffset);
            int keyCount = GetKeyCount(step);
            Vector3 copyOffset = GetCopyOffset(step);

            for (int i = 0; i < keyCount; i++)
            {
                DrawPreviewMarker(spawnPosition + copyOffset * i);
            }
        }

        void DrawPreviewMarker(Vector3 position)
        {
            Vector3 size = Vector3.one * m_PreviewMarkerSize;
            Gizmos.DrawWireCube(position, size);
        }

        int GetPreSpawnBeatCount()
        {
            if (m_PreSpawnBeatCount > 0)
            {
                return m_PreSpawnBeatCount;
            }

            if (m_MaxSpawnedBeats > 0)
            {
                return m_MaxSpawnedBeats;
            }

            return GetActivePatternLength();
        }

        int GetActivePatternLength()
        {
            if (HasChordPattern())
            {
                return m_ChordPattern.Length;
            }

            if (HasDetailedPattern())
            {
                return m_BeatPattern.Length;
            }

            return m_LanePattern != null ? m_LanePattern.Length : 0;
        }

        float GetPreSpawnForwardOffset(int beatIndex)
        {
            if (!m_UseBeatTimingForPreSpawnLayout)
            {
                int firstBeatIndex = m_StartBeatIndex + m_BeatsBeforeHit;
                int localBeatIndex = Mathf.Max(0, beatIndex - firstBeatIndex);
                return m_PreSpawnStartDistance + m_PreSpawnBeatSpacing * localBeatIndex;
            }

            float hitTime = GetHitTimeForBeat(beatIndex);
            return Mathf.Max(0.0f, hitTime * GetPreSpawnForwardSpeed());
        }

        float GetPreSpawnForwardSpeed()
        {
            return m_RhythmPlayerMover != null
                ? m_RhythmPlayerMover.ForwardSpeed
                : m_PreSpawnForwardSpeed;
        }

        void SpawnKeyObject(GameObject prefab, RhythmKeyIndicator indicator, Vector3 spawnPosition, float songTime, int beatIndex)
        {
            GameObject keyObject = Instantiate(prefab, spawnPosition, prefab.transform.rotation);
            SetKeyLayerIfAvailable(keyObject);

            if (m_KeyParent != null && !m_KeepSpawnedKeysWorldLocked)
            {
                keyObject.transform.SetParent(m_KeyParent, true);
            }

            RhythmBeatSyncedKey beatKey = keyObject.GetComponent<RhythmBeatSyncedKey>();
            if (beatKey == null)
            {
                beatKey = keyObject.AddComponent<RhythmBeatSyncedKey>();
            }

            float hitTime = GetHitTimeForBeat(beatIndex);
            float spawnTime = Mathf.Min(songTime, hitTime);
            beatKey.Initialize(indicator, m_AudioSource, spawnTime, hitTime, m_ResolveDistance);
        }

        void SetKeyLayerIfAvailable(GameObject keyObject)
        {
            if (keyObject == null)
            {
                return;
            }

            int keyLayer = LayerMask.NameToLayer(k_DefaultKeyLayerName);
            if (keyLayer == -1)
            {
                keyLayer = LayerMask.NameToLayer(k_DefaultKeyLayerNamePascal);
            }

            if (keyLayer == -1)
            {
                return;
            }

            SetLayerRecursively(keyObject.transform, keyLayer);
        }

        void SetLayerRecursively(Transform target, int layer)
        {
            if (target == null)
            {
                return;
            }

            target.gameObject.layer = layer;
            for (int i = 0; i < target.childCount; i++)
            {
                SetLayerRecursively(target.GetChild(i), layer);
            }
        }

        int GetKeyCount(BeatPatternStep step)
        {
            if (step != null && step.KeyCount > 0)
            {
                return step.KeyCount;
            }

            return Mathf.Max(1, m_DefaultKeyCount);
        }

        Vector3 GetCopyOffset(BeatPatternStep step)
        {
            if (step != null && step.CopyOffset != Vector3.zero)
            {
                return step.CopyOffset;
            }

            return m_DefaultCopyOffset;
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

        bool HasChordPattern()
        {
            return m_ChordPattern != null && m_ChordPattern.Length > 0;
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

        Vector3 GetSpawnPosition(int laneIndex, Transform indicatorTransform, BeatPatternStep step, float forwardOffset)
        {
            Vector3 spawnPosition;
            if (step != null && step.SpawnPoint != null)
            {
                spawnPosition = step.SpawnPoint.position;
            }
            else if (forwardOffset > 0.0f)
            {
                spawnPosition = indicatorTransform.position;
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

            if (forwardOffset > 0.0f)
            {
                spawnPosition += m_FallbackSpawnDirection.normalized * forwardOffset;
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
