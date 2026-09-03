---
uid: xr-layers-optimization
---
# Optimize for Composition Layers

Learn how to optimize performance in your Composition Layers project.

You can configure optional settings to optimize performance in projects that use Composition Layers, as outlined in the following sections.

## Single-Pass Instanced Rendering

Single-pass instanced (SPI) rendering improves app performance by drawing geometry for both eyes in a single render pass.

When enabled, Unity draws geometry in a single render pass for both eyes, instead of a separate pass for each eye, which reduces CPU overhead and GPU geometry. SPI rendering provides measurable CPU and GPU performance benefits, and memory usage benefits even at small instance counts. These benefits scale even more with more complex geometry and material instances.

To learn how to enable Single-pass Instanced rendering, refer to [Set the render mode](xref:um-single-pass-stereo-rendering#set-the-render-mode) (Unity Manual).

> [!NOTE]
> To effectively use SPI rendering, use fewer projection layers with more content in each layer, instead of more small layers with less content in each.

## GPU instancing

GPU instancing is a per-material setting that reduces draw calls to the GPU and reduces CPU overhead. By batching draw calls for every object using a certain material, GPU instancing reduces the time needed to render, and improves performance.

For more information, refer to [Enable GPU Instancing for a prebuilt shader](xref:um-gpu-instancing-enable) (Unity Manual).

## Additional resources

* [Set the render mode](xref:um-single-pass-stereo-rendering#set-the-render-mode) (Unity Manual)
* [Enable GPU Instancing for a prebuilt shader](xref:um-gpu-instancing-enable) (Unity Manual)