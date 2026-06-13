# Patrolling Guard Sample

An advanced example demonstrating AI behavior with detection, pursuit, patrol, and evasion patterns using two interacting state machines.

## Features Demonstrated

- Complex `State<T>` context with mutable shared data
- Multiple transition paths from states (Patrol can go to Idle OR Alert)
- Waypoint-based movement with Transform references
- Detection system with range and angle checks
- State-specific visual feedback (color changes)
- Return-to-patrol behavior after losing target
- Target evasion AI with wander/flee/safe states
- Rigidbody-based collision to prevent guard/target overlap
- Hysteresis-based detection (awareness range vs danger range)

## Setup Instructions

After importing, open `PatrollingGuard.unity` and configure:

1. **Guard GameObject**:
    - Add `StateMachineRunner` component
    - Add `GuardController` component
    - Assign `GuardGraph` to the Graph field
    - Create a child Sphere for state indicator

2. **Configure GuardController**:
    - Create empty GameObjects as waypoints
    - Assign waypoints to the Waypoints list
    - Assign a target Transform (e.g., the player)
    - Set detection range and angle

3. **Waypoints**:
    - Create 3-4 empty GameObjects positioned around the scene
    - Name them "Waypoint1", "Waypoint2", etc.
    - Assign them to the Guard's Waypoints list

4. **Target GameObject**:
    - Create a capsule as the "target"
    - Add `Rigidbody` component (useGravity = false, freeze Y position + all rotation)
    - Add `TargetController` component
    - Add `StateMachineRunner` component
    - Assign `TargetGraph` to the Graph field
    - Set `TargetController` as the shared context on the Runner
    - Assign the Guard's Transform to `TargetController.Guard`
    - Create a child Sphere for state indicator
    - Assign its Transform to the Guard's Target field

5. **Physics Setup**:
    - Add `Rigidbody` to Guard (useGravity = false, freeze Y position + all rotation)
    - Assign the Guard's Rigidbody to `GuardController.Rigidbody`
    - Assign the Target's Rigidbody to `TargetController.Rigidbody`
    - Both GameObjects should have CapsuleColliders (on child objects)

6. **Create TargetGraph**:
    - Create a new State Machine Graph asset named `TargetGraph`
    - Add states: WanderState, WanderPauseState, FleeState, SafeState
    - Wire transitions (see Target State Machine Flow below)

## Guard State Machine Flow

```
                            ┌────────────────────────────────┐
                            │                                │
                            ▼                                │
Entry → Patrol ←──→ Idle    │                                │
           │         │      │                                │
           │ (detect)│      │                                │
           ▼         ▼      │                                │
         Alert ◄────────────┤                                │
           │                │                                │
    ┌──────┴──────┐         │                                │
    │             │         │                                │
    ▼             ▼         │                                │
  Chase ───→ Return ────────┘
```

## Target State Machine Flow

```
Entry → Wander ←──→ WanderPause
           │  ↑          │
           │  │          │ (guard detected)
           │  │          ▼
           │  └──── Safe ←── Flee
           │         ↑       ↑
           │ (guard) │       │
           └─────────┼──→ Flee
                     │
                     └── (guard re-detected)
```

**Transitions:**

- Entry → WanderState
- WanderState "Reached Destination" → WanderPauseState
- WanderState "Guard Detected" → FleeState
- WanderPauseState "Resume Wander" → WanderState
- WanderPauseState "Guard Detected" → FleeState
- FleeState "Reached Safety" → SafeState
- SafeState "Resume Wander" → WanderState
- SafeState "Guard Detected" → FleeState

## Guard States

- **PatrolState**: Moves between waypoints, checks for target detection
- **IdleState**: Waits at waypoint for random duration, checks for detection
- **AlertState**: Brief pause upon detection, decides to chase or give up
- **ChaseState**: Pursues target at increased speed
- **ReturnState**: Returns to patrol route after losing target

## Target States

- **WanderState**: Picks random positions within area bounds and moves to them
- **WanderPauseState**: Brief pause between wander destinations, still alert
- **FleeState**: Runs away from guard at increased speed
- **SafeState**: Recovery cooldown after fleeing, returns to wander

## Key Concepts

### Detection System

The `GuardController` provides detection logic:

```csharp
public bool CanSeeTarget()
{
    if (_target == null) return false;

    // Check range
    if (distance > _detectionRange) return false;

    // Check angle (field of view)
    var angle = Vector3.Angle(transform.forward, directionToTarget);
    if (angle > _detectionAngle) return false;

    return true;
}
```

### State Context

All states share data through the `GuardController`:

```csharp
SharedContext.LastKnownTargetPosition = SharedContext.Target.position;
SharedContext.ReturnPosition = SharedContext.Transform.position;
```

### Visual Feedback

States update the guard's visual indicator:

```csharp
public override void OnEnter()
{
    SharedContext.SetChaseVisual(); // Turns indicator red
}
```

### Target Evasion

The `TargetController` uses omnidirectional awareness with hysteresis:

- **Awareness range** (6 units): triggers flee when guard enters
- **Danger range** (10 units): guard must exceed this distance before target feels safe

This prevents rapid state oscillation when the guard is near the boundary.

### Rigidbody Collision

Both guard and target use `Rigidbody.MovePosition()` for movement with dynamic Rigidbodies
(useGravity = false, frozen Y position and rotation). This ensures the physics engine
prevents the two from overlapping while movement remains code-controlled.
