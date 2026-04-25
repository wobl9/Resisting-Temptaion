using System.Collections.Generic;
using ShatteredForge.Core;
using ShatteredForge.Input;
using ShatteredForge.Prototype;
using ShatteredForge.Run;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShatteredForge.Combat
{
    /// <summary>
    /// Spawns a simple arena, player, and enemies; notifies <see cref="PlayableLoopDemo"/> when a room is cleared or the player dies.
    /// </summary>
    /// ..
    public class CombatRoomBootstrap : MonoBehaviour
    {
        [SerializeField] private PlayableLoopDemo loopDemo;
        [SerializeField] private Vector3 arenaSize = new(22f, 1f, 22f);

        [Header("Camera (original static view)")]
        [SerializeField] [Range(40f, 70f)] private float cameraPitch = 55f;
        [SerializeField] private float cameraHeight = 22f;
        [SerializeField] private float cameraBackOffset = -16f;

        private void Awake()
        {
            if (loopDemo == null)
            {
                loopDemo = GetComponent<PlayableLoopDemo>();
            }

            if (loopDemo == null)
            {
                loopDemo = FindFirstObjectByType<PlayableLoopDemo>();
            }
        }

        private Transform _root;
        private GameObject _floor;
        private SimplePlayerController _player;
        private SimpleProjectile _projectileTemplate;
        private readonly List<SimpleEnemy> _enemies = new();
        private bool _nonCombatWaiting;
        private string _nonCombatHint = string.Empty;
        private bool _roomStarted;
        private bool _spawnedCombatEnemies;

        public bool HasActiveCombatRoom => _spawnedCombatEnemies && _enemies.Count > 0;
        public bool WaitingForNonCombatConfirm => _nonCombatWaiting;

        private void LateUpdate()
        {
            if (loopDemo == null || !loopDemo.IsInRun)
            {
                ClearWorld();
                return;
            }

            if (!_roomStarted)
            {
                return;
            }

            if (_nonCombatWaiting && DemoInput.KeyDown(Key.Space))
            {
                _nonCombatWaiting = false;
                _roomStarted = false;
                loopDemo.ApplyCurrentRoomClearedFromGameplay();
                return;
            }

            if (_nonCombatWaiting)
            {
                return;
            }

            if (!_spawnedCombatEnemies)
            {
                return;
            }

            _enemies.RemoveAll(e => e == null || e.IsDead);
            if (_enemies.Count != 0)
            {
                return;
            }

            _spawnedCombatEnemies = false;
            _roomStarted = false;
            loopDemo.ApplyCurrentRoomClearedFromGameplay();
        }

        public void OnRunStartedOrRoomAdvanced()
        {
            if (loopDemo == null || !loopDemo.IsInRun)
            {
                return;
            }

            ClearEnemiesOnly();
            _nonCombatWaiting = false;
            _spawnedCombatEnemies = false;
            _roomStarted = true;

            if (!loopDemo.TryGetCurrentRoom(out var room))
            {
                _roomStarted = false;
                return;
            }

            EnsureWorld();
            EnsurePlayer(loopDemo.CurrentRun);

            switch (room)
            {
                case RoomType.Combat:
                    _spawnedCombatEnemies = true;
                    SpawnGrunts(4, 3f, 2.3f);
                    break;
                case RoomType.Elite:
                    _spawnedCombatEnemies = true;
                    SpawnGrunts(2, 3f, 2.5f);
                    SpawnElite(18f, 2f);
                    break;
                case RoomType.Boss:
                    _spawnedCombatEnemies = true;
                    SpawnBoss(32f, 1.35f);
                    break;
                case RoomType.Rest:
                    _nonCombatWaiting = true;
                    _nonCombatHint = "Rest site — press Space to continue.";
                    break;
                case RoomType.Shop:
                case RoomType.Forge:
                case RoomType.Event:
                    _nonCombatWaiting = true;
                    _nonCombatHint = $"{room} — press Space to continue.";
                    break;
                default:
                    _spawnedCombatEnemies = true;
                    SpawnGrunts(3, 3f, 2.2f);
                    break;
            }
        }

        private void OnGUI()
        {
            if (!_nonCombatWaiting || string.IsNullOrEmpty(_nonCombatHint))
            {
                return;
            }

            GUI.Label(new Rect(20, 200, 800, 28), _nonCombatHint);
        }

        private void EnsureWorld()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("CombatWorld").transform;
            _root.SetParent(transform, false);

            ConfigureMainCameraForArena();

            _floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _floor.name = "Floor";
            _floor.transform.SetParent(_root, false);
            _floor.transform.localScale = arenaSize;
            _floor.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            Object.Destroy(_floor.GetComponent<Collider>());

            var wallT = arenaSize.x * 0.5f + 0.5f;
            CreateWall("WallN", new Vector3(0f, 1f, wallT), new Vector3(arenaSize.x + 2f, 2f, 1f));
            CreateWall("WallS", new Vector3(0f, 1f, -wallT), new Vector3(arenaSize.x + 2f, 2f, 1f));
            CreateWall("WallE", new Vector3(wallT, 1f, 0f), new Vector3(1f, 2f, arenaSize.z + 2f));
            CreateWall("WallW", new Vector3(-wallT, 1f, 0f), new Vector3(1f, 2f, arenaSize.z + 2f));

            BuildProjectileTemplate();
        }

        /// <summary>
        /// Original static camera from first implementation.
        /// </summary>
        private void ConfigureMainCameraForArena()
        {
            if (Camera.main == null && GameObject.FindGameObjectWithTag("MainCamera") == null)
            {
                var camGo = new GameObject("Main Camera");
                var cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
                cam.transform.SetPositionAndRotation(
                    new Vector3(0f, cameraHeight, cameraBackOffset),
                    Quaternion.Euler(cameraPitch, 0f, 0f));
            }

            if (UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length == 0)
            {
                var sun = new GameObject("Directional Light");
                var light = sun.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.05f;
                sun.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
            }
        }

        private void CreateWall(string name, Vector3 pos, Vector3 scale)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            w.transform.SetParent(_root, false);
            w.transform.localPosition = pos;
            w.transform.localScale = scale;
        }

        private void BuildProjectileTemplate()
        {
            if (_projectileTemplate != null)
            {
                return;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ProjectileTemplate";
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            Object.Destroy(go.GetComponent<Collider>());
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.35f;
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _projectileTemplate = go.AddComponent<SimpleProjectile>();
            _projectileTemplate.damage = 1f;
            go.transform.localScale = Vector3.one * 0.35f;
        }

        private void EnsurePlayer(RunState run)
        {
            if (_player != null)
            {
                _player.BindRun(run, () => loopDemo.ApplyPlayerDeathFromGameplay());
                return;
            }

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Player";
            body.transform.SetParent(_root, false);
            body.transform.position = new Vector3(-6f, 0f, 0f);
            Object.Destroy(body.GetComponent<Collider>());
            var cc = body.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0f, 1f, 0f);

            _player = body.AddComponent<SimplePlayerController>();
            _player.SetProjectileTemplate(_projectileTemplate);
            _player.BindRun(run, () => loopDemo.ApplyPlayerDeathFromGameplay());
        }

        private void SpawnGrunts(int count, float hp, float speed)
        {
            for (var i = 0; i < count; i++)
            {
                var e = CreateEnemy($"Grunt_{i}", hp, speed);
                var ring = Random.Range(0f, Mathf.PI * 2f);
                var dist = Random.Range(6f, 9f);
                e.transform.position = new Vector3(Mathf.Cos(ring) * dist, 0f, Mathf.Sin(ring) * dist);
            }
        }

        private void SpawnElite(float hp, float speed)
        {
            var e = CreateEnemy("Elite", hp, speed);
            e.transform.localScale = Vector3.one * 1.35f;
            e.transform.position = new Vector3(7f, 0f, 2f);
        }

        private void SpawnBoss(float hp, float speed)
        {
            var e = CreateEnemy("Boss", hp, speed);
            e.transform.localScale = Vector3.one * 2.2f;
            e.transform.position = new Vector3(8f, 0f, 0f);
        }

        private SimpleEnemy CreateEnemy(string name, float hp, float speed)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(_root, false);
            var col = go.GetComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.5f;
            col.center = new Vector3(0f, 1f, 0f);

            var rend = go.GetComponent<Renderer>();
            if (rend != null && name.Contains("Boss"))
            {
                rend.material.color = new Color(0.55f, 0.1f, 0.15f);
            }
            else if (rend != null && name.Contains("Elite"))
            {
                rend.material.color = new Color(0.35f, 0.1f, 0.55f);
            }
            else if (rend != null)
            {
                rend.material.color = new Color(0.15f, 0.35f, 0.2f);
            }

            var enemy = go.AddComponent<SimpleEnemy>();
            enemy.Configure(hp, speed);
            enemy.SetTarget(_player.transform);
            _enemies.Add(enemy);
            return enemy;
        }

        private void ClearEnemiesOnly()
        {
            foreach (var e in _enemies)
            {
                if (e != null)
                {
                    Destroy(e.gameObject);
                }
            }

            _enemies.Clear();
        }

        private void ClearWorld()
        {
            ClearEnemiesOnly();
            _nonCombatWaiting = false;
            _roomStarted = false;
            _spawnedCombatEnemies = false;
            if (_player != null)
            {
                Destroy(_player.gameObject);
                _player = null;
            }

            if (_root != null)
            {
                Destroy(_root.gameObject);
                _root = null;
            }

            _floor = null;
        }
    }
}
