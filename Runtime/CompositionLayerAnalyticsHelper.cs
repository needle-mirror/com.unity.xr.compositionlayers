using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using Unity.XR.CompositionLayers.Extensions;
#if LIFECYCLE_APIS_AVAILABLE
using Unity.Scripting.LifecycleManagement;
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.XR.CompositionLayers
{
    internal partial class CompositionLayerAnalyticsHelper
    {
#if LIFECYCLE_APIS_AVAILABLE
        [NoAutoStaticsCleanup]
#endif
        internal static readonly Type k_UIMirrorComponentType = System.Reflection.Assembly.Load("Unity.XR.CompositionLayers.UIInteraction").GetType("Unity.XR.CompositionLayers.UIInteraction.InteractableUIMirror");
        /// <summary>
        /// Reference to all the Layer Types and their usage in all scenes created in the project.
        /// </summary>
        internal static Dictionary<string, int> LayerTypesUsage
        {
            get => m_layerTypesUsage;
        }
#if LIFECYCLE_APIS_AVAILABLE
        [AutoStaticsCleanup]
#endif
        private static Dictionary<string, int> m_layerTypesUsage = new Dictionary<string, int>();
        /// <summary>
        /// Tracks the number of ColorScaleAndBias components in the active scene
        /// </summary>
        internal static int TotalLayerTypesUsage
        {
            get => m_totalLayerTypesUsage;
        }
#if LIFECYCLE_APIS_AVAILABLE
        [AutoStaticsCleanup]
#endif
        private static int m_totalLayerTypesUsage = 0;
        /// <summary>
        /// Tracks the number of ColorScaleAndBias components in the active scene
        /// </summary>
        internal static int ColorScaleAndBiasExtensionsUsage
        {
            get => m_colorScaleAndBiasExtensionsUsage;
        }
#if LIFECYCLE_APIS_AVAILABLE
        [AutoStaticsCleanup]
#endif
        private static int m_colorScaleAndBiasExtensionsUsage = 0;

        /// <summary>
        /// Checks if the Splash Screen is enabled in the project
        /// </summary>
        internal static bool IsEnabledSplashScreen
        {
            get => m_isEnabledSplashScreen;
        }
#if LIFECYCLE_APIS_AVAILABLE
        [AutoStaticsCleanup]
#endif
        private static bool m_isEnabledSplashScreen = false;

        /// <summary>
        /// Loops through all scenes in the Project Assets folder
        /// Checking each Scene for CompositionLayer components
        /// </summary>
        internal static void ProcessCompositionLayersAnalyticsData()
        {
            m_layerTypesUsage = new Dictionary<string, int>();
            m_colorScaleAndBiasExtensionsUsage = 0;
            m_totalLayerTypesUsage = 0;

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
            foreach (var layerTypeUsage in m_layerTypesUsage.Values)
            {
                m_totalLayerTypesUsage += layerTypeUsage;
            }

            m_isEnabledSplashScreen = CompositionLayersRuntimeSettings.Instance.EnableSplashScreen;
        }

        /// <summary>
        /// Method to get the number of each type of CompositionLayer components in any given scene
        /// </summary>
        /// <param name="scene"></param>
        internal static void ProcessSceneLayerTypesUsage()
        {
            var manager = CompositionLayerManager.Instance;
            if (manager == null) return;

            foreach (CompositionLayer layer in manager.CompositionLayers)
            {
                if (layer == null)
                    continue;

                var layerDataType = layer.LayerData?.GetType();
                if (layerDataType == null)
                    continue;

                var layerTypeName = layerDataType.FullName;
                var extensions = layer.Extensions;

                // To prevent countinug DefaultLayers automatically generated in each new scene.
                if ((layerDataType == typeof(DefaultLayerData)) && layer.hideFlags.HasFlag(DefaultLayerData.DefaultLayerHideFlags))
                { continue; }

                if ((layerDataType == typeof(QuadLayerData) || layerDataType == typeof(CylinderLayerData)))
                {
                    // Check for Ineractable UI Component
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

                    if (texturesExtension != null && (texturesExtension.sourceTexture ==
                                                      TexturesExtension.SourceTextureEnum.AndroidSurface))
                    {
                        layerTypeName += ".AndroidSurface";
                    }
                }

                foreach (var extension in extensions)
                {
                    if (extension.GetType() == typeof(ColorScaleBiasExtension))
                    {
                        m_colorScaleAndBiasExtensionsUsage++;
                        break;
                    }
                }

                if (m_layerTypesUsage.ContainsKey(layerTypeName))
                {
                    m_layerTypesUsage[layerTypeName]++;
                }
                else
                {
                    m_layerTypesUsage[layerTypeName] = 1;
                }
            }
        }
    }
}
