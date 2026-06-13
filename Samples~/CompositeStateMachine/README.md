# Composite State Machine Sample

An advanced example demonstrating sub-state machine composition for modular, reusable behavior patterns.

## Features Demonstrated

- SubStateMachineNode usage for graph composition
- Parent/child graph relationships
- Multiple exit nodes with named exits
- Context passing to child graphs
- Reusable sub-graphs for common patterns
- Combat system with attack and defense sequences

## Setup Instructions

After importing, open or create `CompositeStateMachine.unity` and configure:

1. **Fighter GameObject**:
    - Create a Capsule as the character body
    - Add `StateMachineRunner` component
    - Add `CombatController` component
    - Assign `MainGraph` to the Graph field

2. **Visual Feedback Setup**:
    - Assign the Capsule's Renderer to the Character Renderer field
    - Create a Cube as a "weapon" and parent it to the Fighter
    - Assign the weapon Transform to the Weapon Transform field

3. **Configure Sub-Graph References** (IMPORTANT):
    - Open `MainGraph` in the Visual State Machine editor
    - Select the "AttackSequence" SubStateMachine node
    - Drag `AttackSequence.asset` into the Sub Graph field
    - Select the "DefendSequence" SubStateMachine node
    - Drag `DefendSequence.asset` into the Sub Graph field

## Controls

- **Left Mouse Button**: Attack (triggers attack sequence)
- **Right Mouse Button (Hold)**: Block (triggers defend sequence)
- **Right Mouse Button (Release)**: Release block
- **Space**: Simulate being hit (while blocking, causes stagger)

## State Machine Architecture

### Main Graph

```
Entry → IdleCombat ─┬─(Attack)─→ [AttackSequence] ─→ IdleCombat
                    │
                    └─(Defend)─→ [DefendSequence] ─┬─(Complete)────→ IdleCombat
                                                   └─(Interrupted)─→ IdleCombat
```

### AttackSequence Sub-Graph

```
Entry → Windup → Strike → Recovery → Exit("Complete")
```

### DefendSequence Sub-Graph

```
Entry → Block ─┬─(Hit)──────→ Stagger → Exit("Interrupted")
               ├─(Release)──→ Exit("Complete")
               └─(Timeout)──→ Exit("Complete")
```

## Custom States

### Main Graph States

- **IdleCombatState**: Waits for attack or defend input

### Attack Sequence States

- **WindupState**: Preparing the attack (pulls weapon back)
- **StrikeState**: Executing the attack (swings weapon forward)
- **RecoveryState**: Post-attack recovery (vulnerable period)

### Defend Sequence States

- **BlockState**: Active blocking (reduces incoming damage)
- **StaggerState**: Hit while blocking (temporary stun)

## Key Concepts

### Sub-Graph Composition

Sub-graphs allow you to encapsulate common behavior patterns:

```
Main Graph:
  IdleCombat → [AttackSequence SubGraph] → IdleCombat

The AttackSequence sub-graph runs independently,
and when it reaches its Exit node, control returns
to the parent graph.
```

### Named Exits

The DefendSequence has two exit points:

- **Complete**: Normal exit (block released or timed out)
- **Interrupted**: Player was hit while blocking

The parent graph can handle each exit differently:

```csharp
// In DefendSequence, BlockState defines multiple transitions:
[Transition("Release")]
public Action OnRelease;  // → Exit("Complete")

[Transition("Hit")]
public Action OnHit;      // → Stagger → Exit("Interrupted")
```

### Context Sharing

Sub-graphs share the same context as the parent:

```csharp
// Both main graph and sub-graphs access CombatController
public class WindupState : State<CombatController>
{
    public override void OnEnter()
    {
        SharedContext.SetWindupVisual();
    }
}
```

### Visual Feedback

States update the character's visual appearance:

```csharp
public void SetWindupVisual()
{
    SetColor(_windupColor);
    // Pull weapon back for attack
    if (_weaponTransform != null)
    {
        _weaponTransform.localRotation = Quaternion.Euler(-45f, 0f, 0f);
    }
}
```

## Benefits of Composition

1. **Reusability**: Attack and defend sequences can be reused in different contexts
2. **Clarity**: Complex behaviors are broken into manageable sub-graphs
3. **Maintenance**: Changes to attack behavior only require editing AttackSequence
4. **Testing**: Sub-graphs can be tested in isolation
5. **Scalability**: Add new combat moves by creating additional sub-graphs
