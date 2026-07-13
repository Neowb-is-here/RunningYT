using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Updates a UI Slider based on progress through the current runner level.
    /// </summary>
    public class LevelProgressSlider : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        Slider m_Slider;

        [SerializeField]
        TMP_Text m_PercentageText;

        [SerializeField]
        Transform m_Target;

        [SerializeField]
        Transform m_StartPoint;

        [SerializeField]
        Transform m_EndPoint;

        [Header("Progress")]
        [SerializeField]
        bool m_UseLoadedLevelLength = true;

        [SerializeField]
        bool m_CaptureTargetStartOnEnable = true;

        [SerializeField]
        float m_ManualStartZ;

        [SerializeField]
        float m_ManualEndZ = 100.0f;

        [Header("Display")]
        [SerializeField]
        bool m_SmoothValue = true;

        [SerializeField, Min(0.0f)]
        float m_SmoothSpeed = 8.0f;

        [SerializeField]
        bool m_DisableSliderInput = true;

        [SerializeField]
        string m_PercentageFormat = "0%";

        bool m_HasCapturedStart;
        bool m_HasInitializedValue;

        void Awake()
        {
            ResolveSlider();
            ConfigureSlider();
        }

        void OnEnable()
        {
            if (m_CaptureTargetStartOnEnable)
            {
                m_HasCapturedStart = false;
            }

            ResolveTarget();
            CaptureStartIfNeeded();
            ConfigureSlider();
            ResetSliderValue();
            UpdateSlider(true);
        }

        void OnValidate()
        {
            m_SmoothSpeed = Mathf.Max(0.0f, m_SmoothSpeed);
        }

        void Update()
        {
            ResolveTarget();
            CaptureStartIfNeeded();
            UpdateSlider(false);
        }

        void ResolveSlider()
        {
            if (m_Slider == null)
            {
                m_Slider = GetComponent<Slider>();
            }
        }

        void ResolveTarget()
        {
            if (m_Target != null)
            {
                return;
            }

            if (PlayerController.Instance != null)
            {
                m_Target = PlayerController.Instance.Transform != null
                    ? PlayerController.Instance.Transform
                    : PlayerController.Instance.transform;
            }
        }

        void CaptureStartIfNeeded()
        {
            if (!m_CaptureTargetStartOnEnable || m_HasCapturedStart || m_Target == null)
            {
                return;
            }

            m_ManualStartZ = m_Target.position.z;
            m_HasCapturedStart = true;
        }

        void ConfigureSlider()
        {
            if (m_Slider == null)
            {
                return;
            }

            m_Slider.minValue = 0.0f;
            m_Slider.maxValue = 1.0f;

            if (m_DisableSliderInput)
            {
                m_Slider.interactable = false;
            }
        }

        void ResetSliderValue()
        {
            if (m_Slider == null)
            {
                return;
            }

            m_Slider.value = 0.0f;
            UpdatePercentageText(0.0f);
            m_HasInitializedValue = false;
        }

        void UpdateSlider(bool instant)
        {
            ResolveSlider();
            if (m_Slider == null || m_Target == null)
            {
                if (m_Slider != null && !m_HasInitializedValue)
                {
                    m_Slider.value = 0.0f;
                    UpdatePercentageText(0.0f);
                }

                return;
            }

            float progress = GetProgress01();
            if (instant || !m_HasInitializedValue || !m_SmoothValue || m_SmoothSpeed <= 0.0f)
            {
                m_Slider.value = progress;
                UpdatePercentageText(progress);
                m_HasInitializedValue = true;
                return;
            }

            m_Slider.value = Mathf.Lerp(m_Slider.value, progress, Time.deltaTime * m_SmoothSpeed);
            UpdatePercentageText(m_Slider.value);
        }

        void UpdatePercentageText(float progress)
        {
            if (m_PercentageText == null)
            {
                return;
            }

            m_PercentageText.text = progress.ToString(m_PercentageFormat);
        }

        float GetProgress01()
        {
            float startZ = GetStartZ();
            float endZ = GetEndZ(startZ);

            if (Mathf.Approximately(startZ, endZ))
            {
                return 0.0f;
            }

            return Mathf.Clamp01(Mathf.InverseLerp(startZ, endZ, m_Target.position.z));
        }

        float GetStartZ()
        {
            return m_StartPoint != null ? m_StartPoint.position.z : m_ManualStartZ;
        }

        float GetEndZ(float startZ)
        {
            if (m_EndPoint != null)
            {
                return m_EndPoint.position.z;
            }

            if (m_UseLoadedLevelLength &&
                LevelManager.Instance != null &&
                LevelManager.Instance.LevelDefinition != null)
            {
                return LevelManager.Instance.LevelDefinition.LevelLength;
            }

            if (Mathf.Approximately(startZ, m_ManualEndZ))
            {
                return startZ + 0.01f;
            }

            return m_ManualEndZ;
        }

        public void ResetProgressStart()
        {
            m_HasCapturedStart = false;
            CaptureStartIfNeeded();
            UpdateSlider(true);
        }
    }
}
