using Unity.XR.CoreUtils;
using UnityEngine;

namespace Unity.XR.CompositionLayers
{
    /// <summary>
    /// Settings class for composition layer emulation in standalone.
    /// </summary>
    [ScriptableSettingsPath("Assets/CompositionLayers/UserSettings")]
    public class CompositionLayersRuntimeSettings : ScriptableSettings<CompositionLayersRuntimeSettings>
    {
        [SerializeField]
        [Tooltip("Enable or disable emulation of composition layers in standalone builds.")]
        bool m_EmulationInStandalone = false;

        internal bool EmulationInStandalone => m_EmulationInStandalone;

    }
}
