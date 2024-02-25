using System;
using UnityEngine;

namespace Unity.XR.CompositionLayers.Layers
{
    /// <summary>
    /// This class is used to keep platform dependent properties.
    /// The specific platforms define additional attributes for this inherited class.
    /// </summary>
    [Serializable]
    public class PlatformLayerData
    {
        /// <summary>
        /// Serialize all SerializeField to text.
        /// </summary>
        /// <returns>Serialized text.</returns>
        public virtual string Serialize()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Deserialize all SerializeField from text.
        /// </summary>
        /// <param name="text">Serialized text.</param>
        public virtual void Deserialize(string text)
        {
            if (!string.IsNullOrEmpty(text))
                JsonUtility.FromJsonOverwrite(text, this);
        }
    }
}
