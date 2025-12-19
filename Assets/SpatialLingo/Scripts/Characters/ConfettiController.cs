// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpatialLingo.Characters
{
    [MetaCodeSample("SpatialLingo")]
    public class ConfettiController : MonoBehaviour
    {
        [SerializeField] private GameObject m_confettiPrefab;

        public void ShowConfettiPresentation(Vector3 location, Quaternion rotation, int totalCount = 3, float randomOffset = 0.5f, float randomDelay = 0.4f)
        {
            // Instantiate N prefabs at random locations from where this transform is located
            for (var i = 0; i < totalCount; i++)
            {
                _ = StartCoroutine(DelayShowDestroy(location, rotation, randomOffset, Random.Range(0, randomDelay), 2.0f));
            }
        }

        private IEnumerator DelayShowDestroy(Vector3 location, Quaternion rotation, float randomOffset, float delayStart, float delayEnd)
        {
            yield return new WaitForSeconds(delayStart);
            var instance = Instantiate(m_confettiPrefab);
            instance.transform.rotation = rotation;
            instance.transform.position = location + new Vector3(
                Random.Range(0.0f, randomOffset) - randomOffset * 0.5f,
                Random.Range(0.0f, randomOffset) - randomOffset * 0.5f,
                Random.Range(0.0f, randomOffset) - randomOffset * 0.5f);
            yield return new WaitForSeconds(delayEnd);
            Destroy(instance);
        }
    }
}