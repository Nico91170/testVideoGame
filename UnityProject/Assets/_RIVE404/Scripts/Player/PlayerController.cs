using System.Collections.Generic;
using Rive404.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rive404.Player
{
    /// <summary>One CharacterController owns movement. Key positions work on AZERTY and QWERTY.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public float WalkSpeed = 6f;
        public float SprintSpeed = 10f;
        public float JumpHeight = 1.4f;
        public float Gravity = -25f;
        public float TurnSpeed = 14f;
        public float DodgeSpeed = 17f;
        public float DodgeDuration = 0.28f;
        public float DodgeCooldown = 0.8f;
        public float LightDamage = 26f;
        public float HeavyDamage = 45f;

        public Vector3 Velocity { get; private set; }
        public Vector3 AimPosition => _controller != null
            ? transform.TransformPoint(_controller.center) + Vector3.up * 0.15f
            : transform.position + Vector3.up;
        public bool IsInvulnerable => Time.time < _dodgeUntil || Time.time < _hurtUntil;
        public bool LastAttackHeavy { get; private set; }
        public bool IsAttacking => Time.time < _attackUntil;

        private GameRuntime _runtime;
        private CharacterController _controller;
        private float _verticalSpeed;
        private float _groundedUntil;
        private float _dodgeUntil;
        private float _nextDodge;
        private float _hurtUntil;
        private float _attackUntil;
        private float _hitAt;
        private bool _attackPending;
        private Vector3 _dodgeDirection;
        private readonly Collider[] _hits = new Collider[48];
        private readonly HashSet<EnemyAgent> _attackTargets = new HashSet<EnemyAgent>();
        private Transform _visual;
        private Vector3 _visualPosition;
        private Vector3 _visualScale;
        private Quaternion _visualRotation;
        private float _standingHeight;
        private Vector3 _standingCenter;
        private bool _crouched;
        private LineRenderer _slash;
        private float _slashUntil;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _hurtBlock;
        private LocomotionAnimation _animation;
        private AudioSource _audio;
        private AudioClip _footstepClip;
        private AudioClip _swingClip;
        private float _nextFootstep;

        public void Init(GameRuntime runtime)
        {
            _runtime = runtime;
            _controller = GetComponent<CharacterController>();
            _controller.minMoveDistance = 0f;
            _standingHeight = _controller.height;
            _standingCenter = _controller.center;
            // The runtime attaches the model before Init. Keep its original pivot.
            if (transform.childCount > 0)
            {
                _visual = transform.GetChild(0);
                _visualPosition = _visual.localPosition;
                _visualScale = _visual.localScale;
                _visualRotation = _visual.localRotation;
                _animation = LocomotionAnimation.Attach(_visual, "Nico");
            }
            _renderers = GetComponentsInChildren<Renderer>();
            _hurtBlock = new MaterialPropertyBlock();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0.5f;
            _audio.volume = 0.45f;
            _footstepClip = Resources.Load<AudioClip>("Audio/Footstep");
            _swingClip = Resources.Load<AudioClip>("Audio/Swing");
            _verticalSpeed = -2f;
        }

        private void Update()
        {
            if (_runtime == null || _controller == null || !_controller.enabled)
                return;
            UpdateFeedback();
            if (!_runtime.CanAcceptInput)
            {
                Velocity = Vector3.zero;
                if (_animation != null) _animation.SetSpeed(0f);
                // A dialogue cannot release a pending hit when it closes.
                _attackPending = false;
                return;
            }

            Keyboard keys = Keyboard.current;
            Vector2 axis = Vector2.zero;
            if (keys != null)
            {
                axis.x = ((keys.dKey.isPressed || keys.rightArrowKey.isPressed) ? 1f : 0f)
                       - ((keys.aKey.isPressed || keys.leftArrowKey.isPressed) ? 1f : 0f);
                axis.y = ((keys.wKey.isPressed || keys.upArrowKey.isPressed) ? 1f : 0f)
                       - ((keys.sKey.isPressed || keys.downArrowKey.isPressed) ? 1f : 0f);
                axis = Vector2.ClampMagnitude(axis, 1f);
            }
            Transform cameraTransform = Camera.main != null ? Camera.main.transform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            Vector3 direction = forward * axis.y + right * axis.x;
            bool grounded = _controller.isGrounded;
            if (grounded)
            {
                _groundedUntil = Time.time + 0.12f;
                if (_verticalSpeed < 0f) _verticalSpeed = -2f;
            }
            SetCrouch(keys != null && keys.cKey.isPressed);

            if (keys != null && keys.spaceKey.wasPressedThisFrame && Time.time <= _groundedUntil
                && !_crouched && Time.time >= _dodgeUntil)
            {
                _verticalSpeed = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                _groundedUntil = -1f;
            }
            if (keys != null && keys.leftAltKey.wasPressedThisFrame)
                TryDodge(direction);

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame) TryAttack(false);
                if (mouse.rightButton.wasPressedThisFrame) TryAttack(true);
            }

            float speed = _crouched ? WalkSpeed * 0.5f : WalkSpeed;
            bool sprint = keys != null && keys.leftShiftKey.isPressed && direction.sqrMagnitude > 0.01f
                          && !_crouched && !IsAttacking;
            sprint = sprint && _runtime.TrySpendEnergy(5f * Time.deltaTime);
            if (sprint) speed = SprintSpeed;
            if (IsAttacking) speed *= 0.55f;
            Vector3 horizontal = direction * speed;
            if (Time.time < _dodgeUntil)
                horizontal = _dodgeDirection * DodgeSpeed;
            else if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction), 1f - Mathf.Exp(-TurnSpeed * Time.deltaTime));

            _verticalSpeed = Mathf.Max(_verticalSpeed + Gravity * Time.deltaTime, -35f);
            Vector3 before = transform.position;
            _controller.Move((horizontal + Vector3.up * _verticalSpeed) * Time.deltaTime);
            Velocity = (transform.position - before) / Mathf.Max(Time.deltaTime, 0.0001f);
            if (_animation != null) _animation.SetSpeed(Vector3.ProjectOnPlane(Velocity, Vector3.up).magnitude);
            if (_controller.isGrounded && horizontal.magnitude > 0.5f && Time.time >= _nextFootstep)
            {
                if (_footstepClip != null) _audio.PlayOneShot(_footstepClip, _crouched ? 0.4f : 0.65f);
                _nextFootstep = Time.time + (sprint ? 0.28f : 0.43f);
            }
            if ((_controller.collisionFlags & CollisionFlags.Above) != 0 && _verticalSpeed > 0f)
                _verticalSpeed = 0f;
            if (_attackPending && Time.time >= _hitAt)
            {
                _attackPending = false;
                ResolveAttack();
            }
            AnimateVisual(horizontal.magnitude);
        }

        private void TryDodge(Vector3 direction)
        {
            if (Time.time < _nextDodge || !_runtime.CanAcceptInput || _crouched
                || !_runtime.TrySpendEnergy(8f)) return;
            _dodgeDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;
            _dodgeUntil = Time.time + DodgeDuration;
            _nextDodge = Time.time + DodgeCooldown;
            _attackPending = false;
            _attackUntil = 0f;
            transform.rotation = Quaternion.LookRotation(_dodgeDirection);
        }

        public void TryAttack(bool heavy)
        {
            if (_runtime == null || !_runtime.CanAcceptInput || IsAttacking || Time.time < _dodgeUntil)
                return;
            if (heavy && !_runtime.TrySpendEnergy(6f)) return;
            LastAttackHeavy = heavy;
            _attackUntil = Time.time + (heavy ? 0.78f : 0.42f);
            _hitAt = Time.time + (heavy ? 0.30f : 0.12f);
            _attackPending = true;
            if (_audio != null && _swingClip != null) _audio.PlayOneShot(_swingClip, heavy ? 1f : 0.7f);
        }

        private void ResolveAttack()
        {
            float range = LastAttackHeavy ? 3.3f : 2.7f;
            Vector3 origin = AimPosition;
            int count = Physics.OverlapSphereNonAlloc(origin, range, _hits, ~0, QueryTriggerInteraction.Ignore);
            _attackTargets.Clear();
            for (int i = 0; i < count; i++)
            {
                EnemyAgent enemy = _hits[i].GetComponentInParent<EnemyAgent>();
                if (enemy == null || enemy.IsDead || !_attackTargets.Add(enemy)) continue;
                Vector3 delta = Vector3.ProjectOnPlane(enemy.transform.position - transform.position, Vector3.up);
                if (delta.sqrMagnitude > range * range || (delta.sqrMagnitude > 0.01f
                    && Vector3.Dot(transform.forward, delta.normalized) < (LastAttackHeavy ? -0.05f : 0.3f)))
                    continue;
                if (!CombatGeometry.ClearPath(origin, enemy.transform.position + Vector3.up * 0.2f,
                    transform, enemy.transform)) continue;
                enemy.TakeDamage(LastAttackHeavy ? HeavyDamage : LightDamage);
            }
            ShowSlash(range);
        }

        /// <summary>Called by the runtime after damage; prevents repeated hits from overlapping attacks.</summary>
        public void NotifyDamaged(float invulnerabilitySeconds = 0.7f)
        {
            _hurtUntil = Time.time + invulnerabilitySeconds;
        }

        /// <summary>Teleport without dragging a CharacterController through the world.</summary>
        public void RecoverAt(Vector3 position)
        {
            if (_controller == null) _controller = GetComponent<CharacterController>();
            bool wasEnabled = _controller != null && _controller.enabled;
            if (_controller != null) _controller.enabled = false;
            transform.position = position;
            if (_controller != null) _controller.enabled = wasEnabled;
            _verticalSpeed = -2f;
            _attackPending = false;
            _attackUntil = _dodgeUntil = 0f;
            _hurtUntil = Time.time + 1.5f;
            Velocity = Vector3.zero;
        }

        private void SetCrouch(bool crouch)
        {
            if (crouch == _crouched) return;
            if (!crouch)
            {
                Vector3 head = transform.TransformPoint(_standingCenter)
                    + Vector3.up * (_standingHeight * 0.5f - _controller.radius);
                Collider[] overhead = Physics.OverlapSphere(head, _controller.radius * 0.92f, ~0,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider obstacle in overhead)
                    if (!obstacle.transform.IsChildOf(transform)) return;
            }
            _crouched = crouch;
            _controller.height = crouch ? _standingHeight * 0.6f : _standingHeight;
            _controller.center = _standingCenter - Vector3.up * (_standingHeight - _controller.height) * 0.5f;
        }

        private void AnimateVisual(float speed)
        {
            if (_visual == null) return;
            float bob = _controller.isGrounded && speed > 0.3f ? Mathf.Sin(Time.time * 12f) * 0.035f : 0f;
            float lean = Time.time < _dodgeUntil ? 22f : _crouched ? 12f : 0f;
            float swing = IsAttacking ? Mathf.Sin((_attackUntil - Time.time) * 9f) * 13f : 0f;
            _visual.localPosition = _visualPosition + Vector3.up * bob;
            _visual.localScale = new Vector3(_visualScale.x, _visualScale.y * (_crouched ? 0.6f : 1f),
                _visualScale.z);
            _visual.localRotation = _visualRotation * Quaternion.Euler(lean, swing, 0f);
        }

        private void ShowSlash(float range)
        {
            if (_slash == null)
            {
                GameObject slash = new GameObject("Trajectoire de frappe");
                slash.transform.SetParent(transform, false);
                _slash = slash.AddComponent<LineRenderer>();
                _slash.material = CombatGeometry.CreateEffectMaterial();
                _slash.useWorldSpace = false;
                _slash.positionCount = 13;
                _slash.startWidth = 0.09f;
                _slash.endWidth = 0.025f;
                _slash.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Color color = LastAttackHeavy ? new Color(1f, 0.48f, 0.1f) : new Color(0.3f, 1f, 1f);
            _slash.startColor = _slash.endColor = color;
            for (int i = 0; i < _slash.positionCount; i++)
            {
                float angle = Mathf.Lerp(-70f, 70f, i / 12f) * Mathf.Deg2Rad;
                _slash.SetPosition(i, new Vector3(Mathf.Sin(angle) * range * 0.7f,
                    _controller.center.y + 0.15f + Mathf.Sin(i / 12f * Mathf.PI) * 0.12f,
                    Mathf.Cos(angle) * range * 0.7f));
            }
            _slash.enabled = true;
            _slashUntil = Time.time + 0.16f;
        }

        private void UpdateFeedback()
        {
            if (_slash != null && Time.time >= _slashUntil) _slash.enabled = false;
            if (_renderers == null) return;
            bool flash = Time.time < _hurtUntil && ((int)(Time.time * 12f) % 2 == 0);
            foreach (Renderer visual in _renderers)
            {
                if (visual == null) continue;
                if (flash)
                {
                    _hurtBlock.SetColor("_BaseColor", new Color(1f, 0.25f, 0.25f));
                    _hurtBlock.SetColor("_Color", new Color(1f, 0.25f, 0.25f));
                    visual.SetPropertyBlock(_hurtBlock);
                }
                else visual.SetPropertyBlock(null);
            }
        }

        private void OnDestroy()
        {
            if (_slash != null && _slash.sharedMaterial != null) Destroy(_slash.sharedMaterial);
        }
    }
}
