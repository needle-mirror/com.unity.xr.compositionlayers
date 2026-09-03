using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CompositionLayers.Provider;
using Unity.XR.CompositionLayers.Services;
using LayerInfo = Unity.XR.CompositionLayers.Services.CompositionLayerManager.LayerInfo;

namespace Unity.XR.CompositionLayers.Tests
{
    // Awaited in [Test] async Task methods as `await new WaitForUpdateCall(this);`. Swaps itself in as
    // the manager's ILayerProvider for exactly one update cycle, forwards that cycle's report to the
    // wrapped context, then restores the previous provider and completes the await. Uses
    // AwaitableCompletionSource (the Unity 6 Awaitable-based equivalent of TaskCompletionSource) so
    // completion is signalled from the manager's LateUpdate and resumes on the main thread.
    sealed class WaitForUpdateCall : ILayerProvider
    {
        readonly AwaitableCompletionSource<bool> m_Completion = new();
        bool m_UpdateCalled;
        ILayerProvider m_ReplacedProvider;
        ILayerProvider m_WrappedContext;

        public WaitForUpdateCall(ILayerProvider context)
        {
            m_WrappedContext = context;
            m_ReplacedProvider = CompositionLayerManager.Instance.LayerProvider;
            CompositionLayerManager.Instance.LayerProvider = this;
        }

        public Awaitable<bool>.Awaiter GetAwaiter() => m_Completion.Awaitable.GetAwaiter();

        public void SetInitialState(List<LayerInfo> layers) => m_UpdateCalled = false;

        public void CleanupState() { }

        public void UpdateLayers(List<LayerInfo> createdLayers, List<int> removedLayers, List<LayerInfo> modifiedLayers, List<LayerInfo> activeLayers)
        {
            if (m_UpdateCalled)
                return;

            m_WrappedContext?.UpdateLayers(createdLayers, removedLayers, modifiedLayers, activeLayers);

            m_UpdateCalled = true;
        }

        public void LateUpdate()
        {
            if (m_UpdateCalled)
            {
                CompositionLayerManager.Instance.LayerProvider = m_ReplacedProvider;
                m_Completion.TrySetResult(true);
            }
        }

        public void OnRenderObject() { }
    }
}
