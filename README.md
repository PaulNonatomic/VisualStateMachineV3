# Visual State Machine V3

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Unity 6+](https://img.shields.io/badge/Unity-6.0%2B-blue.svg)](https://unity.com)
[![Status: Alpha](https://img.shields.io/badge/Status-Alpha-orange.svg)](#status)

A Unity 6+ package for visually building and monitoring state machines using Unity's experimental GraphView API. Open source under the MIT licence and under active development.

## Status

VSM3 is pre-1.0 (currently `0.x`). The API surface is stabilising but **breaking changes may land between minor versions** until 1.0. Pin to a specific tag if you're using it in production work, and watch the [CHANGELOG](CHANGELOG.md) for migration notes.

Feedback, bug reports, and PRs are welcome — open an issue or discussion on the GitHub repo.

## Table of Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
    - [States](#states)
    - [State Lifecycle](#state-lifecycle)
    - [Attributes](#attributes)
    - [Blackboard](#blackboard)
    - [Data Flow](#data-flow)
    - [Typed Context](#typed-context)
    - [Sub-State Machines](#sub-state-machines)
    - [Portal Nodes](#portal-nodes)
    - [Transition Timing](#transition-timing)
    - [State Styling](#state-styling)
- [Code-First State Machines](#code-first-state-machines)
- [Graph Variants](#graph-variants)
- [Built-in States](#built-in-states)
- [Editor Features](#editor-features)
    - [Graph Window](#graph-window)
    - [Regions](#regions)
    - [SubGraph Preview](#subgraph-preview)
    - [Layout Tools](#layout-tools)
- [Debugging Tools](#debugging-tools)
    - [Runtime Monitor](#runtime-monitor)
    - [Breakpoints](#breakpoints)
    - [Transition Timeline](#transition-timeline)
    - [Replay Controller](#replay-controller)
    - [Transition History Export](#transition-history-export)
- [Performance](#performance)
    - [Update Modes](#update-modes)
    - [Instance Pooling](#instance-pooling)
- [Building for IL2CPP & WebGL](#building-for-il2cpp--webgl)
- [Integrations](#integrations)
    - [Addressables](#addressables)
    - [Visual Scripting](#visual-scripting)
- [API Reference](#api-reference)
- [Extending VSM3](#extending-vsm3)
- [Testing Custom States](#testing-custom-states)
- [Sample Projects](#sample-projects)
- [Troubleshooting](#troubleshooting)
- [Licence](#licence)
- [Contributing](#contributing)

## Requirements

- **Unity 6 (6000.0) or later** - Required for GraphView API compatibility
- .NET Standard 2.1

> **Note:** This package uses Unity's experimental GraphView API (`UnityEditor.Experimental.GraphView`) for its visual node-based editor. While this API has been available in
> earlier Unity versions, Unity 6 provides improved stability and features.

## Installation

### Via Package Manager

Add the following to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.nonatomic.visualstatemachinev3": "https://github.com/PaulNonatomic/VisualStateMachineV3.git"
  }
}
```

### Manual Installation

Clone or download this repository into your project's `Packages` folder.

## Quick Start

### 1. Create a State

Create a new C# script that inherits from `State`:

```csharp
using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

public class IdleState : State
{
    [DataIn("Movement Input")]
    public Vector2 MovementInput;

    [DataOut("Idle Duration")]
    public float IdleDuration;

    [Transition("On Move")]
    public Action OnMove;

    [Transition("On Jump")]
    public Action OnJump;

    private float _enterTime;

    public override void OnEnter()
    {
        _enterTime = Time.time;
        Debug.Log("Entered Idle State");
    }

    public override void OnUpdate()
    {
        IdleDuration = Time.time - _enterTime;

        if (MovementInput.magnitude > 0.1f)
        {
            OnMove?.Invoke();
        }
    }

    public override void OnExit()
    {
        Debug.Log($"Exited Idle State after {IdleDuration:F2}s");
    }
}
```

### 2. Create a State Machine Graph

1. Right-click in the Project window
2. Select **Create > Visual State Machine > State Machine Graph**
3. Double-click the created asset to open the Graph Editor

### 3. Build Your Graph

1. Right-click in the graph to add states
2. Connect transition ports to define state flow
3. Add an Entry node to define the starting state
4. Optionally add Exit nodes for completion states

### 4. Run the State Machine

Add a `StateMachineRunner` component to a GameObject and assign your graph:

```csharp
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private StateMachineGraph _graph;

    private StateMachineRunner _runner;

    private void Start()
    {
        _runner = gameObject.AddComponent<StateMachineRunner>();
        _runner.Initialize(_graph);
        _runner.StartStateMachine();
    }

    private void Update()
    {
        var input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        _runner.Blackboard.Set("Movement Input", input);
    }
}
```

## Core Concepts

### States

States are the building blocks of your state machine. Each state inherits from `State` (or `State<TContext>` for typed context access) and overrides lifecycle methods to define
behavior.

### State Lifecycle

States have both synchronous and asynchronous lifecycle methods:

```csharp
public class ExampleState : State
{
    // --- Synchronous ---
    public override void OnEnter() { }        // Called when entering the state
    public override void OnUpdate() { }       // Called every frame (per UpdateMode)
    public override void OnFixedUpdate() { }  // Called at fixed timestep
    public override void OnLateUpdate() { }   // Called at end of frame
    public override void OnExit() { }         // Called when leaving the state
    public override void OnReset() { }        // Called when returning to pool

    // --- Asynchronous (with cancellation support) ---
    public override Task OnEnterAsync(CancellationToken ct) { }
    public override Task OnEnterAsync() { }
    public override Task OnUpdateAsync(CancellationToken ct) { }
    public override Task OnUpdateAsync() { }
    public override Task OnExitAsync(CancellationToken ct) { }
    public override Task OnExitAsync() { }
}
```

**Available properties within a state:**

| Property       | Type                   | Description                           |
|----------------|------------------------|---------------------------------------|
| `StateMachine` | `StateMachineInstance` | The parent state machine instance     |
| `Blackboard`   | `Blackboard`           | Shared key-value store                |
| `GameObject`   | `GameObject`           | The owning GameObject                 |
| `Transform`    | `Transform`            | The owning Transform                  |
| `Context`      | `IStateMachineContext` | Shared context object                 |
| `IsActive`     | `bool`                 | Whether the state is currently active |

**Utility methods:**

| Method                   | Description                                       |
|--------------------------|---------------------------------------------------|
| `GetComponent<T>()`      | Gets a component from the owning GameObject       |
| `GetOrAddComponent<T>()` | Gets or adds a component on the owning GameObject |
| `Log(message)`           | Logs a message with state context                 |
| `LogWarning(message)`    | Logs a warning with state context                 |
| `LogError(message)`      | Logs an error with state context                  |

### Attributes

#### `[DataIn]`

Marks a field as an input port. Values are populated from the Blackboard before `OnEnter()`.

```csharp
[DataIn("Speed", Required = true, Tooltip = "Movement speed")]
public float Speed = 5f;

[DataIn]
public Transform Target;
```

#### `[DataOut]`

Marks a field as an output port. Values are written to the Blackboard after `OnExit()`.

```csharp
[DataOut("Result")]
public float CalculatedValue;

[DataOut]
public Vector3 FinalPosition;
```

#### `[Transition]`

Marks an `Action` field as a transition trigger. Invoke to transition to the connected state.

```csharp
[Transition("On Complete", Color = "#00FF00")]
public Action OnComplete;

[Transition("On Fail")]
public Action OnFail;
```

#### `[Exposed]`

Marks a Blackboard variable for Inspector visibility.

```csharp
// In your blackboard setup
[Exposed("Player Health")]
public int Health = 100;
```

### Blackboard

The Blackboard is a shared key-value store for passing data between states.

```csharp
// Set a value
Blackboard.Set("playerHealth", 100);
Blackboard.Set("targetPosition", new Vector3(10, 0, 5));

// Get a value
var health = Blackboard.Get<int>("playerHealth");
var position = Blackboard.Get<Vector3>("targetPosition");

// Try get (safe)
if (Blackboard.TryGet<int>("playerHealth", out var hp))
{
    Debug.Log($"Health: {hp}");
}

// Subscribe to changes
Blackboard.Subscribe<int>("playerHealth", (oldValue, newValue) =>
{
    Debug.Log($"Health changed from {oldValue} to {newValue}");
});

// Check if key exists
if (Blackboard.HasKey("playerHealth"))
{
    // ...
}
```

#### Blackboard Variable Nodes

You can visually connect blackboard variables to state inputs in the graph editor:

1. Create a variable in the Blackboard panel
2. Drag the variable onto the graph to create a **Get** node
3. Connect the Get node's output to a state's `[DataIn]` port

This provides a visual representation of data flow and ensures values are bound when entering states.

#### Exposed Variables & Per-Instance Overrides

Variables can be exposed in the graph editor for per-instance customization:

1. In the Blackboard panel, toggle the "Expose" checkbox on a variable
2. The variable will appear in the `StateMachineRunner` Inspector
3. Each runner instance can override the default value

```csharp
// Access overridden values at runtime
var runner = GetComponent<StateMachineRunner>();

// Values are automatically applied from overrides when the state machine starts
// You can also set values programmatically:
runner.SetBlackboardValue("Speed", 10f);
var speed = runner.GetBlackboardValue<float>("Speed");
```

Supported override types: `int`, `float`, `bool`, `string`, `Vector2`, `Vector3`, `Color`, and `UnityEngine.Object` references (GameObjects, ScriptableObjects, etc.).

#### Custom Blackboard Types

By default the blackboard supports primitives, math types, and Unity objects. To add custom types (such as enums), use the `[BlackboardType]` attribute:

```csharp
using Nonatomic.VSM.Attributes;

[BlackboardType]
public enum GamePhase { Menu, Playing, Paused, GameOver }

[BlackboardType(DisplayName = "AI State", Category = "AI")]
public enum AIBehavior { Idle, Patrol, Chase, Attack }
```

Attributed types are auto-discovered at startup and appear in the blackboard "Add Variable" menu.

For types defined in immutable packages where you cannot add the attribute, use the manual registration API in an `[InitializeOnLoad]` class:

```csharp
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Editor.Blackboard;

[InitializeOnLoad]
public static class ExternalTypeRegistration
{
    static ExternalTypeRegistration()
    {
        BlackboardTypeRegistry.RegisterEnum<SomePlugin.Direction>();
        BlackboardTypeEditorRegistry.RegisterEnum<SomePlugin.Direction>();
    }
}
```

`[BlackboardType]` also works on serializable structs and classes. They serialize through
`JsonUtility` and get a reflected default-value editor in the blackboard panel:

```csharp
using System;
using Nonatomic.VSM.Attributes;

[Serializable]
[BlackboardType(Category = "Combat")]
public struct DamageInfo
{
    public int Amount;
    public float Knockback;
}
```

Custom types work under IL2CPP with no extra configuration — see
[Building for IL2CPP & WebGL](#building-for-il2cpp--webgl).

#### Best Practices

**Prefer `[DataIn]`/`[DataOut]` ports over direct blackboard access.** While states can read and write blackboard variables in code via `Blackboard.Set()` and `Blackboard.Get()`, this hides data flow from the graph. Instead, use `[DataIn]` and `[DataOut]` fields connected to blackboard variable nodes:

```csharp
// Prefer: data flow is visible in the graph
public class ScoreState : State
{
    [DataIn("Points")]
    public int Points;

    [DataOut("Total Score")]
    public int TotalScore;

    public override void OnEnter()
    {
        TotalScore += Points;
    }
}

// Avoid: hidden dependency on a blackboard key
public class ScoreState : State
{
    public override void OnEnter()
    {
        var points = Blackboard.Get<int>("Points");
        Blackboard.Set("Total Score", points + Blackboard.Get<int>("Total Score"));
    }
}
```

Using ports keeps the graph self-documenting — you can see where data comes from and where it goes by following the connections. It also makes states more reusable since they have no hidden dependencies on specific blackboard key names.

### Data Flow

Data flows through the state machine via:

1. **Blackboard Variables** - Shared data accessible from any state
2. **Port Connections** - Visual connections between state outputs and inputs
3. **Optional Typed Context** - Strongly-typed shared context object

```csharp
// Access blackboard from within a state
public class DamageState : State
{
    [DataIn]
    public int DamageAmount;

    public override void OnEnter()
    {
        var currentHealth = Blackboard.Get<int>("Health");
        Blackboard.Set("Health", currentHealth - DamageAmount);
    }
}
```

### Typed Context

For strongly-typed data sharing between states, use `State<TContext>` and `StateMachineController<TContext>`:

```csharp
public class Enemy : MonoBehaviour
{
    [SerializeField] private StateMachineGraph _graph;
    private StateMachineController<Enemy> _controller;

    // Context data - states access these via SharedContext
    public float Health { get; set; } = 100f;
    public Transform Target { get; set; }

    private void Awake()
    {
        _controller = new StateMachineController<Enemy>(_graph, this);
        _controller.OnStateChanged += (prev, next) =>
            Debug.Log($"State changed: {prev?.GetType().Name} -> {next?.GetType().Name}");
        _controller.Start();
    }

    private void Update() => _controller.Update();
    private void FixedUpdate() => _controller.FixedUpdate();
    private void OnDestroy() => _controller.Dispose();
}
```

States access the typed context through `SharedContext`:

```csharp
public class ChaseState : State<Enemy>
{
    [Transition("Reached")] public Action OnReached;

    public override void OnUpdate()
    {
        var enemy = SharedContext;
        var direction = (enemy.Target.position - enemy.Transform.position).normalized;
        enemy.Transform.position += direction * Time.deltaTime * 5f;

        if (Vector3.Distance(enemy.Transform.position, enemy.Target.position) < 1f)
        {
            OnReached?.Invoke();
        }
    }
}
```

You can also create a typed runner by subclassing `StateMachineRunner<TContext>`:

```csharp
public class EnemyRunner : StateMachineRunner<EnemyContext>
{
    protected override EnemyContext CreateSharedContext()
    {
        return new EnemyContext { AlertLevel = 0 };
    }
}
```

### Sub-State Machines

Create hierarchical state machines by embedding one graph inside another using SubStateMachine nodes:

1. Create a separate `StateMachineGraph` asset for the sub-state machine
2. In the parent graph, right-click and add a **Sub State Machine** node
3. Assign the sub-graph to the node
4. Connect the node's **Enter** port from a transition and **Exit** port to continue flow

#### How It Works

When execution reaches a SubStateMachine node:

1. The parent creates a child `StateMachineInstance` for the sub-graph
2. The child runs independently (parent delegates Update calls to it)
3. When the child reaches an **Exit** node, it fires `OnExited`
4. The parent resumes execution from the SubStateMachine node's exit connection

#### Data Modes

Configure how the child accesses parent data:

| Mode         | Description                                                          |
|--------------|----------------------------------------------------------------------|
| **Inherit**  | Child blackboard inherits from parent (can read parent variables)    |
| **Isolated** | Child has independent blackboard; use explicit input/output mappings |
| **Shared**   | Child uses the same blackboard instance as parent (tight coupling)   |

#### Context Inheritance

Sub-state machines inherit the parent's typed context (`State<TContext>`):

- **Same context type**: Works seamlessly - child states access the same context
- **Compatible context type**: Works if sub-graph expects a base type (e.g., sub-graph uses `MonoBehaviour`, parent provides `PlayerController`)
- **Incompatible context type**: Validation error - sub-graph requires a type the parent can't provide
- **No context required**: Always works - sub-graph doesn't need typed context

#### Limitations

| Limit                   | Value   | Description                                 |
|-------------------------|---------|---------------------------------------------|
| **Max Nesting Depth**   | 16      | Maximum levels of nested sub-state machines |
| **Circular References** | Blocked | Graph A -> B -> A is detected and prevented |

Both limits are enforced at edit-time (validation) and runtime (with error logging and graceful continuation).

#### Exposed Variable Ports

Sub-state machine nodes can display **typed data ports** for the child graph's blackboard variables, making data flow between parent and child fully visible in the graph editor.

##### Exposing Variables in a Child Graph

1. Open the sub-graph asset in the graph editor
2. In the Blackboard panel, expand a variable to reveal its settings
3. Set the **Sub-Graph Port** dropdown to control how it appears on the parent's SubStateMachine node:

| Direction       | Description                                                              |
|-----------------|--------------------------------------------------------------------------|
| **None**        | Variable is not exposed (default)                                        |
| **Input**       | Appears as an input port — parent provides the value, child reads it     |
| **Output**      | Appears as an output port — child writes the value, parent reads it      |
| **InputOutput** | Appears as both an input and output port                                 |

##### Using Exposed Ports in the Parent Graph

Once a child graph has exposed variables, the parent graph's SubStateMachine node automatically shows typed data ports for them:

- **Input ports** accept connections from blackboard Get nodes or state `[DataOut]` ports in the parent graph. When no connection is present, an **inline field** lets you set the value directly on the node — just like any other data input port.
- **Output ports** can be connected to blackboard Set nodes in the parent graph, writing child values back to the parent's blackboard when the sub-graph exits.

##### How Data Flows at Runtime

**On child start** (input ports):
1. Values from connected parent blackboard variables are written to the child's blackboard
2. If no connection exists, the inline value set on the SubStateMachine node is used instead

**On child exit** (output ports):
1. Values from the child's blackboard are written to connected parent blackboard variables

This layered approach means connections take priority over inline values, and inline values take priority over the child graph's defaults.

##### Example: Reusable Traffic Light with Direction Input

```
Child graph (TrafficLightGraph):
  Blackboard:
    - Direction (enum, exposed as Input)
    - IsActive (bool, exposed as Output)

Parent graph (IntersectionGraph):
  [Blackboard: NorthSouthDir] --Get--> [SubStateMachine: TrafficLightGraph].Direction
  [SubStateMachine: TrafficLightGraph].IsActive --Set--> [Blackboard: NSActive]
```

The same `TrafficLightGraph` can be reused multiple times with different direction values, each wired visually in the parent graph.

### Portal Nodes

Portal nodes allow "teleporting" transitions across the graph without visible connections. Useful for:

- Reducing visual clutter in complex graphs
- Creating common exit points that multiple states can reach
- Organizing large graphs into logical sections

#### Usage

1. Add a **Portal Out** node where you want to "jump from"
2. Add a **Portal In** node where you want to "jump to"
3. Set both nodes to the same **Channel** (color-coded for easy identification)
4. Connect states to the Portal Out's input, and the Portal In's output to destination states

```
[State A] --> [Portal Out (Ch 0)] ~~~~ [Portal In (Ch 0)] --> [State B]
                    ^                         |
              (no visible connection - matched by channel)
```

#### Rules

- **Many-to-One**: Multiple Portal Out nodes can target the same Portal In channel
- **One Portal In per Channel**: Only one Portal In node allowed per channel (validated)
- **16 Channels**: Color-coded channels (0-15) for visual organization
- **Custom Labels**: Each portal node can have a custom label for clarity

### Transition Timing

Transitions can have configurable delay times:

- **Instant (0s)**: Transition happens immediately
- **Delayed**: Waits specified time before transitioning

**Same-Frame Protection**: The system limits instant transitions to prevent infinite loops (default: 100 per frame).

```csharp
// Configure in StateMachineInstance
stateMachine.MaxSameFrameTransitions = 50;
```

### State Styling

Customize how states appear in the graph editor using attributes.

#### `[StateStyle]`

Set the background color and icon of a state node:

```csharp
using Nonatomic.VSM.Attributes;

[StateStyle(Color = StateColors.Red)]
public class CombatState : State { }

[StateStyle(Color = StateColors.Blue)]
public class IdleState : State { }

[StateStyle(Color = "#FF6B35")]  // Custom hex color
public class CustomState : State { }
```

**Available Colors** (`StateColors` class):

| Name          | Hex       | Aliases  |
|---------------|-----------|----------|
| `Grey`        | `#444444` | `Gray`   |
| `Red`         | `#990e23` |          |
| `Orange`      | `#B06101` |          |
| `LimeGreen`   | `#6d9111` |          |
| `Green`       | `#116f1c` |          |
| `ForestGreen` | `#08704a` |          |
| `Teal`        | `#066670` | `Cyan`   |
| `LightBlue`   | `#037091` |          |
| `Blue`        | `#084870` |          |
| `Purple`      | `#4a0e99` | `Indigo` |
| `Violet`      | `#740e99` |          |
| `Pink`        | `#750b55` |          |
| `Cantera`     | `#551C25` | `Brown`  |
| `Dijon`       | `#a89d46` | `Yellow` |
| `Black`       | `#000000` |          |
| `White`       | `#ffffff` |          |

#### `[StateInfo]`

Add documentation that appears in the editor:

```csharp
[StateInfo(
    Tooltip = "Brief hover text",
    Description = "Detailed description shown in inspector",
    Category = "Combat/Melee"
)]
public class AttackState : State { }
```

## Code-First State Machines

You can create state machines entirely in code using the fluent `StateMachineBuilder` API:

```csharp
using Nonatomic.VSM.Core;
using UnityEngine;

public class CodeBasedController : MonoBehaviour
{
    private StateMachineInstance _stateMachine;

    private void Start()
    {
        _stateMachine = StateMachineBuilder.Create(gameObject)
            .AddState<IdleState>()
            .AddState<MoveState>()
            .AddState<JumpState>()
            .SetEntryState<IdleState>()
            .AddTransition<IdleState, MoveState>("OnMove")
            .AddTransition<IdleState, JumpState>("OnJump")
            .AddTransition<MoveState, IdleState>("OnStop")
            .AddTransition<JumpState, IdleState>("OnLand")
            .AddVariable("Speed", 5f)
            .AddVariable("JumpForce", 10f)
            .BuildAndStart();
    }

    private void Update()
    {
        _stateMachine?.Update();
    }

    private void OnDestroy()
    {
        _stateMachine?.Stop();
    }
}
```

### Builder Methods

| Method                                   | Returns                | Description                                                   |
|------------------------------------------|------------------------|---------------------------------------------------------------|
| `Create(gameObject)`                     | `StateMachineBuilder`  | Creates a builder with GameObject context                     |
| `Create()`                               | `StateMachineBuilder`  | Creates a builder without GameObject                          |
| `AddState<T>(key)`                       | `StateRef`             | Adds a state type with optional key (defaults to type name)   |
| `AddState(type, key)`                    | `StateRef`             | Adds a state type by Type reference                           |
| `SetEntryState<T>()`                     | `StateMachineBuilder`  | Sets which state to start in                                  |
| `SetEntryState(stateRef)`                | `StateMachineBuilder`  | Sets entry state from a StateRef                              |
| `AddTransition<TFrom, TTo>(name, delay)` | `StateMachineBuilder`  | Adds a transition between state types                         |
| `AddTransition(from, name, to, delay)`   | `StateMachineBuilder`  | Adds a transition with string keys                            |
| `AddVariable<T>(key, default, exposed)`  | `StateMachineBuilder`  | Adds a blackboard variable                                    |
| `WithContext(context)`                   | `StateMachineBuilder`  | Sets a custom context                                         |
| `WithLayout(mode)`                       | `StateMachineBuilder`  | Sets layout mode: `Hierarchical` (default), `Grid`, or `None` |
| `WithoutLayout()`                        | `StateMachineBuilder`  | Disables automatic layout                                     |
| `Build()`                                | `StateMachineInstance` | Creates the instance                                          |
| `BuildAndStart()`                        | `StateMachineInstance` | Creates and immediately starts the instance                   |

### Node Positioning

By default, the builder automatically arranges nodes using hierarchical layout. You can customize this:

```csharp
// Manual positioning
var idle = builder.AddState<IdleState>().AtPosition(100, 100);
var move = builder.AddState<MoveState>().AtPosition(400, 100);
var jump = builder.AddState<JumpState>().AtPosition(400, 250);

// Or disable auto-layout entirely
builder.WithoutLayout();

// Or use grid layout instead
builder.WithLayout(GraphLayoutMode.Grid);
```

### Multiple Instances of the Same State

When you need multiple instances of the same state type, use custom keys and `StateRef`:

```csharp
var builder = StateMachineBuilder.Create(gameObject);

// Store references to states
var patrolA = builder.AddState<PatrolState>("patrol_a");
var patrolB = builder.AddState<PatrolState>("patrol_b");
var chase = builder.AddState<ChaseState>();

// Use references for type-safe transitions
var sm = builder
    .SetEntryState(patrolA)
    .AddTransition(patrolA, "OnComplete", patrolB)
    .AddTransition(patrolB, "OnComplete", patrolA)
    .AddTransition(patrolA, "OnDetect", chase)
    .AddTransition(patrolB, "OnDetect", chase)
    .BuildAndStart();
```

You can also chain transitions directly from `StateRef`:

```csharp
var builder = StateMachineBuilder.Create(gameObject);

var idle = builder.AddState<IdleState>();
var move = builder.AddState<MoveState>();
var jump = builder.AddState<JumpState>();

// Chain transitions from each state
idle.TransitionTo("OnMove", move)
    .TransitionTo("OnJump", jump);

move.TransitionTo("OnStop", idle);

jump.TransitionTo("OnLand", idle);

var sm = builder
    .SetEntryState(idle)
    .BuildAndStart();
```

## Graph Variants

Graph Variants provide a delta-based inheritance system for state machine graphs. A variant inherits from a base graph and stores only the differences (overrides), keeping assets
lightweight and version-control friendly.

### Creating a Variant

1. Select a `StateMachineGraph` asset in the Project window
2. Right-click and select **Create Variant**
3. A new `StateMachineGraphVariant` asset is created referencing the base graph

Alternatively, use **Create > Visual State Machine > State Machine Graph Variant** from the Assets menu.

### What Can Be Overridden

| Element                  | Override Options                                           |
|--------------------------|------------------------------------------------------------|
| **Nodes**                | Modify properties, replace state type, or disable entirely |
| **Connections**          | Override transition time, easing curve, or remove          |
| **Blackboard Variables** | Override default value, exposed flag                       |

Variants can also **add** new nodes, connections, and blackboard variables that exist only in the variant.

### How It Works

- Changes to the base graph automatically propagate to all variants
- Overrides are applied on top of the base graph at merge time
- Disabled nodes are excluded along with all their connections
- Variants support nesting up to 8 levels deep (variant of a variant)
- Circular references are detected and prevented

### Usage with StateMachineRunner

Use a variant anywhere you would use a regular graph. Assign it to a `StateMachineRunner` or `StateMachineController` and it behaves like a fully resolved graph:

```csharp
[SerializeField] private StateMachineGraphVariant _hardModeVariant;

private void Start()
{
    var runner = gameObject.AddComponent<StateMachineRunner>();
    runner.Initialize(_hardModeVariant);
    runner.StartStateMachine();
}
```

## Built-in States

VSM3 ships with a library of ready-to-use states organized by category. All built-in states follow a consistent pattern with `[DataIn]` inputs, `[DataOut]` outputs, and
`[Transition]` exit ports.

### Timing

| State                | Description                                | Key Inputs                    | Transitions |
|----------------------|--------------------------------------------|-------------------------------|-------------|
| **DelayState**       | Waits for a duration before transitioning  | `Duration`, `UseUnscaledTime` | Complete    |
| **RandomDelayState** | Waits for a random duration within a range | `MinDuration`, `MaxDuration`  | Complete    |

### Logic & Branching

| State                  | Description                          | Key Inputs                       | Transitions            |
|------------------------|--------------------------------------|----------------------------------|------------------------|
| **ConditionState**     | Branches on a boolean value          | `Condition`, `Invert`            | True, False            |
| **CompareState**       | Compares two numeric values          | `ValueA`, `ValueB`, `Operator`   | True, False            |
| **CompareStringState** | Compares two strings                 | `ValueA`, `ValueB`, `IgnoreCase` | Equal, Not Equal       |
| **RandomBranchState**  | Random two-way branch by probability | `ProbabilityA`                   | Path A, Path B         |
| **RandomBranch3State** | Random three-way branch by weight    | `WeightA`, `WeightB`, `WeightC`  | Path A, Path B, Path C |

### Behavior Tree Patterns

| State                 | Description                                  | Key Inputs                | Transitions        |
|-----------------------|----------------------------------------------|---------------------------|--------------------|
| **SequenceGateState** | AND-gate: all conditions must be true        | `ConditionA`-`D`          | Success, Failure   |
| **SelectorGateState** | Priority selector: transitions on first true | `ConditionA`-`D`          | A, B, C, D, None   |
| **RepeaterState**     | Loop counter with configurable repeat count  | `RepeatCount`, `Infinite` | Loop, Done         |
| **CooldownState**     | Prevents re-entry for a cooldown duration    | `Duration`                | Ready, On Cooldown |
| **TimeoutState**      | Fires timeout after a duration elapses       | `Duration`                | Timeout            |

### Animator

| State                           | Description                          | Key Inputs                                    | Transitions |
|---------------------------------|--------------------------------------|-----------------------------------------------|-------------|
| **SetAnimatorTriggerState**     | Sets an Animator trigger parameter   | `Animator`, `TriggerName`                     | Done        |
| **ResetAnimatorTriggerState**   | Resets an Animator trigger parameter | `Animator`, `TriggerName`                     | Done        |
| **SetAnimatorBoolState**        | Sets an Animator bool parameter      | `Animator`, `ParameterName`, `Value`          | Done        |
| **SetAnimatorFloatState**       | Sets an Animator float parameter     | `Animator`, `ParameterName`, `Value`          | Done        |
| **SetAnimatorIntState**         | Sets an Animator int parameter       | `Animator`, `ParameterName`, `Value`          | Done        |
| **SetAnimatorSpeedState**       | Sets Animator playback speed         | `Animator`, `Speed`                           | Done        |
| **SetAnimatorLayerWeightState** | Sets an Animator layer weight        | `Animator`, `Layer`, `Weight`                 | Done        |
| **GetAnimatorFloatState**       | Reads an Animator float parameter    | `Animator`, `ParameterName`                   | Done        |
| **GetAnimatorBoolState**        | Reads an Animator bool and branches  | `Animator`, `ParameterName`                   | True, False |
| **GetAnimatorIntState**         | Reads an Animator int parameter      | `Animator`, `ParameterName`                   | Done        |
| **PlayAnimatorState**           | Plays an Animator state directly     | `Animator`, `StateName`                       | Done        |
| **CrossFadeAnimatorState**      | Crossfades to an Animator state      | `Animator`, `StateName`, `TransitionDuration` | Done        |
| **WaitForAnimatorStateState**   | Waits for Animator to reach a state  | `Animator`, `StateName`                       | Complete    |

### Audio

| State                     | Description                             | Key Inputs                      | Transitions |
|---------------------------|-----------------------------------------|---------------------------------|-------------|
| **PlayAudioState**        | Plays an audio clip (fire and forget)   | `Clip`, `AudioSource`, `Volume` | Done        |
| **PlayAudioAndWaitState** | Plays a clip and waits for it to finish | `Clip`, `AudioSource`, `Volume` | Complete    |
| **StopAudioState**        | Stops audio playback                    | `AudioSource`                   | Done        |

### Transform

| State                | Description                        | Key Inputs                               | Transitions |
|----------------------|------------------------------------|------------------------------------------|-------------|
| **SetPositionState** | Sets a transform's position        | `Target`, `Position`, `UseLocalSpace`    | Done        |
| **SetRotationState** | Sets a transform's rotation        | `Target`, `EulerAngles`, `UseLocalSpace` | Done        |
| **SetScaleState**    | Sets a transform's local scale     | `Target`, `Scale`                        | Done        |
| **LookAtState**      | Makes a transform look at a target | `Source`, `TargetTransform`              | Done        |
| **MoveToState**      | Translates a transform over time   | `Target`, `Destination`, `Duration`      | Complete    |

### GameObject

| State                     | Description                       | Key Inputs                     | Transitions |
|---------------------------|-----------------------------------|--------------------------------|-------------|
| **SetActiveState**        | Enables or disables a GameObject  | `Target`, `Active`             | Done        |
| **SpawnPrefabState**      | Instantiates a prefab             | `Prefab`, `Position`, `Parent` | Done        |
| **DestroyState**          | Destroys a GameObject             | `Target`, `Delay`              | Done        |
| **DestroyImmediateState** | Destroys a GameObject immediately | `Target`                       | Done        |

### Particles

| State                        | Description                              | Key Inputs                                  | Transitions |
|------------------------------|------------------------------------------|---------------------------------------------|-------------|
| **PlayParticleState**        | Plays a ParticleSystem (fire and forget) | `ParticleSystem`, `WithChildren`            | Done        |
| **PlayParticleAndWaitState** | Plays and waits for completion           | `ParticleSystem`, `WithChildren`            | Complete    |
| **StopParticleState**        | Stops a ParticleSystem                   | `ParticleSystem`, `Clear`, `StopBehavior`   | Done        |
| **SetParticleEmissionState** | Sets emission rate                       | `ParticleSystem`, `Enabled`, `RateOverTime` | Done        |

### Timeline

| State                        | Description                               | Key Inputs                   | Transitions |
|------------------------------|-------------------------------------------|------------------------------|-------------|
| **PlayTimelineState**        | Plays a Timeline and waits for completion | `Asset`, `Director`, `Speed` | Complete    |
| **PauseTimelineState**       | Pauses a playing timeline                 | `Director`                   | Done        |
| **ResumeTimelineState**      | Resumes a paused timeline                 | `Director`, `Speed`          | Done        |
| **StopTimelineState**        | Stops a playing timeline                  | `Director`                   | Done        |
| **WaitForPlaybackTimeState** | Waits for a specific playback time        | `Director`, `TargetTime`     | Reached     |

### Input

| State                       | Description                    | Key Inputs            | Transitions |
|-----------------------------|--------------------------------|-----------------------|-------------|
| **WaitForKeyState**         | Waits for a specific key press | `Key`, `InputMode`    | Pressed     |
| **WaitForAnyKeyState**      | Waits for any key press        | -                     | Pressed     |
| **WaitForMouseButtonState** | Waits for a mouse button click | `Button`, `InputMode` | Clicked     |

### Dialog

| State                 | Description                                    | Key Inputs                    | Transitions |
|-----------------------|------------------------------------------------|-------------------------------|-------------|
| **DialogState**       | Displays dialog text, waits for player advance | `Text`, `Speaker`, `Portrait` | Continue    |
| **TimedDialogState**  | Displays dialog, auto-advances after delay     | `Text`, `Speaker`, `Duration` | Continue    |
| **DialogChoiceState** | Presents up to four choices                    | `ChoiceA`-`D`                 | Choice A-D  |
| **ClearDialogState**  | Clears the dialog UI                           | `OnClear` (UnityEvent)        | Done        |

### Events

| State                      | Description                    | Key Inputs       | Transitions |
|----------------------------|--------------------------------|------------------|-------------|
| **InvokeEventState**       | Invokes a UnityEvent           | `Event`          | Done        |
| **InvokeStringEventState** | Invokes a UnityEvent\<string\> | `Event`, `Value` | Done        |
| **InvokeFloatEventState**  | Invokes a UnityEvent\<float\>  | `Event`, `Value` | Done        |
| **InvokeIntEventState**    | Invokes a UnityEvent\<int\>    | `Event`, `Value` | Done        |

### Utility

| State               | Description                      | Key Inputs             | Transitions |
|---------------------|----------------------------------|------------------------|-------------|
| **LogState**        | Logs a message to the console    | `Message`, `Level`     | Done        |
| **DebugBreakState** | Pauses the editor for inspection | `Message`, `BreakOnce` | Continue    |

## Editor Features

### Graph Window

Open via **Window > Visual State Machine > Graph Editor**

- **Node Creation**: Right-click to add states from a searchable menu
- **Connections**: Drag from port to port to create transitions
- **Navigation**: Use breadcrumbs for sub-state machines
- **Zoom/Pan**: Scroll to zoom, middle-click to pan
- **Search**: Ctrl+F to search for nodes and regions

### Regions

Regions are visual containers for organizing nodes into logical groups. They are purely organizational and do not affect state machine execution.

#### Creating a Region

1. Right-click on the graph canvas
2. Select **Add Node > Organization > Region**
3. A new region appears at the click location

#### Working with Regions

- **Rename**: Double-click the region header to edit the title (Enter to confirm, Escape to cancel)
- **Add nodes**: Drag any node into the region to group it
- **Remove nodes**: Drag a node out of the region to ungroup it
- **Resize**: Drag the region edges to adjust the size
- **Move**: Drag the region header to reposition it along with all contained nodes
- **Delete**: Right-click the region and select **Delete Group** (contained nodes are preserved)

Region layout is persistent across editor sessions. Regions also appear in the graph search bar for quick navigation.

### SubGraph Preview

Quickly inspect the contents of a Sub State Machine without navigating into it. Each SubStateMachine node has a **Peek** button alongside the existing **Open** button.

#### Usage

1. Click the **Peek** button on any SubStateMachine node
2. A floating preview popup appears showing a miniature view of the sub-graph
3. Click **Peek** again (or press **Escape**) to close the popup

#### Preview Controls

| Action         | Control                                                |
|----------------|--------------------------------------------------------|
| **Pan**        | Drag with left or middle mouse button                  |
| **Zoom**       | Scroll wheel (0.25x to 4x)                             |
| **Recenter**   | Click the recenter button in the header or press **F** |
| **Move popup** | Drag the popup header                                  |
| **Close**      | Click X, press Escape, or click Peek again             |

The preview shows all nodes with color-coded types, connection curves with directional arrows, and a node count in the info bar. It auto-scales to fit all nodes and follows its
owner node when repositioned.

### Layout Tools

- **Auto Layout**: Automatically arranges nodes hierarchically from left to right based on connections
- **Grid Layout**: Arranges nodes in a simple grid pattern
- Use the toolbar **Layout** menu or **Auto Layout** button to organize messy graphs

## Debugging Tools

### Runtime Monitor

Open via **Window > Visual State Machine > Runtime Monitor**

The Runtime Monitor provides a live view of all running state machine instances during play mode:

- **Instance list**: All active instances grouped by graph, with status indicators (green = running, yellow = paused, gray = stopped, red = error)
- **Current state tracking**: See which state each instance is currently in
- **Blackboard inspector**: View live blackboard variable values and types
- **Quick navigation**: Click "Open" to jump to the active state in the graph editor

### Breakpoints

Set breakpoints on state nodes to pause the editor when specific states are entered during play mode.

#### Setting a Breakpoint

1. Right-click a state node in the graph editor
2. Select **Add Breakpoint** - a red dot appears on the node
3. Enter play mode - the editor pauses when that state is entered

#### Breakpoint Types

| Type              | Indicator  | Description                                          |
|-------------------|------------|------------------------------------------------------|
| **On Entry**      | Red dot    | Pauses whenever the state is entered                 |
| **On Transition** | Red dot    | Pauses only when entered via a specific port         |
| **Conditional**   | Orange dot | Pauses when a condition expression evaluates to true |
| **Disabled**      | Gray dot   | Breakpoint exists but is inactive                    |

#### Conditional Breakpoint Expressions

Right-click a breakpoint node and select **Edit Breakpoint Condition…** to open the condition editor. Supported syntax:

```
Health < 10          // Numeric comparison
Score >= 100         // Greater-or-equal
IsAlive == false     // Boolean comparison
Status == "Running"  // String comparison
!GameOver            // Boolean negation
GameOver             // Simple truthy check
```

#### Breakpoint List Panel

A sidebar panel in the graph editor shows all breakpoints for the current graph:

- Toggle individual breakpoints on/off
- Toggle all breakpoints globally
- Click a node name to navigate to it in the graph
- Clear all breakpoints at once

### Transition Timeline

The transition timeline view (visible in the Runtime Monitor details panel) shows a chronological record of all state transitions:

- **Timestamp**: When each transition occurred (millisecond precision)
- **Transition label**: "FromState -> ToState" with trigger port name
- **Duration badge**: Time spent in the previous state
- **Blackboard snapshots**: Click any transition to see the blackboard state at that moment

#### Diff Mode

Compare blackboard state between any two transitions:

1. Enable **Diff Mode** in the timeline toolbar
2. Right-click a transition to set it as the diff anchor (highlighted orange)
3. Click another transition to compare
4. View side-by-side changes: orange = changed, green = added, red = removed

### Replay Controller

Step through recorded transition history to review state machine behavior:

| Control            | Description                                         |
|--------------------|-----------------------------------------------------|
| **Play**           | Auto-play through transitions at configurable speed |
| **Pause**          | Stop on the current transition                      |
| **Step Forward**   | Advance one transition                              |
| **Step Backward**  | Go back one transition                              |
| **Return to Live** | Exit replay and follow the live state               |
| **Speed**          | Cycle through 0.5x, 1x, 2x, 4x playback             |

### Transition History Export

Export the complete transition history to JSON for external analysis or bug reports:

1. Click the export button in the Debug Timeline panel
2. Choose a save location
3. The JSON file includes: instance metadata, all transitions with timestamps, trigger ports, durations, and full blackboard snapshots at each point

```json
{
  "instanceId": "...",
  "graphName": "PlayerStateMachine",
  "ownerName": "Player",
  "exportedAt": "2025-02-15T14:30:00",
  "transitions": [
    {
      "timestamp": "2025-02-15T14:29:50",
      "from": { "nodeId": "...", "stateName": "Idle" },
      "to": { "nodeId": "...", "stateName": "Running" },
      "triggerPort": "OnMove",
      "durationFromPrevious": "0.500s",
      "blackboard": { "Speed": "5.5", "Direction": "Forward" }
    }
  ]
}
```

## Performance

### Update Modes

Configure how the state machine updates via the `StateMachineRunner` component:

| Mode            | Description                                                      |
|-----------------|------------------------------------------------------------------|
| **Update**      | Ticks in `MonoBehaviour.Update()` (default)                      |
| **FixedUpdate** | Ticks in `MonoBehaviour.FixedUpdate()` for physics-driven states |
| **LateUpdate**  | Ticks in `MonoBehaviour.LateUpdate()` for camera/follow states   |
| **Manual**      | No automatic ticking; call `Instance.Update()` yourself          |

Set in the Inspector on `StateMachineRunner`, or in code:

```csharp
runner.UpdateMode = UpdateMode.FixedUpdate;
```

### Instance Pooling

`StateMachinePool` reduces garbage collection pressure by reusing state machine instances:

```csharp
var pool = new StateMachinePool();

// Pre-warm during loading to avoid runtime allocation spikes
pool.Prewarm(enemyGraph, count: 10);

// Optional: cap pool size per graph (0 = unlimited)
pool.SetMaxPoolSize(enemyGraph, maxSize: 20);

// Rent an instance instead of creating one
var instance = pool.Rent(enemyGraph, context);
instance.Start();

// Return to pool when done instead of destroying
instance.Stop();
pool.Return(instance);
```

The pool automatically grows beyond the pre-warmed capacity when needed. Instances call `PrepareForReuse()` and `OnReset()` on all states before being returned, ensuring clean
reuse.

**Pool events:**

| Event        | Description                                       |
|--------------|---------------------------------------------------|
| `OnRented`   | Instance rented (includes `wasNewlyCreated` flag) |
| `OnReturned` | Instance returned to pool                         |
| `OnPoolGrew` | Pool auto-grew beyond pre-warmed capacity         |

## Building for IL2CPP & WebGL

VSM3 supports both the Mono and IL2CPP scripting backends. The 0.4 release is validated with
Windows IL2CPP and WebGL builds at **Managed Stripping Level = High**, with sample state
machines (including states defined in their own assemblies) running end-to-end.

### Managed code stripping is handled automatically

VSM3 instantiates states from type names stored in graph assets and binds ports by
reflection — references the Unity linker cannot see, which would normally cause custom
states to be stripped from IL2CPP builds. The package handles this for you with two
mechanisms:

- A `link.xml` shipped inside the package preserves VSM3's own runtime assemblies.
- A build-time processor scans your project for `State` subclasses and `[BlackboardType]`
  types — including those in your own assemblies — and automatically generates preservation
  entries for them.

Your custom states and blackboard types survive any Managed Stripping Level with **no
configuration required**. You do not need to write a `link.xml` or add `[Preserve]`
attributes for VSM3 types.

If generation ever fails you will see `[VSM] Failed to generate link.xml` in the build log.
The build still completes (the package's own assemblies remain preserved), but custom
states may be stripped — please report it as a bug; adding your own `link.xml` covering
your state assemblies works as a stopgap.

### Custom value-type blackboard variables under IL2CPP

Blackboard variables are backed by generic classes (`BlackboardVariable<T>`). IL2CPP
requires generic code to exist ahead of time, but Unity's full generic sharing (always
available on VSM3's minimum supported version, 2022.3) generates it automatically — custom
`[BlackboardType]` structs work out of the box. If an exotic platform configuration ever
reports missing AOT code for one of your value types, force generation with an explicit
instantiation hint anywhere in your code:

```csharp
// Never called — exists only so IL2CPP generates the concrete generic code.
static void AotHint() => new BlackboardVariable<DamageInfo>();
```

## Integrations

VSM3 integrations are optional assemblies that only compile when their dependencies are present. No configuration is needed - install the required Unity package and the integration
activates automatically.

### Addressables

**Requires:** `com.unity.addressables` package

Provides async asset loading and instantiation through Unity's Addressables system:

| State                       | Description                               | Key Inputs                             | Transitions     |
|-----------------------------|-------------------------------------------|----------------------------------------|-----------------|
| **LoadAddressableState**    | Loads an addressable asset asynchronously | `AssetReference`                       | Loaded, Failed  |
| **SpawnAddressableState**   | Instantiates an addressable prefab        | `AssetReference`, `Parent`, `Position` | Spawned, Failed |
| **ReleaseAddressableState** | Releases an addressable instance          | `Instance`                             | Done            |

All addressable states support cancellation and report progress during loading.

### Visual Scripting

**Requires:** `com.unity.visualscripting` package

Allows designers to build state logic using Unity's Visual Scripting graphs instead of C#:

**VisualScriptingState** - Executes a `ScriptGraphAsset` as the state body. The graph receives `StateEnter`, `StateUpdate`, and `StateExit` custom events. When `SyncBlackboard` is
enabled (default), blackboard variables are synced to graph variables on enter and synced back on exit.

**Custom Units** available inside Visual Script graphs:

| Unit                 | Category       | Description                                        |
|----------------------|----------------|----------------------------------------------------|
| **Complete State**   | VSM            | Triggers the Done transition to exit the state     |
| **Get VSM Variable** | VSM/Blackboard | Reads a variable from the state machine blackboard |
| **Set VSM Variable** | VSM/Blackboard | Writes a variable to the state machine blackboard  |

## API Reference

### StateMachineBuilder

```csharp
public class StateMachineBuilder
{
    // Creation
    static StateMachineBuilder Create(GameObject gameObject);
    static StateMachineBuilder Create();

    // States (AddState returns StateRef for positioning and chaining)
    StateRef AddState<T>(string key = null);
    StateRef AddState(Type stateType, string key = null);
    StateMachineBuilder SetEntryState(string stateKey);
    StateMachineBuilder SetEntryState<T>();
    StateMachineBuilder SetEntryState(StateRef stateRef);

    // Transitions
    StateMachineBuilder AddTransition(string fromKey, string transitionName, string toKey, float delay = 0);
    StateMachineBuilder AddTransition<TFrom, TTo>(string transitionName, float delay = 0);

    // Variables
    StateMachineBuilder AddVariable<T>(string key, T defaultValue = default, bool exposed = false);

    // Context
    StateMachineBuilder WithContext(IStateMachineContext context);

    // Layout
    StateMachineBuilder WithLayout(GraphLayoutMode mode);
    StateMachineBuilder WithoutLayout();

    // Build
    StateMachineInstance Build();
    StateMachineInstance BuildAndStart();

    // Advanced
    StateMachineGraph GetGraph();
    StateNode GetStateNode(string key);
}
```

### StateMachineRunner

```csharp
public class StateMachineRunner : MonoBehaviour
{
    // Initialize with a graph asset
    void Initialize(StateMachineGraph graph);
    void Initialize(StateMachineGraph graph, IStateMachineContext context);

    // Control
    void StartStateMachine();
    void StopStateMachine();
    void RestartStateMachine();
    void PauseStateMachine();
    void ResumeStateMachine();

    // Configuration
    UpdateMode UpdateMode { get; set; }

    // Access
    Blackboard Blackboard { get; }
    StateMachineInstance Instance { get; }
    StateMachineGraph Graph { get; set; }
    bool IsRunning { get; }
    State CurrentState { get; }

    // Blackboard shortcuts
    void SetBlackboardValue<T>(string key, T value);
    T GetBlackboardValue<T>(string key, T defaultValue = default);

    // Per-instance variable overrides (set via Inspector)
    List<BlackboardOverride> VariableOverrides { get; }

    // Events
    event Action<State, State> OnStateChanged;
}
```

### StateMachineController\<TContext\>

A non-MonoBehaviour helper for running state machines with a typed context. Use this when you want to embed a state machine inside an existing MonoBehaviour and share
strongly-typed data with states.

**When to use Controller vs Runner:**

- Use `StateMachineRunner` when you want a quick drag-and-drop MonoBehaviour with Inspector support and exposed variable overrides
- Use `StateMachineController<TContext>` when you want to embed a state machine inside an existing MonoBehaviour and share typed context data with states

```csharp
public class StateMachineController<TContext> where TContext : class
{
    // Constructor
    StateMachineController(StateMachineGraph graph, TContext context, GameObject owner = null);

    // Control
    void Start();
    void Stop();
    void Pause();
    void Resume();
    void Restart();

    // Lifecycle - call from your MonoBehaviour
    void Update();
    void FixedUpdate();
    void LateUpdate();

    // Access
    StateMachineInstance Instance { get; }
    TContext Context { get; }
    State CurrentState { get; }
    Blackboard Blackboard { get; }
    bool IsRunning { get; }
    bool IsPaused { get; }

    // Blackboard shortcuts
    void SetBlackboardValue<T>(string key, T value);
    T GetBlackboardValue<T>(string key, T defaultValue = default);

    // Cleanup
    void Dispose();

    // Events
    event Action<State, State> OnStateChanged;
    event Action OnStarted;
    event Action OnStopped;
}

// Non-generic version (no typed context)
public class StateMachineController : StateMachineController<object>
{
    StateMachineController(StateMachineGraph graph, GameObject owner);
}
```

### StateMachineInstance

```csharp
public class StateMachineInstance
{
    // Events
    event Action<State, State> OnStateChanged;
    event Action OnStarted;
    event Action OnStopped;
    event Action<string> OnExited;  // Fired when reaching an Exit node
    event Action<string, string> OnTransitionTriggered;  // (fromNodeId, toNodeId)

    // Properties
    State CurrentState { get; }
    string CurrentStateNodeId { get; }
    Blackboard Blackboard { get; }
    StateMachineGraph Graph { get; }
    StateMachineInstance Parent { get; }  // Parent if this is a sub-state machine
    StateMachineInstance ActiveChildInstance { get; }  // Active sub-state machine
    bool IsRunning { get; }
    bool IsPaused { get; }
    bool IsInSubStateMachine { get; }
    int NestingDepth { get; }  // 0 for root, 1+ for nested
    object SharedContext { get; }  // For State<TContext> states

    // Configuration
    int MaxSameFrameTransitions { get; set; }  // Default: 100
    const int MaxSubStateMachineDepth = 16;

    // Lifecycle
    void Start();
    Task StartAsync();
    void Stop();
    Task StopAsync();
    void Pause();
    void Resume();

    // Update (call from MonoBehaviour)
    void Update();
    void FixedUpdate();
    void LateUpdate();
    Task UpdateAsync();

    // Context
    void SetSharedContext<TContext>(TContext context);
}
```

### Blackboard

```csharp
public class Blackboard
{
    // Events
    event Action<string, object, object> OnVariableChanged;

    // Get/Set
    T Get<T>(string key);
    T Get<T>(string key, T defaultValue);
    void Set<T>(string key, T value);
    bool TryGet<T>(string key, out T value);

    // Queries
    bool HasKey(string key);
    IEnumerable<string> GetAllKeys();
    Type GetValueType(string key);

    // Subscriptions
    IDisposable Subscribe<T>(string key, Action<T, T> onChange);

    // Hierarchy
    void SetParent(Blackboard parent);
}
```

### State

```csharp
public abstract class State
{
    // References
    StateMachineInstance StateMachine { get; }
    IStateMachineContext Context { get; }
    Blackboard Blackboard { get; }
    GameObject GameObject { get; }
    Transform Transform { get; }
    bool IsActive { get; }

    // Synchronous lifecycle (override these)
    virtual void OnEnter() { }
    virtual void OnUpdate() { }
    virtual void OnFixedUpdate() { }
    virtual void OnLateUpdate() { }
    virtual void OnExit() { }
    virtual void OnReset() { }  // Called when returning to pool

    // Asynchronous lifecycle (override these)
    virtual Task OnEnterAsync(CancellationToken ct) { }
    virtual Task OnEnterAsync() { }
    virtual Task OnUpdateAsync(CancellationToken ct) { }
    virtual Task OnUpdateAsync() { }
    virtual Task OnExitAsync(CancellationToken ct) { }
    virtual Task OnExitAsync() { }

    // Utilities
    T GetComponent<T>() where T : Component;
    T GetOrAddComponent<T>() where T : Component;
    void Log(string message);
    void LogWarning(string message);
    void LogError(string message);
}

// Generic version with typed context
public abstract class State<TContext> : State where TContext : class
{
    TContext SharedContext { get; }
    Type RequiredContextType { get; }
    bool HasValidContext { get; }
}
```

### StateMachinePool

```csharp
public class StateMachinePool
{
    // Pre-warming
    void Prewarm(StateMachineGraph graph, int count, IStateMachineContext context = null);
    void SetMaxPoolSize(StateMachineGraph graph, int maxSize);

    // Rent/Return
    StateMachineInstance Rent(StateMachineGraph graph, IStateMachineContext context,
        object sharedContext = null, StateMachineInstance parent = null);
    void Return(StateMachineInstance instance);

    // Stats
    int TotalPooled { get; }
    int TotalCreated { get; }

    // Events
    event Action<StateMachineInstance, bool> OnRented;   // (instance, wasNewlyCreated)
    event Action<StateMachineInstance> OnReturned;
    event Action<StateMachineGraph, int> OnPoolGrew;     // (graph, newSize)
}
```

## Extending VSM3

The supported extension points in 0.4:

| Extension point          | How                                                                                                        |
|--------------------------|------------------------------------------------------------------------------------------------------------|
| Custom states            | Subclass `State` or `State<TContext>` — see [Quick Start](#quick-start) and [Attributes](#attributes)        |
| Custom blackboard types  | `[BlackboardType]` on enums and serializable structs/classes — see [Custom Blackboard Types](#custom-blackboard-types) |
| State node appearance    | `[StateStyle]` and `[StateInfo]` attributes — see [State Styling](#state-styling)                            |
| Runtime graph appearance | `RuntimeGraphTheme` asset (**Create → Visual State Machine → Runtime Graph Theme**) for the world-space runtime graph view |

### Custom state conventions

- **One state class per file, named after the state.** This keeps type resolution
  predictable and is required by the rename-safe resolution tooling planned for 0.5.
- **Treat state class names, namespaces, and assemblies as part of your save format.**
  Graphs currently reference states by assembly-qualified name, so renaming a state class
  (or moving it to another namespace or assembly) breaks existing graph references to it.
  Rename-safe, GUID-based resolution is planned for 0.5; until then, rename with care and
  re-assign affected nodes in the graph editor afterwards.
- **Builds need no extra work.** Custom states are preserved automatically under IL2CPP
  managed stripping — see [Building for IL2CPP & WebGL](#building-for-il2cpp--webgl).

### What is intentionally internal in 0.4

The graph's node-type hierarchy (`GraphNode` subclasses), port kinds, and editor node views
are not extension points in 0.4 — custom node types serialized into user assets would
inherit the type-rename fragility described above. Opening these hierarchies is planned for
0.5, after rename-safe type resolution lands. If you need a custom node vocabulary today,
model it with custom states.

## Testing Custom States

You can unit test state machines in Unity's EditMode tests using NUnit. The key pattern is to build a `StateMachineGraph` programmatically and run it through a
`StateMachineInstance`.

### Test Setup

Create a test class with setup and teardown that manages the graph lifecycle:

```csharp
using NUnit.Framework;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

[TestFixture]
public class MyStateTests
{
    private StateMachineGraph _graph;
    private StateMachineInstance _instance;

    [SetUp]
    public void SetUp()
    {
        _graph = ScriptableObject.CreateInstance<StateMachineGraph>();
    }

    [TearDown]
    public void TearDown()
    {
        _instance?.Stop();
        _instance = null;

        if (_graph != null)
        {
            Object.DestroyImmediate(_graph);
        }
    }
}
```

### Building a Test Graph

Construct a graph with entry node, state nodes, and connections:

```csharp
private void SetupGraph()
{
    var entry = new EntryNode { Id = "entry" };
    var stateA = new StateNode
    {
        Id = "stateA",
        DisplayName = "State A",
        StateTypeAssemblyQualifiedName = typeof(MyStateA).AssemblyQualifiedName
    };
    var exit = new ExitNode { Id = "exit", ExitName = "Done" };

    _graph.AddNode(entry);
    _graph.AddNode(stateA);
    _graph.AddNode(exit);
    _graph.EntryNodeId = "entry";

    // Entry -> State A
    _graph.AddConnection(new Connection
    {
        Id = "c1",
        SourceNodeId = "entry",
        SourcePortName = "out",
        TargetNodeId = "stateA",
        TargetPortName = "in",
        ConnectionType = ConnectionType.Transition
    });
}
```

### Testing State Lifecycle

Create test states with counters to verify enter/update/exit calls:

```csharp
using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

private class TestState : State
{
    public static int EnterCount;
    public static int ExitCount;

    [Transition("Next")] public Action next;

    public override void OnEnter() => EnterCount++;
    public override void OnExit() => ExitCount++;

    public static void ResetCounters()
    {
        EnterCount = 0;
        ExitCount = 0;
    }
}

[Test]
public void Start_EntersFirstState()
{
    TestState.ResetCounters();
    SetupGraph();

    _instance = new StateMachineInstance(_graph);
    _instance.Start();

    Assert.AreEqual(1, TestState.EnterCount);
}
```

### Testing Transitions

Verify state transitions by invoking transition actions:

```csharp
[Test]
public void Transition_MovesToNextState()
{
    SetupGraph(); // Graph with StateA -> StateB connection
    _instance = new StateMachineInstance(_graph);
    _instance.Start();

    // Trigger the transition
    var currentState = _instance.CurrentState as TestStateA;
    currentState?.next?.Invoke();

    Assert.IsInstanceOf<TestStateB>(_instance.CurrentState);
}
```

### Testing Blackboard Data Flow

Test that blackboard values are available to states:

```csharp
[Test]
public void Blackboard_ValuesAccessibleFromInstance()
{
    SetupGraph();
    _instance = new StateMachineInstance(_graph);

    _instance.Blackboard.Set("health", 100);

    Assert.AreEqual(100, _instance.Blackboard.Get<int>("health"));
}
```

To test `[DataIn]` bindings, add blackboard variable nodes and data connections to your test graph:

```csharp
// Add a blackboard variable
_graph.AddBlackboardVariable(new BlackboardVariableDefinition
{
    Key = "speed",
    ValueType = typeof(float),
    DefaultValue = 5.0f
});

// Add a blackboard Get node
var bbNode = new BlackboardVariableNode
{
    Id = "bbSpeed",
    VariableKey = "speed",
    Mode = BlackboardNodeMode.Get
};
_graph.AddNode(bbNode);

// Connect blackboard output to state input
_graph.AddConnection(new Connection
{
    Id = "dataConn",
    SourceNodeId = "bbSpeed",
    SourcePortName = "value",
    TargetNodeId = "stateA",
    TargetPortName = "_speed",
    ConnectionType = ConnectionType.BlackboardRead
});
```

### Testing with the Builder API

For simpler test setups, use `StateMachineBuilder`:

```csharp
[Test]
public void Builder_CreatesWorkingStateMachine()
{
    var instance = StateMachineBuilder.Create()
        .AddState<IdleState>()
        .AddState<MoveState>()
        .SetEntryState<IdleState>()
        .AddTransition<IdleState, MoveState>("OnMove")
        .BuildAndStart();

    Assert.IsInstanceOf<IdleState>(instance.CurrentState);

    instance.Stop();
}
```

### Test Assembly Setup

Place EditMode tests in `Tests/EditMode/` with an assembly definition referencing:

- `Nonatomic.VSM.Runtime` (for `State`, `StateMachineInstance`, etc.)
- `UnityEngine.TestRunner`
- `UnityEditor.TestRunner`

## Sample Projects

The sample projects are maintained on this repository's `samples` branch while they are
polished for release. They are exercised against the package continuously — including the
IL2CPP/WebGL build validation — and will ship as Package Manager-importable samples in a
future release.

## Troubleshooting

### "Possible infinite loop detected"

You have a cycle of instant transitions. Add transition delays or break the cycle.

### "Entry node has no outgoing transition"

The entry node must be connected to an initial state. If you've just updated the package, try deleting and recreating the connection from the Entry node.

### State inputs are null or have default values

- Ensure the blackboard variable is set before the transition
- Check that the blackboard variable node is connected to the state's input port
- Verify the variable has a value set (either default in the graph or override in the runner)
- Mark the input as not required with a default value if it's optional

### Exposed variables not appearing in Runner Inspector

- Toggle the "Expose" checkbox in the Blackboard panel
- Ensure the graph asset is assigned to the StateMachineRunner
- Check that the variable type is supported (primitives, Vector2/3, Color, UnityEngine.Object)

### Graph not updating at runtime

Make sure you're calling `Update()` on the StateMachineInstance, or use StateMachineRunner which handles this automatically. Check that `UpdateMode` is not set to `Manual` unless
you intend to update manually.

### "SubStateMachine nesting depth exceeded"

You have more than 16 levels of nested sub-state machines. This usually indicates a circular reference or overly complex hierarchy. Simplify your graph structure.

### "Circular sub-state machine reference detected"

Graph A contains Graph B which contains Graph A (directly or indirectly). Each sub-graph must be unique in the parent chain. Restructure to avoid cycles.

### Sub-state machine context type error

The sub-graph requires a typed context that's incompatible with the parent's context. Solutions:

- Change the sub-graph to use the same context type as parent
- Change the sub-graph to use a base type (e.g., `MonoBehaviour` instead of specific class)
- Use `State` instead of `State<TContext>` in the sub-graph if context isn't needed

### Portal node has no matching target

A Portal Out node's channel doesn't have a corresponding Portal In node. Add a Portal In node with the same channel ID, or delete the orphaned Portal Out.

### Breakpoint not pausing the editor

- Check that breakpoints are globally enabled (toggle in the Breakpoint List panel)
- Verify the individual breakpoint is enabled (not gray)
- For conditional breakpoints, verify the expression syntax matches a supported format
- Breakpoints only work during play mode in the editor

## Licence

VSM3 is released under the [MIT licence](LICENSE). You're free to use it in personal or commercial projects, modify it, and redistribute it. Attribution is appreciated but not required.

## Contributing

VSM3 is developed on the `develop` branch with feature work happening on `feat/*` branches via git worktrees. Open an issue to discuss bigger changes before sending a PR.

Coding standards: tabs (4-width), Allman braces, `_camelCase` private fields, one State class per `.cs` file (required for GUID-based type resolution), no `#region` directives. Full house style: <https://github.com/PaulNonatomic/CodingStandards>.
