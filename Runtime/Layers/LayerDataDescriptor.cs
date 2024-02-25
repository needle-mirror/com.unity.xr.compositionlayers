using System;

namespace Unity.XR.CompositionLayers.Layers
{
    /// <summary>
    /// Defines the meta data that is associated with a <see cref="LayerData"/> type.
    /// </summary>
    public readonly struct LayerDataDescriptor : IEquatable<LayerDataDescriptor>
    {
        /// <summary>The source that is providing the <see cref="LayerData"/> type.</summary>
        public readonly string Provider;

        /// <summary>The display name of the <see cref="LayerData"/> type.</summary>
        public readonly string Name;

        /// <summary>The full type name used for identifying this <see cref="LayerData"/> type.</summary>
        public readonly string TypeFullName;

        /// <summary>A description of what the <see cref="LayerData"/> does and how it is used.</summary>
        public readonly string Description;

        /// <summary>Path to the icon folder used for the <see cref="LayerData"/>.</summary>
        public readonly string IconPath;

        /// <summary>The icon used for the inspector of the <see cref="LayerData"/> object.</summary>
        public readonly string InspectorIcon;

        /// <summary>The icon used in the Composition Layer Window for <see cref="CompositionLayer"/>s using this type of <see cref="LayerData"/>.</summary>
        public readonly string ListViewIcon;

        /// <summary> Should new instance of the <see cref="LayerData"/> be an overlay or underlay. </summary>
        public readonly bool PreferOverlay;

        /// <summary>This layer type supports world or camera relative transforms.</summary>
        public readonly bool SupportTransform;

        /// <summary>The <see cref="Type"/> of the <see cref="LayerData"/>.</summary>
        public readonly Type DataType;

        /// <summary>Suggested extension types to use with the <see cref="LayerData"/> on the <see cref="CompositionLayer"/>.</summary>
        public readonly Type[] SuggestedExtensions;

        static readonly LayerDataDescriptor k_Empty = new("", "", "", "", "", "", "", true, false, null, new Type[] { });

        /// <summary>
        /// <see cref="LayerDataDescriptor"/> with all empty or null values
        /// </summary>
        public static LayerDataDescriptor Empty => k_Empty;

        /// <summary>
        /// Creates a new <see cref="LayerDataDescriptor"/>
        /// </summary>
        /// <param name="provider">The source that is providing the <see cref="LayerData"/> type.</param>
        /// <param name="name">The display name of the <see cref="LayerData"/> type.</param>
        /// <param name="typeFullName">The unique class Id key used for finding the <see cref="LayerData"/> type.</param>
        /// <param name="description">A description of what the <see cref="LayerData"/> does and how it is used.</param>
        /// <param name="iconPath">The icon used for the inspector of the <see cref="LayerData"/> object.</param>
        /// <param name="inspectorIcon">The icon used for the inspector of the <see cref="LayerData"/> object.</param>
        /// <param name="listViewIcon">The icon used in the Composition Layer Window for <see cref="CompositionLayer"/>s using this type of <see cref="LayerData"/>.</param>
        /// <param name="preferOverlay">Should new instance of the <see cref="LayerData"/> be an overlay or underlay.</param>
        /// <param name="supportTransform">This layer type supports world or camera relative transforms.</param>
        /// <param name="dataType">The <see cref="Type"/> of the <see cref="LayerData"/>.</param>
        /// <param name="suggestedExtensions">Suggested extension types to use with the <see cref="LayerData"/> on the <see cref="CompositionLayer"/>.</param>
        public LayerDataDescriptor(string provider, string name, string typeFullName, string description, string iconPath,
                                   string inspectorIcon, string listViewIcon, bool preferOverlay, bool supportTransform, Type dataType,
                                   Type[] suggestedExtensions)
        {
            Provider = provider;
            Name = name;
            TypeFullName = typeFullName;
            Description = description;
            IconPath = iconPath;
            InspectorIcon = inspectorIcon;
            ListViewIcon = listViewIcon;
            PreferOverlay = preferOverlay;
            SupportTransform = supportTransform;
            DataType = dataType;
            SuggestedExtensions = suggestedExtensions;
        }

        /// <see cref="IEquatable{T}"/>
        public bool Equals(LayerDataDescriptor other)
        {
            return Provider == other.Provider && Name == other.Name && TypeFullName == other.TypeFullName
                && Description == other.Description && IconPath == other.IconPath
                && InspectorIcon == other.InspectorIcon && ListViewIcon == other.ListViewIcon
                && PreferOverlay == other.PreferOverlay && SupportTransform == other.SupportTransform && DataType == other.DataType
                && Equals(SuggestedExtensions, other.SuggestedExtensions);
        }

        /// <see cref="IEquatable{T}"/>
        public override bool Equals(object obj)
        {
            return obj is LayerDataDescriptor other && Equals(other);
        }

        /// <see cref="IEquatable{T}"/>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Provider);
            hashCode.Add(Name);
            hashCode.Add(TypeFullName);
            hashCode.Add(Description);
            hashCode.Add(IconPath);
            hashCode.Add(InspectorIcon);
            hashCode.Add(ListViewIcon);
            hashCode.Add(PreferOverlay);
            hashCode.Add(SupportTransform);
            hashCode.Add(DataType);
            hashCode.Add(SuggestedExtensions);
            return hashCode.ToHashCode();
        }

        /// <see cref="IEquatable{T}"/>
        public static bool operator ==(LayerDataDescriptor left, LayerDataDescriptor right)
        {
            return left.Equals(right);
        }

        /// <see cref="IEquatable{T}"/>
        public static bool operator !=(LayerDataDescriptor left, LayerDataDescriptor right)
        {
            return !left.Equals(right);
        }
    }
}
