using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.XR.CompositionLayers.Rendering.Editor
{
    public static class GraphicsSettingsHelpers
    {
        public enum ShaderType
        {
            BlitCopyHDR,
            Uber,
        }

        static readonly string[] s_ShaderNames = new string[]
        {
            "Unlit/XRCompositionLayers/BlitCopyHDR",
            "Unlit/XRCompositionLayers/Uber",
        };

        public static void AddAlwaysIncludedShaders(ShaderType shaderType)
        {
            AddAlwaysIncludedShader(s_ShaderNames[(int)shaderType]);
        }

        static GraphicsSettings s_GraphicsSettings;

        static void AddAlwaysIncludedShader(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"Shader not found: {shaderName}");
                return;
            }

            if (s_GraphicsSettings == null)
            {
                s_GraphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
                if (s_GraphicsSettings == null)
                    return;
            }

            var graphicsSettingsSerializedObject = new SerializedObject(s_GraphicsSettings);
            var alwaysIncludedShadersSerializedProperty = graphicsSettingsSerializedObject.FindProperty("m_AlwaysIncludedShaders");
            if (alwaysIncludedShadersSerializedProperty == null)
                return;

            int arraySize = alwaysIncludedShadersSerializedProperty.arraySize;

            for (int arrayIndex = 0; arrayIndex < arraySize; ++arrayIndex)
            {
                if (alwaysIncludedShadersSerializedProperty.GetArrayElementAtIndex(arrayIndex)?.objectReferenceValue == shader)
                {
                    return;
                }
            }

            alwaysIncludedShadersSerializedProperty.InsertArrayElementAtIndex(arraySize);
            alwaysIncludedShadersSerializedProperty.GetArrayElementAtIndex(arraySize).objectReferenceValue = shader;
            graphicsSettingsSerializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }
}
