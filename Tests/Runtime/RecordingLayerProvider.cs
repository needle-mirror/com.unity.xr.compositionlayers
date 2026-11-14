using System.Collections.Generic;
using Unity.XR.CompositionLayers.Provider;
using LayerInfo = Unity.XR.CompositionLayers.Services.CompositionLayerManager.LayerInfo;

namespace Unity.XR.CompositionLayers.Tests
{
    // Keeps every UpdateLayers call as its own report rather than overwriting. Created and removed layers
    // are single-shot and the manager clears them each Update, so a test that samples one cycle late cannot
    // tell an event that never happened from one it missed.
    class RecordingLayerProvider : ILayerProvider
    {
        internal class Report
        {
            public readonly List<LayerInfo> CreatedLayers = new();
            public readonly List<int> RemovedLayers = new();
            public readonly List<LayerInfo> ModifiedLayers = new();
            public readonly List<LayerInfo> ActiveLayers = new();
        }

        readonly List<Report> m_Reports = new();

        public string Name { get; }

        public IReadOnlyList<Report> Reports => m_Reports;

        public int SetInitialStateCount { get; private set; }

        public int CleanupStateCount { get; private set; }

        public RecordingLayerProvider(string name)
        {
            Name = name;
        }

        public void Clear()
        {
            m_Reports.Clear();
            SetInitialStateCount = 0;
            CleanupStateCount = 0;
        }

        public void SetInitialState(List<LayerInfo> layers)
        {
            SetInitialStateCount++;
        }

        public void CleanupState()
        {
            CleanupStateCount++;
        }

        public void UpdateLayers(List<LayerInfo> createdLayers, List<int> removedLayers,
            List<LayerInfo> modifiedLayers, List<LayerInfo> activeLayers)
        {
            var report = new Report();

            if (createdLayers != null)
                report.CreatedLayers.AddRange(createdLayers);

            if (removedLayers != null)
                report.RemovedLayers.AddRange(removedLayers);

            if (modifiedLayers != null)
                report.ModifiedLayers.AddRange(modifiedLayers);

            if (activeLayers != null)
                report.ActiveLayers.AddRange(activeLayers);

            m_Reports.Add(report);
        }

        public void LateUpdate() { }

        public void OnRenderObject() { }

        public bool WasCreated(CompositionLayer layer)
        {
            foreach (var report in m_Reports)
            {
                if (Contains(report.CreatedLayers, layer))
                    return true;
            }

            return false;
        }

        public bool WasModified(CompositionLayer layer)
        {
            foreach (var report in m_Reports)
            {
                if (Contains(report.ModifiedLayers, layer))
                    return true;
            }

            return false;
        }

        public bool WasActive(CompositionLayer layer)
        {
            foreach (var report in m_Reports)
            {
                if (Contains(report.ActiveLayers, layer))
                    return true;
            }

            return false;
        }

        public bool WasRemoved(int layerId)
        {
            foreach (var report in m_Reports)
            {
                foreach (var removedId in report.RemovedLayers)
                {
                    if (removedId == layerId)
                        return true;
                }
            }

            return false;
        }

        // Removed layers are reported by id rather than by reference, so they are not considered here.
        internal static bool Owns(Report report, CompositionLayer layer)
        {
            return Contains(report.CreatedLayers, layer)
                || Contains(report.ModifiedLayers, layer)
                || Contains(report.ActiveLayers, layer);
        }

        static bool Contains(List<LayerInfo> layerInfos, CompositionLayer layer)
        {
            foreach (var layerInfo in layerInfos)
            {
                if (layerInfo.Layer == layer)
                    return true;
            }

            return false;
        }
    }
}
