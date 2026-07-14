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

        void Awake()
        {
            m_Transform = transform;
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
