using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HyperCasual.Runner
{
    /// <summary>
    /// A base class for all objects which populate a
    /// LevelDefinition. This class includes all logic 
    /// necessary for snapping an object to a level's grid.
    /// </summary>
    [ExecuteInEditMode]
    public class Spawnable : MonoBehaviour
    {
        protected Transform m_Transform;

        LevelDefinition m_LevelDefinition;
        Vector3 m_Position;
        bool m_SnappedThisFrame;
        float m_PreviousGridSize;

        [SerializeField]
        bool m_SnapToGrid = true;

        /// <summary>
        /// The position of this Spawnable, as it is saved.
        /// This value does not factor in any snapping.
        /// </summary>
        public Vector3 SavedPosition => m_Position;

        protected virtual void Awake()
        {
            m_Transform = transform;

            if (LevelManager.Instance != null)
            {
#if UNITY_EDITOR
                if (PrefabUtility.IsPartOfNonAssetPrefabInstance(gameObject))
#endif
                m_Transform.SetParent(LevelManager.Instance.transform);
            }
        }

        /// <summary>
        /// Sets the local scale of this spawnable object.
        /// </summary>
        /// <param name="scale">
        /// The scale to apply to this spawnable object.
        /// </param>
        public virtual void SetScale(Vector3 scale)
        {
           m_Transform.localScale = scale;
        }

        /// <summary>
        /// Link this spawnable object to the provided levelDefinition so it
        /// can accurately snap to that levels grid if applicable.
        /// </summary>
        /// <param name="levelDefinition">
        /// The LevelDefinition this SpawnableObject belongs to.
        /// </param>
        public void SetLevelDefinition(LevelDefinition levelDefinition)
        {
            if (levelDefinition == null)
            {
                return;
            }

            m_LevelDefinition = levelDefinition;
        }

        /// <summary>
        /// This method can be overriden in classes that extend Spawnable
        /// to hold any logic needed to reset that object to its default state.
        /// </summary>
        public virtual void ResetSpawnable() {}

        protected virtual void OnEnable()
        {
            m_Transform = transform;
            m_Position = m_Transform.position;
            m_Transform.hasChanged = false;

            if (LevelManager.Instance != null && !Application.isPlaying)
            {
                // Ensure level definition is set for spawnable prefabs
                // created by the user while the level is open in the
                // level editor.
                SetLevelDefinition(LevelManager.Instance.LevelDefinition);
                SnapToGrid();
            }
        }

        protected virtual void Update()
        {
            if (!Application.isPlaying && m_LevelDefinition != null)
            {
                if (m_Transform.hasChanged)
                {
                    m_Position = m_Transform.position;
                    m_Transform.hasChanged = false;

                    if (m_LevelDefinition.SnapToGrid)
                    {
                        SnapToGrid();
                    }

                    SetScale(m_Transform.localScale);
                }
                else if (m_PreviousGridSize != m_LevelDefinition.GridSize)
                {
                    SnapToGrid();
                }
            }
        }

        /// <summary>
        /// If applicable, snap this spawnable object to the grid of the 
        /// current LevelDefinition.
        /// </summary>
        protected virtual void SnapToGrid()
        {
            if (!m_SnapToGrid || m_LevelDefinition == null)
            {
                return;
            }

            Vector3 position = m_Position;

            position.x = m_LevelDefinition.GridSize * Mathf.Round(position.x/m_LevelDefinition.GridSize);
            position.z = m_LevelDefinition.GridSize * Mathf.Round(position.z/m_LevelDefinition.GridSize);

            m_Transform.position = position;

            // Update previous grid size
            m_PreviousGridSize = m_LevelDefinition.GridSize;

            // Do not allow a snap to enable this flag
            m_Transform.hasChanged = false;
        }
    }
}
