# Composition Layers tests

Unit and integration tests for the `com.unity.xr.compositionlayers` package. Everything here runs in the Unity Editor with no XR device or headset. The tests are pure logic and check the message flow between `CompositionLayerManager` and `ILayerProvider`.

## How the tests observe behavior

The base class `CompositionLayerManagerTestBase` implements `ILayerProvider`, which is the same interface a real XR backend (OpenXR, MOXR & AXR) implements.

In every test, the test class acts as the backend. Each update, the `CompositionLayerManager` hands the provider four lists (created, removed, modified, and active), and the base class records them into
fields such as `hasModifiedLayers`, `lastActiveLayers`, and `WasReportedModified(layer)`. This is how a test observes what the manager decided.

`WaitForUpdateCall(this)` temporarily swaps the test in as the manager's provider, waits exactly one update cycle, then captures what the manager reported.

### Two kinds of tests

- `[Test]`: synchronous, pure logic. No scene, no frames. Use these for math and data on bare
  `LayerData` instances (geometry, `CopyFrom`, descriptor lookup). Fast and reliable.
- `[Test] async Task` with `await new WaitForUpdateCall(this)`: needs real frames. Use
  these for anything that flows through the manager (change reporting, ordering, activation).

### The modified-then-settles test pattern

Most `[Test] async Task` cases follow this rhythm:

```
change a property
await new WaitForUpdateCall(this);   // modified == true  (reported the change; guards stale rendering)
await new WaitForUpdateCall(this);   // modified == false (went quiet; guards per-frame churn)
set the same value again
await new WaitForUpdateCall(this);   // modified == false (no-op writes report nothing)
```

These tests protect against two possible regressions: a layer or extension that never reports a change (stale rendering), or one that reports every frame (wasted GPU work).
