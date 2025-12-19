# Mixed Reality Utility Kit (MRUK)

## Introduction

Meta's [Mixed Reality Utility Kit](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-overview/) is a set of tools that provide spatial data and tracking, providing apps with a better understanding of the user's immediate environment.

## Use in Spatial Lingo

MRUK is used for the initial placement of Golly Gosh and the tree upon entering the app. It helps ensure the spawn point is in a sensible location for the user, allowing the user to interact with Golly Gosh and the tree in a multitude of setups and environments.

This is used in [RoomSense.cs](../Assets/SpatialLingo/Scripts/Utilities/RoomSense.cs), in the `SpawnCoroutine()` function. First, MRUK's room object is queried. [`FindSpawnPositions.StartSpawn()`](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-content-placement#findspawnpositions) is called to instantiate a dummy object, which is then adjusted further to ensure Golly Gosh is properly facing the user. To prevent a "teleporting" effect from multiple transform updates, the dummy object is swapped for Golly Gosh as soon as the transform is finalized.

Also see [RoomSense.prefab](../Assets/SpatialLingo/Prefabs/Utilities/RoomSense.prefab) for MRUK configuration. In particular, the `[BuildingBlock] Find Spawn Positions` GameObject is used to find the starting position for Golly Gosh and the language tree.  
![SpawnPrefab.png](Images/MRUK/SpawnPrefab.png)

## Sample Scenes

![MRUK-Gym.png](Images/MRUK/MRUK-Gym.png)  
In [GymScene.unity](../Assets/SpatialLingo/Scenes/GymScene.unity), the MRUK panel allows toggling the overlay for your current room scan.

![SpawnSample.png](Images/MRUK/SpawnSample.png)  
In [FindSpawnPositionsSample.unity](../Assets/SpatialLingo/Scenes/Samples/FindSpawnPositionsSample.unity), the floor placement building block from MRUK is used to place multilpe objects on the user's floor on start.
