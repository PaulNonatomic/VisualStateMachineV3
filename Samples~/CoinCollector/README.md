# Coin Collector Sample

An intermediate example demonstrating game state management, data flow, and condition-based transitions.

## Features Demonstrated

- `State<T>` pattern with shared `GameController` context
- Game state management (Menu, Playing, Win, GameOver)
- Multiple transition paths based on conditions
- `[DataOut]` ports for outputting state data
- Event-based communication between states and game objects
- Jump nodes for restarting the game loop

## Gameplay

1. Press **SPACE** to start
2. Collect **5 coins** before time runs out (30 seconds)
3. Walk into coins to collect them
4. Press **SPACE** after win/lose to play again

## Setup Instructions

After importing, open `CoinCollector.unity` and configure:

1. **Game Controller GameObject**:
    - Add `StateMachineRunner` component
    - Add `GameController` component
    - Assign `CoinCollectorGraph` to the Graph field
    - Set up UI references (optional - can use Find)

2. **Coins**:
    - Create spheres with `CoinPickup` component
    - Add a Collider and set as Trigger
    - Tag the player/camera as "Player"

3. **UI (optional)**:
    - Create Canvas with Text elements for Score, Timer, Message
    - Name them "ScoreText", "TimerText", "MessageText"
    - Create panels for Menu, Game, and Result screens

## State Machine Flow

```
Entry → Menu ──(SPACE)──→ Playing ─┬─(All coins)─→ Win ──(SPACE)──┐
                                   │                               │
                                   └─(Time up)──→ GameOver ───────┤
                                                                   │
         ↑                                                         │
         └──────────────────────────────────────────(JumpOut)──────┘
```

## Custom States

- **MenuState**: Displays "Press SPACE to Start", waits for input
- **PlayingState**: Runs the game, tracks score and time, fires transitions
- **WinState**: Shows victory message with final score
- **GameOverState**: Shows failure message with coins collected

## Key Concepts

### Event-Based Communication

The `GameController` fires events when coins are collected:

```csharp
public event Action<int> OnCoinCollected;

public void CollectCoin()
{
    Score++;
    OnCoinCollected?.Invoke(Score);
}
```

States subscribe to these events:

```csharp
public override void OnEnter()
{
    SharedContext.OnCoinCollected += HandleCoinCollected;
}
```

### Multiple Transitions

`PlayingState` has two transitions that fire based on different conditions:

```csharp
if (SharedContext.HasWon)
    OnCheckResult?.Invoke();

if (SharedContext.IsTimeUp)
    OnTimeUp?.Invoke();
```
