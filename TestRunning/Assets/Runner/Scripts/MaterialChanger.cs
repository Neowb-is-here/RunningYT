using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace HyperCasual.Runner
{
    public class MaterialChanger : MonoBehaviour
    {
        [SerializeField]
        GameObject[] m_TargetObjects;

        [SerializeField]
        Material m_Material;

        [SerializeField]
        bool m_IncludeChildren = true;

        [SerializeField]
        bool m_ApplyOnStart = true;

        [SerializeField]
        bool m_ReplaceAllMaterialSlots = true;

        [SerializeField, Min(0)]
        int m_MaterialSlotIndex;

        [SerializeField, Tooltip("Shared material avoids creating material instances for many objects.")]
        bool m_UseSharedMaterial = true;

        void Start()
        {
            if (m_ApplyOnStart)
            {
                ApplyMaterial();
            }
        }

        [ContextMenu("Apply Material")]
        public void ApplyMaterial()
        {
            ApplyMaterial(false);
        }

        [ContextMenu("Apply Material Permanently")]
        public void ApplyMaterialPermanently()
        {
            ApplyMaterial(true);
        }

        void ApplyMaterial(bool saveSceneChanges)
        {
            if (m_Material == null || m_TargetObjects == null)
            {
                return;
            }

            for (int i = 0; i < m_TargetObjects.Length; i++)
            {
                ApplyToTarget(m_TargetObjects[i], saveSceneChanges);
            }

#if UNITY_EDITOR
            if (saveSceneChanges && !Application.isPlaying)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                }
            }
#endif
        }

        void ApplyToTarget(GameObject targetObject, bool saveSceneChanges)
        {
            if (targetObject == null)
            {
                return;
            }

            Renderer[] renderers = m_IncludeChildren
                ? targetObject.GetComponentsInChildren<Renderer>(true)
                : targetObject.GetComponents<Renderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                ApplyToRenderer(renderers[i], saveSceneChanges);
            }
        }

        void ApplyToRenderer(Renderer targetRenderer, bool saveSceneChanges)
        {
            if (targetRenderer == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (saveSceneChanges && !Application.isPlaying)
            {
                Undo.RecordObject(targetRenderer, "Apply Material");
            }
#endif

            Material[] materials = m_UseSharedMaterial
                ? targetRenderer.sharedMaterials
                : targetRenderer.materials;

            if (materials == null || materials.Length == 0)
            {
                return;
            }

            if (m_ReplaceAllMaterialSlots)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = m_Material;
                }
            }
            else if (m_MaterialSlotIndex < materials.Length)
            {
                materials[m_MaterialSlotIndex] = m_Material;
            }

            if (m_UseSharedMaterial)
            {
                targetRenderer.sharedMaterials = materials;
            }
            else
            {
                targetRenderer.materials = materials;
            }

#if UNITY_EDITOR
            if (saveSceneChanges && !Application.isPlaying)
            {
                EditorUtility.SetDirty(targetRenderer);
            }
#endif
        }
    }
}
