using System;
using System.Collections.Generic;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using Unity.XR.CoreUtils;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace Unity.XR.CompositionLayers.Emulation
{
    static class EmulatedCompositionLayerUtils
    {
        [NoAutoStaticsCleanup]
        static readonly Dictionary<Type, Type> k_EmulatedLayerDataTypes = new();

        internal static bool IsRuntimeEmulationSupported()
        {
            if (!Application.isPlaying)
                return true;

#if ENABLE_UNITY_XR
            return !CompositionLayerUtils.IsDisplaySubsystemActive();
#else
            return true;
#endif
        }

        /// <summary>
        /// The Default Scene Layer's Color Scale and Bias to apply through emulation, or null when none applies.
        /// </summary>
        internal static ColorScaleBiasExtension GetEmulatedColorScaleBias(
            CompositionLayer defaultLayer, CameraType cameraType)
        {
            if (defaultLayer == null || !IsColorScaleBiasEmulated(defaultLayer, cameraType))
                return null;

            var colorScaleBiasExtension = defaultLayer.GetComponent<ColorScaleBiasExtension>();
            if (colorScaleBiasExtension == null || !colorScaleBiasExtension.isActiveAndEnabled)
                return null;

            return colorScaleBiasExtension;
        }

        // Edit mode and the Scene View preview the tint whatever the render path, as they do emulated layers.
        static bool IsColorScaleBiasEmulated(CompositionLayer defaultLayer, CameraType cameraType)
        {
            return !Application.isPlaying ||
                cameraType == CameraType.SceneView ||
                defaultLayer.RenderPath == CompositionLayerRenderPath.Emulated;
        }

        /// <summary>
        /// Whether emulated layers may draw. False only in a standalone player with no XR display running and the
        /// deprecated <c>EmulationInStandalone</c> setting off.
        /// </summary>
        internal static bool IsEmulationAllowedInPlayer(
            bool isStandalonePlayer, bool isXRDisplayActive, bool emulationInStandalone)
        {
            return !isStandalonePlayer || isXRDisplayActive || emulationInStandalone;
        }

        /// <summary>
        /// The deprecated Emulation In Standalone setting that the standalone player gate reads.
        /// </summary>
        internal static bool IsEmulationInStandaloneEnabled =>
            RuntimeSettingsLookup.Current.IsEmulationInStandaloneEnabled;

        /// <summary>
        /// Whether emulated layers may draw on the current platform, as decided by
        /// <see cref="IsEmulationAllowedInPlayer(bool, bool, bool)"/>.
        /// </summary>
        internal static bool IsEmulationAllowedInPlayer()
        {
#if !UNITY_EDITOR && UNITY_STANDALONE
            return IsEmulationAllowedInPlayer(true, IsXRDisplayActive(), IsEmulationInStandaloneEnabled);
#else
            return true;
#endif
        }

#if !UNITY_EDITOR && UNITY_STANDALONE
        static bool IsXRDisplayActive()
        {
#if ENABLE_UNITY_XR
            return CompositionLayerUtils.IsDisplaySubsystemActive();
#else
            return false;
#endif
        }
#endif

        static EmulatedCompositionLayerUtils()
        {
            GetEmulatedLayerDataTypes();
        }

        static void GetEmulatedLayerDataTypes()
        {
            var emulatedLayerDataTypes = new List<Type>();
            typeof(EmulatedLayerData).GetExtensionsOfClass(emulatedLayerDataTypes);
            foreach (var emulatedLayerDataType in emulatedLayerDataTypes)
            {
                if (emulatedLayerDataType.IsAbstract)
                    continue;

                var attributes = emulatedLayerDataType.GetCustomAttributes(typeof(EmulatedLayerDataTypeAttribute), false) as EmulatedLayerDataTypeAttribute[];
                if (attributes == null || attributes.Length == 0)
                {
                    throw new Exception($"{emulatedLayerDataType.FullName} requires an `EmulatedLayerDataTypeAttribute` to be used!");
                }

                foreach (var attribute in attributes)
                {
                    if (!k_EmulatedLayerDataTypes.TryAdd(attribute.LayerDataType, emulatedLayerDataType))
                    {
                        k_EmulatedLayerDataTypes.TryGetValue(attribute.LayerDataType, out var associatedType);
                        throw new Exception($"Cannot use {attribute.LayerDataType} with {emulatedLayerDataType.FullName}. The type is already using ${associatedType}!");
                    }
                }
            }
        }

        internal static Type GetEmulatedLayerDataType<T>() where T : LayerData
        {
            return GetEmulatedLayerDataType(typeof(T));
        }

        internal static Type GetEmulatedLayerDataType(Type type)
        {
            if (type == null)
                return null;

            k_EmulatedLayerDataTypes.TryGetValue(type, out var emulatedLayerDataType);
            return emulatedLayerDataType;
        }
    }
}
