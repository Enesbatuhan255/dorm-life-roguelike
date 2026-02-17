# Bootstrap Setup

1. Open `Assets/Scenes/SampleScene.unity`.
2. Create an empty GameObject named `Bootstrap`.
3. Add `GameBootstrap` component to that object.
4. Save scene.

This ensures `IStatSystem` and `ITimeManager` are registered in `ServiceLocator` on startup.
