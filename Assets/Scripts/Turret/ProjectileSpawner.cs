using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace TurretDemo
{
    /// <summary>
    /// 발사체 프리팹의 ObjectPool 생성/대여/반납과 <see cref="ProjectileMover"/> 초기화를 담당합니다.
    /// </summary>
    public static class ProjectileSpawner
    {
        private const int DefaultPoolCapacity = 32;
        private const int DefaultPoolMaxSize = 256;

        private static readonly Dictionary<(int prefabId, int defaultCapacity, int maxSize), ObjectPool<GameObject>> PoolsByConfig =
            new Dictionary<(int prefabId, int defaultCapacity, int maxSize), ObjectPool<GameObject>>();

        /// <summary>
        /// Muzzle 위치·회전에 맞춰 pool에서 발사체를 대여하고 등속 이동 파라미터를 적용합니다.
        /// </summary>
        /// <param name="projectilePrefab">발사체 프리팹.</param>
        /// <param name="spawnPosition">월드 생성 위치.</param>
        /// <param name="spawnRotation">월드 생성 회전(전진 방향).</param>
        /// <param name="speedUnitsPerSecond">전진 속도.</param>
        /// <param name="lifeTimeSeconds">수명(초).</param>
        /// <param name="parent">정리용 부모(없으면 null).</param>
        /// <returns>생성된 인스턴스(실패 시 null).</returns>
        public static GameObject Spawn(
            GameObject projectilePrefab,
            Vector3 spawnPosition,
            Quaternion spawnRotation,
            float speedUnitsPerSecond,
            float lifeTimeSeconds,
            float damageAmount,
            int shooterTeamId,
            int poolDefaultCapacity,
            int poolMaxSize,
            Transform parent)
        {
            if (projectilePrefab == null)
            {
                return null;
            }

            int normalizedDefaultCapacity = Mathf.Max(poolDefaultCapacity, 1);
            int normalizedMaxSize = Mathf.Max(poolMaxSize, normalizedDefaultCapacity);
            ObjectPool<GameObject> pool = GetOrCreatePool(projectilePrefab, normalizedDefaultCapacity, normalizedMaxSize);
            GameObject instance = pool.Get();
            instance.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            instance.transform.SetParent(parent);

            PooledObjectLifecycle lifecycle = instance.GetComponent<PooledObjectLifecycle>();
            if (lifecycle == null)
            {
                lifecycle = instance.AddComponent<PooledObjectLifecycle>();
            }

            lifecycle.SetReleaseAction(() => pool.Release(instance));

            ProjectileMover mover = instance.GetComponent<ProjectileMover>();
            if (mover != null)
            {
                mover.Initialize(speedUnitsPerSecond, lifeTimeSeconds, damageAmount, shooterTeamId);
            }

            return instance;
        }

        private static ObjectPool<GameObject> GetOrCreatePool(GameObject projectilePrefab)
        {
            return GetOrCreatePool(projectilePrefab, DefaultPoolCapacity, DefaultPoolMaxSize);
        }

        private static ObjectPool<GameObject> GetOrCreatePool(GameObject projectilePrefab, int defaultCapacity, int maxSize)
        {
            int prefabId = projectilePrefab.GetInstanceID();
            (int prefabId, int defaultCapacity, int maxSize) key = (prefabId, defaultCapacity, maxSize);
            if (PoolsByConfig.TryGetValue(key, out ObjectPool<GameObject> existingPool))
            {
                return existingPool;
            }

            ObjectPool<GameObject> createdPool = null;
            createdPool = new ObjectPool<GameObject>(
                createFunc: () => Object.Instantiate(projectilePrefab),
                actionOnGet: instance => instance.SetActive(true),
                actionOnRelease: instance =>
                {
                    instance.SetActive(false);
                    instance.transform.SetParent(null);
                },
                actionOnDestroy: instance => Object.Destroy(instance),
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);

            PoolsByConfig.Add(key, createdPool);
            return createdPool;
        }
    }
}
