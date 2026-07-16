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
