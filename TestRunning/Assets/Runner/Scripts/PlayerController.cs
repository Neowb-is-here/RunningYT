using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HyperCasual.Runner
{
    /// <summary>
    /// A class used to control a player in a Runner
    /// game. Includes logic for player movement as well as 
    /// other gameplay logic.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        /// <summary> Returns the PlayerController. </summary>
        public static PlayerController Instance => s_Instance;
        static PlayerController s_Instance;

        [SerializeField]
        Animator m_Animator;

        [SerializeField]
        SkinnedMeshRenderer m_SkinnedMeshRenderer;

        [SerializeField]
        PlayerSpeedPreset m_PlayerSpeed = PlayerSpeedPreset.Medium;

        [SerializeField]
        float m_CustomPlayerSpeed = 10.0f;

        [SerializeField]
        bool m_StartAtTargetSpeed = true;

        [SerializeField]
        float m_AccelerationSpeed = 10.0f;

        [SerializeField]
        float m_DecelerationSpeed = 20.0f;

        [SerializeField]
        float m_HorizontalSpeedFactor = 0.5f;

        [SerializeField]
        float m_ScaleVelocity = 2.0f;

        [SerializeField]
        bool m_AutoMoveForward = true;

        [Header("Crouch")]
        [SerializeField]
        Transform m_CameraTransform;

        [SerializeField]
        float m_CrouchCrouchedY = 1.0f;

        [SerializeField, Min(0.0f)]
        float m_CrouchLerpSpeed = 10.0f;

        bool m_IsCrouched;
        float m_CrouchTargetY = 1.43f;
        float m_CrouchNormalY = 1.43f;
        Vector3 m_CameraOffset;
        bool m_HasCameraOffset;

        [Header("Jump")]
        [SerializeField]
        float m_JumpHeight = 1.0f;

        [SerializeField, Min(0.01f)]
        float m_JumpDuration = 0.5f;

        [SerializeField]
        float m_BigJumpHeight = 2.0f;

        [SerializeField, Min(0.01f)]
        float m_BigJumpDuration = 0.75f;

        [Header("Enemy Raycast")]
        [SerializeField]
        bool m_EnableEnemyRaycast = true;

        [SerializeField]
        Transform m_EnemyRaycastOrigin;

        [SerializeField]
        Vector3 m_EnemyRaycastOffset = Vector3.up;

        [SerializeField, Min(0.0f)]
        float m_EnemyRaycastDistance = 10.0f;

        [SerializeField]
        LayerMask m_EnemyLayerMask;

        [SerializeField]
        QueryTriggerInteraction m_EnemyRaycastTriggerInteraction = QueryTriggerInteraction.Collide;

        [SerializeField]
        bool m_DebugEnemyRaycast;

        [Header("OneTwo Stick")]
        [SerializeField]
        bool m_EnableOneTwoStick = true;

        [SerializeField]
        string m_OneTwoTag = "OneTwo";

        [SerializeField]
        LayerMask m_OneTwoLayerMask;

        [SerializeField]
        Transform m_OneTwoRaycastOrigin;

        [SerializeField]
        Vector3 m_OneTwoRaycastOffset = Vector3.up;

        [SerializeField, Min(0.0f)]
        float m_OneTwoRaycastDistance = 1.25f;

        [SerializeField, Min(0.0f)]
        float m_OneTwoStickDuration = 2.0f;

        [SerializeField]
        QueryTriggerInteraction m_OneTwoRaycastTriggerInteraction = QueryTriggerInteraction.Collide;

        [SerializeField]
        bool m_OneTwoResumeAtTargetSpeed = true;

        [SerializeField]
        bool m_DebugOneTwoRaycast;

        bool m_IsJumping;
        float m_JumpTime;
        float m_CurrentJumpHeight;
        float m_CurrentJumpDuration;
        bool m_HasEnemyRaycastHit;
        GameObject m_CurrentEnemyRaycastHitObject;
        EnemyRaycastAnimation m_CurrentEnemyRaycastTarget;

        Vector3 m_LastPosition;
        float m_StartHeight;
        bool m_IsOneTwoSticking;
        float m_OneTwoStickTimer;
        Collider m_CurrentOneTwoCollider;
        Collider[] m_PlayerColliders;
        readonly List<Collider> m_PassedOneTwoColliders = new List<Collider>();

        const float k_MinimumScale = 0.1f;
        const int k_DefaultEnemyLayer = 6;
        const float k_EnemyRaycastGizmoRadius = 0.15f;
        static readonly string s_Speed = "Speed";

        enum PlayerSpeedPreset
        {
            Slow,
            Medium,
            Fast,
            Custom
        }

        Transform m_Transform;
        Vector3 m_StartPosition;
        bool m_HasInput;
        float m_MaxXPosition;
        float m_XPos;
        float m_ZPos;
        float m_TargetPosition;
        float m_Speed;
        float m_TargetSpeed;
        Vector3 m_Scale;
        Vector3 m_TargetScale;
        Vector3 m_DefaultScale;

        const float k_HalfWidth = 0.5f;
        const float k_LeftLaneX = -2.03f;
        const float k_CenterLaneX = 0.0f;
        const float k_RightLaneX = 2.03f;
        const float k_LaneSnapThreshold = 0.2f;

        int m_TargetLane = 1; // 0: left, 1: center, 2: right

        /// <summary> The player's root Transform component. </summary>
        public Transform Transform => m_Transform;

        /// <summary> The player's current speed. </summary>
        public float Speed => m_Speed;

        /// <summary> The player's target speed. </summary>
        public float TargetSpeed => m_TargetSpeed;

        /// <summary> Returns true while the player's enemy raycast is detecting an enemy. </summary>
        public bool HasEnemyRaycastTarget => m_CurrentEnemyRaycastTarget != null;

        /// <summary> Returns true while the player's enemy raycast is hitting the enemy layer. </summary>
        public bool HasEnemyRaycastHit => m_HasEnemyRaycastHit;

        /// <summary> The current enemy GameObject hit by the player's enemy raycast. </summary>
        public GameObject CurrentEnemyRaycastHitObject => m_CurrentEnemyRaycastHitObject;

        /// <summary> The player's minimum possible local scale. </summary>
        public float MinimumScale => k_MinimumScale;

        /// <summary> The player's current local scale. </summary>
        public Vector3 Scale => m_Scale;

        /// <summary> The player's target local scale. </summary>
        public Vector3 TargetScale => m_TargetScale;

        /// <summary> The player's default local scale. </summary>
        public Vector3 DefaultScale => m_DefaultScale;

        /// <summary> The player's default local height. </summary>
        public float StartHeight => m_StartHeight;

        /// <summary> The player's default local height. </summary>
        public float TargetPosition => m_TargetPosition;

        /// <summary> The player's maximum X position. </summary>
        public float MaxXPosition => m_MaxXPosition;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

            Initialize();
        }

        void Reset()
        {
            SetDefaultEnemyLayerMask();
        }

        void OnValidate()
        {
            if (m_EnemyRaycastDistance < 0.0f)
            {
                m_EnemyRaycastDistance = 0.0f;
            }

            m_JumpDuration = Mathf.Max(0.01f, m_JumpDuration);
            m_BigJumpDuration = Mathf.Max(0.01f, m_BigJumpDuration);
            m_CrouchLerpSpeed = Mathf.Max(0.0f, m_CrouchLerpSpeed);
            m_OneTwoRaycastDistance = Mathf.Max(0.0f, m_OneTwoRaycastDistance);
            m_OneTwoStickDuration = Mathf.Max(0.0f, m_OneTwoStickDuration);

            if (m_EnemyLayerMask.value == 0)
            {
                SetDefaultEnemyLayerMask();
            }
        }

        void SetDefaultEnemyLayerMask()
        {
            m_EnemyLayerMask = 1 << k_DefaultEnemyLayer;
        }

        /// <summary>
        /// Set up all necessary values for the PlayerController.
        /// </summary>
        public void Initialize()
        {
            m_Transform = transform;
            m_StartPosition = m_Transform.position;
            m_DefaultScale = m_Transform.localScale;
            m_Scale = m_DefaultScale;
            m_TargetScale = m_Scale;

            if (m_SkinnedMeshRenderer != null)
            {
                m_StartHeight = m_SkinnedMeshRenderer.bounds.size.y;
            }
            else 
            {
                m_StartHeight = 1.0f;
            }

            ResetSpeed();
        }

        /// <summary>
        /// Returns the current default speed based on the currently
        /// selected PlayerSpeed preset.
        /// </summary>
        public float GetDefaultSpeed()
        {
            switch (m_PlayerSpeed)
            {
                case PlayerSpeedPreset.Slow:
                    return 5.0f;

                case PlayerSpeedPreset.Medium:
                    return 10.0f;

                case PlayerSpeedPreset.Fast:
                    return 20.0f;
            }

            return m_CustomPlayerSpeed;
        }

        /// <summary>
        /// Adjust the player's current speed
        /// </summary>
        public void AdjustSpeed(float speed)
        {
            AdjustSpeed(speed, false);
        }

        /// <summary>
        /// Adjust the player's target speed and optionally apply the change immediately.
        /// </summary>
        public void AdjustSpeed(float speed, bool applyInstantly)
        {
            m_TargetSpeed += speed;
            m_TargetSpeed = Mathf.Max(0.0f, m_TargetSpeed);

            if (applyInstantly)
            {
                m_Speed += speed;
                m_Speed = Mathf.Clamp(m_Speed, 0.0f, m_TargetSpeed);
            }
        }

        /// <summary>
        /// Reset the player's current speed to their default speed
        /// </summary>
        public void ResetSpeed()
        {
            m_TargetSpeed = GetDefaultSpeed();
            m_Speed = m_StartAtTargetSpeed ? m_TargetSpeed : 0.0f;
        }

        /// <summary>
        /// Adjust the player's current scale
        /// </summary>
        public void AdjustScale(float scale)
        {
            m_TargetScale += Vector3.one * scale;
            m_TargetScale = Vector3.Max(m_TargetScale, Vector3.one * k_MinimumScale);
        }

        /// <summary>
        /// Reset the player's current speed to their default speed
        /// </summary>
        public void ResetScale()
        {
            m_Scale = m_DefaultScale;
            m_TargetScale = m_DefaultScale;
        }

        /// <summary>
        /// Returns the player's transform component
        /// </summary>
        public Vector3 GetPlayerTop()
        {
            return m_Transform.position + Vector3.up * (m_StartHeight * m_Scale.y - m_StartHeight);
        }

        /// <summary>
        /// Sets the target X position of the player
        /// </summary>
        public void SetDeltaPosition(float normalizedDeltaPosition)
        {
            if (m_MaxXPosition == 0.0f)
            {
                Debug.LogError("Player cannot move because SetMaxXPosition has never been called or Level Width is 0. If you are in the LevelEditor scene, ensure a level has been loaded in the LevelEditor Window!");
            }

            float fullWidth = m_MaxXPosition * 2.0f;
            m_TargetPosition = m_TargetPosition + fullWidth * normalizedDeltaPosition;
            m_TargetPosition = Mathf.Clamp(m_TargetPosition, -m_MaxXPosition, m_MaxXPosition);
            m_HasInput = true;
        }

        /// <summary>
        /// Stops player movement
        /// </summary>
        public void CancelMovement()
        {
            m_HasInput = false;
        }

        /// <summary>
        /// Set the level width to keep the player constrained
        /// </summary>
        public void SetMaxXPosition(float levelWidth)
        {
            // Level is centered at X = 0, so the maximum player
            // X position is half of the level width
            m_MaxXPosition = levelWidth * k_HalfWidth;
        }

        /// <summary>
        /// Returns player to their starting position
        /// </summary>
        public void ResetPlayer()
        {
            m_Transform.position = m_StartPosition;
            m_XPos = 0.0f;
            m_ZPos = m_StartPosition.z;
            m_TargetPosition = 0.0f;
            m_TargetLane = 1; // Reset to center lane

            m_LastPosition = m_Transform.position;

            m_HasInput = false;

            ResetOneTwoStickState();
            ResetSpeed();
            ResetScale();
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            Keyboard keyboard = Keyboard.current;
            UpdateOneTwoStick(deltaTime);

            // Update Scale

            if (!Approximately(m_Transform.localScale, m_TargetScale))
            {
                m_Scale = Vector3.Lerp(m_Scale, m_TargetScale, deltaTime * m_ScaleVelocity);
                m_Transform.localScale = m_Scale;
            }

            // Update Speed

            if (m_IsOneTwoSticking)
            {
                m_Speed = 0.0f;
            }
            else if (!m_AutoMoveForward && !m_HasInput)
            {
                Decelerate(deltaTime, 0.0f);
            }
            else if (m_TargetSpeed < m_Speed)
            {
                Decelerate(deltaTime, m_TargetSpeed);
            }
            else if (m_TargetSpeed > m_Speed)
            {
                Accelerate(deltaTime, m_TargetSpeed);
            }

            float speed = m_Speed * deltaTime;

            // Update position

            m_ZPos += speed;

            if (m_HasInput)
            {
                float horizontalSpeed = speed * m_HorizontalSpeedFactor;

                float newPositionTarget = Mathf.Lerp(m_XPos, m_TargetPosition, horizontalSpeed);
                float newPositionDifference = newPositionTarget - m_XPos;

                newPositionDifference = Mathf.Clamp(newPositionDifference, -horizontalSpeed, horizontalSpeed);

                m_XPos += newPositionDifference;
            }
            else
            {
                // Keyboard lane system only active when no touch/mouse input
                if (keyboard != null && keyboard.aKey.wasPressedThisFrame)
                {
                    if (m_TargetLane == 1) // Center
                    {
                        m_TargetLane = 0; // Move to left
                    }
                    else if (m_TargetLane == 2) // Right
                    {
                        m_TargetLane = 1; // Move to center
                    }
                    // If already at left (0), stay at left
                }

                if (keyboard != null && keyboard.dKey.wasPressedThisFrame)
                {
                    if (m_TargetLane == 0) // Left
                    {
                        m_TargetLane = 1; // Move to center
                    }
                    else if (m_TargetLane == 1) // Center
                    {
                        m_TargetLane = 2; // Move to right
                    }
                    // If already at right (2), stay at right
                }

                // Lerp to target lane position
                float targetLaneX = GetLaneXPosition(m_TargetLane);
                float horizontalSpeed = speed * m_HorizontalSpeedFactor;
                
                float newPositionTarget = Mathf.Lerp(m_XPos, targetLaneX, horizontalSpeed);
                float newPositionDifference = newPositionTarget - m_XPos;
                
                newPositionDifference = Mathf.Clamp(newPositionDifference, -horizontalSpeed, horizontalSpeed);
                
                m_XPos += newPositionDifference;

                // Snap to lane if very close
                if (Mathf.Abs(m_XPos - targetLaneX) < k_LaneSnapThreshold)
                {
                    m_XPos = targetLaneX;
                }
            }

            float jumpY = UpdateJump(deltaTime);
            m_Transform.position = new Vector3(m_XPos, m_StartPosition.y + jumpY, m_ZPos);

            UpdateCrouchCamera(deltaTime);
            UpdateEnemyRaycast();

            if (m_Animator != null && m_Animator.runtimeAnimatorController != null && deltaTime > 0.0f)
            {
                float distanceTravelledSinceLastFrame = (m_Transform.position - m_LastPosition).magnitude;
                float distancePerSecond = distanceTravelledSinceLastFrame / deltaTime;

                m_Animator.SetFloat(s_Speed, distancePerSecond);
            }

            if (m_Transform.position != m_LastPosition)
            {
                m_Transform.forward = Vector3.Lerp(m_Transform.forward, Vector3.forward, deltaTime * 10.0f);
            }

            m_LastPosition = m_Transform.position;
        }

        void UpdateOneTwoStick(float deltaTime)
        {
            if (m_IsOneTwoSticking)
            {
                m_OneTwoStickTimer -= deltaTime;
                if (m_OneTwoStickTimer <= 0.0f)
                {
                    ReleaseOneTwoStick();
                }

                return;
            }

            UpdateOneTwoStickDetection();
        }

        void UpdateOneTwoStickDetection()
        {
            if (!m_EnableOneTwoStick || m_OneTwoStickDuration <= 0.0f || m_OneTwoRaycastDistance <= 0.0f)
            {
                return;
            }

            bool hasHit = TryGetOneTwoHit(out RaycastHit hit);
            if (m_DebugOneTwoRaycast)
            {
                Vector3 origin = GetOneTwoRaycastOrigin();
                Vector3 direction = GetOneTwoRaycastDirection();
                Debug.DrawRay(origin, direction * m_OneTwoRaycastDistance, hasHit ? Color.magenta : Color.white);
            }

            if (hasHit)
            {
                BeginOneTwoStick(hit.collider);
            }
        }

        bool TryGetOneTwoHit(out RaycastHit nearestHit)
        {
            nearestHit = new RaycastHit();

            int layerMask = GetOneTwoLayerMask();
            if (layerMask == 0)
            {
                return false;
            }

            RaycastHit[] hits = Physics.RaycastAll(
                GetOneTwoRaycastOrigin(),
                GetOneTwoRaycastDirection(),
                m_OneTwoRaycastDistance,
                layerMask,
                m_OneTwoRaycastTriggerInteraction);

            if (hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsValidOneTwoCollider(hits[i].collider))
                {
                    nearestHit = hits[i];
                    return true;
                }
            }

            return false;
        }

        bool IsValidOneTwoCollider(Collider candidate)
        {
            if (candidate == null || m_PassedOneTwoColliders.Contains(candidate))
            {
                return false;
            }

            if (string.IsNullOrEmpty(m_OneTwoTag))
            {
                return true;
            }

            Transform candidateTransform = candidate.transform;
            while (candidateTransform != null)
            {
                if (candidateTransform.CompareTag(m_OneTwoTag))
                {
                    return true;
                }

                candidateTransform = candidateTransform.parent;
            }

            return false;
        }

        void BeginOneTwoStick(Collider blocker)
        {
            if (blocker == null)
            {
                return;
            }

            m_CurrentOneTwoCollider = blocker;
            m_OneTwoStickTimer = m_OneTwoStickDuration;
            m_IsOneTwoSticking = true;
            m_Speed = 0.0f;
        }

        void ReleaseOneTwoStick()
        {
            Collider blocker = m_CurrentOneTwoCollider;
            if (blocker != null && !m_PassedOneTwoColliders.Contains(blocker))
            {
                m_PassedOneTwoColliders.Add(blocker);
                SetPlayerCollisionWith(blocker, true);
            }

            m_CurrentOneTwoCollider = null;
            m_IsOneTwoSticking = false;
            m_OneTwoStickTimer = 0.0f;

            if (m_OneTwoResumeAtTargetSpeed)
            {
                m_Speed = m_TargetSpeed;
            }
        }

        void ResetOneTwoStickState()
        {
            for (int i = 0; i < m_PassedOneTwoColliders.Count; i++)
            {
                SetPlayerCollisionWith(m_PassedOneTwoColliders[i], false);
            }

            m_PassedOneTwoColliders.Clear();
            m_CurrentOneTwoCollider = null;
            m_IsOneTwoSticking = false;
            m_OneTwoStickTimer = 0.0f;
        }

        void SetPlayerCollisionWith(Collider targetCollider, bool ignore)
        {
            if (targetCollider == null)
            {
                return;
            }

            if (m_PlayerColliders == null || m_PlayerColliders.Length == 0)
            {
                m_PlayerColliders = GetComponentsInChildren<Collider>();
            }

            for (int i = 0; i < m_PlayerColliders.Length; i++)
            {
                Collider playerCollider = m_PlayerColliders[i];
                if (playerCollider != null && playerCollider != targetCollider)
                {
                    Physics.IgnoreCollision(playerCollider, targetCollider, ignore);
                }
            }
        }

        Vector3 GetOneTwoRaycastOrigin()
        {
            if (m_OneTwoRaycastOrigin != null)
            {
                return m_OneTwoRaycastOrigin.position;
            }

            Transform raycastTransform = m_Transform != null ? m_Transform : transform;
            return raycastTransform.position + m_OneTwoRaycastOffset;
        }

        Vector3 GetOneTwoRaycastDirection()
        {
            Transform raycastTransform = m_Transform != null ? m_Transform : transform;
            return raycastTransform.forward.sqrMagnitude > 0.0f ? raycastTransform.forward.normalized : Vector3.forward;
        }

        int GetOneTwoLayerMask()
        {
            return m_OneTwoLayerMask.value != 0 ? m_OneTwoLayerMask.value : Physics.DefaultRaycastLayers;
        }

        void Accelerate(float deltaTime, float targetSpeed)
        {
            m_Speed += deltaTime * m_AccelerationSpeed;
            m_Speed = Mathf.Min(m_Speed, targetSpeed);
        }

        void Decelerate(float deltaTime, float targetSpeed)
        {
            m_Speed -= deltaTime * m_DecelerationSpeed;
            m_Speed = Mathf.Max(m_Speed, targetSpeed);
        }

        float UpdateJump(float deltaTime)
        {
            Keyboard keyboard = Keyboard.current;
            if (!m_IsJumping && keyboard != null)
            {
                if (keyboard.leftCtrlKey.wasPressedThisFrame)
                {
                    StartJump(m_BigJumpHeight, m_BigJumpDuration);
                }
                else if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    StartJump(m_JumpHeight, m_JumpDuration);
                }
            }

            if (m_IsJumping)
            {
                m_JumpTime += deltaTime;
                float normalizedTime = m_JumpTime / m_CurrentJumpDuration;
                float jumpY = 4.0f * m_CurrentJumpHeight * normalizedTime * (1.0f - normalizedTime);

                if (normalizedTime >= 1.0f)
                {
                    m_IsJumping = false;
                    return 0.0f;
                }

                return jumpY;
            }

            return 0.0f;
        }

        void StartJump(float jumpHeight, float jumpDuration)
        {
            m_IsJumping = true;
            m_JumpTime = 0.0f;
            m_CurrentJumpHeight = jumpHeight;
            m_CurrentJumpDuration = Mathf.Max(0.01f, jumpDuration);

            if (m_CameraTransform != null)
            {
                if (!m_HasCameraOffset)
                {
                    CacheCameraOffset();
                }

                m_CrouchTargetY = m_CrouchNormalY;
                SetCameraRelativeY(m_CrouchNormalY);
            }
        }

        void UpdateCrouchCamera(float deltaTime)
        {
            Keyboard keyboard = Keyboard.current;

            // Don't allow toggling crouch while jumping; camera should stay at normal height during jump
            if (!m_IsJumping && keyboard != null && keyboard.leftShiftKey.wasPressedThisFrame)
            {
                m_IsCrouched = !m_IsCrouched;
                m_CrouchTargetY = m_IsCrouched ? m_CrouchCrouchedY : m_CrouchNormalY;
            }

            if (m_CameraTransform == null)
            {
                return;
            }

            if (!m_HasCameraOffset)
            {
                CacheCameraOffset();
            }

            if (m_IsJumping)
            {
                m_CrouchTargetY = m_CrouchNormalY;
            }

            float currentY = GetCameraRelativeY();
            float newY = m_IsJumping
                ? m_CrouchTargetY
                : Mathf.Lerp(currentY, m_CrouchTargetY, deltaTime * m_CrouchLerpSpeed);

            SetCameraRelativeY(newY);
        }

        void CacheCameraOffset()
        {
            m_CameraOffset = m_CameraTransform.position - m_Transform.position;
            m_HasCameraOffset = true;
            m_CrouchNormalY = GetCameraRelativeY();
            m_CrouchTargetY = m_IsCrouched ? m_CrouchCrouchedY : m_CrouchNormalY;
        }

        float GetCameraRelativeY()
        {
            if (m_CameraTransform.parent != null)
            {
                return m_CameraTransform.localPosition.y;
            }

            return m_CameraTransform.position.y - m_Transform.position.y;
        }

        void SetCameraRelativeY(float relativeY)
        {
            if (m_CameraTransform.parent != null)
            {
                Vector3 localPosition = m_CameraTransform.localPosition;
                localPosition.y = relativeY;
                m_CameraTransform.localPosition = localPosition;
            }
            else
            {
                Vector3 targetWorldPosition = m_Transform.position + m_CameraOffset;
                targetWorldPosition.y = m_Transform.position.y + relativeY;
                m_CameraTransform.position = targetWorldPosition;
            }
        }

        void UpdateEnemyRaycast()
        {
            int enemyLayerMask = GetEnemyLayerMask();
            if (!m_EnableEnemyRaycast || enemyLayerMask == 0)
            {
                m_HasEnemyRaycastHit = false;
                m_CurrentEnemyRaycastHitObject = null;
                m_CurrentEnemyRaycastTarget = null;
                return;
            }

            Vector3 origin = GetEnemyRaycastOrigin();
            Vector3 direction = GetEnemyRaycastDirection();

            bool hasHit = Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                m_EnemyRaycastDistance,
                enemyLayerMask,
                m_EnemyRaycastTriggerInteraction);

            m_HasEnemyRaycastHit = hasHit;

            if (m_DebugEnemyRaycast)
            {
                Debug.DrawRay(origin, direction * m_EnemyRaycastDistance, hasHit ? Color.red : Color.yellow);
            }

            if (!hasHit)
            {
                m_CurrentEnemyRaycastHitObject = null;
                m_CurrentEnemyRaycastTarget = null;
                return;
            }

            EnemyRaycastAnimation enemyAnimation = hit.collider.GetComponentInParent<EnemyRaycastAnimation>();
            if (enemyAnimation == null)
            {
                enemyAnimation = hit.collider.GetComponentInChildren<EnemyRaycastAnimation>();
            }

            if (enemyAnimation == null)
            {
                m_CurrentEnemyRaycastHitObject = GetEnemyRaycastFallbackHitObject(hit.collider);
                m_CurrentEnemyRaycastTarget = null;
                return;
            }

            m_CurrentEnemyRaycastHitObject = enemyAnimation.gameObject;

            if (enemyAnimation == m_CurrentEnemyRaycastTarget)
            {
                return;
            }

            m_CurrentEnemyRaycastTarget = enemyAnimation;
            enemyAnimation.PlayReaction();
        }

        GameObject GetEnemyRaycastFallbackHitObject(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return null;
            }

            return hitCollider.attachedRigidbody != null
                ? hitCollider.attachedRigidbody.gameObject
                : hitCollider.gameObject;
        }

        Vector3 GetEnemyRaycastOrigin()
        {
            if (m_EnemyRaycastOrigin != null)
            {
                return m_EnemyRaycastOrigin.position;
            }

            Transform raycastTransform = m_Transform != null ? m_Transform : transform;
            return raycastTransform.position + m_EnemyRaycastOffset;
        }

        Vector3 GetEnemyRaycastDirection()
        {
            Transform raycastTransform = m_Transform != null ? m_Transform : transform;
            return raycastTransform.forward.sqrMagnitude > 0.0f ? raycastTransform.forward.normalized : Vector3.forward;
        }

        int GetEnemyLayerMask()
        {
            return m_EnemyLayerMask.value != 0 ? m_EnemyLayerMask.value : 1 << k_DefaultEnemyLayer;
        }

        void OnDrawGizmos()
        {
            if (!m_DebugEnemyRaycast || !m_EnableEnemyRaycast)
            {
                return;
            }

            Vector3 origin = GetEnemyRaycastOrigin();
            Vector3 direction = GetEnemyRaycastDirection();
            int enemyLayerMask = GetEnemyLayerMask();

            bool hasHit = Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                m_EnemyRaycastDistance,
                enemyLayerMask,
                m_EnemyRaycastTriggerInteraction);

            Vector3 endPoint = hasHit ? hit.point : origin + direction * m_EnemyRaycastDistance;

            Gizmos.color = hasHit ? Color.red : Color.yellow;
            Gizmos.DrawLine(origin, endPoint);
            Gizmos.DrawWireSphere(origin, k_EnemyRaycastGizmoRadius);

            if (hasHit)
            {
                Gizmos.DrawSphere(hit.point, k_EnemyRaycastGizmoRadius);
            }
        }

        bool Approximately(Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
        }

        float GetLaneXPosition(int lane)
        {
            switch (lane)
            {
                case 0:
                    return k_LeftLaneX;
                case 2:
                    return k_RightLaneX;
                default:
                    return k_CenterLaneX;
            }
        }

        void UpdateCurrentLane()
        {
            // Determine which lane we're closest to
            float distToLeft = Mathf.Abs(m_XPos - k_LeftLaneX);
            float distToCenter = Mathf.Abs(m_XPos - k_CenterLaneX);
            float distToRight = Mathf.Abs(m_XPos - k_RightLaneX);

            if (distToLeft < distToCenter && distToLeft < distToRight)
            {
                m_TargetLane = 0; // Closest to left
            }
            else if (distToRight < distToCenter)
            {
                m_TargetLane = 2; // Closest to right
            }
            else
            {
                m_TargetLane = 1; // Closest to center
            }
        }

        float GetTargetXForLeftInput()
        {
            if (m_TargetLane == 0) // Left lane
            {
                return k_LeftLaneX; // Can't go further left
            }
            else if (m_TargetLane == 1) // Center lane
            {
                return k_LeftLaneX; // Move to left lane
            }
            else if (m_TargetLane == 2) // Right lane
            {
                return k_CenterLaneX; // Move to center lane
            }
            return m_XPos;
        }

        float GetTargetXForRightInput()
        {
            if (m_TargetLane == 0) // Left lane
            {
                return k_CenterLaneX; // Move to center lane
            }
            else if (m_TargetLane == 1) // Center lane
            {
                return k_RightLaneX; // Move to right lane
            }
            else if (m_TargetLane == 2) // Right lane
            {
                return k_RightLaneX; // Can't go further right
            }
            return m_XPos;
        }
    }
}
