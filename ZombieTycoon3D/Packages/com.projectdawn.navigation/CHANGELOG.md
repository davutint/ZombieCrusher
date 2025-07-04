# Changelog
All notable changes to this package will be documented in this file. The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)

## [4.0.15] - 2025-03-10
- Fixed Astar pathing resulting in errors during carving

## [4.0.14] - 2025-03-04
- Fixed TryGetCorners not working

## [4.0.13] - 2025-01-23
- Added root motion locomotion sample

## [4.0.12] - 2025-01-02
- Fixed A* Pathfinding Project 5.3.0 version to work
- Fixed selecting agent in subscene would result in errors

## [4.0.11] - 2025-01-02
- Fixed all warnings in Unity 6 version

## [4.0.10] - 2024-12-25
- Fixed scene Mass Astar Entities
- Fixed subscene navmesh loading resulting in "Failed to map agent position to nav mesh location" for first frame
- Updated to use ecs 1.3.5 version

## [4.0.9] - 2024-11-12
- Added AGENTS_NAVIGATION_FAKE_ASSEMBLY_REFERENCE for faking depedency on ecs/collection assembly reference
- Fixed compatibility with the A* Pathfinding Project package version 5.2.5
- Added AgentAstarPathingAuthoring from A* Pathfinding Project to work with subscenes

## [4.0.8] - 2024-10-06
- Added Hybrid game object and entity connection. As result in dots hierarchy you can select runtime mode and see actualy entity connected to it
- Fixed compatibility with the A* Pathfinding Project package version 5.2.
- Fixed compatibility with the A* Pathfinding Project package version 5.2.6.

## [4.0.7] - 2024-10-05
- Moved gizmos buffer from editor for easier debuging, still works only in editor
- Fixed rare exception: ArgumentException: Radius must be greater than zero
- Fixed rare exception: InvalidOperationException: The ComponentTypeHandle<Pathfinding.ECS.ManagedState>
- Fixed agent editor exception in prefab mode

## [4.0.6] - 2024-07-25
- Changed spatial clear to parallel that improves performance drastically
- Fixed seeking on wide navmesh links
- Changed link detection to use velocity to avoid cases where agent overshots the link start

## [4.0.5] - 2024-07-17
- Added area cost
- Added to AgentNavMeshAuthoring SetAreaCost and GetAreaCost
- Fixed NavMeshCorners.TryGetCorners to correctly handle empty array
- Fixed NavMeshPath.EndLocation properly match destination in all cases
- Changed found path no longer allocates temp memory
- Changed NavMeshPathState.Finished into NavMeshPathState.FinishedFullPath and NavMeshPathState.FinishedPartialPath

## [4.0.4] - 2024-06-11
- Added to AgentNavMeshAuthoring CreateCorners
- Added NavMeshCorners for simplified accessing of path corners
- Added Weight in AgentSeperationAuthoring
- Fixed smart stop gizmos drawing
- Changed agent separation to allow enable/disable
- Changed selected agent navmesh authoring now draws corners

## [4.0.3] - 2024-04-28
- Fixed smart stop baker for give up timer

## [4.0.2] - 2024-04-14
- Fixed to work with A* Pathfind Project 5.0.9

## [4.0.1] - 2024-04-14
- Fixed astar link traversal

## [4.0.0] - 2024-04-13
- Added integration with A* Pathfinding Project! This includes new component `Agent A* Pathing` that uses package global pathing.
- Added off mesh link support
- Added custom link traversal tutorial and scene
- Added Speed property to AgentBody
- Fixed navmesh path no longer being calculated 2x times
- Fixed NavMeshPath.Default to set Grounded to true

## [3.5.2] - 2023-12-14
- Fixed AgentNavMeshPath correctly bake UseWalls in subscene
- Fixed NavMeshQuery to correctly return partial path in case all nodes visited instead of just return then it is out of iterations

## [3.5.1] - 2023-12-12
- Fixed ECS NavMesh cleanup
- Fixed for crowds gizmos field capacity error

## [3.5.0] - 2023-12-02
- Added AgentActionSystemGroup
- Added AgentSpatialSystemGroup
- Added AgentPathingSystemGroup
- Changed NavMesh Constrained into Grounded
- Changed AgentSteering into AgentLocomotion
- Changed AgentTransformSystemGroup into AgentDisplacementSystemGroup
- Changed AgentSteeringSystemGroup into AgentSeekingSystemGroup
- Fixed navigation settings null reference exception

## [3.4.2] - 2023-11-14
- Added Agent.Default
- Fixed navigation layers not working with query capacity 0

## [3.4.1] - 2023-11-10
- Fixed issues with navigation layer setting to nothing

## [3.4.0] - 2023-11-08
- Added path progress in case agents is moved outside agents system group
- Added Layers to Agent/AgentCollider/AgentSeparation/AgentSonarAvoid/AgentReciprocalAvoid
- Added Query Check setting. The maximum number of nearby neighbors will be checked to find closest.
- Added to Smart Stop new behaviour called Give Up Stop.
- Added AgentCollider enable/disable state
- Added Iterations Per Frame for NavMesh
- Added NavMesh Constrained option to control should agent be forced to be on surface
- Removed old 0.65 entities package baking path
- Changed Sonar Time Horizon only include agents that are withing radius. This will improve agent seeking.
- Changed Spatial Partitioning to use parallel jobs
- Changed AgentSystemGroup to be executed before physics

## [3.3.5] - 2023-10-25
- Fixed gizmos drawing for multiple game objects and entities
- Changed ECS gizmos drawing no longer requires to select agent authoring
- Changed GizmosCommandBuffer no longer has parallel version. As this simplifies gizmos logic and did not worked correctly any way

## [3.3.4] - 2023-10-13
- Fixed performance regression with query capacity
- Fixed samples script agent destination missing reference
- Fixed "Leak Detected : Persistent allocates 257 individual allocations" (Made workaround to unity bug that leaks bursted System.OnCreate)

## [3.3.3] - 2023-09-18
- Changed NavMeshQueryStatus.Finished to be obsolete, use FinishedFullPath or FinishedPartialPath. This changes allows distinguish reachable from unreachable paths
- Fixed Regular Update to skip frame with delta zero
- Fixed in docs SonarAvoidance.Set parameters names
- Fixed SonarAvoidance.CopyFrom to also copy angle
- Fixed QueryCylinder using radius for y axis instead of height
- Fixed Query Capacity to more effiently take neighbour agents (Should reduce popping effect with collisions)

## [3.3.2] - 2023-09-01
- Fixed regression from 3.3.1 limited query resulting error "HashMap is full"

## [3.3.1] - 2023-08-31
- Added limited query fully account different agent sizes
- Changed nav mesh remaining distance to return float max value until it reaches few remaining nodes. It was changed, as it was never accurate value which was resulting other issues
- Fixed lower push forces on collision with sonar horizon
- Fixed authoring help links

## [3.3.0] - 2023-08-20
- Added collider settings
- Added spatial partitioning cylinder and circle query with limited neighbour count
- Added map mapping to icons for a reduced aliasing
- Changed project settings from scene based to project wide
- Changed separation to use current shape of agent
- Changed com.unity.entities version to 1.0.14

## [3.2.0] - 2023-07-16
- Added NavMeshSettings singleton
- Added settings component mirroring in the project settings under new tab Agents Navigation
- Added new component Agent Smart Stop from Zerg Samples
- Added default configurations for AgentBody/AgentSteering/AgentCollider/AgentShape/AgentSeparation/NavMeshPath
- Added EntityNodes to AgentNavMeshAuthoring
- Added warning for agent authoring in case no shape is added
- Added error then NavMeshQuerySystem is attempted to be created in edit mode
- Changed velocity to account collision with agents and navmesh
- Changed SonarAvoidance radius scale with velocity
- Changed SonarAvoidance  to be float2 now. First value represents angle of velocity obstacle and second one maximum allowed angle
- Changed AgentCylindreShapeAuthoring icon (I hope you will enjoy new one)
- Fixed SonarAvoidance Walls no longer produces extremely small paths
- Fixed hybrid mode have correct LocalTransform scale 1
- Fixed spatial partitioning to use same same query to avoid inconsistencies
- Fixed component UI updating previous frames values

## [3.1.6] - 2023-04-11
- Changed seperation algorithm
- Added weight property to seperation
- Fixed NavMesh Area Mask correctly work with no sequential layers

## [3.1.5] - 2023-03-29
- Added NavMeshSurface surface baker and now it can be baked in subscene
- Added sample scene low level sonar avoidance to show its usage
- Added enable/disable to AgentNavMeshAuthoring and AgentAvoidAuthoring
- Chanded AgentSonarAvoid and NavMeshPath is now IEnableableComponent
- Fixed support for entities 1.0.0-preview.65
- Fixed acceleration correctling working with huge values
- Fixed sonar avoidance quality regression from 3.1

## [3.1.4] - 2023-02-22
- Changed com.unity.entities package version from 1.0.0-pre.15 to 1.0.0-pre.44
- Fixed AgenAuthroing.Stop to correctly set velocity to zero

## [3.1.3] - 2023-02-16
- Added support for RectTransform

## [3.1.2] - 2023-02-10
- Added SetDestinationDeferred to agent
- Fixed navmesh area mask editor property work correctly
- Changed agent capacity automatically resizing, removed AgentCapacity property for SpatialPartitioningSettings
- Changed gizmos system to be in the same group AgentGizmosSystemGroup

## [3.1.1] - 2023-02-07
- Fixed compilation issue as one of the assembly was not set to Editor

## [3.1.0] - 2023-01-31
- Added new feature to local avoidance `Walls` that accounts for navmesh.
- Added new property to AgentNavMeshAuthoring UseWalls.
- Changed standing agents now puch each other.
- Fixed local avoidance gizmos drawing.
- Fixed then desination either above or below agent would result in error.
- Fixed path failure case then it is out of nodes and path is in progress.

## [3.0.6] - 2023-01-11
- Fixed NavMesh path sometimes discarding destination
- Fixed error drop when selecting agent in subscene "The targets array should not be used inside OnSceneGUI or OnPreviewGUI. Use the single target property instead.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)"

## [3.0.5] - 2022-12-26
- Fixed NavMeshAgent correctly stop if path destination can not be mapped to navmesh
- Fixed that even with OutOfNodes still returns best possible path
- Added NavMeshPath failed state and also prints the error in editor
- Added NavMeshAgent/NavMeshPath added new property MappingExtent that allows controling the maximum distance the agent will be mapped
- Added documentation links to components and package
- Changed documentation to hidden folder as now it is on webpage

## [3.0.4] - 2022-12-23
- Fixed NavMeshAgent correctly handle partial paths (Paths where destination can not be reached)
- Fixed few more cases where NavMesh update would result in "Any jobs using NavMeshQuery must be completed before we mutate the NavMesh."
- Fixed NavMeshAgent in some cases reusing path from other agent
- Changed Zerg scene camera to be centered around controllable units

## [3.0.3] - 2022-12-17
- Added to EntityBehaviour OnEnable and OnDisable
- Added error message box to AgentNavMeshAuthoring, if game objects also has NavMeshObstacle
- Added SetDestination method to AgentAuthoring
- Changed that if agent is not near any NavMesh it will throw error instead moved to the center of the world
- Changed dependency com.unity.entities version to 1.0.14
- Fixed few cases where NavMesh update would result in "Any jobs using NavMeshQuery must be completed before we mutate the NavMesh."

## [3.0.2] - 2022-12-15
- Fixed NavMesh at the end of destination throwing error `System.IndexOutOfRangeException: Index {0} is out of range of '{1}' Length`.
- Fixed transform sync from game object to entity not override transform in most calls.

## [3.0.1] - 2022-12-08
- Added correct documentation
- Added com.unity.modules.ui dependency as samples uses ui
- Removed second navmesh surface from zerg samples

## [3.0.0] - 2022-11-30
- Release as Agents Navigation

## [2.0.0] - 2022-06-09
- Changing velocity avoidance with new smart algorithm
- Changing package to use new Package Manager workflow
- Updating documentation to be more clear and reflect new API changes
- Adding zerg sample

## [1.0.3] - 2022-05-14
- Adding new demo scene "8 - Jobified Boids Navmesh Demo"

## [1.0.2] - 2022-03-19
- Fixing memory leaks in demo scenes

## [1.0.1] - 2022-03-08
- Updated jobs demo to not use physics and small bug fix

## [1.0.0] - 2022-02-22
- Package released