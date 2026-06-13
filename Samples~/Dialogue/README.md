# Dialogue Sample

A branching dialogue system demonstrating how to build NPC conversations with VSM3.

## Features Demonstrated

- `State<T>` pattern with shared `DialogueController` context
- Branching transitions (3-way choice from ChoiceState)
- Convergent flow (TownInfoState and WorkInfoState return to ChoiceState)
- Re-entrant loop (FarewellState returns to IdleState for repeat conversations)
- UGUI panel management from states via SharedContext
- Typewriter text reveal effect
- Serializable dialogue data configured in the Inspector

## Controls

| Key         | Action                             |
|-------------|------------------------------------|
| **E**       | Start conversation (when near NPC) |
| **Space**   | Skip typewriter / advance dialogue |
| **1, 2, 3** | Select a dialogue choice           |

## State Machine Flow

```
                              +--(1: Town)---> TownInfoState ---+
                              |                                  |
Entry -> IdleState --> GreetingState --> ChoiceState --(2: Work)---> WorkInfoState ---+
            ^                                |                                       |
            |                                +--(3: Goodbye)--> FarewellState        |
            |                                                        |               |
            +--------------------------------------------------------+---<-----------+
```

## States

| State             | Color  | Description                                |
|-------------------|--------|--------------------------------------------|
| **IdleState**     | Blue   | Waits for player to press E within range   |
| **GreetingState** | Green  | NPC greeting with typewriter reveal        |
| **ChoiceState**   | Orange | Three dialogue options (1/2/3 keys)        |
| **TownInfoState** | Teal   | NPC describes the town, returns to choices |
| **WorkInfoState** | Purple | NPC offers a quest, returns to choices     |
| **FarewellState** | Red    | NPC says goodbye, returns to idle          |

## Setup

1. Create a scene in `Content/Dialogue.unity`
2. Add a ground plane and an NPC capsule
3. On the NPC, add `StateMachineRunner` and `DialogueController` components
4. Create a `DialogueGraph` asset in `Content/` and assign it to the runner
5. Open the graph editor and add all six states, connecting transitions as shown above
6. Create a Canvas with:
    - **DialoguePanel**: Panel with SpeakerText, DialogueText, and ContinueHint (Text components)
    - **ChoicePanel**: Panel with ChoicePromptText, ChoiceAText, ChoiceBText, ChoiceCText
    - **PromptText**: Floating "Press E to talk" text
7. Assign all UI references on the DialogueController
8. Press Play and walk near the NPC

## Key Concepts

### Typewriter Effect

Text is revealed character-by-character. Pressing Space while text is revealing skips to the end. Pressing Space after text is fully shown advances to the next state.

### Hub-and-Spoke Choices

ChoiceState acts as a central hub. Selecting "town" or "work" branches to an info state that returns back to the hub, allowing the player to explore all options before saying
goodbye.

### Inspector-Driven Content

All dialogue text is configured via SerializeField on the DialogueController, making it easy to change dialogue without modifying code.
