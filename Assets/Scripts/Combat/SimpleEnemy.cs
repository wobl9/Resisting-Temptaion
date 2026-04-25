using UnityEngine;

namespace ShatteredForge.Combat
{
    public class SimpleEnemy : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float maxHealth = 3f;
        [SerializeField] private float contactDamage = 0.06f;
        [SerializeField] private float contactCooldown = 0.9f;

        private Transform _target;
        private float _health;
        private float _nextDamageTime;

        public bool IsDead { get; private set; }

        private void Awake()
        {
            _health = maxHealth;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void Configure(float health, float speed)
        {
            maxHealth = health;
            moveSpeed = speed;
            _health = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead)
            {
                return;
            }

            _health -= amount;
            if (_health <= 0f)
            {
                IsDead = true;
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (IsDead || _target == null)
            {
                return;
            }

            var dir = (_target.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.position += dir.normalized * (moveSpeed * Time.deltaTime);
            }

            var player = _target.GetComponent<SimplePlayerController>();
            if (player == null)
            {
                return;
            }

            var flat = _target.position - transform.position;
            flat.y = 0f;
            if (flat.magnitude > 1.15f || Time.time < _nextDamageTime)
            {
                return;
            }

            _nextDamageTime = Time.time + contactCooldown;
            player.ApplyDamage(contactDamage);
        }
    }
}
