#if UNITY_EDITOR
namespace Unity.XR.CompositionLayers
{
    /// <summary>
    /// Where the Editor looks for and creates the <see cref="CompositionLayersRuntimeSettings"/> asset.
    /// </summary>
    readonly struct RuntimeSettingsLocations
    {
        const string k_AssetFileName = nameof(CompositionLayersRuntimeSettings) + ".asset";
        const string k_ResourcesFolderName = "Resources";

        /// <summary>
        /// The locations in the user's project.
        /// </summary>
        internal static RuntimeSettingsLocations Default =>
            new("Assets/XR/Settings", "Assets/CompositionLayers/UserSettings/Resources", "Assets");

        /// <param name="defaultFolder">The folder a new asset is created in.</param>
        /// <param name="legacyFolder">The <c>Resources</c> folder Composition Layers 6.x created the asset in.</param>
        /// <param name="searchFolder">The folder searched by type when no other location has an asset.</param>
        internal RuntimeSettingsLocations(string defaultFolder, string legacyFolder, string searchFolder)
        {
            DefaultFolder = defaultFolder;
            LegacyFolder = legacyFolder;
            SearchFolder = searchFolder;
        }

        internal string DefaultFolder { get; }

        internal string LegacyFolder { get; }

        internal string SearchFolder { get; }

        internal string DefaultAssetPath => DefaultFolder + "/" + k_AssetFileName;

        /// <summary>
        /// Where Composition Layers 6.x created the asset.
        /// </summary>
        internal string LegacyAssetPath => LegacyFolder + "/" + k_AssetFileName;

        /// <summary>
        /// Whether an asset or folder path is a <c>Resources</c> folder or inside one, at any depth, as earlier
        /// versions kept the settings.
        /// </summary>
        internal static bool IsInResourcesFolder(string path)
        {
            foreach (var name in path.Split('/'))
            {
                if (name == k_ResourcesFolderName)
                    return true;
            }

            return false;
        }
    }
}
#endif
