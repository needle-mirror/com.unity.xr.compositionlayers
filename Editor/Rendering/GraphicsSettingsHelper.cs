using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.XR.CompositionLayers.Rendering
{
    public static class GraphicsSettingsHelpers
    {
        public enum ShaderType
        {
            Uber,
        }

        static readonly string[] s_ShaderNames = new string[]
        {
            "Unlit/XRCompositionLayers/Uber",
        };

        public static bool AddAlwaysIncludedShaders(ShaderType shaderType)
        {
            return AddAlwaysIncludedShader(s_ShaderNames[(int)shaderType]);
        }

        static GraphicsSettings s_GraphicsSettings;

        static bool AddAlwaysIncludedShader(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"Shader not found: {shaderName}");
                return false;
            }

            if (s_GraphicsSettings == null)
            {
                s_GraphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
                if (s_GraphicsSettings == null)
                    return false;
            }

            var graphicsSettingsSerializedObject = new SerializedObject(s_GraphicsSettings);
            var alwaysIncludedShadersSerializedProperty = graphicsSettingsSerializedObject.FindProperty("m_AlwaysIncludedShaders");
            if (alwaysIncludedShadersSerializedProperty == null)
                return false;

            int arraySize = alwaysIncludedShadersSerializedProperty.arraySize;

            for (int arrayIndex = 0; arrayIndex < arraySize; ++arrayIndex)
            {
                if (alwaysIncludedShadersSerializedProperty.GetArrayElementAtIndex(arrayIndex)?.objectReferenceValue == shader)
                {
                    return false;
                }
            }

            alwaysIncludedShadersSerializedProperty.InsertArrayElementAtIndex(arraySize);
            alwaysIncludedShadersSerializedProperty.GetArrayElementAtIndex(arraySize).objectReferenceValue = shader;
            graphicsSettingsSerializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}