using System.Collections;
using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Plays an inspector-configured enemy animation when detected by the player raycast.
    /// </summary>
    public class EnemyRaycastAnimation : MonoBehaviour
    {
        [SerializeField]
        Animator m_Animator;

        [SerializeField]
        AnimationMode m_AnimationMode = AnimationMode.PlayAnimationClip;

        [SerializeField]
        AnimationClip m_AnimationClip;

        [SerializeField]
        string m_StateName;

        [SerializeField]
        string m_TriggerName;

        [SerializeField]
        int m_LayerIndex;

        [SerializeField, Range(0.0f, 1.0f)]
        float m_NormalizedStartTime;

        [SerializeField]
        bool m_ReplayIfAlreadyActive;

        [Header("Deactivate")]
        [SerializeField]
        bool m_DeactivateAfterAnimation = true;

        [SerializeField]
        GameObject m_DeactivateTarget;

        [SerializeField, Min(0.0f)]
        float m_FallbackDeactivateDelay = 1.0f;

        Coroutine m_DeactivateCoroutine;

        enum AnimationMode
        {
            PlayAnimationClip,
            PlayState,
            SetTrigger
        }

        void Awake()
        {
            if (m_Animator == null)
            {
                m_Animator = GetComponentInChildren<Animator>();
            }
        }

        public void PlayReaction()
        {
            if (m_Animator == null || m_Animator.runtimeAnimatorController == null)
            {
                return;
            }

            string playedStateName = null;
            bool startedReaction = false;

            switch (m_AnimationMode)
            {
                case AnimationMode.PlayAnimationClip:
                    playedStateName = m_AnimationClip != null ? m_AnimationClip.name : null;
                    startedReaction = PlayState(playedStateName);
                    break;

                case AnimationMode.PlayState:
                    playedStateName = m_StateName;
                    startedReaction = PlayState(playedStateName);
                    break;

                case AnimationMode.SetTrigger:
                    playedStateName = m_StateName;
                    startedReaction = SetTrigger();
                    break;
            }

            if (startedReaction)
            {
                StartDeactivateAfterAnimation(playedStateName);
            }
        }

        bool PlayState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                return false;
            }

            int stateHash = Animator.StringToHash(stateName);
            int layerIndex = GetLayerIndex();

            if (!m_ReplayIfAlreadyActive && IsCurrentState(stateHash, layerIndex))
            {
                return true;
            }

            m_Animator.Play(stateHash, layerIndex, m_NormalizedStartTime);
            return true;
        }

        bool SetTrigger()
        {
            if (string.IsNullOrEmpty(m_TriggerName))
            {
                return false;
            }

            m_Animator.SetTrigger(Animator.StringToHash(m_TriggerName));
            return true;
        }

        void StartDeactivateAfterAnimation(string stateName)
        {
            if (!m_DeactivateAfterAnimation)
            {
                return;
            }

            if (m_DeactivateCoroutine != null)
            {
                StopCoroutine(m_DeactivateCoroutine);
            }

            m_DeactivateCoroutine = StartCoroutine(DeactivateAfterAnimation(stateName));
        }

        IEnumerator DeactivateAfterAnimation(string stateName)
        {
            yield return null;

            if (!string.IsNullOrEmpty(stateName))
            {
                int stateHash = Animator.StringToHash(stateName);
                int layerIndex = GetLayerIndex();

                yield return WaitForStateToStart(stateHash, layerIndex);
                yield return WaitForStateToFinish(stateHash, layerIndex);
            }
            else
            {
                yield return new WaitForSeconds(m_FallbackDeactivateDelay);
            }

            GameObject target = m_DeactivateTarget != null ? m_DeactivateTarget : gameObject;
            target.SetActive(false);
            m_DeactivateCoroutine = null;
        }

        IEnumerator WaitForStateToStart(int stateHash, int layerIndex)
        {
            float elapsed = 0.0f;
            while (!IsCurrentState(stateHash, layerIndex) && elapsed < m_FallbackDeactivateDelay)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator WaitForStateToFinish(int stateHash, int layerIndex)
        {
            while (IsCurrentState(stateHash, layerIndex))
            {
                AnimatorStateInfo stateInfo = m_Animator.GetCurrentAnimatorStateInfo(layerIndex);
                if (!m_Animator.IsInTransition(layerIndex) && stateInfo.normalizedTime >= 1.0f)
                {
                    yield break;
                }

                yield return null;
            }
        }

        bool IsCurrentState(int stateHash, int layerIndex)
        {
            AnimatorStateInfo stateInfo = m_Animator.GetCurrentAnimatorStateInfo(layerIndex);
            return stateInfo.shortNameHash == stateHash || stateInfo.fullPathHash == stateHash;
        }

        int GetLayerIndex()
        {
            if (m_Animator == null || m_Animator.layerCount == 0)
            {
                return 0;
            }

            return Mathf.Clamp(m_LayerIndex, 0, m_Animator.layerCount - 1);
        }
    }
}
