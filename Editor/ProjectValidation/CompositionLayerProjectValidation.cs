using Unity.Scripting.LifecycleManagement;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CompositionLayers;
using UnityEngine;

#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering.Universal;
using Unity.XR.CompositionLayers.Emulation;
#endif

namespace UnityEditor.XR.CompositionLayers.Editor.ProjectValidation
{
#pragma warning disable CS0618
    [System.Obsolete("CompositionLayerProjectValidation is not intended for use outside the Composition Layers package. It is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release.")]
    public static partial class CompositionLayerProjectValidation
    {
        // Declared before the rules that use it, since static fields initialize in order.
        [NoAutoStaticsCleanup]
        static readonly RuntimeSettingsValidation k_RuntimeSettingsValidation = new(
            RuntimeSettingsLocations.Default, PlayerSettings.GetPreloadedAssets, PlayerSettings.SetPreloadedAssets);

        [NoAutoStaticsCleanup]
        private static readonly CompositionLayerBuildValidationRule[] BuiltinValidationRules =
        {
            new CompositionLayerBuildValidationRule
            {
                Message = "OpenXR Plugin 7.0.0-pre.1 or later is required for composition layers at runtime.",
                CheckPredicate = () =>
                {
#if XR_OPENXR_7_0_0_PRE_1_OR_NEWER
                    return true;
#else
                    return false;
#endif
                },
                FixIt = OpenPackageManager,
                FixItAutomatic = false,
                FixItMessage = "Open Package Manager to install proper OpenXR package version.",
                Error = false,
                buildTargetGroup = BuildTargetGroup.Android,
            },
            RuntimeSettingsValidation.CreateMigrateRule(k_RuntimeSettingsValidation),
            RuntimeSettingsValidation.CreateMissingRule(k_RuntimeSettingsValidation),
            RuntimeSettingsValidation.CreateNotPreloadedRule(k_RuntimeSettingsValidation),
            RuntimeSettingsValidation.CreateOldCopyRule(k_RuntimeSettingsValidation),
            CompositionLayerBuildValidationRuleFactory.CreateLeftoverPreferencesRule(
                new CompositionLayersPreferencesCheck("Assets")),
#if UNITY_RENDER_PIPELINES_UNIVERSAL
            CompositionLayerBuildValidationRuleFactory.CreateHDRRuleForURP(BuildTargetGroup.Android),
            CompositionLayerBuildValidationRuleFactory.CreateAlphaProcessingRuleForURP(BuildTargetGroup.Android),
            CompositionLayerBuildValidationRuleFactory.CreateHDRRuleForURP(BuildTargetGroup.Standalone),
            CompositionLayerBuildValidationRuleFactory.CreateAlphaProcessingRuleForURP(BuildTargetGroup.Standalone),
#endif
        };

        [AutoStaticsCleanup]
        private static List<CompositionLayerBuildValidationRule> CachedValidationList = new List<CompositionLayerBuildValidationRule>(BuiltinValidationRules.Length);


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

#if UNITY_RENDER_PIPELINES_UNIVERSAL
        internal static void DisableHDROnURPAssets(BuildTargetGroup buildTargetGroup)
        {
            foreach (var urpAsset in GetURPAssetsForBuildTarget(buildTargetGroup))
            {
                if (urpAsset != null)
                {
                    urpAsset.supportsHDR = false;
                    EditorUtility.SetDirty(urpAsset);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        internal static void OpenFirstURPAssetWithAlphaOutputOff(BuildTargetGroup buildTargetGroup)
        {
            foreach (var urpAsset in GetURPAssetsForBuildTarget(buildTargetGroup))
            {

                if (urpAsset != null && !urpAsset.allowPostProcessAlphaOutput)
                {
                    Selection.activeObject = urpAsset;
                    EditorGUIUtility.PingObject(urpAsset);
                    return;
                }
            }
        }

        internal static HashSet<UniversalRenderPipelineAsset> GetURPAssetsForBuildTarget(BuildTargetGroup buildTargetGroup)
        {
            QualitySettings.GetRenderPipelineAssetsForPlatform<UniversalRenderPipelineAsset>(buildTargetGroup.ToString(), out var renderPipelineAssets);
            return renderPipelineAssets;
        }
#endif
    }
#pragma warning restore CS0618
}
#endif //UNITY_EDITOR
