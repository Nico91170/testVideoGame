using Rive404.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rive404.Player
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        public float Sensitivity = 0.14f;
        public float Distance = 5f;
        public float TargetHeight = 0.7f;
        public float MinimumPitch = -25f;
        public float MaximumPitch = 70f;
        public float CollisionRadius = 0.22f;
        public float FollowSpeed = 16f;
        public LayerMask CollisionMask = ~0;

        private Transform _target;
        private CharacterController _targetController;
        private float _yaw;
        private float _pitch = 20f;
        private float _actualDistance;
        private Vector3 _focus;
        private readonly RaycastHit[] _collisionHits = new RaycastHit[32];

        public void Init(Transform target)
        {
            _target = target;
            _targetController = target != null ? target.GetComponent<CharacterController>() : null;
            _yaw = target != null ? target.eulerAngles.y : 0f;
            _actualDistance = Distance;
            if (target != null) _focus = FocusPosition;
            Camera cameraComponent = GetComponent<Camera>();
            cameraComponent.nearClipPlane = 0.08f;
            cameraComponent.farClipPlane = 1100f;
            ApplyPosition(true);
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            GameRuntime runtime = GameRuntime.Instance;
            if (runtime == null || runtime.CanAcceptInput)
            {
                Mouse mouse = Mouse.current;
                if (mouse != null)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    _yaw += delta.x * Sensitivity;
                    _pitch = Mathf.Clamp(_pitch - delta.y * Sensitivity, MinimumPitch, MaximumPitch);
                    Distance = Mathf.Clamp(Distance - mouse.scroll.ReadValue().y / 120f * 0.45f, 2.2f, 7.5f);
                }
            }
            ApplyPosition(false);
        }

        private void ApplyPosition(bool instant)
        {
            if (_target == null) return;
            Vector3 destination = FocusPosition;
            // A respawn must not ease the camera across an entire district.
            bool teleport = Vector3.SqrMagnitude(destination - _focus) > 100f;
            float interpolation = 1f - Mathf.Exp(-FollowSpeed * Time.unscaledDeltaTime);
            _focus = instant || teleport ? destination : Vector3.Lerp(_focus, destination, interpolation);
            Quaternion orientation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 backwards = orientation * Vector3.back;
            float allowedDistance = Distance;
            int count = Physics.SphereCastNonAlloc(_focus, CollisionRadius, backwards, _collisionHits,
                Distance, CollisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform obstacle = _collisionHits[i].transform;
                if (obstacle.IsChildOf(_target) || obstacle.IsChildOf(transform)) continue;
                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0.35f, _collisionHits[i].distance - 0.08f));
            }
            // Contract instantly when obstructed, ease out when free. Walls never pass through the lens.
            _actualDistance = instant || allowedDistance < _actualDistance ? allowedDistance
                : Mathf.Lerp(_actualDistance, allowedDistance, interpolation);
            transform.SetPositionAndRotation(_focus + backwards * _actualDistance, orientation);
        }

        private Vector3 FocusPosition => _target.position + Vector3.up * (_targetController != null
            ? _targetController.center.y + Mathf.Min(TargetHeight, _targetController.height * 0.35f)
            : TargetHeight);
    }
}
