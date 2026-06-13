# Siege Tank Sample

A Starcraft-inspired Siege Tank implementation demonstrating the **State Pattern** with VSM3's typed shared context.

## State Flow

```
                         ┌────────────────────────────────────────┐
                         │                                        │
                         ▼                                        │
[Entry] ──▶ [Tank Mode] ──▶ [Sieging Up] ──▶ [Siege Mode] ──▶ [Unsieging]
                ▲                                   │                │
                │                                   │                │
                └───────────────────────────────────┴────────────────┘
```

## States

| State          | Description           | Can Move | Damage     | Range       |
|----------------|-----------------------|----------|------------|-------------|
| **Tank Mode**  | Mobile combat mode    | ✅ Yes    | 15         | 7           |
| **Sieging Up** | Transforming to siege | ❌ No     | Lerping... | Lerping...  |
| **Siege Mode** | Immobile artillery    | ❌ No     | 70         | 13 (min: 2) |
| **Unsieging**  | Transforming to tank  | ❌ No     | Lerping... | Lerping...  |

## Shared Context

All states access the same `SiegeTankContext`:

```csharp
public class SiegeTankContext
{
    // Current state info
    public bool IsSieged { get; set; }
    public bool IsTransforming { get; set; }

    // Combat
    public Transform CurrentTarget { get; set; }
    public float LastFireTime { get; set; }

    // Movement (Tank Mode only)
    public Vector3 MoveDestination { get; set; }
    public bool HasMoveOrder { get; set; }

    // Stats that change between modes
    public float CurrentDamage { get; set; }
    public float CurrentRange { get; set; }
    public float CurrentFireRate { get; set; }
}
```

## Usage

1. Create a `SiegeTankGraph` asset: Right-click → Create → Visual State Machine → Samples → Siege Tank Graph

2. Add states to the graph in the Graph Editor:
    - Add `TankModeState` connected from Entry
    - Add `SiegingUpState` connected from TankMode's "On Siege Command"
    - Add `SiegeModeState` connected from SiegingUp's "On Siege Complete"
    - Add `UnsiegingState` connected from SiegeMode's "On Unsiege Command"
    - Connect Unsieging's "On Unsiege Complete" back to TankMode

3. Add `SiegeTankRunner` component to your tank GameObject

4. Assign the graph and optionally an Animator

5. Press **E** to toggle between Tank Mode and Siege Mode

## Controls

- **E** - Toggle Siege Mode (when stationary)
- Movement is handled via `SiegeTankRunner.MoveTo(position)`
- Targeting is handled via `SiegeTankRunner.SetTarget(transform)`

## Key State Pattern Concepts Demonstrated

1. **Shared Mutable State**: All states read and write to `SiegeTankContext`
2. **State-Specific Behavior**: Each state handles movement/combat differently
3. **Transition Guards**: Can't siege while moving, can't move while sieged
4. **Transformation States**: Intermediate states for animations
5. **Stats Modification**: Context stats change based on current state
