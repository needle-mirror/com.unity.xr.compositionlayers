using System;

namespace Unity.XR.CompositionLayers.Extensions
{
    public partial class TexturesExtension
    {
        /// <summary>
        /// Obsolete. Use <see cref="SourceTexture"/> instead.
        /// </summary>
        [Obsolete("sourceTexture is obsolete as of Composition Layers 7.0.0-pre.1. Use SourceTexture instead. It will be removed in a future release. (UnityUpgradable) -> SourceTexture")]
        public SourceTextureEnum sourceTexture
        {
            get => SourceTexture;
            set => SourceTexture = value;
        }
    }
}
