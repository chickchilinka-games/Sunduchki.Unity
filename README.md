# SunduchkiGame

SunduchkiGame is a multiplayer Unity card game where players guess card ranks in each other's hands.

## Gameplay
- On your turn, you ask an opponent for a card rank.
- You can only ask for a rank that is already in your own hand.
- If you guessed right, you take matching cards and continue your turn.
- If you guessed wrong, you draw a card and pass the turn.
- Bonus cards add special effects and tactical variations.
- First player to collect 5 completed rank stacks wins.

## Architecture
The project uses a pragmatic modular approach inspired by Clean Architecture, DDD, and GRASP.

- `Assets/Scripts/Modules`: core game capabilities (Lobby, TurnSystem, CardRequestSystem, PlayerHand, BonusSystem, DeckSystem, etc.).
- `Assets/Scripts/Features`: Unity-facing implementation/composition layers (`*Impl`, UI, bootstrapping, lifecycle states).
- `AppLifecycle` and state machine coordinate application flow (boot, lobby, game, substates).

### How architecture principles are applied
- Clean Architecture: core rules and use-cases are separated from UI/framework concerns.
- Ports and Adapters: modules expose ports (interfaces) for communication with the outside world; adapters in `Features` and infrastructure modules implement these ports.
- DDD-style boundaries: each module acts as a bounded context with its own contracts, models, and services.
- GRASP:
  - Information Expert: domain services own their business rules.
  - Controller: lifecycle/state layers orchestrate scenarios.
  - Low Coupling / High Cohesion: module boundaries + DI (Zenject) keep dependencies explicit.
  - Indirection/Pure Fabrication: transport and infrastructure wrappers keep domain logic isolated.

## Backend and Realtime
The game client works with a backend via SignalR.

- `Modules/SignalR` contains shared hub transport (`SharedGameHubConnection`) and connection factories.
- SignalR integration acts as an infrastructure adapter behind module ports.
- Game modules subscribe to hub events and invoke backend commands (for example `Ask`, `UseBonus`, `JoinGame`, `LeaveGame`).
- This keeps realtime communication centralized while gameplay modules stay focused on game logic.
