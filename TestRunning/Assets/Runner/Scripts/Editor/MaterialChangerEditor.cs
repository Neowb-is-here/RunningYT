using UnityEditor;
using UnityEngine;

namespace HyperCasual.Runner
{
    [CustomEditor(typeof(MaterialChanger))]
    public class MaterialChangerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            MaterialChanger materialChanger = (MaterialChanger)target;
            if (GUILayout.Button("Apply Material Permanently"))
            {
                materialChanger.ApplyMaterialPermanently();
            }

            if (Application.isPlaying && GUILayout.Button("Apply Material Runtime Only"))
            {
                materialChanger.ApplyMaterial();
            }
        }
    }
}
