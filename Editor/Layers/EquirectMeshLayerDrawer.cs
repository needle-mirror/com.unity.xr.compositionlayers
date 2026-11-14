using System;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace Unity.XR.CompositionLayers.Layers.Editor
{
    [CustomPropertyDrawer(typeof(EquirectMeshLayerData))]
    partial class EquirectMeshLayerDrawer : LayerDataDrawer
    {
        class Styles
        {
            internal GUIContent centralHorizontalAngleDegreesContent;
            internal GUIContent upperVerticalAngleDegreesContent;
            internal GUIContent lowerVerticalAngleDegreesContent;

            internal Styles()
            {
                centralHorizontalAngleDegreesContent = new GUIContent(k_CentralHorizontalAngleNameDegrees, k_CentralHorizontalAngleDegreesToolTip);
                upperVerticalAngleDegreesContent = new GUIContent(k_UpperVerticalAngleNameDegrees, k_UpperVerticalAngleDegreesToolTip);
                lowerVerticalAngleDegreesContent = new GUIContent(k_LowerVerticalAngleNameDegrees, k_LowerVerticalAngleDegreesToolTip);
            }
        }

        const string k_AngleDegrees = " (deg)";

        const string k_CentralHorizontalAnglePropertyName = "m_CentralHorizontalAngle";
        const string k_UpperVerticalAnglePropertyName = "m_UpperVerticalAngle";
        const string k_LowerVerticalAnglePropertyName = "m_LowerVerticalAngle";
        const string k_RadiusPropertyName = "m_Radius";
        const string k_SerializedVersionPropertyName = "m_SerializedVersion";

        const string k_CentralHorizontalAngleName = "Central Hori. Angle";
        const string k_CentralHorizontalAngleNameDegrees = k_CentralHorizontalAngleName + k_AngleDegrees;
        const string k_CentralHorizontalAngleDegreesToolTip = "The visible horizontal angle of the sphere, based at 0 " +
            "degrees, in the range of [0, 360]. It grows symmetrically around the 0 degree angle.";

        const string k_UpperVerticalAngleName = "Upper Vert. Angle";
        const string k_UpperVerticalAngleNameDegrees = k_UpperVerticalAngleName + k_AngleDegrees;
        const string k_VerticalAngleSignToolTip = "Positive values are above the equator (toward the north pole); " +
            "negative values are below it (toward the south pole).";

        const string k_UpperVerticalAngleDegreesToolTip = "The upper vertical angle of the visible portion of the " +
            "sphere, in the range of [-90, 90]. " + k_VerticalAngleSignToolTip;

        const string k_LowerVerticalAngleName = "Lower Vert. Angle";
        const string k_LowerVerticalAngleNameDegrees = k_LowerVerticalAngleName + k_AngleDegrees;
        const string k_LowerVerticalAngleDegreesToolTip = "The lower vertical angle of the visible portion of the " +
            "sphere, in the range of [-90, 90]. " + k_VerticalAngleSignToolTip;

        const string k_UpperLowerOverlapWarning =
            "Upper Vert. Angle should be greater than Lower Vert. Angle. The layer may not display correctly otherwise.";

        [AutoStaticsCleanup]
        static Styles s_Styles;
        static Styles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new Styles();

                return s_Styles;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null || property.propertyType != SerializedPropertyType.ManagedReference || property.managedReferenceValue == null)
                return; // Fix crash on obsolete properties.

            var propertyCopy = property.Copy();
            if (propertyCopy.managedReferenceValue is not EquirectMeshLayerData equirectMeshLayerData)
                return;

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.BeginProperty(position, label, property);
                EditorGUI.LabelField(position, label);
                EditorGUI.indentLevel++;

                var enumerator = property.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var currentProperty = enumerator.Current as SerializedProperty;
                    if (currentProperty == null)
                        continue;

                    // Internal versioning field, not shown in the inspector.
                    if (currentProperty.name == k_SerializedVersionPropertyName)
                        continue;

                    if (currentProperty.name is k_CentralHorizontalAnglePropertyName or k_UpperVerticalAnglePropertyName or k_LowerVerticalAnglePropertyName)
                    {
                        if (currentProperty.name == k_CentralHorizontalAnglePropertyName)
                        {
                            currentProperty.floatValue = EditorGUILayout.Slider(
                                styles.centralHorizontalAngleDegreesContent,
                                equirectMeshLayerData.CentralHorizontalAngleInDegrees,
                                0f,
                                360f) * Mathf.Deg2Rad;
                        }
                        else if (currentProperty.name == k_UpperVerticalAnglePropertyName)
                        {
                            currentProperty.floatValue = EditorGUILayout.Slider(
                                styles.upperVerticalAngleDegreesContent,
                                equirectMeshLayerData.UpperVerticalAngleInDegrees,
                                -90f,
                                90f) * Mathf.Deg2Rad;
                        }
                        else
                        {
                            currentProperty.floatValue = EditorGUILayout.Slider(
                                styles.lowerVerticalAngleDegreesContent,
                                equirectMeshLayerData.LowerVerticalAngleInDegrees,
                                -90f,
                                90f) * Mathf.Deg2Rad;
                        }
                    }
                    else if (currentProperty.name is k_RadiusPropertyName)
                    {
                        // Limit radius to positive values
                        currentProperty.floatValue = Mathf.Max(0f,
                            EditorGUILayout.DelayedFloatField(
                                new GUIContent(currentProperty.displayName, currentProperty.tooltip),
                                currentProperty.floatValue));
                    }
                    else if (currentProperty.name is k_BlendTypePropertyName)
                    {
                        BlendTypePropertyField(currentProperty);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(currentProperty);
                    }
                }

                if (equirectMeshLayerData.UpperVerticalAngle <= equirectMeshLayerData.LowerVerticalAngle)
                    EditorGUILayout.HelpBox(k_UpperLowerOverlapWarning, MessageType.Warning);

                if (change.changed)
                    ApplyChangesWithReportStateChange(property);
            }

            EditorGUI.EndProperty();
            EditorGUI.indentLevel--;
        }
    }
}
