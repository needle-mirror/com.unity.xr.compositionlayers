using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.XR.CompositionLayers.Emulation
{
    /// <summary>
    /// Kept only so scenes that saved this component on their main camera keep loading without a missing-script
    /// warning. Built-in Render Pipeline emulation was removed in Composition Layers 7.0.0-pre.1, so it does nothing.
    /// Remove it from the camera.
    /// </summary>
    [Obsolete("Built-in Render Pipeline emulation was removed in Composition Layers 7.0.0-pre.1. This component does nothing; remove it from the camera. It will be removed in a future release.")]
    [AddComponentMenu("")]
    internal partial class EmulatedColorScaleBias : MonoBehaviour
    {
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
        [AutoStaticsCleanup]
        static bool s_Logged;

        void OnEnable()
        {
            if (s_Logged)
            {
                return;
            }

            s_Logged = true;
            Debug.LogWarning("EmulatedColorScaleBias does nothing and will be removed in a future release. Remove it from the camera.", this);
        }
#endif
    }
}
