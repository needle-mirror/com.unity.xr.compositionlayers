#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.XR.CompositionLayers;

#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering.Universal;
using Unity.XR.CompositionLayers.Emulation;
#endif

namespace UnityEditor.XR.CompositionLayers.Editor.ProjectValidation
{
    public static class CompositionLayerProjectValidation
    {
        private static readonly CompositionLayerBuildValidationRule[] BuiltinValidationRules =
        {
            new CompositionLayerBuildValidationRule
            {
                Message =
                    "Add the Unlit/XRCompositionLayers/Uber.Shader from Packages/XR Composition Layers/Runtime/Shaders folder to Project Settings/Graphics/Built-in Shader Settings/Always Include List for Emulation in Standalone Builds",
                CheckPredicate = () =>
                {
                    var compositionLayersSettings = CompositionLayersRuntimeSettings.Instance;
                    // Won't display the rule if there is no Standalone Emulation configuration or if Emulation is disabled.
                    if (compositionLayersSettings == null || !compositionLayersSettings.EmulationInStandalone)
                        return true;

                    return CheckForShader();
                },
                FixIt = OpenGraphicsSettings,
                FixItAutomatic = false,
                FixItMessage = "Open Project Settings to select one or more interaction profiles.",
                Error = true,
                buildTargetGroup = BuildTargetGroup.Standalone,
            },
            new CompositionLayerBuildValidationRule
            {
                Message = "Install OpenXR Plugin Experimental package 1.12.0-exp.1 to enable composition layer runtime support.",
                CheckPredicate = () =>
                {
#if UNITY_XR_OPENXR_COMPLAYER
                    return true;
#else
                    return false;
#endif
                },
                FixIt = OpenPackageManager,
                FixItAutomatic = false,
                FixItMessage = "Open Package Manager to install proper OpenXR package version.",
                Error = false,
            },
            new CompositionLayerBuildValidationRule
            {
                Message = 
                    "Add the EmulationLayerUniversalScriptableRendererFeature to the current Scriptable Render Pipeline's Renderer Features List to enable Composition Layer emulation.",
                CheckPredicate = () => 
                {
#if UNITY_RENDER_PIPELINES_UNIVERSAL
                    return CheckForRenderFeature();
#else
                    return true;
#endif
                },
#if UNITY_RENDER_PIPELINES_UNIVERSAL
                FixIt = () => AddEmulationLayerUniversalScriptableRendererFeature(),
                FixItAutomatic = true,
#else
                FixIt = null,
                FixItAutomatic = false,
#endif
                FixItMessage =  "Add EmulationLayerUniversalScriptableRendererFeature to the current Scriptable Render Pipeline's Renderer Features List.",
                Error = false,
            },
        };

        private static readonly List<CompositionLayerBuildValidationRule> CachedValidationList = new List<CompositionLayerBuildValidationRule>(BuiltinValidationRules.Length);
        private static readonly string EmulationShaderName = "Unlit/XRCompositionLayers/Uber";

        /// <summary>
        /// Checks for Shader in the Always Included Shaders list in Project Settings
        /// </summary>
        private static bool CheckForShader()
        {
            var graphicsSettings = Unsupported.GetSerializedAssetInterfaceSingleton("GraphicsSettings");
            var graphicsSettingsObject = new SerializedObject(graphicsSettings);
            var alwaysIncludedShaders = graphicsSettingsObject.FindProperty("m_AlwaysIncludedShaders");

            var shader = Shader.Find(EmulationShaderName);
            var shaderCount = alwaysIncludedShaders.arraySize;
            for (int i = 0; i < shaderCount; ++i)
            {
                if (alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks for Render Feature in the current Scriptable Render Pipeline
        /// </summary>
        /// <returns></returns>
        private static bool CheckForRenderFeature()
        {
#if UNITY_RENDER_PIPELINES_UNIVERSAL
            var renderData = GetDefaultRenderer();

            foreach (var feature in renderData.rendererFeatures)
                if (feature.GetType() == typeof(EmulationLayerUniversalScriptableRendererFeature))
                    return true;

            return false;
#else
            return true;
#endif
        }

#if UNITY_RENDER_PIPELINES_UNIVERSAL
        /// <summary>
        /// Adds the EmulationLayerUniversalScriptableRendererFeature to the current renderer
        /// </summary>
        /// <remarks>
        /// Some hackery is required as we can't directly modify the renderer data list.
        /// </remarks>
        private static void AddEmulationLayerUniversalScriptableRendererFeature()
        {
            // Get the default renderer
            ScriptableRendererData data = GetDefaultRenderer();

            // Create the EmulationLayerUniversalScriptableRendererFeature
            EmulationLayerUniversalScriptableRendererFeature feature = 
                ScriptableObject.CreateInstance<EmulationLayerUniversalScriptableRendererFeature>();
            feature.name = nameof(EmulationLayerUniversalScriptableRendererFeature);

            // Store the feature as a sub-asset
            if (EditorUtility.IsPersistent(data))
            {
                EditorUtility.SetDirty(feature);
                AssetDatabase.AddObjectToAsset(feature, data);
            }

            // Add the feature to the renderer
            data.rendererFeatures.Add(feature);

            // Invoke the private method "ValidateRendererFeatures" to validate the renderer features
            var method = data.GetType().GetMethod("ValidateRendererFeatures", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(data, null);

            // Force save the feature
            if (EditorUtility.IsPersistent(data))
            {
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
            }

            // Force domain reload to ensure the feature is properly added
            EditorUtility.RequestScriptReload();
        }

        /// <summary>
        /// Gets the renderer from the current pipeline asset that's marked as default
        /// </summary>
        /// <returns></returns>
        public static ScriptableRendererData GetDefaultRenderer()
        {
            if (UniversalRenderPipeline.asset)
            {
                int defaultRendererIndex = GetDefaultRendererIndex(UniversalRenderPipeline.asset);
                return GetRendererDataList(UniversalRenderPipeline.asset)[defaultRendererIndex];
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
                return null;
            }
        }

        /// <summary>
        /// Gets the default renderer index from the current pipeline asset
        /// </summary>
        private static int GetDefaultRendererIndex(UniversalRenderPipelineAsset asset)
        {
            return (int)typeof(UniversalRenderPipelineAsset)
                .GetField("m_DefaultRendererIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(asset);
        }

        /// <summary>
        /// Gets the renderer data list from the current pipeline asset
        /// Unity 2023.3+ uses a public property, while older versions use a private field, so we need to check for both.
        /// </summary>
        private static System.ReadOnlySpan<ScriptableRendererData> GetRendererDataList(UniversalRenderPipelineAsset asset)
        {
#if UNITY_2023_3_OR_NEWER
            return asset.rendererDataList;
#else
            return new System.ReadOnlySpan<ScriptableRendererData>((ScriptableRendererData[])typeof(UniversalRenderPipelineAsset)
                .GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(asset));
#endif
        }

#endif
        /// <summary>
        /// Opens Graphics Settings Window
        /// </summary>
        private static void OpenGraphicsSettings() => SettingsService.OpenProjectSettings("Project/Graphics");

        /// <summary>
        /// Retrieves All Validation Issues for a given build target group.
        /// </summary>
        /// <param name="issues">List of validation issues to populate. List is cleared before populating.</param>
        /// <param name="buildTarget">Build target group to check for validation issues</param>
        internal static void GetAllValidationIssues(List<CompositionLayerBuildValidationRule> issues, BuildTargetGroup buildTargetGroup)
        {
            issues.Clear();
            issues.AddRange(BuiltinValidationRules.Where(s => s.buildTargetGroup == buildTargetGroup || s.buildTargetGroup == BuildTargetGroup.Unknown));
        }

        /// <summary>
        /// Gathers and evaluates validation issues and adds them to a list if they fail their predicate check.
        /// </summary>
        /// <param name="issues">List of validation issues to populate. List is cleared before populating.</param>
        /// <param name="buildTarget">Build target group to check for validation issues</param>
        internal static void GetCurrentValidationIssues(List<CompositionLayerBuildValidationRule> issues, BuildTargetGroup buildTargetGroup)
        {
            CachedValidationList.Clear();
            CachedValidationList.AddRange(BuiltinValidationRules.Where(s => s.buildTargetGroup == buildTargetGroup || s.buildTargetGroup == BuildTargetGroup.Unknown));

            issues.Clear();
            foreach (var validation in CachedValidationList)
            {
                if (!validation.CheckPredicate?.Invoke() ?? false)
                {
                    issues.Add(validation);
                }
            }
        }

        /// <summary>
        /// Open the Package Manager window to install OpenXR package
        /// </summary>
        private static void OpenPackageManager() => UnityEditor.PackageManager.UI.Window.Open("com.unity.xr.openxr");
    }
}
#endif //UNITY_EDITOR