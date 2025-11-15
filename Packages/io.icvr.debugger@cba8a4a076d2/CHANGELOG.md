1.1.0

- Added vertical debugger window prefab and template configuration for mobile layouts
- Introduced tab sandwich control with custom tweened fold/unfold animation
- Added IResettableView contract and automatic view reset when closing the debugger
- Brought in ObservableCollections packages to support the new vertical layout

1.0.0

- Migrated the debugger runtime from UniRx to the new R3 reactive library and aligned dependencies with our .NET 8 stack.
- Refreshed the in-editor data views (String, Navigation, Tabs) and prefabs to provide richer contextual debugging information.
- Added TextMesh Pro shader variants for URP, HDRP, and mobile pipelines so the debugger overlay renders consistently across rendering backends.
- Updated ICVR Debugger visuals

0.0.20

- Added the filter by the category for logs

0.0.19

- Upgrade ICVR Debugger context - you just need to add that component to Project Context and click the Search plugins button

0.0.18

- Added the facade with receiving log ability
- The context migrated to SceneContext instead of GameObjectContext

0.0.12

- Reporter

0.0.1

- Core
- Logger
