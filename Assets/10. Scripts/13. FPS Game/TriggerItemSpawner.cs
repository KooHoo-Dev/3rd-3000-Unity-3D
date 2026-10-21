using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Study3D.FpsGame
{
    public class TriggerItemSpawner : MonoBehaviour
    {
        [Header("생성할 아이템과 위치")]
        [FormerlySerializedAs("TriggerItemPrefabs")]
        [SerializeField] private TriggerItem[] triggerItemPrefabs;
        [SerializeField] private Transform[] spawnTransforms;

        [Header("간격과 흩어짐")]
        [SerializeField] private float spawnInterval = 3.0f;
        [SerializeField] private float spawnRadius = 3.0f;
        [Tooltip("바닥에 동시에 놓일 수 있는 최대 아이템 수")]
        [SerializeField] private int maxItemCount = 10;

        private readonly List<TriggerItem> spawnedItems = new List<TriggerItem>();

        private void Start()
        {
            StartCoroutine(SpawnCoroutine());
        }

        private IEnumerator SpawnCoroutine()
        {
            while (gameObject.activeInHierarchy)
            {
                yield return new WaitForSeconds(spawnInterval);

                spawnedItems.RemoveAll(item => item == null);
                if (spawnedItems.Count >= maxItemCount) continue;

                var selectedItem = triggerItemPrefabs[Random.Range(0, triggerItemPrefabs.Length)];
                var selectedTransform = spawnTransforms[Random.Range(0, spawnTransforms.Length)];

                Vector2 circlePoint = Random.insideUnitCircle * spawnRadius;

                Vector3 spawnPoint = selectedTransform.position;
                spawnPoint.x += circlePoint.x;
                spawnPoint.z += circlePoint.y;

                spawnedItems.Add(Instantiate(selectedItem, spawnPoint, selectedItem.transform.rotation));
            }
        }
    }
}
