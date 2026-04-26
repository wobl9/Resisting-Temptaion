using ShatteredForge.Core;
using UnityEngine;

namespace ShatteredForge.Combat
{
    public class SimpleEnemy : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float maxHealth = 3f;
        [SerializeField] private float contactDamage = 0.06f;
        [SerializeField] private float contactCooldown = 0.9f;
        [SerializeField] private CharacterPrimaryStats primaryStats = new()
        {
            strength = 8,
            agility = 8,
            vitality = 8,
            intellect = 6
        };
        [SerializeField] private FlatStatBonuses statBonuses = new();

        private Transform _target;
        private float _health;
        private float _nextDamageTime;
        private ComputedCharacterStats _computedStats;

        public bool IsDead { get; private set; }
        public float CurrentHealth => _health;
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            RebuildStats();
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void Configure(float health, float speed)
        {
            maxHealth = health;
            moveSpeed = speed;
            RebuildStats();
        }

        public void Configure(float health, float speed, CharacterPrimaryStats stats, FlatStatBonuses bonuses)
        {
            maxHealth = health;
            moveSpeed = speed;
            primaryStats = stats ?? CharacterPrimaryStats.CreateDefault();
            statBonuses = bonuses ?? new FlatStatBonuses();
            RebuildStats();
        }

        public void Configure(
            float health,
            float speed,
            float baseContactDamage,
            float baseContactCooldown,
            CharacterPrimaryStats stats,
            FlatStatBonuses bonuses)
        {
            maxHealth = health;
            moveSpeed = speed;
            contactDamage = Mathf.Max(0f, baseContactDamage);
            contactCooldown = Mathf.Max(0.05f, baseContactCooldown);
            primaryStats = stats ?? CharacterPrimaryStats.CreateDefault();
            statBonuses = bonuses ?? new FlatStatBonuses();
            RebuildStats();
        }

        public ComputedCharacterStats CurrentStats => _computedStats;

        private void RebuildStats()
        {
            _computedStats = CharacterStatsService.BuildComputed(primaryStats, statBonuses);
            _health = maxHealth + (_computedStats.armor * 0.35f);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead)
            {
                return;
            }

            var armorMitigation = _computedStats != null ? _computedStats.armor * 0.05f : 0f;
            var mitigatedDamage = Mathf.Max(0.1f, amount - armorMitigation);
            _health -= mitigatedDamage;
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
            var statDrivenDamage = _computedStats != null ? _computedStats.damage * 0.002f : 0f;
            player.ApplyDamage(contactDamage + statDrivenDamage);
        }
    }
}
