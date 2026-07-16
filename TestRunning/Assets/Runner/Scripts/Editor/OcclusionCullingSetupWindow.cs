using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HyperCasual.Runner
{
    public class OcclusionCullingSetupWindow : EditorWindow
    {
        bool m_IncludeInactiveChildren = true;

        [MenuItem("Tools/Runner/Occlusion Culling Setup")]
        static void Open()
        {
            GetWindow<OcclusionCullingSetupWindow>("Occlusion Setup");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Occlusion Culling Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select static environment roots in the Hierarchy, mark them for occlusion, then bake in Unity's Occlusion Culling window. Avoid marking moving objects, rhythm keys, player objects, triggers, cameras, and VFX as occlusion static.",
                MessageType.Info);

            m_IncludeInactiveChildren = EditorGUILayout.Toggle("Include Inactive Children", m_IncludeInactiveChildren);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
            {
                if (GUILayout.Button("Mark Selected Meshes as Occluder + Occludee"))
                {
                    MarkSelected(StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                }

                if (GUILayout.Button("Mark Selected Meshes as Occludee Only"))
                {
                    MarkSelected(StaticEditorFlags.OccludeeStatic);
                }

                if (GUILayout.Button("Clear Occlusion Static From Selected Meshes"))
                {
                    ClearSelected();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Use the scene-wide clear if too many objects were accidentally marked static and the occlusion bake hides everything.",
                MessageType.Warning);

            if (GUILayout.Button("Clear Occlusion Static From All Scene Meshes"))
            {
                ClearAllSceneMeshes();
            }

            if (GUILayout.Button("Enable Occlusion Culling On Scene Cameras"))
            {
                EnableCameraOcclusionCulling();
            }

            if (GUILayout.Button("Open Unity Occlusion Culling Window"))
            {
                EditorApplication.ExecuteMenuItem("Window/Rendering/Occlusion Culling");
            }
        }

        void MarkSelected(StaticEditorFlags flagsToAdd)
        {
            MeshRenderer[] renderers = GetSelectedMeshRenderers();
            for (int i = 0; i < renderers.Length; i++)
            {
                GameObject target = renderers[i].gameObject;
                Undo.RecordObject(target, "Mark Occlusion Static");

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(target);
                GameObjectUtility.SetStaticEditorFlags(target, flags | flagsToAdd);
                EditorUtility.SetDirty(target);
            }

            MarkActiveSceneDirty();
            Debug.Log($"Marked {renderers.Length} mesh renderer objects for occlusion culling.");
        }

        void ClearSelected()
        {
            MeshRenderer[] renderers = GetSelectedMeshRenderers();
            StaticEditorFlags occlusionFlags = StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

            for (int i = 0; i < renderers.Length; i++)
            {
                GameObject target = renderers[i].gameObject;
                Undo.RecordObject(target, "Clear Occlusion Static");

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(target);
                GameObjectUtility.SetStaticEditorFlags(target, flags & ~occlusionFlags);
                EditorUtility.SetDirty(target);
            }

            MarkActiveSceneDirty();
            Debug.Log($"Cleared occlusion static flags from {renderers.Length} mesh renderer objects.");
        }

        void ClearAllSceneMeshes()
        {
            MeshRenderer[] renderers = GetActiveSceneMeshRenderers();
            StaticEditorFlags occlusionFlags = StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

            for (int i = 0; i < renderers.Length; i++)
            {
                GameObject target = renderers[i].gameObject;
                Undo.RecordObject(target, "Clear Scene Occlusion Static");

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(target);
                GameObjectUtility.SetStaticEditorFlags(target, flags & ~occlusionFlags);
                EditorUtility.SetDirty(target);
            }

            MarkActiveSceneDirty();
            Debug.Log($"Cleared occlusion static flags from {renderers.Length} mesh renderer objects in the active scene.");
        }

        MeshRenderer[] GetSelectedMeshRenderers()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                return new MeshRenderer[0];
            }

            HashSet<MeshRenderer> renderers = new HashSet<MeshRenderer>();
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                MeshRenderer[] selectedRenderers = selectedObjects[i].GetComponentsInChildren<MeshRenderer>(m_IncludeInactiveChildren);
                for (int j = 0; j < selectedRenderers.Length; j++)
                {
                    renderers.Add(selectedRenderers[j]);
                }
            }

            MeshRenderer[] result = new MeshRenderer[renderers.Count];
            renderers.CopyTo(result);
            return result;
        }

        MeshRenderer[] GetActiveSceneMeshRenderers()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return new MeshRenderer[0];
            }

            MeshRenderer[] allRenderers = FindObjectsOfType<MeshRenderer>(true);
            List<MeshRenderer> sceneRenderers = new List<MeshRenderer>();
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && allRenderers[i].gameObject.scene == activeScene)
                {
                    sceneRenderers.Add(allRenderers[i]);
                }
            }

            return sceneRenderers.ToArray();
        }

        void EnableCameraOcclusionCulling()
        {
            Camera[] cameras = FindObjectsOfType<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                Undo.RecordObject(cameras[i], "Enable Camera Occlusion Culling");
                cameras[i].useOcclusionCulling = true;
                EditorUtility.SetDirty(cameras[i]);
            }

            MarkActiveSceneDirty();
            Debug.Log($"Enabled occlusion culling on {cameras.Length} scene cameras.");
        }

        static void MarkActiveSceneDirty()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }
    }
}
