using UnityEngine;

namespace ShatteredForge.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class SimpleProjectile : MonoBehaviour
    {
        public float damage = 1f;
        public float lifetime = 3f;

        private float _spawnTime;

        private void Awake()
        {
            var rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _spawnTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _spawnTime > lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var enemy = other.GetComponentInParent<SimpleEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
