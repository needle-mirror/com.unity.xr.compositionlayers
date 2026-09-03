//#define DEBUG_USE_EMULATED_PLATFORM_PROVIDER

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Provider;
using Unity.XR.CompositionLayers.Emulation;
#if LIFECYCLE_APIS_AVAILABLE
using Unity.Scripting.LifecycleManagement;
#endif

namespace Unity.XR.CompositionLayers.Services.Editor
{
    /// <summary>
    /// Managing All PlatformProvider/PlatformLayerData for Editor.
    /// </summary>
    [InitializeOnLoad]
    static class EditorPlatformManager
    {
        /// <summary>
        /// All active PlatformProvider. It includes all platforms which are inactive build targets.
        /// </summary>
        internal static IReadOnlyList<PlatformProvider> ActivePlatformProviders { get => s_ActivePlatformProviders; }

        /// <summary>
        /// All active PlatformLayerData. It includes all platforms which are inactive build targets.
        /// </summary>
        internal static IReadOnlyList<Type> ActivePlatformLayerDataTypes { get => s_ActivePlatformLayerDataTypes; }

        internal static IReadOnlyList<PlatformProvider> SupportingHDRProviders { get => s_SupportingHDRProviders; }

#if LIFECYCLE_APIS_AVAILABLE
        [NoAutoStaticsCleanup]
#endif
        static List<PlatformProvider> s_ActivePlatformProviders;
#if LIFECYCLE_APIS_AVAILABLE
        [NoAutoStaticsCleanup]
#endif
        static List<Type> s_ActivePlatformLayerDataTypes;
#if LIFECYCLE_APIS_AVAILABLE
        [NoAutoStaticsCleanup]
#endif
        static List<PlatformProvider> s_SupportingHDRProviders;

        static EditorPlatformManager()
        {
            AssemblyReloadEvents.afterAssemblyReload += Refresh;
            Refresh();
        }

        static void Refresh()
        {
            s_ActivePlatformProviders = GetActivePlatformProviders();
            s_ActivePlatformLayerDataTypes = GetActivePlatformLayerDataTypes();

            if (s_ActivePlatformProviders.Count > 0)
            {
                foreach (var layerProviderType in GetActiveLayerProviderTypes())
                {
                    if (s_ActivePlatformProviders.Find(x => x.LayerProviderType == layerProviderType) == null)
                    {
                        s_ActivePlatformProviders.Add(new DefaultPlatformProvider(layerProviderType));
                    }
                }
            }
            else
            {
                s_ActivePlatformProviders.Add(new DefaultPlatformProvider(typeof(EmulatedLayerProvider)));
            }

            RefreshSupportingHDRProviders();
        }

        static void RefreshSupportingHDRProviders()
        {
            if (s_SupportingHDRProviders == null)
                s_SupportingHDRProviders = new List<PlatformProvider>();
            else
                s_SupportingHDRProviders.Clear();

            var activePlatformProviders = ActivePlatformProviders;
            if (activePlatformProviders != null)
            {
                foreach (var provider in activePlatformProviders)
                {
                    if (!provider.IsInternal() && provider.IsSupportedHDR)
                        s_SupportingHDRProviders.Add(provider);
                }
            }

            PlatformManager.IsSupportedHDR = s_SupportingHDRProviders.Count > 0;
        }

        static List<PlatformProvider> GetActivePlatformProviders()
        {
            var platformProviderTypes = TypeCache.GetTypesDerivedFrom(typeof(PlatformProvider)).ToList();
            platformProviderTypes.RemoveAll(x => x == typeof(DefaultPlatformProvider));
            var platformProviders = new List<PlatformProvider>();
            platformProviderTypes.ForEach(x => platformProviders.Add(Activator.CreateInstance(x) as PlatformProvider));
            return platformProviders;
        }

        static List<Type> GetActivePlatformLayerDataTypes()
        {
            return TypeCache.GetTypesDerivedFrom(typeof(PlatformLayerData)).ToList();
        }

        static List<Type> GetActiveLayerProviderTypes()
        {
            var layerProviderTypes = TypeCache.GetTypesDerivedFrom(typeof(ILayerProvider)).ToList();
#if !DEBUG_USE_EMULATED_PLATFORM_PROVIDER
            layerProviderTypes.RemoveAll(x => x == typeof(EmulatedLayerProvider));
#endif
            return layerProviderTypes;
        }

        /// <summary>
        /// Check for supporting LayerData on target PlatformProvider.
        /// </summary>
        /// <param name="platforProvider">Target PlatformProvider.</param>
        /// <param name="layerData">Target LayerData.</param>
        /// <returns>If layerData is supported, return true. If not, return false.</returns>
        public static bool IsSupportedLayerData(this PlatformProvider platforProvider, LayerData layerData)
        {
            if (layerData == null)
                return true;

            var supportedLayerDataTypes = platforProvider.SupportedLayerDataTypes;
            if (supportedLayerDataTypes == null)
                return true;

            return supportedLayerDataTypes.Contains(layerData.GetType());
        }

        /// <summary>
        /// Check for supporting LayerData(class name) on target PlatformProvider.
        /// </summary>
        /// <param name="platforProvider">Target PlatformProvider.</param>
        /// <param name="layerDataFullName">Target LayerData class name.</param>
        /// <returns>If layerData is supported, return true. If not, return false.</returns>
        public static bool IsSupportedLayerData(this PlatformProvider platforProvider, string layerDataFullName)
        {
            if (string.IsNullOrEmpty(layerDataFullName))
                return true;

            var supportedLayerDataTypes = platforProvider.SupportedLayerDataTypes;
            if (supportedLayerDataTypes == null)
                return true;

            return supportedLayerDataTypes.FirstOrDefault(x => x.FullName == layerDataFullName) != null;
        }

        /// <summary>
        /// Check PlatformProvider attribute which is internal.
        /// </summary>
        /// <param name="platformProvider">Target platform provider. Need to return LayerProviderType.</param>
        /// <return>Display name string.</return>
        internal static bool IsInternal(this PlatformProvider provider)
        {
            var layerProviderType = provider.LayerProviderType;
            if (layerProviderType != null)
            {
                return layerProviderType == typeof(Rendering.MirrorViewLayerProvider) || layerProviderType == typeof(Emulation.EmulatedLayerProvider);
            }

            return false;
        }

        /// <summary>
        /// Check for supporting LayerData(class name) on All PlatformProvider.
        /// </summary>
        /// <param name="layerDataFullName">Target LayerData class name.</param>
        /// <returns>If layerData is supported, return true. If not, return false.</returns>
        public static bool IsSupportedLayerDataAllPlatforms(string layerDataFullName)
        {
            if (string.IsNullOrEmpty(layerDataFullName))
                return true;

            var activePlatformProviders = s_ActivePlatformProviders;
            if (activePlatformProviders == null || activePlatformProviders.Count == 0)
                return true;

            int count = 0;
            foreach (var provider in activePlatformProviders)
            {
                if (provider.IsSupportedLayerData(layerDataFullName))
                    ++count;
            }

            return count == activePlatformProviders.Count;
        }
    }
}
