# Composition Layers tests

Unit and integration tests for the `com.unity.xr.compositionlayers` package. Everything here runs in the Unity Editor with no XR device or headset. The tests are pure logic and check the message flow between `CompositionLayerManager` and `ILayerProvider`.

## How the tests observe behavior

The base class `CompositionLayerManagerTestBase` implements `ILayerProvider`, which is the same interface a real XR backend (OpenXR, MOXR & AXR) implements.

In every test, the test class acts as the backend. Each update, the `CompositionLayerManager` hands the provider four lists (created, removed, modified, and active), and the base class records them into
fields such as `hasModifiedLayers`, `lastActiveLayers`, and `WasReportedModified(layer)`. This is how a test observes what the manager decided.

The base class installs itself as the manager's provider in `SetUp` and clears it in `TearDown`, so the slot stays occupied for the whole test rather than only while an await is pending. `await NextManagerUpdate()` waits for the manager to run one update cycle and report it.

### Observing both providers

The manager talks to two providers: a native one and an emulation one. The base class occupies the native slot itself, and puts a `RecordingLayerProvider` in the emulation slot in place of the real `EmulatedLayerProvider`, so tests observe both sides without any real emulation rendering happening.

`m_NativeRecorder` and `m_EmulationRecorder` keep one report per update cycle rather than only the most recent. `created` and `removed` are single-shot and the manager clears them every `Update`, so a test that samples a cycle late cannot tell an event that never happened from one it missed. Use `WasCreated`, `WasRemoved`, `WasModified`, and `WasActive` to ask across the whole test, and `Reports[i]` for a specific cycle.

`AssertDispatchedToOneProvider(layer)` checks that no single cycle sent a layer to both. This does not hold today, because the manager broadcasts the same lists to both providers; it becomes true once each provider sees only its own layers.

The base also installs a `TestPlatformProvider` on `PlatformManager.ActivePlatformProvider`, and `SetPassthroughLayerType` marks a layer data type as passthrough the way a platform's camera subsystem does at runtime. Both are restored in teardown.

### Two kinds of tests

- `[Test]`: synchronous, pure logic. No scene, no frames. Use these for math and data on bare
  `LayerData` instances (geometry, `CopyFrom`, descriptor lookup). Fast and reliable.
- `[Test] async Task` with `await NextManagerUpdate()`: needs real frames. Use
  these for anything that flows through the manager (change reporting, ordering, activation).

### The modified-then-settles test pattern

Most `[Test] async Task` cases follow this rhythm:

```
change a property
await NextManagerUpdate();   // modified == true  (reported the change; guards stale rendering)
await NextManagerUpdate();   // modified == false (went quiet; guards per-frame churn)
set the same value again
await NextManagerUpdate();   // modified == false (no-op writes report nothing)
```

These tests protect against two possible regressions: a layer or extension that never reports a change (stale rendering), or one that reports every frame (wasted GPU work).
