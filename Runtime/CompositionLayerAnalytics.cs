using System;
using System.Collections.Generic;
using Unity.XR.CompositionLayers.Services;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

#if UNITY_ANALYTICS && ENABLE_CLOUD_SERVICES_ANALYTICS
using UnityEngine.Analytics;
#endif

namespace Unity.XR.CompositionLayers
{
    internal static partial class CompositionLayerAnalytics
    {
        private const int kMaxEventsPerHour = 1000;
        private const int kMaxNumberOfElements = 1000;
        private const string kVendorKey = "unity.compositionlayers";
        private const string kEventUsageMetrics = "compositionlayers_usage";

        [Serializable]
        public struct LayerTypeUsage
        {
            public string LayerTypeName;
            public int UsageCount;
        }
        [Serializable]
        private struct UsageMetricsEvent
#if ENABLE_CLOUD_SERVICES_ANALYTICS && UNITY_ANALYTICS
            : IAnalytic.IData
#endif //ENABLE_CLOUD_SERVICES_ANALYTICS && UNITY_ANALYTICS
        {
            public List<LayerTypeUsage> layerTypesUsage;
            public int compositionLayersCreated;
            public int colorScaleAndBiasComponentsCreated;
            public bool splashScreenCreated;
            public DateTime timestamp;
        }
#if ENABLE_CLOUD_SERVICES_ANALYTICS && UNITY_ANALYTICS
        [AnalyticInfo(eventName: kEventUsageMetrics, vendorKey: kVendorKey, maxEventsPerHour: kMaxEventsPerHour, maxNumberOfElements: kMaxNumberOfElements)]
        private class CompositionLayersUsageAnalytic : IAnalytic
        {
            private UsageMetricsEvent? data = null;

            public CompositionLayersUsageAnalytic(UsageMetricsEvent data)
            {
                this.data = data;
            }

            public bool TryGatherData(out IAnalytic.IData data, [NotNullWhen(false)] out Exception error)
            {
                error = null;
                data = this.data;
                return data != null;
            }
        }
#endif
        private static bool Initialize()
        {
#if ENABLE_TEST_SUPPORT || !ENABLE_CLOUD_SERVICES_ANALYTICS || !UNITY_ANALYTICS
            return false;
#elif UNITY_EDITOR
            return EditorAnalytics.enabled;
#else
            return false;
#endif //ENABLE_TEST_SUPPORT || !ENABLE_CLOUD_SERVICES_ANALYTICS || !UNITY_ANALYTICS
        }

        public static void SendUsageMetricsEvent()
        {
#if UNITY_EDITOR && UNITY_ANALYTICS && ENABLE_CLOUD_SERVICES_ANALYTICS
            if (!Initialize())
                return;

            var data = GatherUsageMetrics();
            SendEditorAnalytics(data);

#endif //UNITY_ANALYTICS && ENABLE_CLOUD_SERVICES_ANALYTICS
        }
        private static UsageMetricsEvent GatherUsageMetrics() {
            CompositionLayerAnalyticsHelper.ProcessCompositionLayersAnalyticsData();

            var layerTypesUsage = new List<LayerTypeUsage>(CompositionLayerAnalyticsHelper.LayerTypesUsage.Count);
            foreach (var layerTypeUsage in CompositionLayerAnalyticsHelper.LayerTypesUsage)
            {
                layerTypesUsage.Add(new LayerTypeUsage { LayerTypeName = layerTypeUsage.Key, UsageCount = layerTypeUsage.Value });
            }

            return new UsageMetricsEvent
            {
                layerTypesUsage = layerTypesUsage,
                compositionLayersCreated = CompositionLayerAnalyticsHelper.TotalLayerTypesUsage,
                colorScaleAndBiasComponentsCreated = CompositionLayerAnalyticsHelper.ColorScaleAndBiasExtensionsUsage,
                splashScreenCreated = CompositionLayerAnalyticsHelper.IsEnabledSplashScreen,
                timestamp = DateTime.Now
            };
        }

#if UNITY_EDITOR && ENABLE_CLOUD_SERVICES_ANALYTICS && UNITY_ANALYTICS
        private static void SendEditorAnalytics(UsageMetricsEvent data) {
            EditorAnalytics.SendAnalytic(new CompositionLayersUsageAnalytic(data));
        }
#endif //UNITY_EDITOR && ENABLE_CLOUD_SERVICES_ANALYTICS && UNITY_ANALYTICS
    }

#if UNITY_EDITOR && ENABLE_CLOUD_SERVICES_ANALYTICS && UNITY_ANALYTICS
    [System.Obsolete("CompositionLayerAnalyticsBuildProcessor is not intended for use outside the Composition Layers package. It is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release.")]
    public class CompositionLayerAnalyticsBuildProcessor : IPostprocessBuildWithReport

    {
        // This value determines the order of execution relative to other build processors.
        public int callbackOrder => 0;

        // This method is called after the build process is complete.
        public void OnPostprocessBuild(BuildReport report)
        {
            // Call your analytics sending method here
            CompositionLayerAnalytics.SendUsageMetricsEvent();
        }
    }
#endif
}
