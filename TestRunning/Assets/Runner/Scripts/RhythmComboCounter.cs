using System.Collections;
using TMPro;
using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Displays a rhythm combo value and plays a small hit reaction when it increases.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class RhythmComboCounter : MonoBehaviour
    {
        public static RhythmComboCounter Instance { get; private set; }

        [Header("References")]
        [SerializeField]
        TMP_Text m_NumberText;

        [SerializeField]
        TMP_Text m_LabelText;

        [SerializeField]
        RectTransform m_AnimatedRoot;

        [Header("Combo")]
        [SerializeField, Min(0)]
        int m_StartCombo;

        [SerializeField, Min(1)]
        int m_DefaultAddAmount = 1;

        [SerializeField]
        bool m_ResetOnEnable = true;

        [SerializeField]
        string m_NumberFormat = "0";

        [SerializeField]
        string m_Label = "COMBO";

        [SerializeField]
        bool m_ShowLabelInNumberTextWhenLabelMissing = true;

        [SerializeField]
        string m_CombinedTextFormat = "{0}\n<size=45%>{1}</size>";

        [Header("Style")]
        [SerializeField]
        bool m_ApplyStyleOnEnable = true;

        [SerializeField]
        Color m_NumberColor = Color.white;

        [SerializeField]
        Color m_LabelColor = Color.white;

        [SerializeField]
        Color m_OutlineColor = new Color(0.02f, 0.02f, 0.03f, 1.0f);

        [SerializeField, Range(0.0f, 1.0f)]
        float m_OutlineWidth = 0.25f;

        [SerializeField, Min(1.0f)]
        float m_NumberFontSize = 140.0f;

        [SerializeField, Min(1.0f)]
        float m_LabelFontSize = 42.0f;

        [Header("Hit Reaction")]
        [SerializeField, Min(0.0f)]
        float m_ShakeDuration = 0.16f;

        [SerializeField, Min(0.0f)]
        float m_ShakeStrength = 10.0f;

        [SerializeField, Min(1.0f)]
        float m_PopScale = 1.12f;

        [SerializeField]
        bool m_UseUnscaledTime = true;

        [Header("Runtime")]
        [SerializeField, Min(0)]
        int m_CurrentCombo;

        Coroutine m_HitReactionCoroutine;
        Vector2 m_BaseAnchoredPosition;
        Vector3 m_BaseLocalScale = Vector3.one;
        bool m_HasCachedRootTransform;

        public int CurrentCombo => m_CurrentCombo;

        void Awake()
        {
            RegisterInstance();
            ResolveReferences();
            CacheRootTransform();
        }

        void OnEnable()
        {
            RegisterInstance();
            ResolveReferences();
            CacheRootTransform();

            if (m_ApplyStyleOnEnable)
            {
                ApplyStyle();
            }

            if (m_ResetOnEnable)
            {
                ResetCombo();
                return;
            }

            UpdateText();
        }

        void OnDisable()
        {
            StopHitReaction();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void OnValidate()
        {
            m_StartCombo = Mathf.Max(0, m_StartCombo);
            m_DefaultAddAmount = Mathf.Max(1, m_DefaultAddAmount);
            m_CurrentCombo = Mathf.Max(0, m_CurrentCombo);
            m_NumberFontSize = Mathf.Max(1.0f, m_NumberFontSize);
            m_LabelFontSize = Mathf.Max(1.0f, m_LabelFontSize);
            m_ShakeDuration = Mathf.Max(0.0f, m_ShakeDuration);
            m_ShakeStrength = Mathf.Max(0.0f, m_ShakeStrength);
            m_PopScale = Mathf.Max(1.0f, m_PopScale);
        }

        public void AddCombo()
        {
            AddCombo(m_DefaultAddAmount);
        }

        public void AddCombo(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            m_CurrentCombo += amount;
            UpdateText();
            PlayHitReaction();
        }

        public void SetCombo(int combo)
        {
            m_CurrentCombo = Mathf.Max(0, combo);
            UpdateText();
        }

        public void ResetCombo()
        {
            SetCombo(m_StartCombo);
            StopHitReaction();
        }

        void RegisterInstance()
        {
            if (Instance == null || Instance == this)
            {
                Instance = this;
            }
        }

        void ResolveReferences()
        {
            if (m_NumberText == null)
            {
                m_NumberText = GetComponent<TMP_Text>();
            }

            if (m_AnimatedRoot == null)
            {
                m_AnimatedRoot = GetComponent<RectTransform>();
            }
        }

        void ApplyStyle()
        {
            ApplyTextStyle(m_NumberText, m_NumberColor, m_NumberFontSize);
            ApplyTextStyle(m_LabelText, m_LabelColor, m_LabelFontSize);
        }

        void ApplyTextStyle(TMP_Text text, Color color, float fontSize)
        {
            if (text == null)
            {
                return;
            }

            text.color = color;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold | FontStyles.Italic;
            text.alignment = TextAlignmentOptions.Center;
            text.outlineColor = m_OutlineColor;
            text.outlineWidth = m_OutlineWidth;
        }

        void UpdateText()
        {
            string numberText = m_CurrentCombo.ToString(m_NumberFormat);

            if (m_NumberText != null)
            {
                m_NumberText.text = m_LabelText == null && m_ShowLabelInNumberTextWhenLabelMissing
                    ? string.Format(m_CombinedTextFormat, numberText, m_Label)
                    : numberText;
            }

            if (m_LabelText != null)
            {
                m_LabelText.text = m_Label;
            }
        }

        void PlayHitReaction()
        {
            if (m_AnimatedRoot == null || (m_ShakeDuration <= 0.0f && Mathf.Approximately(m_PopScale, 1.0f)))
            {
                return;
            }

            StopHitReaction();
            CacheRootTransform();
            m_HitReactionCoroutine = StartCoroutine(HitReaction());
        }

        IEnumerator HitReaction()
        {
            float elapsed = 0.0f;
            float duration = Mathf.Max(0.01f, m_ShakeDuration);

            while (elapsed < duration)
            {
                elapsed += m_UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float strength = 1.0f - Mathf.Clamp01(elapsed / duration);
                Vector2 shakeOffset = Random.insideUnitCircle * m_ShakeStrength * strength;
                float scale = Mathf.Lerp(1.0f, m_PopScale, strength);

                m_AnimatedRoot.anchoredPosition = m_BaseAnchoredPosition + shakeOffset;
                m_AnimatedRoot.localScale = m_BaseLocalScale * scale;

                yield return null;
            }

            RestoreRootTransform();
            m_HitReactionCoroutine = null;
        }

        void CacheRootTransform()
        {
            if (m_AnimatedRoot == null || m_HasCachedRootTransform)
            {
                return;
            }

            m_BaseAnchoredPosition = m_AnimatedRoot.anchoredPosition;
            m_BaseLocalScale = m_AnimatedRoot.localScale;
            m_HasCachedRootTransform = true;
        }

        void StopHitReaction()
        {
            if (m_HitReactionCoroutine != null)
            {
                StopCoroutine(m_HitReactionCoroutine);
                m_HitReactionCoroutine = null;
            }

            RestoreRootTransform();
        }

        void RestoreRootTransform()
        {
            if (m_AnimatedRoot == null || !m_HasCachedRootTransform)
            {
                return;
            }

            m_AnimatedRoot.anchoredPosition = m_BaseAnchoredPosition;
            m_AnimatedRoot.localScale = m_BaseLocalScale;
        }
    }
}
