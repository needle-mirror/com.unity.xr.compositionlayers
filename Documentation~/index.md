---
uid: xr-layers-index
---

# Composition Layers

Composition Layers allow application developers to define areas in a scene or view that are composited separately from the normal 3D rendering. Rendering to a composition layer can provide sharper rendering compared to normal scene rendering. Device makers can also provide their own composition layers to support features like pass-through video or hardware-accelerated video.

The Composition Layers package defines a few common layer types and provides extension mechanisms that allow plug-in authors to add new layer types or add custom extensions to support their specific needs or features.

Refer to the following topics to get started with the Composition Layers package:

| **Topic** | **Description** |
| :---- | :---------- |
| [What's new](xref:xr-layers-whats-new) | Learn about the latest features, improvements, and fixes. |
| [Get started](xref:xr-layers-setup) | Get started with the Composition Layers package. |
| [Overview](xref:xr-layer-overview) | Understand the core concepts behind composition layers. |
| [Use composition layers](xref:xr-layers-using) | Add, configure, and manage composition layers in a Scene. |
| [Composition layer components](xref:xr-layers-components) | Understand the components and extensions used to define composition layers. |
| [Mirror View Renderer](xref:mirror-view-renderer) | Render composition layers to the Main Display when running in XR mode. |
| [Provider implementation guidelines](xref:xr-layers-provider-guide) | Implement Composition Layers support for a specific hardware API. |
| [Known Issues](xref:xr-layer-known-issues) | Review known limitations and issues in this package. |

[Using Composition Layers]: xref:xr-layers-using
[ILayerProvider]: xref:UnityEngine.XR.CompositionLayers.Provider.ILayerProvider
