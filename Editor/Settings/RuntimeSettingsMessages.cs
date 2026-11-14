namespace Unity.XR.CompositionLayers.Editor
{
    /// <summary>
    /// Why the <see cref="CompositionLayersRuntimeSettings"/> asset isn't set up for builds, and the settings page
    /// button that sets it up. Project Validation, the settings page and the build failure share them.
    /// </summary>
    static class RuntimeSettingsMessages
    {
        internal const string k_AssetFileName = nameof(CompositionLayersRuntimeSettings) + ".asset";

        // Where users find Preloaded Assets, since it's buried in the Player settings.
        internal const string k_PreloadedAssets =
            "Preloaded Assets (Project Settings > Player > Other Settings > Optimization)";

        internal const string k_NotPreloaded = k_AssetFileName + " needs to be added to " + k_PreloadedAssets + ".";

        const string k_SetUpWhere =
            "Project Settings > XR Plug-in Management > Composition Layers, or select Fix in Project Validation.";

        internal const string k_BuildIgnoresSettings = k_AssetFileName + " turns on the splash screen or Emulation " +
            "In Standalone, but this player would ignore it because it isn't set up. Set it up in " + k_SetUpWhere;

        internal const string k_BuildConflictingSettings = "More than one " + k_AssetFileName + " is in " +
            k_PreloadedAssets + ", and a player might use either. Set it up in " + k_SetUpWhere;

        internal const string k_BuildNotSetUp = k_AssetFileName + " isn't set up, so emulated composition layers " +
            "won't render in this player. Set it up in " + k_SetUpWhere;

        internal const string k_MigrateButton = "Migrate Settings Asset";
        internal const string k_CreateButton = "Create Settings Asset";
        internal const string k_SetUpButton = "Set Up Settings Asset";

        internal static string NeedsMigrating(RuntimeSettingsLocations locations)
        {
            return $"{k_AssetFileName} is in one of your project's Resources folders and needs to be migrated to " +
                $"{locations.DefaultFolder} and added to {k_PreloadedAssets}.";
        }

        internal static string Missing(RuntimeSettingsLocations locations)
        {
            return $"{k_AssetFileName} needs to be created in {locations.DefaultFolder} and added to " +
                $"{k_PreloadedAssets}.";
        }
    }
}
