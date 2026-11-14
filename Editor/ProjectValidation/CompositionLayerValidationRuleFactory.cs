#if UNITY_EDITOR
namespace UnityEditor.XR.CompositionLayers.Editor.ProjectValidation
{
#pragma warning disable CS0618
    /// <summary>
    /// Validation rule generator for shared validation rules between multiple features.
    /// </summary>
    static class CompositionLayerBuildValidationRuleFactory
    {
        const string k_PreferencesUpgradeGuideUrl =
            "https://docs.unity3d.com/Packages/com.unity.xr.compositionlayers@7.0/" +
            "manual/upgrade-guide-7.0.html#composition-layers-preferences";

        internal static CompositionLayerBuildValidationRule CreateLeftoverPreferencesRule(
            CompositionLayersPreferencesCheck check)
        {
            return new CompositionLayerBuildValidationRule
            {
                Message = "The CompositionLayersPreferences asset is deprecated as of Composition Layers " +
                    "7.0.0-pre.1. Its settings, \"Emulation in Scene\", \"Emulation In Play Mode\" and " +
                    "\"Display Angles As\", are no longer used, and the asset is safe to delete.",
                CheckPredicate = () => !check.HasLeftoverAssets(),
                FixIt = check.DeleteLeftoverAssets,
                FixItAutomatic = true,
                FixItMessage = "Delete every CompositionLayersPreferences asset in the project.",
                HelpLink = k_PreferencesUpgradeGuideUrl,
                Error = false,
                requiresActiveManager = false,
            };
        }

#if UNITY_RENDER_PIPELINES_UNIVERSAL
        internal static CompositionLayerBuildValidationRule CreateHDRRuleForURP(BuildTargetGroup buildTargetGroup)
        {
            return new CompositionLayerBuildValidationRule
            {
                Message = "Disable HDR on Universal Render Pipeline Asset to enable composition layer support.",
                CheckPredicate = () =>
                {
                    foreach (var urpAsset in CompositionLayerProjectValidation.GetURPAssetsForBuildTarget(buildTargetGroup))
                        if (urpAsset != null && urpAsset.supportsHDR)
                            return false;

                    return true;
                },
                FixIt = () => CompositionLayerProjectValidation.DisableHDROnURPAssets(buildTargetGroup),
                FixItAutomatic = true,
                FixItMessage = "Open Graphics Settings to disable HDR on Universal Render Pipeline Asset.",
                Error = false,
                buildTargetGroup = buildTargetGroup
            };
        }

        internal static CompositionLayerBuildValidationRule CreateAlphaProcessingRuleForURP(BuildTargetGroup buildTargetGroup)
        {
            return new CompositionLayerBuildValidationRule
            {
                Message = "Enable **Post-processing** > **Alpha processing** on your Universal Render Pipeline asset if your project uses transparent composition layers (Quad, Cylinder, Equirect, Cubemap, or Projection Eye Rig). If your project only uses Passthrough, you can leave it disabled for better performance.",
                CheckPredicate = () =>
                {
                    foreach (var urpAsset in CompositionLayerProjectValidation.GetURPAssetsForBuildTarget(buildTargetGroup))
                        if (urpAsset != null && !urpAsset.allowPostProcessAlphaOutput)
                            return false;

                    return true;
                },
                FixIt = () => CompositionLayerProjectValidation.OpenFirstURPAssetWithAlphaOutputOff(buildTargetGroup),
                FixItAutomatic = false,
                FixItMessage = "Open the Universal Render Pipeline Asset to enable/disable the Alpha Processing setting under Post Processing.",
                Error = false,
                buildTargetGroup = buildTargetGroup
            };
        }
#endif
    }
#pragma warning restore CS0618
}
#endif
