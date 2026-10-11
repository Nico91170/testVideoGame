using Rive404.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Rive404.Player
{
    public enum EnemyKind { Drone, Sentinel, Corrupted, Boss }
    public enum EnemyState { Patrol, Detect, Chase, Windup, Recover, Return, Disabled, Dead }

    /// <summary>
    /// Enemies use NavMesh when available, or one collision-aware CharacterController otherwise.
    /// Attack damage occurs once at the end of a visible windup and only if its geometric test passes.
    /// </summary>
    public sealed class EnemyAgent : MonoBehaviour
    {
        public string Id;
        public EnemyKind Kind;
        public float DetectionRadius = 18f;
        public float LeashRadius = 42f;
        public float ActiveRadius = 100f;
        public float MaximumHealth { get; private set; }
        public float Health { get; private set; }
        public float HealthRatio => MaximumHealth <= 0f ? 0f : Health / MaximumHealth;
        public bool IsDead => _dead;
        public int Phase => Kind != EnemyKind.Boss ? 1 : HealthRatio > 0.66f ? 1 : HealthRatio > 0.33f ? 2 : 3;
        public EnemyState State { get; private set; }
        public string StateLabel => State == EnemyState.Windup ? "ATTAQUE IMMINENTE"
            : State == EnemyState.Disabled ? "NEUTRALISÉ" : State == EnemyState.Chase ? "POURSUITE"
            : State == EnemyState.Dead ? "HORS LIGNE" : State == EnemyState.Return ? "REPLI" : "SURVEILLANCE";
        public bool IsShielded => Kind == EnemyKind.Boss && _runtime != null && _runtime.State != null
            && !_runtime.IsDebugActive("final_core");

        private GameRuntime _runtime;
        private CharacterController _controller;
        private NavMeshAgent _navAgent;
        private Vector3 _home;
        private Vector3 _patrolTarget;
        private Vector3 _lastKnownPlayer;
        private Vector3 _attackAim;
        private float _speed;
        private float _attackRange;
        private float _damage;
        private float _windup;
        private float _recovery;
        private float _stateUntil;
        private float _lastSeen;
        private float _nextNavigationCheck;
        private float _nextPatrol;
        private float _disabledUntil;
        private float _flashUntil;
        private float _shieldNoticeAt;
        private float _fallSpeed;
        private int _attackNumber;
        private bool _dead;
        private bool _areaAttack;
        private bool _configured;
        private Transform _visual;
        private Vector3 _visualPosition;
        private Quaternion _visualRotation;
        private LineRenderer _telegraph;
        private LineRenderer _beam;
        private float _beamUntil;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _feedback;
        private LocomotionAnimation _animation;
        private AudioSource _audio;
        private AudioClip _impactClip;

        public void Configure(GameRuntime runtime, string id, EnemyKind kind)
        {
            _runtime = runtime;
            Id = id;
            Kind = kind;
            _home = transform.position;
            switch (kind)
            {
                case EnemyKind.Drone:
                    MaximumHealth = 60f; _speed = 4.1f; _attackRange = 8f; _damage = 10f;
                    _windup = 0.85f; _recovery = 1.35f; DetectionRadius = 17f;
                    break;
                case EnemyKind.Sentinel:
                    MaximumHealth = 130f; _speed = 3f; _attackRange = 2.6f; _damage = 18f;
                    _windup = 0.85f; _recovery = 1.25f; DetectionRadius = 20f;
                    break;
                case EnemyKind.Corrupted:
                    MaximumHealth = 85f; _speed = 5.2f; _attackRange = 2.2f; _damage = 13f;
                    _windup = 0.55f; _recovery = 0.9f; DetectionRadius = 21f;
                    break;
                default:
                    MaximumHealth = 520f; _speed = 3.8f; _attackRange = 4f; _damage = 24f;
                    _windup = 1.1f; _recovery = 1.7f; DetectionRadius = 34f; LeashRadius = 45f;
                    break;
            }
            Health = MaximumHealth;
            _configured = true;
            _feedback = new MaterialPropertyBlock();
            State = EnemyState.Patrol;
            _controller = GetComponent<CharacterController>();
            if (_controller == null) _controller = gameObject.AddComponent<CharacterController>();
            _controller.height = 1.9f;
            _controller.radius = kind == EnemyKind.Boss ? 0.75f : 0.4f;
            _controller.center = Vector3.zero;
            _controller.stepOffset = 0.35f;
            _controller.slopeLimit = 45f;
            _controller.skinWidth = 0.05f;
            _controller.minMoveDistance = 0f;
            // Primitive root colliders and a CharacterController should not compete.
            foreach (Collider collider in GetComponents<Collider>())
                if (collider != _controller) collider.enabled = false;
            if (transform.childCount > 0)
            {
                _visual = transform.GetChild(0);
                _visualPosition = _visual.localPosition;
                _visualRotation = _visual.localRotation;
                _animation = LocomotionAnimation.Attach(_visual, kind.ToString());
            }
            _renderers = GetComponentsInChildren<Renderer>();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
            _audio.minDistance = 5f;
            _audio.maxDistance = 45f;
            _audio.volume = 0.65f;
            _impactClip = Resources.Load<AudioClip>("Audio/Impact");
            _patrolTarget = _home;
            if (runtime != null && runtime.State != null && runtime.State.flags.Contains("kill:" + Id))
            {
                _dead = true;
                Health = 0f;
                State = EnemyState.Dead;
                gameObject.SetActive(false);
                return;
            }
            TryEnableNavigation();
        }

        private void Update()
        {
            if (!_configured || _dead || _runtime == null) return;
            UpdateFeedback();
            if (!_runtime.IsPlaying || !_runtime.CanAcceptInput || _runtime.Player == null)
            {
                StopNavigation();
                return;
            }
            Transform player = _runtime.Player.transform;
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance > ActiveRadius)
            {
                StopNavigation();
                return;
            }
            if (Time.time < _disabledUntil)
            {
                State = EnemyState.Disabled;
                StopNavigation();
                HideTelegraph();
                return;
            }
            if (State == EnemyState.Disabled)
            {
                State = EnemyState.Return;
                _stateUntil = 0f;
            }
            if (Time.time >= _nextNavigationCheck && (_navAgent == null || !_navAgent.isOnNavMesh))
                TryEnableNavigation();

            bool seesPlayer = distance <= DetectionRadius && CombatGeometry.ClearPath(
                transform.position + Vector3.up * 0.55f, _runtime.Player.AimPosition,
                transform, player);
            if (seesPlayer)
            {
                _lastSeen = Time.time;
                _lastKnownPlayer = _runtime.Player.AimPosition - Vector3.up * 0.15f;
            }
            bool outsideLeash = (transform.position - _home).sqrMagnitude > LeashRadius * LeashRadius;
            if (outsideLeash && State != EnemyState.Windup && State != EnemyState.Recover)
                State = EnemyState.Return;

            switch (State)
            {
                case EnemyState.Patrol:
                    if (seesPlayer)
                    {
                        State = EnemyState.Detect;
                        _stateUntil = Time.time + 0.45f;
                        StopNavigation();
                    }
                    else Patrol();
                    break;
                case EnemyState.Detect:
                    Face(player.position);
                    if (Time.time >= _stateUntil)
                        State = seesPlayer ? EnemyState.Chase : EnemyState.Patrol;
                    break;
                case EnemyState.Chase:
                    if (Time.time - _lastSeen > 4f || outsideLeash)
                    {
                        State = EnemyState.Return;
                        break;
                    }
                    Face(_lastKnownPlayer);
                    if (seesPlayer && distance <= CurrentAttackRange)
                        BeginAttack(player.position);
                    else MoveTo(_lastKnownPlayer, _speed * (Phase == 3 ? 1.25f : 1f));
                    break;
                case EnemyState.Windup:
                    StopNavigation();
                    if (Time.time >= _stateUntil)
                    {
                        ResolveAttack(player);
                        State = EnemyState.Recover;
                        _stateUntil = Time.time + _recovery / (Phase == 3 ? 1.2f : 1f);
                        HideTelegraph();
                    }
                    break;
                case EnemyState.Recover:
                    StopNavigation();
                    if (Time.time >= _stateUntil) State = EnemyState.Chase;
                    break;
                case EnemyState.Return:
                    MoveTo(_home, _speed);
                    if ((transform.position - _home).sqrMagnitude < 2.5f)
                    {
                        State = EnemyState.Patrol;
                        _nextPatrol = Time.time + 1f;
                    }
                    break;
            }
            if (_visual != null)
            {
                float bob = Kind == EnemyKind.Drone ? Mathf.Sin(Time.time * 3f) * 0.16f
                    : State == EnemyState.Chase ? Mathf.Sin(Time.time * 10f) * 0.03f : 0f;
                float lean = State == EnemyState.Windup ? -12f : State == EnemyState.Recover ? 8f : 0f;
                _visual.localPosition = _visualPosition + Vector3.up * bob;
                _visual.localRotation = _visualRotation * Quaternion.Euler(lean, 0f, 0f);
            }
        }

        private float CurrentAttackRange => Kind == EnemyKind.Boss && Phase >= 2 && _attackNumber % 2 == 1
            ? 6.5f : _attackRange;

        private void Patrol()
        {
            if (Time.time >= _nextPatrol)
            {
                // Fixed phase derived from the position: no shared random state or replay instability.
                float angle = (_home.x * 0.19f + _home.z * 0.11f + _attackNumber * 1.4f
                    + Mathf.Floor(Time.time / 7f) * 1.8f);
                _patrolTarget = _home + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 5f;
                _nextPatrol = Time.time + 7f;
            }
            if ((_patrolTarget - transform.position).sqrMagnitude > 1.2f)
                MoveTo(_patrolTarget, _speed * 0.42f);
            else StopNavigation();
        }

        private void BeginAttack(Vector3 target)
        {
            State = EnemyState.Windup;
            _areaAttack = Kind == EnemyKind.Boss && Phase >= 2 && _attackNumber % 2 == 1;
            _attackAim = Vector3.ProjectOnPlane(target - transform.position, Vector3.up).normalized;
            if (_attackAim.sqrMagnitude < 0.01f) _attackAim = transform.forward;
            transform.rotation = Quaternion.LookRotation(_attackAim);
            // Phase 3 increases pressure but keeps at least 0.7 seconds of boss telegraph.
            _stateUntil = Time.time + (Kind == EnemyKind.Boss && Phase == 3 ? 0.8f : _windup);
            StopNavigation();
            ShowTelegraph(_areaAttack ? 6.5f : _attackRange);
        }

        private void ResolveAttack(Transform player)
        {
            _attackNumber++;
            if (_runtime == null || _runtime.Player == null || _runtime.Player.IsInvulnerable) return;
            Vector3 delta = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
            float range = _areaAttack ? 6.5f : _attackRange;
            if (delta.sqrMagnitude > range * range) return;
            float verticalDistance = Mathf.Abs(_runtime.Player.AimPosition.y - transform.position.y);
            if (_areaAttack && verticalDistance > 1.1f) return;
            if (Kind != EnemyKind.Drone && verticalDistance > (Kind == EnemyKind.Boss ? 2.2f : 1.6f)) return;
            if (!_areaAttack && delta.sqrMagnitude > 0.01f
                && Vector3.Dot(_attackAim, delta.normalized) < (Kind == EnemyKind.Drone ? 0.92f : 0.4f))
                return;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 destination = _runtime.Player.AimPosition;
            if (!CombatGeometry.ClearPath(origin, destination, transform, player)) return;
            if (Kind == EnemyKind.Drone) ShowBeam(origin, destination);
            float damage = _damage * (Kind == EnemyKind.Boss && Phase == 3 ? 1.2f : 1f);
            _runtime.DamagePlayer(damage);
        }

        public void TakeDamage(float amount)
        {
            if (_dead || !_configured || amount <= 0f || _runtime == null || !_runtime.IsPlaying) return;
            if (IsShielded)
            {
                _flashUntil = Time.time + 0.18f;
                if (Time.time >= _shieldNoticeAt)
                {
                    _runtime.ShowMessage("Bouclier de l'Archiviste : utilisez OVERRIDE sur le noyau.");
                    _shieldNoticeAt = Time.time + 3f;
                }
                return;
            }
            int phaseBefore = Phase;
            if (_audio != null && _impactClip != null) _audio.PlayOneShot(_impactClip);
            Health = Mathf.Max(0f, Health - amount);
            _flashUntil = Time.time + 0.17f;
            if (Health <= 0f)
            {
                Die();
                return;
            }
            if (Kind == EnemyKind.Boss && Phase != phaseBefore)
                _runtime.ShowMessage(Phase == 2 ? "Archiviste — phase 2 : éloignez-vous de l'onde rouge."
                    : "Archiviste — phase 3 : alternance frappe et onde, surveillez les cercles.");
            if (State == EnemyState.Patrol || State == EnemyState.Return)
            {
                State = EnemyState.Detect;
                _stateUntil = Time.time + 0.25f;
            }
        }

        public void SetDisabled(float duration)
        {
            if (_dead || Kind == EnemyKind.Boss) return;
            _disabledUntil = Mathf.Max(_disabledUntil, Time.time + Mathf.Max(0f, duration));
            State = EnemyState.Disabled;
            HideTelegraph();
            StopNavigation();
        }

        private void Die()
        {
            _dead = true;
            State = EnemyState.Dead;
            Health = 0f;
            StopNavigation();
            if (_navAgent != null) _navAgent.enabled = false;
            if (_controller != null) _controller.enabled = false;
            HideTelegraph();
            _runtime.Record("kill:" + Id);
            _runtime.ShowMessage(Kind == EnemyKind.Boss ? "L'Archiviste est hors ligne. Le choix vous appartient."
                : "Menace neutralisée.");
            if (_visual != null) _visual.localRotation = _visualRotation * Quaternion.Euler(0f, 0f, 80f);
            Destroy(gameObject, 2f);
        }

        private void TryEnableNavigation()
        {
            _nextNavigationCheck = Time.time + 3f;
            Vector3 ground = transform.position - Vector3.up;
            if (!NavMesh.SamplePosition(ground, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                if (_navAgent != null) _navAgent.enabled = false;
                if (_controller != null) _controller.enabled = true;
                return;
            }
            if (_navAgent == null) _navAgent = GetComponent<NavMeshAgent>();
            if (_navAgent == null) _navAgent = gameObject.AddComponent<NavMeshAgent>();
            _navAgent.enabled = false;
            _navAgent.height = 1.9f;
            _navAgent.radius = Kind == EnemyKind.Boss ? 0.75f : 0.4f;
            _navAgent.baseOffset = 1f;
            _navAgent.speed = _speed;
            _navAgent.acceleration = 14f;
            _navAgent.angularSpeed = 420f;
            _navAgent.stoppingDistance = 0.3f;
            _navAgent.updateRotation = false;
            _controller.enabled = false;
            transform.position = hit.position + Vector3.up * _navAgent.baseOffset;
            _navAgent.enabled = true;
            if (!_navAgent.isOnNavMesh)
            {
                _navAgent.enabled = false;
                _controller.enabled = true;
            }
        }

        private void MoveTo(Vector3 destination, float speed)
        {
            if (_animation != null) _animation.SetSpeed(speed);
            if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = false;
                _navAgent.speed = speed;
                _navAgent.SetDestination(destination - Vector3.up);
                Vector3 lookAt = transform.position + _navAgent.desiredVelocity;
                Face(lookAt);
                return;
            }
            if (_controller == null || !_controller.enabled) return;
            Vector3 movement = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);
            if (movement.sqrMagnitude > 0.05f)
            {
                Face(destination);
                movement = movement.normalized * speed;
            }
            else movement = Vector3.zero;
            _fallSpeed = _controller.isGrounded ? -2f : Mathf.Max(-30f, _fallSpeed - 22f * Time.deltaTime);
            _controller.Move((movement + Vector3.up * _fallSpeed) * Time.deltaTime);
        }

        private void Face(Vector3 destination)
        {
            Vector3 direction = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);
            if (direction.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
                1f - Mathf.Exp(-9f * Time.deltaTime));
        }

        private void StopNavigation()
        {
            if (_animation != null) _animation.SetSpeed(0f);
            if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh) _navAgent.isStopped = true;
        }

        private void ShowTelegraph(float radius)
        {
            if (_telegraph == null)
            {
                GameObject indicator = new GameObject("Zone de danger annoncée");
                indicator.transform.SetParent(transform, false);
                _telegraph = indicator.AddComponent<LineRenderer>();
                _telegraph.material = CombatGeometry.CreateEffectMaterial();
                _telegraph.useWorldSpace = false;
                _telegraph.startWidth = _telegraph.endWidth = Kind == EnemyKind.Boss ? 0.12f : 0.07f;
                _telegraph.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            _telegraph.enabled = true;
            _telegraph.startColor = _telegraph.endColor = _areaAttack ? Color.red : new Color(1f, 0.55f, 0.1f);
            if (_areaAttack)
            {
                _telegraph.loop = true;
                _telegraph.positionCount = 40;
                for (int i = 0; i < 40; i++)
                {
                    float angle = i / 40f * Mathf.PI * 2f;
                    _telegraph.SetPosition(i, new Vector3(Mathf.Sin(angle) * radius, -0.9f, Mathf.Cos(angle) * radius));
                }
            }
            else if (Kind == EnemyKind.Drone)
            {
                _telegraph.loop = false;
                _telegraph.positionCount = 2;
                _telegraph.SetPosition(0, Vector3.up * 0.25f);
                _telegraph.SetPosition(1, Vector3.forward * radius + Vector3.up * 0.25f);
            }
            else
            {
                _telegraph.loop = false;
                _telegraph.positionCount = 17;
                _telegraph.SetPosition(0, new Vector3(0f, -0.9f, 0f));
                for (int i = 1; i < 16; i++)
                {
                    float angle = Mathf.Lerp(-65f, 65f, (i - 1) / 14f) * Mathf.Deg2Rad;
                    _telegraph.SetPosition(i, new Vector3(Mathf.Sin(angle) * radius, -0.9f, Mathf.Cos(angle) * radius));
                }
                _telegraph.SetPosition(16, new Vector3(0f, -0.9f, 0f));
            }
        }

        private void HideTelegraph()
        {
            if (_telegraph != null) _telegraph.enabled = false;
        }

        private void ShowBeam(Vector3 from, Vector3 to)
        {
            if (_beam == null)
            {
                GameObject beam = new GameObject("Impact du drone");
                beam.transform.SetParent(transform, false);
                _beam = beam.AddComponent<LineRenderer>();
                _beam.material = CombatGeometry.CreateEffectMaterial();
                _beam.positionCount = 2;
                _beam.startWidth = 0.08f;
                _beam.endWidth = 0.02f;
                _beam.startColor = _beam.endColor = new Color(1f, 0.15f, 0.1f);
            }
            _beam.SetPosition(0, from);
            _beam.SetPosition(1, to);
            _beam.enabled = true;
            _beamUntil = Time.time + 0.15f;
        }

        private void UpdateFeedback()
        {
            if (_beam != null && Time.time >= _beamUntil) _beam.enabled = false;
            if (_renderers == null) return;
            bool flash = Time.time < _flashUntil;
            bool disabled = Time.time < _disabledUntil;
            foreach (Renderer renderer in _renderers)
            {
                if (renderer == null) continue;
                if (flash || disabled || IsShielded)
                {
                    Color tint = flash ? Color.white : disabled ? new Color(0.25f, 0.9f, 1f)
                        : new Color(0.55f, 0.35f, 0.85f);
                    _feedback.SetColor("_BaseColor", tint);
                    _feedback.SetColor("_Color", tint);
                    renderer.SetPropertyBlock(_feedback);
                }
                else renderer.SetPropertyBlock(null);
            }
        }

        private void OnDestroy()
        {
            if (_telegraph != null && _telegraph.sharedMaterial != null) Destroy(_telegraph.sharedMaterial);
            if (_beam != null && _beam.sharedMaterial != null) Destroy(_beam.sharedMaterial);
        }

        private void OnGUI()
        {
            if (_dead || _runtime == null || !_runtime.IsPlaying || Camera.main == null
                || _runtime.Player == null || _runtime.IsPaused || _runtime.IsDialogueOpen) return;
            float distance = Vector3.Distance(_runtime.Player.transform.position, transform.position);
            if (distance > (Kind == EnemyKind.Boss ? 45f : 22f)) return;
            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position
                + Vector3.up * (Kind == EnemyKind.Boss ? 2.5f : 1.65f));
            if (screen.z <= 0f) return;
            float width = Kind == EnemyKind.Boss ? 230f : 140f;
            float x = screen.x - width * 0.5f;
            float y = Screen.height - screen.y;
            Color old = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x, y, width, 8), Texture2D.whiteTexture);
            GUI.color = IsShielded ? new Color(.6f, .4f, 1f) : new Color(.95f, .25f, .2f);
            GUI.DrawTexture(new Rect(x + 1, y + 1, (width - 2) * HealthRatio, 6), Texture2D.whiteTexture);
            GUI.color = Color.white;
            string label = Kind == EnemyKind.Boss ? "ARCHIVISTE — PHASE " + Phase
                + (IsShielded ? " — BOUCLIER" : "") : Kind == EnemyKind.Drone ? "DRONE"
                : Kind == EnemyKind.Sentinel ? "SENTINELLE" : "CORROMPU";
            GUI.Label(new Rect(x, y - 24, width + 40, 24), label);
            if (State == EnemyState.Windup || State == EnemyState.Disabled)
                GUI.Label(new Rect(x, y + 9, width + 20, 24), StateLabel);
            GUI.color = old;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, DetectionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _configured ? CurrentAttackRange : 2.5f);
        }
    }
}
