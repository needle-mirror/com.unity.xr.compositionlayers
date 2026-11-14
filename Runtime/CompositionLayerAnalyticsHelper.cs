using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using Unity.XR.CompositionLayers.Extensions;
using Unity.Scripting.LifecycleManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.XR.CompositionLayers
{
    partial class CompositionLayerAnalyticsHelper
    {
        [NoAutoStaticsCleanup]
        internal static readonly Type k_UIMirrorComponentType = Type.GetType(
            "Unity.XR.CompositionLayers.UIInteraction.InteractableUIMirror, Unity.XR.CompositionLayers.UIInteraction");

        /// <summary>
        /// Reference to all the Layer Types and their usage in all scenes created in the project.
        /// </summary>
        internal static Dictionary<string, int> LayerTypesUsage => s_LayerTypesUsage;
        [AutoStaticsCleanup]
        static Dictionary<string, int> s_LayerTypesUsage = new();
        /// <summary>
        /// Tracks the number of ColorScaleAndBias components in the active scene
        /// </summary>
        internal static int TotalLayerTypesUsage => s_TotalLayerTypesUsage;
        [AutoStaticsCleanup]
        static int s_TotalLayerTypesUsage;

        /// <summary>
        /// Tracks the number of ColorScaleAndBias components in the active scene
        /// </summary>
        internal static int ColorScaleAndBiasExtensionsUsage => s_ColorScaleAndBiasExtensionsUsage;
        [AutoStaticsCleanup]
        static int s_ColorScaleAndBiasExtensionsUsage;

        /// <summary>
        /// Checks if the Splash Screen is enabled in the project
        /// </summary>
        internal static bool IsEnabledSplashScreen => s_IsEnabledSplashScreen;
        [AutoStaticsCleanup]
        static bool s_IsEnabledSplashScreen;

        /// <summary>
        /// Loops through all scenes in the Project Assets folder
        /// Checking each Scene for CompositionLayer components
        /// </summary>
        internal static void ProcessCompositionLayersAnalyticsData()
        {
            s_LayerTypesUsage = new Dictionary<string, int>();
            s_ColorScaleAndBiasExtensionsUsage = 0;
            s_TotalLayerTypesUsage = 0;

#if UNITY_EDITOR
            // Retrieve all scenes included in the build
            var scenePaths = new List<string>(EditorBuildSettings.scenes.Length);
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenePaths.Add(scene.path);
            }

            foreach (var scenePath in scenePaths)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                ProcessSceneLayerTypesUsage();
            }
#endif
            foreach (var layerTypeUsage in s_LayerTypesUsage.Values)
            {
                s_TotalLayerTypesUsage += layerTypeUsage;
            }

            s_IsEnabledSplashScreen = RuntimeSettingsLookup.Current.EnableSplashScreen;
        }

        /// <summary>
        /// Method to get the number of each type of CompositionLayer components in any given scene
        /// </summary>
        internal static void ProcessSceneLayerTypesUsage()
        {
            var manager = CompositionLayerManager.Instance;
            if (manager == null) return;

            foreach (var layer in manager.CompositionLayers)
            {
                if (layer == null)
                    continue;

                var layerDataType = layer.LayerData?.GetType();
                if (layerDataType == null)
                    continue;

                var layerTypeName = layerDataType.FullName;
                var extensions = layer.Extensions;

                // To prevent counting DefaultLayers automatically generated in each new scene.
                if (layerDataType == typeof(DefaultLayerData)
                    && layer.hideFlags.HasFlag(DefaultLayerData.DefaultLayerHideFlags))
                    continue;

                if (layerDataType == typeof(QuadLayerData) || layerDataType == typeof(CylinderLayerData))
                {
                    // Check for Interactable UI Component
                    if (layer.gameObject.GetComponent(k_UIMirrorComponentType) != null)
                    {
                        layerTypeName += ".InteractableUI";
                    }

                    // Check for AndroidSurface Texture
                    TexturesExtension texturesExtension = null;
                    foreach (var extension in extensions)
                    {
                        if (extension.GetType() == typeof(TexturesExtension))
                        {
                            texturesExtension = extension as TexturesExtension;
                            break;
                        }
                    }

                    if (texturesExtension != null
                        && texturesExtension.SourceTexture == TexturesExtension.SourceTextureEnum.AndroidSurface)
                    {
                        layerTypeName += ".AndroidSurface";
                    }
                }

                foreach (var extension in extensions)
                {
                    if (extension.GetType() == typeof(ColorScaleBiasExtension))
                    {
                        s_ColorScaleAndBiasExtensionsUsage++;
                        break;
                    }
                }

                if (s_LayerTypesUsage.ContainsKey(layerTypeName))
                {
                    s_LayerTypesUsage[layerTypeName]++;
                }
                else
                {
                    s_LayerTypesUsage[layerTypeName] = 1;
                }
            }
        }
    }
}
