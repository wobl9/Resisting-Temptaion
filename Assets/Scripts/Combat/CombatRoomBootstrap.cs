using System.Collections.Generic;
using ShatteredForge.Core;
using ShatteredForge.Input;
using ShatteredForge.Run;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShatteredForge.Combat
{
    /// <summary>
    /// Spawns a simple arena, player, and enemies; notifies a room driver when room is cleared or player dies.
    /// </summary>
    /// ..
    public class CombatRoomBootstrap : MonoBehaviour
    {
        [SerializeField] private Vector3 arenaSize = new(22f, 1f, 22f);

        [Header("Camera (original static view)")]
        [SerializeField] [Range(40f, 70f)] private float cameraPitch = 55f;
        [SerializeField] private float cameraHeight = 22f;
        [SerializeField] private float cameraBackOffset = -16f;

        [Header("Flow")]
        [SerializeField] private bool autoAdvanceNonCombatRooms = true;
        [Header("Arena override")]
        [Tooltip("Optional custom arena root prefab. If set, default floor/walls are not spawned.")]
        [SerializeField] private GameObject overrideArenaPrefab;
        [Header("Enemy stats")]
        [Tooltip("If null, loads Resources/Items/DefaultEnemyStatProfileCatalog.")]
        [SerializeField] private EnemyStatProfileCatalog enemyStatCatalog;
        [Header("Debug")]
        [SerializeField] private bool showEnemyDebugOverlay = true;
        private ICombatRoomDriver _driver;

        private void Awake()
        {
            if (_driver == null)
            {
                _driver = GetComponent<ICombatRoomDriver>();
            }

            if (_driver == null)
            {
                var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (var i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is ICombatRoomDriver candidate)
                    {
                        _driver = candidate;
                        break;
                    }
                }
            }

            if (enemyStatCatalog == null)
            {
                enemyStatCatalog = Resources.Load<EnemyStatProfileCatalog>("Items/DefaultEnemyStatProfileCatalog");
            }
        }

        public void BindDriver(ICombatRoomDriver driver)
        {
            _driver = driver;
        }

        public void SetArenaOverride(GameObject arenaPrefab)
        {
            overrideArenaPrefab = arenaPrefab;
            if (_root != null)
            {
                ClearWorld();
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
        private string _combatProfileId = "grunt";
        private string _eliteProfileId = "elite";
        private string _bossProfileId = "boss";
        private int _regularEnemyCountOverride;
        private float _healthMultiplier = 1f;
        private float _damageMultiplier = 1f;
        private float _moveSpeedMultiplier = 1f;
        private float _attackSpeedMultiplier = 1f;

        public bool HasActiveCombatRoom => _spawnedCombatEnemies && _enemies.Count > 0;
        public bool WaitingForNonCombatConfirm => _nonCombatWaiting;

        private void LateUpdate()
        {
            if (_driver == null || !_driver.IsInRun)
            {
                ClearWorld();
                return;
            }

            if (!_roomStarted)
            {
                return;
            }

            if (_nonCombatWaiting && (autoAdvanceNonCombatRooms || DemoInput.KeyDown(Key.Space)))
            {
                _nonCombatWaiting = false;
                _roomStarted = false;
                _driver.ApplyCurrentRoomClearedFromGameplay();
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
            _driver.ApplyCurrentRoomClearedFromGameplay();
        }

        public void OnRunStartedOrRoomAdvanced()
        {
            if (_driver == null || !_driver.IsInRun)
            {
                return;
            }

            ClearEnemiesOnly();
            _nonCombatWaiting = false;
            _spawnedCombatEnemies = false;
            _roomStarted = true;

            if (!_driver.TryGetCurrentRoom(out var room))
            {
                _roomStarted = false;
                return;
            }

            EnsureWorld();
            EnsurePlayer(_driver.CurrentRun);
            ApplySpawnConfigForRoom(room);

            switch (room)
            {
                case RoomType.Combat:
                    _spawnedCombatEnemies = true;
                    SpawnGrunts(_regularEnemyCountOverride > 0 ? _regularEnemyCountOverride : 4, 3f, 2.3f, _combatProfileId);
                    break;
                case RoomType.Elite:
                    _spawnedCombatEnemies = true;
                    SpawnGrunts(2, 3f, 2.5f, _combatProfileId);
                    SpawnElite(18f, 2f, _eliteProfileId);
                    break;
                case RoomType.Boss:
                    _spawnedCombatEnemies = true;
                    SpawnBoss(32f, 1.35f, _bossProfileId);
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
                    SpawnGrunts(_regularEnemyCountOverride > 0 ? _regularEnemyCountOverride : 3, 3f, 2.2f, _combatProfileId);
                    break;
            }
        }

        private void ApplySpawnConfigForRoom(RoomType room)
        {
            _combatProfileId = "grunt";
            _eliteProfileId = "elite";
            _bossProfileId = "boss";
            _regularEnemyCountOverride = 0;
            _healthMultiplier = 1f;
            _damageMultiplier = 1f;
            _moveSpeedMultiplier = 1f;
            _attackSpeedMultiplier = 1f;

            if (_driver is not ICombatRoomSpawnConfigProvider provider ||
                !provider.TryGetSpawnConfig(room, out var cfg))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(cfg.combatProfileId)) _combatProfileId = cfg.combatProfileId.Trim();
            if (!string.IsNullOrWhiteSpace(cfg.eliteProfileId)) _eliteProfileId = cfg.eliteProfileId.Trim();
            if (!string.IsNullOrWhiteSpace(cfg.bossProfileId)) _bossProfileId = cfg.bossProfileId.Trim();
            _regularEnemyCountOverride = Mathf.Max(0, cfg.regularEnemyCountOverride);
            _healthMultiplier = Mathf.Max(0.1f, cfg.healthMultiplier);
            _damageMultiplier = Mathf.Max(0f, cfg.damageMultiplier);
            _moveSpeedMultiplier = Mathf.Max(0.1f, cfg.moveSpeedMultiplier);
            _attackSpeedMultiplier = Mathf.Max(0.1f, cfg.attackSpeedMultiplier);
        }

        private void OnGUI()
        {
            if (!_nonCombatWaiting || string.IsNullOrEmpty(_nonCombatHint))
            {
                DrawEnemyDebugOverlay();
                return;
            }

            GUI.Label(new Rect(20, 200, 800, 28), _nonCombatHint);
            DrawEnemyDebugOverlay();
        }

        private void DrawEnemyDebugOverlay()
        {
            if (!showEnemyDebugOverlay || _enemies.Count == 0 || Camera.main == null)
            {
                return;
            }

            for (var i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                var world = enemy.transform.position + Vector3.up * 2.35f;
                var screen = Camera.main.WorldToScreenPoint(world);
                if (screen.z <= 0f)
                {
                    continue;
                }

                var x = screen.x - 75f;
                var y = Screen.height - screen.y - 32f;
                var stats = enemy.CurrentStats;
                var dmg = stats?.damage ?? 0;
                var arm = stats?.armor ?? 0;
                var atkSpd = stats?.attackSpeed ?? 1f;
                var crit = (stats?.critChance ?? 0f) * 100f;
                var mana = stats?.mana ?? 0;
                var magicPower = stats?.magicPower ?? 0;
                var res = stats?.elementalResists;
                var fire = res?.fire ?? 0;
                var cold = res?.cold ?? 0;
                var lightning = res?.lightning ?? 0;

                var healthRatio = enemy.MaxHealth > 0.001f ? Mathf.Clamp01(enemy.CurrentHealth / enemy.MaxHealth) : 0f;
                var prevColor = GUI.color;
                GUI.color = healthRatio switch
                {
                    > 0.66f => new Color(0.6f, 1f, 0.6f),
                    > 0.33f => new Color(1f, 0.9f, 0.45f),
                    _ => new Color(1f, 0.45f, 0.45f)
                };

                var label = $"HP {enemy.CurrentHealth:0.0}/{enemy.MaxHealth:0.0}  DMG {dmg:0.##}  ARM {arm:0.##}  ASPD {atkSpd:0.00}  CRIT {crit:0.#}%";
                GUI.Label(new Rect(x - 30f, y - 10f, 420f, 24f), label);
                var line2 = $"MANA {mana}  M.PWR {magicPower}  RES F/C/L {fire}/{cold}/{lightning}";
                GUI.Label(new Rect(x - 30f, y + 8f, 420f, 24f), line2);
                GUI.color = prevColor;
            }
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

            if (overrideArenaPrefab != null)
            {
                var custom = Instantiate(overrideArenaPrefab, _root);
                custom.name = $"ArenaOverride_{overrideArenaPrefab.name}";
            }
            else
            {
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
            }

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

            if (UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).Length == 0)
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
                _player.BindRun(run, () => _driver?.ApplyPlayerDeathFromGameplay());
                _player.BindStatsProvider(() => _driver != null ? _driver.CurrentComputedStats : null);
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
            _player.BindRun(run, () => _driver?.ApplyPlayerDeathFromGameplay());
            _player.BindStatsProvider(() => _driver != null ? _driver.CurrentComputedStats : null);
        }

        private void SpawnGrunts(int count, float hp, float speed, string enemyProfileId = "grunt")
        {
            var profile = ResolveEnemyProfile(string.IsNullOrWhiteSpace(enemyProfileId) ? "grunt" : enemyProfileId.Trim(), hp, speed);
            for (var i = 0; i < count; i++)
            {
                var e = CreateEnemy($"Grunt_{i}", profile);
                var ring = Random.Range(0f, Mathf.PI * 2f);
                var dist = Random.Range(6f, 9f);
                e.transform.position = new Vector3(Mathf.Cos(ring) * dist, 0f, Mathf.Sin(ring) * dist);
            }
        }

        private void SpawnElite(float hp, float speed, string enemyProfileId = "elite")
        {
            var profile = ResolveEnemyProfile(string.IsNullOrWhiteSpace(enemyProfileId) ? "elite" : enemyProfileId.Trim(), hp, speed);
            var e = CreateEnemy("Elite", profile);
            e.transform.localScale = Vector3.one * 1.35f;
            e.transform.position = new Vector3(7f, 0f, 2f);
        }

        private void SpawnBoss(float hp, float speed, string enemyProfileId = "boss")
        {
            var profile = ResolveEnemyProfile(string.IsNullOrWhiteSpace(enemyProfileId) ? "boss" : enemyProfileId.Trim(), hp, speed);
            var e = CreateEnemy("Boss", profile);
            e.transform.localScale = Vector3.one * 2.2f;
            e.transform.position = new Vector3(8f, 0f, 0f);
        }

        private EnemyStatProfileCatalog.Entry ResolveEnemyProfile(string id, float fallbackHp, float fallbackSpeed)
        {
            var scaledFallbackHp = Mathf.Max(0.1f, fallbackHp * _healthMultiplier);
            var scaledFallbackSpeed = Mathf.Max(0.1f, fallbackSpeed * _moveSpeedMultiplier);
            if (enemyStatCatalog != null && enemyStatCatalog.TryGet(id, out var entry) && entry != null)
            {
                return new EnemyStatProfileCatalog.Entry
                {
                    id = entry.id,
                    health = Mathf.Max(0.1f, entry.health * _healthMultiplier),
                    moveSpeed = Mathf.Max(0.1f, entry.moveSpeed * _moveSpeedMultiplier),
                    contactDamage = Mathf.Max(0f, entry.contactDamage * _damageMultiplier),
                    contactCooldown = Mathf.Max(0.05f, entry.contactCooldown / _attackSpeedMultiplier),
                    primaryStats = entry.primaryStats,
                    flatBonuses = entry.flatBonuses
                };
            }

            return new EnemyStatProfileCatalog.Entry
            {
                id = id,
                health = scaledFallbackHp,
                moveSpeed = scaledFallbackSpeed,
                contactDamage = Mathf.Max(0f, 0.06f * _damageMultiplier),
                contactCooldown = Mathf.Max(0.05f, 0.9f / _attackSpeedMultiplier),
                primaryStats = CharacterPrimaryStats.CreateDefault(),
                flatBonuses = new FlatStatBonuses()
            };
        }

        private SimpleEnemy CreateEnemy(string name, EnemyStatProfileCatalog.Entry profile)
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
            enemy.Configure(
                profile.health,
                profile.moveSpeed,
                profile.contactDamage,
                profile.contactCooldown,
                profile.primaryStats,
                profile.flatBonuses);
            enemy.SetTarget(_player.transform);
            if (_driver != null)
            {
                var pid = profile.id;
                enemy.ConfigureKillLootReporter(p => _driver.NotifyEnemyKillLoot(p), pid);
            }

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
