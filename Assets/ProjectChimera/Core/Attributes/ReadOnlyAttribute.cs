using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectChimera.Core
{
    /// <summary>
    /// Attribute to make fields read-only in the Unity Inspector
    /// Used for displaying runtime statistics and debug information
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR

    /// <summary>
    /// Property drawer for ReadOnly attribute
    /// Makes fields appear grayed out and non-editable in the inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var previousGUIState = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = previousGUIState;
        }
    }
#endif
}