#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Unity.XR.CompositionLayers.Emulation
{
    internal static class EmulationLayerUniversalScriptableRendererManager
    {
        static Dictionary<Camera, List<CommandBuffer>> m_UnderlayCommandBuffers = new Dictionary<Camera, List<CommandBuffer>>();
        static Dictionary<Camera, List<CommandBuffer>> m_OverlayCommandBuffers = new Dictionary<Camera, List<CommandBuffer>>();

        internal static void AddCommandBuffer(Camera camera, CommandBuffer cmd, int order)
        {
            var commandBuffersDictionary = (order >= 0) ? m_OverlayCommandBuffers : m_UnderlayCommandBuffers;
            List<CommandBuffer> commandBuffers;
            if (!commandBuffersDictionary.TryGetValue(camera, out commandBuffers))
            {
                commandBuffers = new List<CommandBuffer>();
                commandBuffersDictionary.Add(camera, commandBuffers);
            }

            commandBuffers.Add(cmd);
        }

        internal static void Clear()
        {
            m_UnderlayCommandBuffers.Clear();
            m_OverlayCommandBuffers.Clear();
        }

        internal static List<CommandBuffer> GetCommandBuffers(Camera camera, bool isOverray)
        {
            Dictionary<Camera, List<CommandBuffer>> commandBuffersDictionary = isOverray ? m_OverlayCommandBuffers : m_UnderlayCommandBuffers;
            List<CommandBuffer> commandBuffers;
            if (commandBuffersDictionary.TryGetValue(camera, out commandBuffers))
            {
                return commandBuffers;
            }

            return null;
        }
    }
}
#endif
