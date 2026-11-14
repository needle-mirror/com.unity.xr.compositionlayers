using System;
using UnityEngine;

namespace Unity.XR.CompositionLayers
{
    /// <summary>
    /// Obsolete. <see cref="CompositionLayer"/> draws its own Scene view outline for Quad and Cylinder layers, so this
    /// component does nothing. It remains only so scenes and prefabs saved by earlier versions keep loading; the layer
    /// removes it from its GameObject the next time the scene opens in the Unity Editor.
    /// </summary>
    [Obsolete("CompositionLayer draws its own outline. CompositionOutline does nothing, is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release.")]
    [AddComponentMenu("")]
    public class CompositionOutline : MonoBehaviour
    {
    }
}
