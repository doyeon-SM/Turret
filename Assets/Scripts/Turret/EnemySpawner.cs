using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

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

        [SerializeField]
        [Min(1)]
        [Tooltip("Enemy pool 기본 용량입니다.")]
        private int poolDefaultCapacity = 8;

        [SerializeField]
        [Min(1)]
        [Tooltip("Enemy pool이 유지할 최대 인스턴스 수.")]
        private int poolMaxSize = 128;

        private readonly List<GameObject> aliveEnemies = new List<GameObject>(32);
        private ObjectPool<GameObject> enemyPool;
        private float nextSpawnTimeSeconds;

        private void Awake()
        {
            if (enemyPrefab == null)
            {
                return;
            }

            int defaultCapacity = Mathf.Max(Mathf.Max(initialSpawnCount, poolDefaultCapacity), 1);
            int maxSize = Mathf.Max(poolMaxSize, defaultCapacity);
            enemyPool = new ObjectPool<GameObject>(
                createFunc: CreateEnemyInstance,
                actionOnGet: enemy => enemy.SetActive(true),
                actionOnRelease: enemy =>
                {
                    if (enemy != null)
                    {
                        enemy.SetActive(false);
                    }
                },
                actionOnDestroy: enemy =>
                {
                    if (enemy != null)
                    {
                        Destroy(enemy);
                    }
                },
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

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
            if (enemyPool == null || spawnPoints == null || spawnPoints.Length == 0)
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

            GameObject spawned = enemyPool.Get();
            spawned.transform.SetPositionAndRotation(spawnPoint.position, rotation);
            spawned.transform.SetParent(enemyRoot);

            PooledObjectLifecycle lifecycle = spawned.GetComponent<PooledObjectLifecycle>();
            if (lifecycle == null)
            {
                lifecycle = spawned.AddComponent<PooledObjectLifecycle>();
            }

            lifecycle.SetReleaseAction(() => ReleaseEnemy(spawned));

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
                GameObject aliveEnemy = aliveEnemies[index];
                if (aliveEnemy == null || !aliveEnemy.activeInHierarchy)
                {
                    aliveEnemies.RemoveAt(index);
                }
            }
        }

        private GameObject CreateEnemyInstance()
        {
            GameObject createdEnemy = Instantiate(enemyPrefab);
            createdEnemy.SetActive(false);
            return createdEnemy;
        }

        private void ReleaseEnemy(GameObject enemy)
        {
            if (enemy == null || enemyPool == null)
            {
                return;
            }

            aliveEnemies.Remove(enemy);
            enemyPool.Release(enemy);
        }
    }
}
