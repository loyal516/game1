using UnityEngine;

namespace Overthrone
{
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
        public Transform target;
        public Vector3 localOffset = new Vector3(0f, 2.2f, -4.8f);
        public float lookHeight = 1.35f;
        public float followSharpness = 18f;
        public bool avoidObstacles = true;
        public float collisionRadius = 0.25f;
        public float collisionBuffer = 0.15f;
        public LayerMask collisionMask = ~0;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var lookTarget = target.position + Vector3.up * lookHeight;
            var desiredPosition = target.TransformPoint(localOffset);
            var resolvedPosition = ResolveCameraPosition(lookTarget, desiredPosition);
            transform.position = Vector3.Lerp(transform.position, resolvedPosition, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            transform.LookAt(lookTarget);
        }

        public Vector3 ResolveCameraPosition(Vector3 lookTarget, Vector3 desiredPosition)
        {
            var offset = desiredPosition - lookTarget;
            var distance = offset.magnitude;
            if (!avoidObstacles || distance <= Mathf.Epsilon)
            {
                return desiredPosition;
            }

            var direction = offset / distance;
            if (!Physics.SphereCast(
                    lookTarget,
                    Mathf.Max(0.01f, collisionRadius),
                    direction,
                    out var hit,
                    distance,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return desiredPosition;
            }

            var safeDistance = Mathf.Max(0f, hit.distance - Mathf.Max(0f, collisionBuffer));
            return lookTarget + direction * safeDistance;
        }
    }
}
