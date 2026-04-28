using System;
using ShatteredForge.Core;
using ShatteredForge.Input;
using UnityEngine;

namespace ShatteredForge.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float turnSpeed = 14f;
        [SerializeField] private float fireInterval = 0.35f;
        [SerializeField] private Transform fireOrigin;

        [Header("Camp / mouse-look mode")]
        [Tooltip("WASD move along transform.right/forward (Gothic-style), not world XZ.")]
        [SerializeField] private bool moveRelativeToFacing;

        [Tooltip("If off, yaw comes from external driver (e.g. camp camera rig); movement no longer steers facing.")]
        [SerializeField] private bool rotateFromMovementDirection = true;

        private CharacterController _controller;
        private SimpleProjectile _projectileTemplate;
        private RunState _runState;
        private Action _onDeath;
        private Func<ComputedCharacterStats> _statsProvider;
        private float _nextFire;
        private Vector3 _lastMoveDir = Vector3.forward;

        public Vector3 PlanarFacingDirection => _lastMoveDir;

        /// <summary>
        /// Call after external code sets <c>transform.rotation</c> (camp mouse yaw) so auto-fire / facing stay consistent.
        /// </summary>
        public void SyncPlanarFacingFromTransform()
        {
            var f = transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude > 0.0001f)
            {
                _lastMoveDir = f.normalized;
            }
        }

        /// <summary>
        /// Camp hub: strafe relative to view, camera drives yaw.
        /// </summary>
        public void ConfigureForCampHub()
        {
            moveRelativeToFacing = true;
            rotateFromMovementDirection = false;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public void SetProjectileTemplate(SimpleProjectile template)
        {
            _projectileTemplate = template;
        }

        public void BindRun(RunState runState, Action onDeath)
        {
            _runState = runState;
            _onDeath = onDeath;
        }

        public void BindStatsProvider(Func<ComputedCharacterStats> statsProvider)
        {
            _statsProvider = statsProvider;
        }

        private void Update()
        {
            var raw = DemoInput.ReadMoveXZ();
            Vector3 move;
            if (moveRelativeToFacing)
            {
                move = transform.right * raw.x + transform.forward * raw.z;
                if (move.sqrMagnitude > 1f)
                {
                    move.Normalize();
                }
                else if (move.sqrMagnitude > 0.0001f)
                {
                    move.Normalize();
                }
            }
            else
            {
                move = raw;
            }

            _controller.Move(move * (moveSpeed * Time.deltaTime));
            if (rotateFromMovementDirection && move.sqrMagnitude > 0.0001f)
            {
                _lastMoveDir = move.normalized;
                var targetRotation = Quaternion.LookRotation(_lastMoveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            if (Time.time >= _nextFire)
            {
                var attackSpeed = _statsProvider?.Invoke()?.attackSpeed ?? 1f;
                var effectiveInterval = fireInterval / Mathf.Max(0.2f, attackSpeed);
                _nextFire = Time.time + effectiveInterval;
                Fire();
            }
        }

        public void ApplyDamage(float normalizedAmount)
        {
            if (_runState == null)
            {
                return;
            }

            var armorMitigation = _statsProvider?.Invoke()?.armor ?? 0;
            var mitigated = Mathf.Max(0.01f, normalizedAmount - armorMitigation * 0.0008f);
            _runState.hpState = Mathf.Clamp01(_runState.hpState - mitigated);
            if (_runState.hpState <= 0.01f)
            {
                _onDeath?.Invoke();
            }
        }

        private void Fire()
        {
            if (_projectileTemplate == null)
            {
                return;
            }

            var origin = fireOrigin != null ? fireOrigin.position : transform.position + Vector3.up * 0.5f;
            var nearest = FindNearestEnemy(origin, 24f);
            var dir = nearest != null
                ? (nearest.position - origin).normalized
                : transform.forward;

            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = transform.forward;
            }

            var proj = Instantiate(_projectileTemplate, origin, Quaternion.LookRotation(dir));
            proj.gameObject.SetActive(true);
            var stats = _statsProvider?.Invoke();
            if (stats != null)
            {
                var isCrit = UnityEngine.Random.value < stats.critChance;
                var critMultiplier = isCrit ? 1.75f : 1f;
                var physical = stats.damage * 0.10f;
                var magical = stats.magicPower * 0.08f;
                proj.damage = Mathf.Max(1f, (physical + magical) * critMultiplier);
            }
            var rb = proj.GetComponent<Rigidbody>();
            rb.linearVelocity = dir * 18f;
        }

        private Transform FindNearestEnemy(Vector3 from, float maxDist)
        {
            var enemies = FindObjectsByType<SimpleEnemy>(FindObjectsInactive.Exclude);
            Transform best = null;
            var bestDist = maxDist * maxDist;
            foreach (var e in enemies)
            {
                if (e == null || e.IsDead)
                {
                    continue;
                }

                var d = e.transform.position - from;
                d.y = 0f;
                var sq = d.sqrMagnitude;
                if (sq < bestDist)
                {
                    bestDist = sq;
                    best = e.transform;
                }
            }

            return best;
        }
    }
}
