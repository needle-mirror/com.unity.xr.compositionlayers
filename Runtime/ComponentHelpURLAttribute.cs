using System;
using System.Diagnostics;
using UnityEngine;

namespace Unity.XR.CompositionLayers
{
    [Conditional("UNITY_EDITOR")]
    class CompositionLayersHelpURLAttribute : HelpURLAttribute
    {
        public CompositionLayersHelpURLAttribute(Type type)
            : base(HelpURL(type)) { }

        static string HelpURL(Type type)
        {
            return DocumentationInfo.BaseURLPath +
                DocumentationInfo.PackageURLPath +
                DocumentationInfo.Version +
                DocumentationInfo.APIPath +
                type.FullName +
                DocumentationInfo.ExtPath;
        }
    }

    class DocumentationInfo
    {
        internal const string BaseURLPath = "https://docs.unity3d.com";
        internal const string PackageURLPath = "/Packages/com.unity.xr.compositionlayers@";
        internal const string APIPath = "/api/";
        internal const string ExtPath = ".html";

        const string k_FallbackVersion = "0.1.2";

        internal static readonly string Version;

        static DocumentationInfo()
        {
#if UNITY_EDITOR
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DocumentationInfo).Assembly);
            if (packageInfo == null)
            {
                Version = k_FallbackVersion;
                return;
            }

            var splitVersion = packageInfo.version.Split('.');
            Version = $"{splitVersion[0]}.{splitVersion[1]}";
#else
            Version = k_FallbackVersion;
#endif
        }
    }
}
