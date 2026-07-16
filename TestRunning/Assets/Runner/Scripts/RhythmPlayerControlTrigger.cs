using System.Collections.Generic;
using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Applies rhythm player movement controls when the player enters a trigger collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class RhythmPlayerControlTrigger : MonoBehaviour
    {
        const string k_PlayerTag = "Player";

        public enum LaneCommand
        {
            NoChange,
            Left,
            Center,
            Right
        }

        public enum HeightCommand
        {
            NoChange,
            Standing,
            Shift
        }

        [SerializeField]
        RhythmPlayerMover m_PlayerMover;

        [SerializeField]
        string m_TriggerTag = k_PlayerTag;

        [Header("Controls")]
        [SerializeField]
        LaneCommand m_LaneCommand = LaneCommand.NoChange;

        [SerializeField]
        HeightCommand m_HeightCommand = HeightCommand.NoChange;

        [SerializeField, Tooltip("When enabled, the trigger acts like holding the selected control while the player stays inside it.")]
        bool m_ClearOnExit = true;

        readonly HashSet<Collider> m_OverlappingColliders = new HashSet<Collider>();
        RhythmPlayerMover m_ActivePlayerMover;

        void Awake()
        {
            ConfigureCollider();
            ConfigurePhysicsBody();
        }

        void Reset()
        {
            ConfigureCollider();
            ConfigurePhysicsBody();
        }

        void OnValidate()
        {
            ConfigureCollider();

            Rigidbody triggerRigidbody = GetComponent<Rigidbody>();
            if (triggerRigidbody != null)
            {
                ConfigurePhysicsBody(triggerRigidbody);
            }
        }

        void OnDisable()
        {
            if (m_ClearOnExit)
            {
                ClearControls();
            }

            m_OverlappingColliders.Clear();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!CanUseCollider(other))
            {
                return;
            }

            m_OverlappingColliders.Add(other);

            RhythmPlayerMover playerMover = ResolvePlayerMover(other);
            if (playerMover == null)
            {
                return;
            }

            m_ActivePlayerMover = playerMover;
            ApplyControls(playerMover);
        }

        void OnTriggerExit(Collider other)
        {
            if (!m_OverlappingColliders.Remove(other) || m_OverlappingColliders.Count > 0 || !m_ClearOnExit)
            {
                return;
            }

            ClearControls();
        }

        void ApplyControls(RhythmPlayerMover playerMover)
        {
            if (m_LaneCommand != LaneCommand.NoChange)
            {
                playerMover.SetLaneTarget(ToLaneTarget(m_LaneCommand));
            }

            if (m_HeightCommand != HeightCommand.NoChange)
            {
                playerMover.SetHeightTarget(ToHeightTarget(m_HeightCommand));
            }
        }

        void ClearControls()
        {
            if (m_ActivePlayerMover == null)
            {
                return;
            }

            if (m_LaneCommand != LaneCommand.NoChange)
            {
                m_ActivePlayerMover.ClearLaneTarget(ToLaneTarget(m_LaneCommand));
            }

            if (m_HeightCommand != HeightCommand.NoChange)
            {
                m_ActivePlayerMover.ClearHeightTarget(ToHeightTarget(m_HeightCommand));
            }

            m_ActivePlayerMover = null;
        }

        RhythmPlayerMover ResolvePlayerMover(Collider playerCollider)
        {
            if (m_PlayerMover != null)
            {
                return m_PlayerMover;
            }

            RhythmPlayerMover playerMover = playerCollider.GetComponentInParent<RhythmPlayerMover>();
            if (playerMover != null)
            {
                return playerMover;
            }

            Rigidbody attachedRigidbody = playerCollider.attachedRigidbody;
            if (attachedRigidbody != null)
            {
                playerMover = attachedRigidbody.GetComponentInParent<RhythmPlayerMover>();
                if (playerMover != null)
                {
                    return playerMover;
                }
            }

            return FindObjectOfType<RhythmPlayerMover>();
        }

        bool CanUseCollider(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(m_TriggerTag))
            {
                return true;
            }

            if (other.CompareTag(m_TriggerTag))
            {
                return true;
            }

            Rigidbody attachedRigidbody = other.attachedRigidbody;
            if (attachedRigidbody != null && attachedRigidbody.CompareTag(m_TriggerTag))
            {
                return true;
            }

            Transform current = other.transform.parent;
            while (current != null)
            {
                if (current.CompareTag(m_TriggerTag))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        RhythmPlayerMover.LaneTarget ToLaneTarget(LaneCommand laneCommand)
        {
            switch (laneCommand)
            {
                case LaneCommand.Left:
                    return RhythmPlayerMover.LaneTarget.Left;
                case LaneCommand.Right:
                    return RhythmPlayerMover.LaneTarget.Right;
                default:
                    return RhythmPlayerMover.LaneTarget.Center;
            }
        }

        RhythmPlayerMover.HeightTarget ToHeightTarget(HeightCommand heightCommand)
        {
            return heightCommand == HeightCommand.Shift
                ? RhythmPlayerMover.HeightTarget.Shift
                : RhythmPlayerMover.HeightTarget.Standing;
        }

        void ConfigureCollider()
        {
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        void ConfigurePhysicsBody()
        {
            Rigidbody triggerRigidbody = GetComponent<Rigidbody>();
            if (triggerRigidbody == null)
            {
                triggerRigidbody = gameObject.AddComponent<Rigidbody>();
            }

            ConfigurePhysicsBody(triggerRigidbody);
        }

        void ConfigurePhysicsBody(Rigidbody triggerRigidbody)
        {
            triggerRigidbody.isKinematic = true;
            triggerRigidbody.useGravity = false;
        }
    }
}
