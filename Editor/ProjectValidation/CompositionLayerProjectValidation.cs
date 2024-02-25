#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.XR.CompositionLayers;

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
                Message = "Install OpenXR Plugin Experimental package 1.11.0-exp.1 to enable composition layer runtime support.",
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
            }
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