# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-06-12

### Added

- IL2CPP managed-stripping support: the package ships a `link.xml` preserving its own
  runtime, and a build-time processor automatically preserves user `State` subclasses and
  `[BlackboardType]` types — no consumer configuration required. Validated with Windows
  IL2CPP and WebGL builds at Managed Stripping Level High.
- `[BlackboardType]` works on serializable structs and classes as well as enums — they
  serialise through `JsonUtility` and get a reflected default-value editor in the
  blackboard panel.
- `[Transition(Color = ...)]` hints are now rendered: transition port dots and their edges
  take the declared colour. Built-in branching states (Condition, Compare, CompareString,
  IsSubGraphBranch, SequenceGate) ship with semantic green/red transitions.
- Transition breakpoints: right-click a transition edge to add a breakpoint that pauses
  only when the target state is entered through that specific transition. The node's
  breakpoint indicator now flashes when any breakpoint is hit.
- `[StateInfo(HelpUrl = ...)]` adds an "Open Documentation" action to the state node
  context menu.
- Graph validation warns when a `[DataIn(Required = true)]` input has no value source.
- README guides: "Building for IL2CPP & WebGL" and "Extending VSM3" (supported extension
  points, custom-state conventions, and what stays internal in 0.4).

### Changed

- The sample projects moved out of the package to this repository's `samples` branch while
  they are polished; they will return as Package Manager-importable samples in a future
  release.

### Fixed

- The package now compiles for player builds. The runtime graph-mutation API (`AddNode`,
  `AddConnection`, `AddBlackboardVariable`, and related methods) was wrongly guarded as
  editor-only, which broke compilation of every non-editor build.
- OnTransition breakpoints never matched the transition that triggered them (the check
  read the previous transition's history record); they now evaluate the in-flight
  trigger port.
- Variant blackboard exposure overrides silently had no effect on the runner inspector
  (only one of the two exposure flags was written).
- `TimeoutState` documentation promised a non-existent "Cancel input"; it now describes
  the real behaviour.

### Removed

- Dead `ExposedAttribute` and unused port `Category` metadata.
- The orphaned blackboard `IsReadOnly` flag (it had no authoring path and no enforcement).
- The unimplemented `GraphNodeType.Comment` enum value (no comment node ever existed;
  portal enum values are pinned so existing graph assets are unaffected).
- Dead `RuntimeMonitorView` editor class.

## [0.3.0] - 2026-06-05

### Added

- Runtime graph visualization layer in a dedicated `Nonatomic.VSM.Visualization` assembly:
  - `RuntimeGraphView` renders a state machine graph in UI Toolkit and reflects live state
    from a running instance — suitable for world-space rendering (`PanelSettings` set to
    World Space) or runtime debug overlays.
  - `RuntimeGraphTheme` ScriptableObject for full visual customisation (colours, node sizing,
    edge style, active-state highlight, port colours).
  - Pluggable edge rendering via `IRuntimeEdgeRenderer`, with Default and Wire renderers. The
    Wire renderer honours per-connection control points so runtime edges match the editor.

### Changed

- Released under the MIT licence.
- Visualization code is isolated in its own assembly so projects that do not need runtime
  rendering can exclude it.

## [0.2.0] - 2026-03-27

### Added

- Rewritten auto layout algorithm that works backwards from leaf nodes
  - BFS distance scoring from entry node across all connection types
  - Right-edge alignment for input nodes, left-edge alignment for output nodes
  - Port-order-aware vertical stacking (input and output ports sorted by declaration order)
  - Overlap detection with automatic repositioning for output nodes
  - Two-pass layout using actual rendered node dimensions for pixel-accurate alignment
  - Automatic viewport centering on entry node after layout
- Blackboard variable exposure system with Input/Output/InputOutput directions
- SubStateMachine exposed port support with inline fields
- Extensible blackboard type registry with attribute-based registration
- Collection states (GetCollectionElementState, CycleIndexState)
- Math states (ConditionState with inversion support)
- TrafficLight sample expanded with multi-country intersection systems
- Dialogue sample with branching NPC conversations

### Changed

- Auto layout now operates on the currently viewed graph (supports sub-graphs)
- NodeSizeTracker waits for width stabilization before tracking to prevent position drift
- NodeSizeEstimator improved width calculation accounting for both input and output port labels
- Delayed NodeSizeTracker creation to prevent false-positive repositioning on graph load

### Fixed

- Node position drift when switching between graphs
- Auto layout jumping to parent graph when applied to a sub-graph
- Graph view not centering after auto layout

## [0.1.2] - 2025-12-28

### Changed

- Condensed the node view for the Entry and Exit nodes
- Condensed the node view for the JumpIn and JumpOut nodes

### Fixed

- Auto layout now positions JumpIn nodes near their corresponding JumpOut nodes
- Added additional samples
- Gradient edge lines
- DelayState now functions without graph open
- Additional icons

## [0.1.1] - 2025-12-27

### Changed

- Entry and Exit nodes now use compact title-bar layout with embedded ports
- Removed node ID footer display from all nodes
- Improved auto layout
- Added a comprehensive set of unit tests

### Removed

- NodeIdFooter helper class

## [0.1.0] - 2025-12-27

### Added

- Inline fields for unconnected input ports (string, int, float, bool, Vector2, Vector3, Color, enum, UnityEngine.Object)
- Dynamic sub-graph assignment via SubGraph input port and blackboard variables
- StateMachineGraph blackboard variable type
- Duplicate option in blackboard context menu
- Unified "Add Node" search window with Flow Control section
- Auto-center view on entry node for new graphs

### Changed

- Consolidated context menu to single "Add Node" option
- Entry node spawns at origin
- SubStateMachineNode ports use Action type for correct transition classification
- Entry node validation reduced to Warning

### Fixed

- Blackboard double-click rename
- SubStateMachineNode exit transition handling
- SubStateMachineNode info display not updating when sub-graph changed

### Refactored

- Extracted ActiveStateIndicator helper class for node highlighting
- Extracted NodeIdFooter helper for consistent node ID display
- Consolidated ~830 lines of duplicate code across node views
