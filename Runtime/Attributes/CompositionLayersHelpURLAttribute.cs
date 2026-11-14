#if UNITY_EDITOR
#define CAN_USE_CUSTOM_HELP_URL
#endif

using System;
using System.Diagnostics;
using UnityEngine;

namespace Unity.XR.CompositionLayers
{
#if CAN_USE_CUSTOM_HELP_URL

    using UnityEditor.PackageManager;

    [Conditional("UNITY_EDITOR")]
    class CompositionLayersHelpURLAttribute : HelpURLAttribute
    {
        const string k_BaseURL = "https://docs.unity3d.com";
        const string k_MidURL = "/Packages/com.unity.xr.compositionlayers@";
        const string k_ApiURL = "/api/";
        internal const string k_FallbackVersion = "7.0";
        const string k_PackageName = "com.unity.xr.compositionlayers";
        const string k_EndURL = ".html";

        static readonly string k_PackageVersion;

        static CompositionLayersHelpURLAttribute()
        {
            var packageInfo = PackageInfo.FindForPackageName(k_PackageName);

#if UNITY_EDITOR
            if (packageInfo == null)
            {
                k_PackageVersion = k_FallbackVersion;
                return;
            }

            var splitVersion = packageInfo.version.Split('.');
            k_PackageVersion = $"{splitVersion[0]}.{splitVersion[1]}";
#else
            k_PackageVersion = k_FallbackVersion;
#endif
        }

        public CompositionLayersHelpURLAttribute(Type type)
            : base(HelpURL(type)) {}

        static string HelpURL(Type type)
        {
            return $"{k_BaseURL}{k_MidURL}{k_PackageVersion}{k_ApiURL}{type.FullName}{k_EndURL}";
        }
    }
#else // PackageInfo is editor-only, so players get an inert attribute
    [Conditional("UNITY_EDITOR")]
    class CompositionLayersHelpURLAttribute : Attribute
    {
        public CompositionLayersHelpURLAttribute(Type type) { }
    }
#endif
}
