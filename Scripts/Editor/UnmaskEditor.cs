using UnityEditor;
using UnityEngine;

namespace Oultrox.UIExtensions.Editor
{
    [CustomEditor(typeof(Unmask))]
    public class UnmaskEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Grab properties
            SerializedProperty modeProp = serializedObject.FindProperty("m_Mode");

            SerializedProperty sameSceneTargetProp = serializedObject.FindProperty("m_SameSceneTarget");
            SerializedProperty fitTargetCanvasNameProp = serializedObject.FindProperty("m_FitTargetCanvasName");
            SerializedProperty fitTargetNameProp = serializedObject.FindProperty("m_FitTargetName");
            SerializedProperty fitNonCanvasTargetNameProp = serializedObject.FindProperty("m_FitNonCanvasTargetName");
            SerializedProperty fitNonCanvasTargetCameraNameProp = serializedObject.FindProperty("m_FitNonCanvasTargetCameraName");

            // Draw mode selector
            EditorGUILayout.PropertyField(modeProp);

            // Draw relevant fields based on mode
            UnmaskMode mode = (UnmaskMode)modeProp.enumValueIndex;

            switch (mode)
            {
                case UnmaskMode.SameScene:
                    EditorGUILayout.PropertyField(sameSceneTargetProp);
                    break;

                case UnmaskMode.CrossSceneCanvas:
                    EditorGUILayout.PropertyField(fitTargetCanvasNameProp);
                    EditorGUILayout.PropertyField(fitTargetNameProp);
                    break;

                case UnmaskMode.CrossSceneNonCanvas:
                    EditorGUILayout.PropertyField(fitNonCanvasTargetNameProp);
                    EditorGUILayout.PropertyField(fitNonCanvasTargetCameraNameProp);
                    break;
            }

            // Draw the rest of the properties normally
            DrawPropertiesExcluding(
                serializedObject,
                "m_Script", // Unity auto field
                "m_Mode",
                "m_SameSceneTarget",
                "m_FitTargetCanvasName",
                "m_FitTargetName",
                "m_FitNonCanvasTargetName",
                "m_FitNonCanvasTargetCameraName"
            );

            serializedObject.ApplyModifiedProperties();
        }
    }
}
