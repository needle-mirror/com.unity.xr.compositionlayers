#if UNITY_RENDER_PIPELINES_HDRENDER
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

#pragma warning disable CS0618

namespace Unity.XR.CompositionLayers.Emulation
{
    internal static class EmulationLayerHighDefinitionCustomPassImpl
    {
        internal static void Execute(CustomPassContext ctx, List<EmulatedLayerData> emulationLayers)
        {
            var camera = ctx.hdCamera.camera;
            var commandArgs = new EmulatedLayerData.CommandArgs(camera);
            ctx.cmd.DisableShaderKeyword("COMPOSITION_LAYERS_OVERRIDE_SHADER_VARIABLES_GLOBAL");

            // The custom pass volumes are global, so this runs for every camera, including UI, capture, and
            // projection rig cameras. Only the cameras the provider selected may draw emulated layers, which is the
            // same pairing the Universal pass applies.
            if (!EmulatedLayerProvider.IsSupported(camera))
                return;

            foreach (var layerData in emulationLayers)
            {
                if (layerData.IsSupported(camera))
                {
                    layerData.AddToCommandBuffer(ctx.cmd, commandArgs);
                }
            }
        }
    }

    /// <summary>
    /// Obsolete. High Definition Render Pipeline support for layer emulation and the Mirror View Renderer is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release. Use the Universal Render Pipeline.
    /// </summary>
    [System.Obsolete("High Definition Render Pipeline support for layer emulation and the Mirror View Renderer is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release. Use the Universal Render Pipeline.")]
    public class EmulationLayerHighDefinitionUnderlayCustomPass : CustomPass
    {
        protected override void Execute(CustomPassContext ctx)
        {
            EmulationLayerHighDefinitionCustomPassImpl.Execute(ctx, EmulationLayerScriptableRendererManager.GetEmulatedLayerDataList(false));
        }
    }

    /// <summary>
    /// Obsolete. High Definition Render Pipeline support for layer emulation and the Mirror View Renderer is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release. Use the Universal Render Pipeline.
    /// </summary>
    [System.Obsolete("High Definition Render Pipeline support for layer emulation and the Mirror View Renderer is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release. Use the Universal Render Pipeline.")]
    public class EmulationLayerHighDefinitionOverlayCustomPass : CustomPass
    {
        protected override void Execute(CustomPassContext ctx)
        {
            EmulationLayerHighDefinitionCustomPassImpl.Execute(ctx, EmulationLayerScriptableRendererManager.GetEmulatedLayerDataList(true));
        }
    }

    [ExecuteInEditMode]
    internal partial class EmulationLayerHighDefinitionVolumeManager : MonoBehaviour
    {
        [AutoStaticsCleanup]
        static EmulationLayerHighDefinitionVolumeManager _instance = null;

        static void InternalDestroy(Object obj)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        static EmulationLayerHighDefinitionVolumeManager instance
        {
            get
            {
                if (_instance == null)
                {
                    var instances = Resources.FindObjectsOfTypeAll<EmulationLayerHighDefinitionVolumeManager>();
                    if (instances != null && instances.Length > 0)
                    {
                        _instance = instances[0];
                        for (int i = 1; i < instances.Length; ++i)
                        {
                            InternalDestroy(instances[i]);
                        }
                    }
                    else
                    {
                        var gameObject = new GameObject(typeof(EmulationLayerHighDefinitionVolumeManager).ToString());
                        gameObject.hideFlags = HideFlags.HideAndDontSave;
                        if (Application.isPlaying)
                            GameObject.DontDestroyOnLoad(gameObject);

                        _instance = gameObject.AddComponent<EmulationLayerHighDefinitionVolumeManager>();
                    }
                }

                return _instance;
            }
        }

        public static void ActivateCustomPassVolumes()
        {
            bool foundOverlay = false, foundUnderlay = false;
            var volumes = instance.GetComponents<CustomPassVolume>();
            if (volumes != null)
            {
                foreach (var volume in volumes)
                {
                    var customPasses = volume.customPasses;
                    foreach (var customPass in customPasses)
                    {
                        if (customPass is EmulationLayerHighDefinitionUnderlayCustomPass)
                        {
                            foundUnderlay = true;
                        }
                        else if (customPass is EmulationLayerHighDefinitionOverlayCustomPass)
                        {
                            foundOverlay = true;
                        }
                    }
                }
            }

            if (!foundUnderlay)
            {
                var volume = instance.gameObject.AddComponent<CustomPassVolume>();
                volume.isGlobal = true;
                volume.injectionPoint = CustomPassInjectionPoint.BeforeRendering;
                volume.customPasses.Add(new EmulationLayerHighDefinitionUnderlayCustomPass());
            }
            if (!foundOverlay)
            {
                var volume = instance.gameObject.AddComponent<CustomPassVolume>();
                volume.isGlobal = true;
                volume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
                volume.customPasses.Add(new EmulationLayerHighDefinitionOverlayCustomPass());
            }
        }

        public static void DeactivateCustomPassVolumes()
        {
            if (_instance)
            {
                var volumes = _instance.GetComponents<CustomPassVolume>();
                if (volumes != null)
                {
                    foreach (var volume in volumes)
                    {
                        InternalDestroy(volume);
                    }
                }

                InternalDestroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}

#pragma warning restore CS0618
#endif
