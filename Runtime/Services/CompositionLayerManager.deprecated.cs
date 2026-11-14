using System;
using UnityEngine;

namespace Unity.XR.CompositionLayers.Services
{
    public sealed partial class CompositionLayerManager
    {
        /// <summary>
        /// Obsolete. Use <see cref="MainCameraCache"/> instead.
        /// </summary>
        [Obsolete("mainCameraCache is obsolete as of Composition Layers 7.0.0-pre.1. Use MainCameraCache instead. It will be removed in a future release. (UnityUpgradable) -> MainCameraCache")]
        public static Camera mainCameraCache => MainCameraCache;
    }
}
