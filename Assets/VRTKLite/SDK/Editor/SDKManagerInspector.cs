using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace VRTKLite.SDK.Editor
{
    [CustomEditor(typeof(SDKManager))]
    public class SDKManagerInspector : UnityEditor.Editor
    {
        ReorderableList setupsList;

        void OnEnable()
        {
            SDKManager sdkManager = target as SDKManager;
            if (sdkManager == null) { return; }

            setupsList = new ReorderableList(
                serializedObject,
                serializedObject.FindProperty("Setups"))
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "SDK Setups");
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty element =
                        setupsList
                            .serializedProperty
                            .GetArrayElementAtIndex(index);

                    rect.y += 2;
                    rect.height = EditorGUIUtility.singleLineHeight;

                    EditorGUI.PropertyField(
                        rect,
                        element,
                        GUIContent.none);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            setupsList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        void OnDisable()
        {
            setupsList = null;
        }
    }
}