using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Unity.XR.CompositionLayers.Tests
{
    /// <summary>
    /// Deactivates this GameObject at startup when no XR display is running.
    /// </summary>
    public class DeactivateWithoutXRDisplay : MonoBehaviour
    {
        readonly List<XRDisplaySubsystem> m_DisplaySubsystems = new();

        void Start()
        {
            SubsystemManager.GetSubsystems(m_DisplaySubsystems);
            foreach (var displaySubsystem in m_DisplaySubsystems)
            {
                if (displaySubsystem.running)
                    return;
            }

            gameObject.SetActive(false);
        }
    }
}
