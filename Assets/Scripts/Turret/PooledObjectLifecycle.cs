using System;
using UnityEngine;

namespace TurretDemo
{
    /// <summary>
    /// ObjectPool에서 꺼낸 오브젝트의 반환 콜백을 보관하고 재사용 시점에 호출합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledObjectLifecycle : MonoBehaviour
    {
        private Action releaseAction;

        /// <summary>
        /// 현재 인스턴스의 pool 반환 동작을 등록합니다.
        /// </summary>
        public void SetReleaseAction(Action releaseCallback)
        {
            releaseAction = releaseCallback;
        }

        /// <summary>
        /// 등록된 반환 동작이 있으면 pool로 되돌리고, 없으면 Destroy로 안전하게 정리합니다.
        /// </summary>
        public void ReturnToPoolOrDestroy()
        {
            if (releaseAction != null)
            {
                releaseAction.Invoke();
                return;
            }

            Destroy(gameObject);
        }
    }
}
