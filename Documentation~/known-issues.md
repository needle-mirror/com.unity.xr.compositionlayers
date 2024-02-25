---
uid: xr-layer-known-issues
---

# Known Issues
 
* If using XR Rig or XR Origin to set up tracking space, make sure Requested Tracking Mode or Tracking Origin Mode set to Device or Floor. Leaving it to Default or Not Specified can cause view offset issues when using Projection Eye Rig.
* Equirect layer type - When running your application on an Android head-mounted display (HMD) with the Equirect layer, you may encounter clipping at the top and bottom edges of the displayed content. This can result in parts of the shape being cut off.
* Equirect layer type - When deploying your application to an Android-based head-mounted display (HMD) with the Equirect layer, when both the Upper and Lower Vertical Angles fall into negative values, particularly below approximately -30 degrees, unexpected behaviors may manifest. These behaviors include image flipping, opacity anomalies affecting surrounding objects, or the layer failing to display altogether.
* Equirect layer type - Upon entering play mode while the Equirect layer is present in the scene, the Equirect layer will extend to occupy the entire field of view within the HMD, completely filling the visual space.
* Cube layer type - Textures with Mipmaps aren't supported.
* Projection layer type - Single Pass Instanced rendering is not currently supported, which likely affect performance. Future releases will add Single Pass Instance rendering support.