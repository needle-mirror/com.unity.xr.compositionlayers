using System;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;

namespace Unity.XR.CompositionLayers.Tests
{
    [RequireComponent(typeof(CompositionLayer))]
    [AddComponentMenu("")]
    class UserDefaultLayerTestComponent : MonoBehaviour
    {
        [SerializeField]
        CompositionLayer m_CompositionLayer;

        void Awake()
        {
            m_CompositionLayer = GetComponent<CompositionLayer>();
        }

        void OnEnable()
        {
            CompositionLayerManager.Instance.SetDefaultSceneCompositionLayer(m_CompositionLayer);
        }

        void OnDisable()
        {
            if (CompositionLayerManager.ManagerActive && CompositionLayerManager.Instance.DefaultSceneCompositionLayer == m_CompositionLayer)
                CompositionLayerManager.Instance.ResetDefaultSceneCompositionLayer();
        }

        void OnValidate()
        {
            m_CompositionLayer = GetComponent<CompositionLayer>();
        }
    }
}
