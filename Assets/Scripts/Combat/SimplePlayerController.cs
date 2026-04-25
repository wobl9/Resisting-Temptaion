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

        private CharacterController _controller;
        private SimpleProjectile _projectileTemplate;
        private RunState _runState;
        private Action _onDeath;
        private float _nextFire;
        private Vector3 _lastMoveDir = Vector3.forward;

        public Vector3 PlanarFacingDirection => _lastMoveDir;

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

        private void Update()
        {
            var move = DemoInput.ReadMoveXZ();
            _controller.Move(move * (moveSpeed * Time.deltaTime));
            if (move.sqrMagnitude > 0.0001f)
            {
                _lastMoveDir = move.normalized;
                var targetRotation = Quaternion.LookRotation(_lastMoveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            if (Time.time >= _nextFire)
            {
                _nextFire = Time.time + fireInterval;
                Fire();
            }
        }

        public void ApplyDamage(float normalizedAmount)
        {
            if (_runState == null)
            {
                return;
            }

            _runState.hpState = Mathf.Clamp01(_runState.hpState - normalizedAmount);
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
            var rb = proj.GetComponent<Rigidbody>();
            rb.linearVelocity = dir * 18f;
        }

        private Transform FindNearestEnemy(Vector3 from, float maxDist)
        {
            var enemies = FindObjectsByType<SimpleEnemy>(FindObjectsSortMode.None);
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
