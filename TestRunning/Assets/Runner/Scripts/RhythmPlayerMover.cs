using UnityEngine;
using UnityEngine.InputSystem;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Moves the rhythm player rig forward through static rhythm keys.
    /// </summary>
    public class RhythmPlayerMover : MonoBehaviour
    {
        [System.Serializable]
        class Follower
        {
            public Transform Target;
            public bool FollowLateralMovement;
            public bool FollowVerticalMovement;
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

        [Header("Timing")]
        [SerializeField]
        bool m_MoveOnStart = true;

        [Header("Followers")]
        [SerializeField]
        bool m_MoveFollowersWithPlayer = true;

        [SerializeField]
        Follower[] m_Followers;

        Transform m_Transform;
        double m_ScheduledStartDspTime;
        double m_LastMovementDspTime;
        bool m_CanMove;
        bool m_HasScheduledStart;
        bool m_HasRuntimeMovementState;
        bool m_UseDspDeltaTime;

        public float ForwardSpeed => m_ForwardSpeed;

        void Awake()
        {
            m_Transform = transform;
            if (!m_HasRuntimeMovementState)
            {
                m_CanMove = m_MoveOnStart;
            }
        }

        void OnValidate()
        {
            m_ForwardSpeed = Mathf.Max(0.0f, m_ForwardSpeed);
            m_LaneMoveSpeed = Mathf.Max(0.01f, m_LaneMoveSpeed);
            m_HeightMoveSpeed = Mathf.Max(0.01f, m_HeightMoveSpeed);

            if (m_LeftSnapX > m_RightSnapX)
            {
                float leftSnapX = m_RightSnapX;
                m_RightSnapX = m_LeftSnapX;
                m_LeftSnapX = leftSnapX;
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

        void Update()
        {
            if (m_Transform == null)
            {
                m_Transform = transform;
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
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.leftShiftKey.isPressed)
            {
                return m_LeftShiftY;
            }

            return m_StandingY;
        }

        float GetTargetX()
        {
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
