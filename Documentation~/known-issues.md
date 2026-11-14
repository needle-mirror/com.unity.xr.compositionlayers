---
uid: xr-layer-known-issues
---

# Known Issues

Review known limitations and issues in this package.

* Equirect layer type - When running your application on an Android head-mounted display (HMD) with the Equirect layer set to a 360 Central Angle, you may see a vertical 1px seam in the sphere.
* Unsupported layer types - When a provider plug-in composites layers on a device that doesn't support a layer's type, such as Cylinder, Equirect, or Cube, that layer doesn't render. The layer doesn't render through emulation instead, and other layers in the scene still render. For the layer types a device supports, refer to the documentation for your provider plug-in and device.
