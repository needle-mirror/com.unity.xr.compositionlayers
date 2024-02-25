#if UNITY_RENDER_PIPELINES_HDRENDER
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;

namespace Unity.XR.CompositionLayers.Emulation
{
    internal static class EmulationLayerHighDefinitionCustomPassImpl
    {
        internal static void Execute(CustomPassContext ctx, List<EmulatedLayerData> emulationLayers)
        {
            foreach (var layerData in emulationLayers)
            {
                EmulatedCameraData cameraData;
                if (EmulationLayerHighDefinitionCustomPassManager.Contains(ctx.hdCamera.camera, out cameraData) && layerData.IsSupported(ctx.hdCamera.camera))
                {
                    var commandArgs = new EmulatedLayerData.CommandArgs(cameraData);
                    layerData.AddToCommandBuffer(ctx.cmd, commandArgs);
                }
            }
        }
    }

    public class EmulationLayerHighDefinitionUnderlayCustomPass : CustomPass
    {
        internal static List<EmulatedLayerData> m_EmulationLayers = new List<EmulatedLayerData>();

        protected override void Execute(CustomPassContext ctx)
        {
            EmulationLayerHighDefinitionCustomPassImpl.Execute(ctx, m_EmulationLayers);
        }
    }

    public class EmulationLayerHighDefinitionOverlayCustomPass : CustomPass
    {
        internal static List<EmulatedLayerData> m_EmulationLayers = new List<EmulatedLayerData>();

        protected override void Execute(CustomPassContext ctx)
        {
            EmulationLayerHighDefinitionCustomPassImpl.Execute(ctx, m_EmulationLayers);
        }
    }

    internal static class EmulationLayerHighDefinitionCustomPassManager
    {
        static HashSet<EmulatedCameraData> m_ActiveCameras; // Copyed from EmulatedLayerProvider.m_ActiveCameras

        internal static void Bind(HashSet<EmulatedCameraData> activeCameras)
        {
            m_ActiveCameras = activeCameras;
        }

        internal static bool Contains(Camera camera, out EmulatedCameraData cameraData)
        {
            if (m_ActiveCameras != null)
            {
                foreach (var activeCamera in m_ActiveCameras)
                {
                    if (activeCamera.Camera == camera)
                    {
                        cameraData = activeCamera;
                        return true;
                    }
                }
            }

            cameraData = new EmulatedCameraData();
            return false;
        }

        internal static void Add(EmulatedLayerData layerData, int order)
        {
            if (order >= 0)
            {
                EmulationLayerHighDefinitionOverlayCustomPass.m_EmulationLayers.Add(layerData);
            }
            else
            {
                EmulationLayerHighDefinitionUnderlayCustomPass.m_EmulationLayers.Add(layerData);
            }
        }

        internal static void Clear()
        {
            EmulationLayerHighDefinitionUnderlayCustomPass.m_EmulationLayers.Clear();
            EmulationLayerHighDefinitionOverlayCustomPass.m_EmulationLayers.Clear();
        }
    }
}
#endif
