using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Displays and updates a simple action counter for runner inputs.
    /// </summary>
    public class RunnerActionCounter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        TMP_Text m_CounterText;

        [SerializeField]
        Transform m_PlayerTransform;

        [SerializeField]
        Transform m_CameraTransform;

        [Header("Passive Counter")]
        [SerializeField, Min(0.0f)]
        float m_StartValue;

        [SerializeField, Min(0.0f)]
        float m_PassiveAddAmount = 0.1f;

        [SerializeField, Min(0.01f)]
        float m_PassiveAddInterval = 0.5f;

        [Header("Action Values")]
        [FormerlySerializedAs("m_EnemyLaneInputAddAmount")]
        [SerializeField, Min(0.0f)]
        float m_LaneInputAddAmount = 0.2f;

        [SerializeField, Min(0.0f)]
        float m_EnemyDetectedAddAmount = 0.2f;

        [SerializeField, Min(0.0f)]
        float m_JumpAddAmount = 0.3f;

        [SerializeField, Min(0.0f)]
        float m_CrouchAddAmount = 0.3f;

        [SerializeField, Min(0.0f)]
        float m_BigJumpAddAmount = 0.5f;

        [Header("Crouch")]
        [SerializeField]
        float m_CrouchCameraYThreshold = 1.43f;

        [Header("Display")]
        [SerializeField]
        string m_NumberFormat = "0.0";

        [SerializeField]
        string m_TextSuffix;

        [Header("Runtime")]
        [SerializeField, Min(0.0f)]
        float m_CurrentKcal;

        float m_PassiveTimer;
        bool m_WaitingForCrouchLower;
        GameObject m_LastDetectedEnemy;

        void Awake()
        {
            ResolveReferences();
        }

        void OnEnable()
        {
            m_CurrentKcal = m_StartValue;
            m_PassiveTimer = 0.0f;
            m_WaitingForCrouchLower = false;
            m_LastDetectedEnemy = null;
            UpdateCounterText();
        }

        void OnValidate()
        {
            m_StartValue = Mathf.Max(0.0f, m_StartValue);
            m_PassiveAddAmount = Mathf.Max(0.0f, m_PassiveAddAmount);
            m_PassiveAddInterval = Mathf.Max(0.01f, m_PassiveAddInterval);
            m_LaneInputAddAmount = Mathf.Max(0.0f, m_LaneInputAddAmount);
            m_EnemyDetectedAddAmount = Mathf.Max(0.0f, m_EnemyDetectedAddAmount);
            m_JumpAddAmount = Mathf.Max(0.0f, m_JumpAddAmount);
            m_CrouchAddAmount = Mathf.Max(0.0f, m_CrouchAddAmount);
            m_BigJumpAddAmount = Mathf.Max(0.0f, m_BigJumpAddAmount);
            m_CurrentKcal = Mathf.Max(0.0f, m_CurrentKcal);

            if (Application.isPlaying)
            {
                UpdateCounterText();
            }
        }

        void Update()
        {
            ResolveReferences();
            UpdatePassiveCounter();
            UpdateEnemyDetectionCounter();
            UpdateInputCounter();
            UpdatePendingCrouchCounter();
            UpdateCounterText();
        }

        void ResolveReferences()
        {
            if (m_CounterText == null)
            {
                m_CounterText = GetComponent<TMP_Text>();
            }

            if (PlayerController.Instance == null)
            {
                return;
            }

            if (m_PlayerTransform == null)
            {
                m_PlayerTransform = PlayerController.Instance.Transform != null
                    ? PlayerController.Instance.Transform
                    : PlayerController.Instance.transform;
            }
        }

        void UpdatePassiveCounter()
        {
            m_PassiveTimer += Time.deltaTime;

            while (m_PassiveTimer >= m_PassiveAddInterval)
            {
                m_PassiveTimer -= m_PassiveAddInterval;
                AddValue(m_PassiveAddAmount);
            }
        }

        void UpdateInputCounter()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool laneInputPressed = keyboard.aKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame;
            if (laneInputPressed)
            {
                AddValue(m_LaneInputAddAmount);
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                AddValue(m_JumpAddAmount);
            }

            if (keyboard.leftCtrlKey.wasPressedThisFrame)
            {
                AddValue(m_BigJumpAddAmount);
            }

            if (keyboard.leftShiftKey.wasPressedThisFrame)
            {
                float cameraY = GetCameraY();
                m_WaitingForCrouchLower = cameraY >= m_CrouchCameraYThreshold;
            }
        }

        void UpdatePendingCrouchCounter()
        {
            if (!m_WaitingForCrouchLower)
            {
                return;
            }

            if (GetCameraY() < m_CrouchCameraYThreshold)
            {
                AddValue(m_CrouchAddAmount);
                m_WaitingForCrouchLower = false;
            }
        }

        void UpdateEnemyDetectionCounter()
        {
            PlayerController player = PlayerController.Instance;
            if (player == null || !player.HasEnemyRaycastHit)
            {
                m_LastDetectedEnemy = null;
                return;
            }

            GameObject detectedEnemy = player.CurrentEnemyRaycastHitObject;
            if (detectedEnemy == null || detectedEnemy == m_LastDetectedEnemy)
            {
                return;
            }

            m_LastDetectedEnemy = detectedEnemy;
            AddValue(m_EnemyDetectedAddAmount);
        }

        float GetCameraY()
        {
            if (m_CameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    m_CameraTransform = mainCamera.transform;
                }
            }

            if (m_CameraTransform == null)
            {
                return float.MaxValue;
            }

            if (m_CameraTransform.parent != null)
            {
                return m_CameraTransform.localPosition.y;
            }

            if (m_PlayerTransform != null)
            {
                return m_CameraTransform.position.y - m_PlayerTransform.position.y;
            }

            return m_CameraTransform.position.y;
        }

        void AddValue(float amount)
        {
            if (amount <= 0.0f)
            {
                return;
            }

            m_CurrentKcal += amount;
            UpdateCounterText();
        }

        void UpdateCounterText()
        {
            if (m_CounterText != null)
            {
                string valueText = m_CurrentKcal.ToString(m_NumberFormat);
                m_CounterText.text = string.IsNullOrEmpty(m_TextSuffix)
                    ? valueText
                    : valueText + " " + m_TextSuffix;
            }
        }
    }
}
