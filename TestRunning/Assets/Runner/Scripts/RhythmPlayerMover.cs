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

        [Header("Followers")]
        [SerializeField]
        bool m_MoveFollowersWithPlayer = true;

        [SerializeField]
        Follower[] m_Followers;

        Transform m_Transform;
        int m_CurrentLane = 1;

        void Awake()
        {
            m_Transform = transform;
            m_CurrentLane = GetClosestLane(m_Transform.position.x);
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

        void Update()
        {
            if (m_Transform == null)
            {
                m_Transform = transform;
            }

            Vector3 previousPosition = m_Transform.position;
            Vector3 nextPosition = previousPosition;
            float deltaTime = Time.deltaTime;

            nextPosition.z += m_ForwardSpeed * deltaTime;
            UpdateTargetLane();
            nextPosition.x = Mathf.MoveTowards(
                nextPosition.x,
                GetLaneX(m_CurrentLane),
                m_LaneMoveSpeed * deltaTime);
            nextPosition.y = Mathf.MoveTowards(
                nextPosition.y,
                GetTargetY(),
                m_HeightMoveSpeed * deltaTime);

            Vector3 delta = nextPosition - previousPosition;
            m_Transform.position = nextPosition;
            MoveFollowers(delta);
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

        void UpdateTargetLane()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool snapLeft = keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
            bool snapRight = keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;

            if (snapLeft == snapRight)
            {
                return;
            }

            m_CurrentLane += snapLeft ? -1 : 1;
            m_CurrentLane = Mathf.Clamp(m_CurrentLane, 0, 2);
        }

        float GetLaneX(int lane)
        {
            switch (lane)
            {
                case 0:
                    return m_LeftSnapX;
                case 2:
                    return m_RightSnapX;
                default:
                    return m_CenterSnapX;
            }
        }

        int GetClosestLane(float xPosition)
        {
            float leftDistance = Mathf.Abs(xPosition - m_LeftSnapX);
            float centerDistance = Mathf.Abs(xPosition - m_CenterSnapX);
            float rightDistance = Mathf.Abs(xPosition - m_RightSnapX);

            if (leftDistance < centerDistance && leftDistance < rightDistance)
            {
                return 0;
            }

            if (rightDistance < centerDistance)
            {
                return 2;
            }

            return 1;
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
