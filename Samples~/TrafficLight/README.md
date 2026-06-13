# Traffic Light Sample

Demonstrates Visual State Machine concepts using traffic light systems from different countries. Shows how the same state classes can be reused across different graph topologies,
and how a single controller can coordinate multiple visual groups.

## Features Demonstrated

### Level 1: Paired Traffic Lights

- Country-specific light sequences (UK, US, Germany, Japan)
- `State<T>` pattern with shared context
- Timed transitions using `[DataIn("Duration")]`
- Jump nodes for looping state flows
- Array-based controller driving multiple light housings in sync

### Level 2: 4-Way Intersection

- Phase-based states controlling two traffic light groups (NS and EW)
- Safety "all red" clearing phases between direction changes
- `IntersectionController` coordinating two `TrafficLightController` instances
- Same state pattern scaled to intersection-level complexity

## Country Traffic Light Sequences

### United Kingdom

Red → Red+Yellow → Green → Yellow → repeat

The red+yellow "get ready" phase signals that the light is about to turn green.

### United States

Red → Green → Yellow → repeat

The standard three-phase sequence.

### Germany

Red → Red+Yellow → Green → Yellow → repeat

Similar to the UK but with shorter durations.

### Japan

Red → Green → Flashing Green → Yellow → repeat

The flashing green phase warns pedestrians and drivers that the light is about to change.

## Setup Instructions

### Level 1: Paired Traffic Lights

1. Open `TrafficLight.unity`
2. Each country has a parent GameObject with:
    - `StateMachineRunner` component — assign the country's graph (e.g., `TrafficLightGraph_UK`)
    - `TrafficLightController` component — assign the light MeshRenderers from both housings
3. Play the scene to see all four countries cycling simultaneously

### Level 2: Intersection

1. The Intersection GameObject has:
    - `StateMachineRunner` component — assign an intersection graph (e.g., `IntersectionGraph_UK`)
    - `IntersectionController` component — assign the NS and EW `TrafficLightController` references
2. Each direction group (NS, EW) has its own `TrafficLightController` managing its light housings
3. Swap the intersection graph in the Inspector to compare country behaviors

## State Machine Flows

### Level 1 (e.g., UK)

```
Entry → Red (3s) → RedYellow (2s) → Green (5s) → Yellow (2s) → JumpOut
                                                                    ↓
JumpIn ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
```

### Level 2 (e.g., UK Intersection)

```
Entry → AllRed (1s) → NS RedYellow (2s) → NS Green (5s) → NS Yellow (2s) → AllRed (1s) → EW RedYellow (2s) → EW Green (5s) → EW Yellow (2s) → JumpOut
                                                                                                                                                     ↓
JumpIn ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
```

## Custom States

### Level 1 States (`State<TrafficLightController>`)

- **RedLightState** — Activates red light, waits for duration
- **YellowLightState** — Activates yellow light, waits for duration
- **GreenLightState** — Activates green light, waits for duration
- **RedYellowLightState** — Activates red+yellow together (UK/Germany)
- **FlashingGreenLightState** — Flashes green on/off at configurable interval (Japan)

### Level 2 States (`State<IntersectionController>`)

- **AllRedPhaseState** — Both groups red (safety clearing)
- **NSGreenPhaseState** / **EWGreenPhaseState** — One direction green, other red
- **NSYellowPhaseState** / **EWYellowPhaseState** — One direction yellow, other red
- **NSRedYellowPhaseState** / **EWRedYellowPhaseState** — One direction red+yellow, other red
- **NSFlashingGreenPhaseState** / **EWFlashingGreenPhaseState** — One direction flashing green, other red
