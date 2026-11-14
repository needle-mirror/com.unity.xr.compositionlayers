using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.XR.CompositionLayers.Editor")]
[assembly: InternalsVisibleTo("Unity.XR.CompositionLayers.Tests")]
[assembly: InternalsVisibleTo("Unity.XR.CompositionLayers.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.XR.Oculus")]
// PSVR2 renders its reprojection layers through ImageFilters (BlitParams, TargetParams, Blit, BeginBatch, EndBatch).
// Those members are obsolete for everyone else and will be removed in a future release. This grant keeps PSVR2 building.
[assembly: InternalsVisibleTo("Unity.XR.PSVR2")]
[assembly: InternalsVisibleTo("Unity.XR.Simulation")]
[assembly: InternalsVisibleTo("Unity.XR.CompositionLayers.UIInteraction")]
