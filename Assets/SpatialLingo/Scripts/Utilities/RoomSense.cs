// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using UnityEngine;

namespace SpatialLingo.Utilities
{
    [MetaCodeSample("SpatialLingo")]
    public class RoomSense : MonoBehaviour
    {
        [Header("Spawn Position Finder")]
        [SerializeField] private FindSpawnPositions m_spawnPositionFinder;

        [MetaCodeSample("SpatialLingo")]
        public class SpawnPositionResult
        {
            public Vector3 Position { get; private set; }
            public bool Success { get; private set; }

            public SpawnPositionResult(Vector3 position, bool success)
            {
                Position = position;
                Success = success;
            }
        }
        public delegate void FindSpawnPositionEvent(SpawnPositionResult result);
        public FindSpawnPositionEvent FindSpawnPosition;

        public void FindSpawnPositions()
        {
            _ = StartCoroutine(SpawnCoroutine(m_spawnPositionFinder));
        }

        private IEnumerator SpawnCoroutine(FindSpawnPositions spawnPositionFinder)
        {
            // Wait for MRUK to have spawning data ready
            while (MRUK.Instance == null || MRUK.Instance.GetCurrentRoom() == null)
            {
                yield return null;
            }
            var room = MRUK.Instance.GetCurrentRoom();
            spawnPositionFinder.StartSpawn(room);

            var spawned = FindFirstObjectByType<RoomSenseSpawnObject>();
            if (spawned == null)
            {
                Debug.LogWarning("RoomSense - Spawning - Initialize: Failed to find RoomSenseSpawnObject");
                FindSpawnPosition?.Invoke(null);
                yield break;
            }

            var position = spawned.transform.position;
            Destroy(spawned);
            FindSpawnPosition?.Invoke(new SpawnPositionResult(position, true));
        }
    }
}