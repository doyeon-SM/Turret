using System.Collections.Generic;
using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// 랜덤 SpawnPoint에서 Enemy를 생성하고 최대 개수를 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GameObject enemyPrefab;

        [SerializeField]
        [Tooltip("랜덤 생성 위치 목록.")]
        private Transform[] spawnPoints;

        [SerializeField]
        [Tooltip("생성된 Enemy의 부모(선택).")]
        private Transform enemyRoot;

        [Header("Spawn Settings")]
        [SerializeField]
        [Min(1)]
        private int initialSpawnCount = 4;

        [SerializeField]
        [Min(1)]
        private int maxAliveCount = 8;

        [SerializeField]
        [Min(0.1f)]
        private float spawnIntervalSeconds = 1f;

        [SerializeField]
        [Tooltip("생성 시 Turret 중앙점을 향하도록 forward를 배치합니다.")]
        private Transform lookAtCenter;

        [SerializeField]
        [Min(0.1f)]
        private float enemyMoveSpeedUnitsPerSecond = 5f;

        [SerializeField]
        [Min(0.1f)]
        private float enemyLifeTimeSeconds = 10f;

        private readonly List<GameObject> aliveEnemies = new List<GameObject>(32);
        private float nextSpawnTimeSeconds;

        private void Start()
        {
            for (int spawnIndex = 0; spawnIndex < initialSpawnCount; spawnIndex++)
            {
                if (!TrySpawnOneEnemy())
                {
                    break;
                }
            }
        }

        private void Update()
        {
            RemoveDestroyedEntries();
            if (Time.time < nextSpawnTimeSeconds)
            {
                return;
            }

            if (aliveEnemies.Count >= maxAliveCount)
            {
                return;
            }

            if (TrySpawnOneEnemy())
            {
                nextSpawnTimeSeconds = Time.time + spawnIntervalSeconds;
            }
        }

        private bool TrySpawnOneEnemy()
        {
            if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                return false;
            }

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (spawnPoint == null)
            {
                return false;
            }

            Quaternion rotation = spawnPoint.rotation;
            if (lookAtCenter != null)
            {
                Vector3 toCenter = lookAtCenter.position - spawnPoint.position;
                if (toCenter.sqrMagnitude > 1e-8f)
                {
                    rotation = Quaternion.LookRotation(toCenter.normalized, Vector3.up);
                }
            }

            GameObject spawned = Instantiate(enemyPrefab, spawnPoint.position, rotation, enemyRoot);
            EnemyLinearMover mover = spawned.GetComponent<EnemyLinearMover>();
            if (mover != null)
            {
                mover.Initialize(enemyMoveSpeedUnitsPerSecond, enemyLifeTimeSeconds);
            }

            aliveEnemies.Add(spawned);
            return true;
        }

        private void RemoveDestroyedEntries()
        {
            for (int index = aliveEnemies.Count - 1; index >= 0; index--)
            {
                if (aliveEnemies[index] == null)
                {
                    aliveEnemies.RemoveAt(index);
                }
            }
        }
    }
}
