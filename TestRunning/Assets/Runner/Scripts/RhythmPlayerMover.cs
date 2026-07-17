using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Moves the rhythm player rig forward through static rhythm keys.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RhythmPlayerMover : MonoBehaviour
    {
        public enum LaneTarget
        {
            Center,
            Left,
            Right
        }

        public enum HeightTarget
        {
            Standing,
            Shift
        }

        [System.Serializable]
        class Follower
        {
            public Transform Target;
            public bool FollowLateralMovement;
            public bool FollowVerticalMovement;
        }

        struct DebugLineMarker
        {
            public Vector3 Center;
            public float CreatedAt;

            public DebugLineMarker(Vector3 center, float createdAt)
            {
                Center = center;
                CreatedAt = createdAt;
            }
        }

        [Header("Movement")]
        [SerializeField, Min(0.0f)]
        float m_ForwardSpeed = 6.0f;

        [SerializeField, Min(0.01f)]
        float m_LaneMoveSpeed = 4.0f;

        [SerializeField]
        float m_LeftSnapX = -0.623f;

        [SerializeField]
        float m_CenterSnapX;

        [SerializeField]
        float m_RightSnapX = 0.623f;

        [Header("Height")]
        [SerializeField]
        float m_StandingY;

        [SerializeField]
        float m_LeftShiftY = 0.789f;

        [SerializeField, Min(0.01f)]
        float m_HeightMoveSpeed = 4.0f;

        [Header("Input")]
        [SerializeField]
        bool m_AllowKeyboardInput = true;

        [Header("Timing")]
        [SerializeField]
        bool m_MoveOnStart = true;

        [Header("Followers")]
        [SerializeField]
        bool m_MoveFollowersWithPlayer = true;

        [SerializeField]
        Follower[] m_Followers;

        [Header("Debug Marker")]
        [SerializeField, Tooltip("Press Space during Play Mode to place a Scene view gizmo line at the player's current position.")]
        bool m_EnableSpaceDebugLine = true;

        [SerializeField, Tooltip("When enabled, pressing Space toggles pause while using the debug marker tool.")]
        bool m_CanPauseWithSpace;

        [SerializeField, Tooltip("Pause AudioListener audio together with Time.timeScale so the song also stops while paused.")]
        bool m_PauseAudioListenerWithGame = true;

        [SerializeField, Tooltip("Offset from the player position used when placing each debug line.")]
        Vector3 m_DebugLineOffset;

        [SerializeField, Tooltip("Extra world Z offset from the player position used when placing each debug line.")]
        float m_DebugLineZOffset;

        [SerializeField, Min(0.01f), Tooltip("Half the width of each debug line. The full line width is this value multiplied by 2.")]
        float m_DebugLineHalfWidth = 3.0f;

        [SerializeField, Min(0.01f)]
        float m_DebugLineCenterRadius = 0.08f;

        [SerializeField, Min(1)]
        int m_MaxDebugLineCount = 32;

        [SerializeField, Min(0.0f), Tooltip("Seconds before a debug line disappears. Use 0 to keep markers until Backspace or Play Mode stops.")]
        float m_DebugLineLifetime;

        [SerializeField]
        Color m_DebugLineColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);

        [SerializeField]
        bool m_DrawDebugLinesOnlyWhenSelected;

        Transform m_Transform;
        double m_ScheduledStartDspTime;
        double m_LastMovementDspTime;
        bool m_CanMove;
        bool m_HasScheduledStart;
        bool m_HasRuntimeMovementState;
        bool m_UseDspDeltaTime;
        bool m_HasExternalLaneTarget;
        bool m_HasExternalHeightTarget;
        LaneTarget m_ExternalLaneTarget = LaneTarget.Center;
        HeightTarget m_ExternalHeightTarget = HeightTarget.Standing;
        readonly List<DebugLineMarker> m_DebugLineMarkers = new List<DebugLineMarker>();
        bool m_IsPausedByDebugTool;
        float m_TimeScaleBeforeDebugPause = 1.0f;
        bool m_AudioListenerPauseBeforeDebugPause;

        public float ForwardSpeed => m_ForwardSpeed;

        void Awake()
        {
            m_Transform = transform;
            ConfigurePhysicsBody();

            if (!m_HasRuntimeMovementState)
            {
                m_CanMove = m_MoveOnStart;
            }
        }

        void Reset()
        {
            ConfigurePhysicsBody();
        }

        void OnValidate()
        {
            m_ForwardSpeed = Mathf.Max(0.0f, m_ForwardSpeed);
            m_LaneMoveSpeed = Mathf.Max(0.01f, m_LaneMoveSpeed);
            m_HeightMoveSpeed = Mathf.Max(0.01f, m_HeightMoveSpeed);
            m_DebugLineHalfWidth = Mathf.Max(0.01f, m_DebugLineHalfWidth);
            m_DebugLineCenterRadius = Mathf.Max(0.01f, m_DebugLineCenterRadius);
            m_MaxDebugLineCount = Mathf.Max(1, m_MaxDebugLineCount);
            m_DebugLineLifetime = Mathf.Max(0.0f, m_DebugLineLifetime);

            if (m_LeftSnapX > m_RightSnapX)
            {
                float leftSnapX = m_RightSnapX;
                m_RightSnapX = m_LeftSnapX;
                m_LeftSnapX = leftSnapX;
            }

            Rigidbody playerRigidbody = GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                ConfigurePhysicsBody(playerRigidbody);
            }
        }

        public void ScheduleMovementStart(double startDspTime)
        {
            m_ScheduledStartDspTime = startDspTime;
            m_LastMovementDspTime = startDspTime;
            m_HasScheduledStart = true;
            m_CanMove = false;
            m_UseDspDeltaTime = true;
            m_HasRuntimeMovementState = true;
        }

        public void StartMovement()
        {
            m_HasScheduledStart = false;
            m_CanMove = true;
            m_UseDspDeltaTime = false;
            m_HasRuntimeMovementState = true;
        }

        public void StopMovement()
        {
            m_HasScheduledStart = false;
            m_CanMove = false;
            m_UseDspDeltaTime = false;
            m_HasRuntimeMovementState = true;
        }

        public void CancelScheduledStart()
        {
            m_HasScheduledStart = false;
            m_CanMove = m_MoveOnStart;
            m_UseDspDeltaTime = false;
            m_HasRuntimeMovementState = true;
        }

        public void SetLaneTarget(LaneTarget laneTarget)
        {
            m_ExternalLaneTarget = laneTarget;
            m_HasExternalLaneTarget = true;
        }

        public void ClearLaneTarget()
        {
            m_HasExternalLaneTarget = false;
        }

        public void ClearLaneTarget(LaneTarget laneTarget)
        {
            if (m_HasExternalLaneTarget && m_ExternalLaneTarget == laneTarget)
            {
                ClearLaneTarget();
            }
        }

        public void SetHeightTarget(HeightTarget heightTarget)
        {
            m_ExternalHeightTarget = heightTarget;
            m_HasExternalHeightTarget = true;
        }

        public void ClearHeightTarget()
        {
            m_HasExternalHeightTarget = false;
        }

        public void ClearHeightTarget(HeightTarget heightTarget)
        {
            if (m_HasExternalHeightTarget && m_ExternalHeightTarget == heightTarget)
            {
                ClearHeightTarget();
            }
        }

        public void ClearExternalTargets()
        {
            ClearLaneTarget();
            ClearHeightTarget();
        }

        void Update()
        {
            if (m_Transform == null)
            {
                m_Transform = transform;
            }

            UpdateSpaceDebugLineInput();
            RemoveExpiredDebugLines();

            if (m_IsPausedByDebugTool)
            {
                return;
            }

            if (!TryGetMovementDeltaTime(out float deltaTime))
            {
                return;
            }

            Vector3 previousPosition = m_Transform.position;
            Vector3 nextPosition = previousPosition;

            nextPosition.z += m_ForwardSpeed * deltaTime;
            nextPosition.x = Mathf.MoveTowards(
                nextPosition.x,
                GetTargetX(),
                m_LaneMoveSpeed * deltaTime);
            nextPosition.y = Mathf.MoveTowards(
                nextPosition.y,
                GetTargetY(),
                m_HeightMoveSpeed * deltaTime);

            Vector3 delta = nextPosition - previousPosition;
            m_Transform.position = nextPosition;
            MoveFollowers(delta);
        }

        void OnDisable()
        {
            if (m_IsPausedByDebugTool)
            {
                SetDebugPause(false);
            }
        }

        void OnDrawGizmos()
        {
            if (m_DrawDebugLinesOnlyWhenSelected)
            {
                return;
            }

            DrawDebugLineGizmos();
        }

        void OnDrawGizmosSelected()
        {
            if (!m_DrawDebugLinesOnlyWhenSelected)
            {
                return;
            }

            DrawDebugLineGizmos();
        }

        void UpdateSpaceDebugLineInput()
        {
            if (!m_EnableSpaceDebugLine && !m_CanPauseWithSpace)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (m_EnableSpaceDebugLine)
                {
                    AddDebugLineMarker();
                }

                if (m_CanPauseWithSpace)
                {
                    SetDebugPause(!m_IsPausedByDebugTool);
                }
            }

            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                m_DebugLineMarkers.Clear();
            }
        }

        void SetDebugPause(bool pause)
        {
            if (pause)
            {
                if (m_IsPausedByDebugTool)
                {
                    return;
                }

                m_TimeScaleBeforeDebugPause = Time.timeScale;
                m_AudioListenerPauseBeforeDebugPause = AudioListener.pause;
                Time.timeScale = 0.0f;

                if (m_PauseAudioListenerWithGame)
                {
                    AudioListener.pause = true;
                }

                m_IsPausedByDebugTool = true;
                return;
            }

            if (!m_IsPausedByDebugTool)
            {
                return;
            }

            Time.timeScale = m_TimeScaleBeforeDebugPause;

            if (m_PauseAudioListenerWithGame)
            {
                AudioListener.pause = m_AudioListenerPauseBeforeDebugPause;
            }

            double currentDspTime = AudioSettings.dspTime;
            if (m_HasScheduledStart && currentDspTime >= m_ScheduledStartDspTime)
            {
                m_ScheduledStartDspTime = currentDspTime;
                m_LastMovementDspTime = currentDspTime;
            }
            else if (m_UseDspDeltaTime)
            {
                m_LastMovementDspTime = currentDspTime;
            }

            m_IsPausedByDebugTool = false;
        }

        void AddDebugLineMarker()
        {
            Transform markerTransform = m_Transform != null ? m_Transform : transform;
            m_DebugLineMarkers.Add(new DebugLineMarker(
                markerTransform.position + m_DebugLineOffset + Vector3.forward * m_DebugLineZOffset,
                Time.time));

            while (m_DebugLineMarkers.Count > m_MaxDebugLineCount)
            {
                m_DebugLineMarkers.RemoveAt(0);
            }
        }

        void RemoveExpiredDebugLines()
        {
            if (m_DebugLineLifetime <= 0.0f || m_DebugLineMarkers.Count == 0)
            {
                return;
            }

            float oldestAllowedTime = Time.time - m_DebugLineLifetime;
            for (int i = m_DebugLineMarkers.Count - 1; i >= 0; i--)
            {
                if (m_DebugLineMarkers[i].CreatedAt < oldestAllowedTime)
                {
                    m_DebugLineMarkers.RemoveAt(i);
                }
            }
        }

        void DrawDebugLineGizmos()
        {
            if (!m_EnableSpaceDebugLine || m_DebugLineMarkers.Count == 0)
            {
                return;
            }

            Color previousColor = Gizmos.color;
            Gizmos.color = m_DebugLineColor;

            for (int i = 0; i < m_DebugLineMarkers.Count; i++)
            {
                Vector3 center = m_DebugLineMarkers[i].Center;
                Gizmos.DrawLine(
                    center + Vector3.left * m_DebugLineHalfWidth,
                    center + Vector3.right * m_DebugLineHalfWidth);
                Gizmos.DrawWireSphere(center, m_DebugLineCenterRadius);
            }

            Gizmos.color = previousColor;
        }

        bool TryGetMovementDeltaTime(out float deltaTime)
        {
            deltaTime = 0.0f;
            double currentDspTime = AudioSettings.dspTime;

            if (m_HasScheduledStart)
            {
                if (currentDspTime < m_ScheduledStartDspTime)
                {
                    return false;
                }

                m_HasScheduledStart = false;
                m_CanMove = true;
                m_LastMovementDspTime = m_ScheduledStartDspTime;
            }

            if (!m_CanMove)
            {
                return false;
            }

            if (m_UseDspDeltaTime)
            {
                deltaTime = Mathf.Max(0.0f, (float)(currentDspTime - m_LastMovementDspTime));
                m_LastMovementDspTime = currentDspTime;
                return true;
            }

            deltaTime = Time.deltaTime;
            return true;
        }

        float GetTargetY()
        {
            if (m_HasExternalHeightTarget)
            {
                return m_ExternalHeightTarget == HeightTarget.Shift ? m_LeftShiftY : m_StandingY;
            }

            if (!m_AllowKeyboardInput)
            {
                return m_StandingY;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.leftShiftKey.isPressed)
            {
                return m_LeftShiftY;
            }

            return m_StandingY;
        }

        float GetTargetX()
        {
            if (m_HasExternalLaneTarget)
            {
                return GetLaneTargetX(m_ExternalLaneTarget);
            }

            if (!m_AllowKeyboardInput)
            {
                return m_CenterSnapX;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return m_CenterSnapX;
            }

            bool moveLeft = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            bool moveRight = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

            if (moveLeft == moveRight)
            {
                return m_CenterSnapX;
            }

            return moveLeft ? m_LeftSnapX : m_RightSnapX;
        }

        float GetLaneTargetX(LaneTarget laneTarget)
        {
            switch (laneTarget)
            {
                case LaneTarget.Left:
                    return m_LeftSnapX;
                case LaneTarget.Right:
                    return m_RightSnapX;
                default:
                    return m_CenterSnapX;
            }
        }

        void ConfigurePhysicsBody()
        {
            Rigidbody playerRigidbody = GetComponent<Rigidbody>();
            if (playerRigidbody == null)
            {
                playerRigidbody = gameObject.AddComponent<Rigidbody>();
            }

            ConfigurePhysicsBody(playerRigidbody);
        }

        void ConfigurePhysicsBody(Rigidbody playerRigidbody)
        {
            playerRigidbody.isKinematic = true;
            playerRigidbody.useGravity = false;
        }

        void MoveFollowers(Vector3 delta)
        {
            if (!m_MoveFollowersWithPlayer || delta == Vector3.zero || m_Followers == null)
            {
                return;
            }

            for (int i = 0; i < m_Followers.Length; i++)
            {
                Follower follower = m_Followers[i];
                if (follower == null)
                {
                    continue;
                }

                Transform followerTransform = follower.Target;
                if (followerTransform == null || followerTransform == m_Transform || followerTransform.IsChildOf(m_Transform))
                {
                    continue;
                }

                Vector3 followerDelta = delta;
                if (!follower.FollowLateralMovement)
                {
                    followerDelta.x = 0.0f;
                }

                if (!follower.FollowVerticalMovement)
                {
                    followerDelta.y = 0.0f;
                }

                if (followerDelta == Vector3.zero)
                {
                    continue;
                }

                followerTransform.position += followerDelta;
            }
        }
    }
}
