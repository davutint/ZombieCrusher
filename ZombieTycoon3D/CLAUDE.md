# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ZombieTycoon3D is a Unity 6 (6000.2.7f2) game built using Unity DOTS (Data-Oriented Technology Stack) / ECS (Entity Component System). The game features zombie-spawning mechanics where vehicles crush zombies with physics-based blood effects. This is a performance-oriented project designed to handle large numbers of entities efficiently.

## Architecture

### DOTS/ECS Structure

The project follows Unity's DOTS architecture with three main concepts:
- **Components (IComponentData)**: Pure data containers with no logic (e.g., `ZombieComponent`, `ZombieTag`, `VehicleTag`)
- **Systems (ISystem)**: Logic that processes entities with specific components (e.g., `ZombieNavigationSystem`, `ZombieVehicleCollisionSystem`)
- **Authoring MonoBehaviours**: Bridge between GameObjects and ECS entities (e.g., `ZombieSpawnManagerAuthoring`, `ZombieAuthoring`)

### Key Systems

**Zombie Management:**
- `ZombieSpawnSystem.cs` - Spawns zombies at defined spawn points (currently commented out)
- `OptimizedZombieSpawnSystem.cs` - Alternative spawning implementation
- `ZombieBatchSpawnSystem.cs` - Batch spawning for performance
- `ZombiePoolSystem.cs` - Entity pooling system
- `ZombieNavigationSystem.cs` - Controls zombie movement using Project Dawn Navigation
- `ZombieVehicleCollisionSystem.cs` - Handles zombie death when hit by vehicles
- `ZombieCountSystem.cs` - Tracks alive/killed zombie counts

**Blood/VFX System:**
- `BloodMeshSpawnSystem.cs` - Spawns physics-based blood mesh effects when zombies die
- `BloodMeshCleanupSystem.cs` - Removes blood meshes after lifetime expires
- Uses PhysicsVelocity for ragdoll-like blood scatter effects

**Animation System:**
- `RunningAnimationSystem.cs` - Procedural limb animation for running characters
- `InitializeLimbRotationsSystem.cs` - Stores original rotations before animation
- Works with `LimbReferences` component to animate arms/legs

**UI & Metrics:**
- `UIUpdateSystem.cs` - Updates UI with zombie counts
- `ZombieUIManager.cs` - Manages UI display
- `FPSSystem.cs` - Performance monitoring

### Core Components

Located in `Assets/Scripts/_DOTSSCRIPTS/ZombieComponent.cs`:
- `ZombieComponent` - moveSpeed, health, isAlive
- `ZombieTag` - Empty tag for queries
- `VehicleTag` - Marks vehicle entities
- `ZombieSpawnSettings` - Singleton for spawn configuration
- `SpawnPointElement` - DynamicBuffer for multiple spawn locations
- `UIUpdateRequest` - Event component for UI updates

### Third-Party Packages

**Project Dawn Navigation** (com.projectdawn.navigation v4.1.1):
- High-performance navigation system for agents
- Used for zombie pathfinding toward vehicles
- Key component: `AgentBody` with `SetDestination()` method
- Documentation: https://lukaschod.github.io/agents-navigation-docs/manual/index.html

**Rukhanka Animation System** (com.rukhanka.animation v2.2.1):
- ECS-based animation system for skeletal animations
- Documentation: https://docs.rukhanka.com
- Used for character animations in DOTS

**Unity Vehicles Package** (com.unity.vehicles v0.1.0-exp.10):
- Experimental vehicle physics system
- Sample scripts in `Assets/Samples/Vehicles/`

## Directory Structure

```
Assets/
├── Scripts/
│   ├── _DOTSSCRIPTS/          # All ECS systems and components
│   ├── GameJamScripts/         # Animation and performance systems
│   ├── Cars/                   # Vehicle-related scripts
│   └── [Legacy scripts]        # MonoBehaviour scripts (Player.cs, Enemy.cs, etc.)
├── Scenes/
│   ├── DOTSTEST.unity          # Main DOTS testing scene
│   └── MainMenu.unity
├── _ASSETS/                    # Game assets
├── _Prefabs/                   # Prefab storage
└── _Animations/                # Animation assets

Packages/
├── com.projectdawn.navigation/
├── com.rukhanka.animation/
└── manifest.json
```

## Build & Development Commands

**Opening the Project:**
- Open with Unity Hub using Unity 6000.2.7f2

**Running the Game:**
- Open `Assets/Scenes/DOTSTEST.unity` in Unity Editor
- Press Play button in editor

**Build:**
- File > Build Settings > Build (configured for platform in Build Settings)

**Testing:**
- Unity Test Framework available (com.unity.test-framework v1.6.0)
- No specific test commands configured

## DOTS Development Guidelines

**System Update Order:**
- Use `[UpdateInGroup]` attributes to control execution order
- Common groups: `FixedStepSimulationSystemGroup`, `SimulationSystemGroup`
- Use `[UpdateBefore]` and `[UpdateAfter]` for fine-grained control

**Burst Compilation:**
- Always use `[BurstCompile]` attribute on systems for performance
- Burst-compiled code cannot use Debug.Log (wrap in `#if UNITY_EDITOR`)
- Avoid managed references in Burst jobs

**EntityCommandBuffer Pattern:**
- Used for structural changes (add/remove components, instantiate/destroy entities)
- Always dispose of ECB after Playback()
- Use `Allocator.Temp` for single-frame ECBs

**Component Queries:**
- Use `SystemAPI.Query<>()` for iterating entities
- `.WithAll<T>()` - requires component
- `.WithNone<T>()` - excludes entities with component
- `.WithEntityAccess()` - provides Entity handle in query

**Authoring/Baking:**
- Create MonoBehaviour authoring scripts for designer workflow
- Implement `Baker<T>` class to convert GameObject to Entity
- Use `GetEntity()` to convert GameObject references to Entity references

## Common Patterns in This Codebase

**Singleton Pattern:**
```csharp
var spawnManagerEntity = SystemAPI.GetSingletonEntity<ZombieSpawnSettings>();
var settings = SystemAPI.GetComponentRW<ZombieSpawnSettings>(spawnManagerEntity);
```

**Spawning Entities:**
```csharp
var newEntity = state.EntityManager.Instantiate(prefabEntity);
```

**Navigation (Project Dawn):**
```csharp
if (SystemAPI.HasComponent<AgentBody>(entity)) {
    var agent = SystemAPI.GetComponentRW<AgentBody>(entity);
    agent.ValueRW.SetDestination(targetPosition);
}
```

**Event-Driven Architecture:**
- Use empty request components (e.g., `BloodMeshSpawnRequest`)
- Systems create request entities
- Other systems process and destroy requests

## Known Issues & TODOs

- `ZombieSpawnSystem.cs` has commented-out implementation
- Navigation system has TODO comments about API usage
- Blood mesh system uses distance-based collision detection (distanceSq < 4f) as temporary solution
- Some legacy MonoBehaviour scripts coexist with DOTS systems

## Rendering

- Universal Render Pipeline (URP) v17.2.0
- Post-processing v3.5.0
- Super Simple Skybox (OccaSoftware)
- Uses `RenderMesh` for ECS entities

## Input

- New Input System (com.unity.inputsystem v1.14.2)
- Input actions defined in `Assets/InputSystem_Actions.inputactions`
