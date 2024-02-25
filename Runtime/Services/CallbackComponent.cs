using System;
using UnityEngine;

namespace Unity.XR.CompositionLayers.Services
{
    /// <summary>
    /// MonoBehaviour used the drive the <see cref="CompositionLayerManager" />.
    /// There should be one and only one instance of this at any one time.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    class CallbackComponent : MonoBehaviour
    {
        /// <summary> Called at end of <see cref="Awake"/> </summary>
        internal Action Awoke;

        /// <summary> Called at end of  <see cref="Update"/> </summary>
        internal Action Updated;

        /// <summary> Called at end of  <see cref="LateUpdate"/> </summary>
        internal Action LateUpdated;

        /// <summary> Called at end of  <see cref="OnDestroy"/> </summary>
        internal Action Destroyed;

        void Awake()
        {
            Awoke?.Invoke();
        }

        void Update()
        {
            Updated?.Invoke();
        }

        void LateUpdate()
        {
            LateUpdated?.Invoke();
        }

        void OnDestroy()
        {
            Destroyed?.Invoke();
        }
    }
}
