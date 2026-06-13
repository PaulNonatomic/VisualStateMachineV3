# Door Sample

An intermediate example demonstrating input-driven states and animation transitions.

## Features Demonstrated

- `State<T>` pattern with shared context (`DoorController`)
- Input handling in `OnUpdate` (E key to interact)
- Intermediate animation states (Opening/Closing with Lerp)
- Cyclic state flow (Closed → Opening → Open → Closing → Closed)
- Auto-close timeout functionality

## Setup Instructions

After importing, open `Door.unity` and configure the Door GameObject:

1. **Add Components** (if missing):
    - Add `StateMachineRunner` component
    - Add `DoorController` component

2. **Configure StateMachineRunner**:
    - Assign `DoorGraph` to the Graph field
    - Enable "Auto Start"

3. **Configure DoorController**:
    - Door Pivot: The transform that rotates (usually a child object)
    - Open Angle: Target rotation when open (default: 90)
    - Open Speed: Animation speed multiplier (default: 2)
    - Auto Close Delay: Seconds before auto-closing (default: 5)
    - Interaction Range: How close player must be (default: 3)

4. **Play the scene**:
    - Move camera within range of the door
    - Press E to open/close the door
    - Door auto-closes after 5 seconds if left open

## State Machine Flow

```
Entry → Closed ──(E pressed)──→ Opening ──(animation done)──→ Open
           ↑                                                    │
           │                                          (E or timeout)
           │                                                    ↓
           └────────(animation done)────── Closing ←────────────┘
```

## Custom States

- **ClosedState**: Door is fully closed, waits for E key input
- **OpeningState**: Lerps door rotation toward open angle
- **OpenState**: Door is open, waits for E key or auto-close timeout
- **ClosingState**: Lerps door rotation back to closed

## Key Concepts

### Shared Context

All states inherit from `State<DoorController>` and access the controller via `SharedContext`:

```csharp
SharedContext.CurrentAngle = 90f;
SharedContext.ApplyRotation();
```

### Animation States

Opening and Closing states use `Mathf.Lerp` in `OnUpdate` for smooth animation:

```csharp
SharedContext.CurrentAngle = Mathf.Lerp(current, target, Time.deltaTime * speed);
```
