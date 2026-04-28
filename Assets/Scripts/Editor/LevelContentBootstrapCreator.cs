using System.Collections.Generic;
using ShatteredForge.Items;
using ShatteredForge.Levels;
using UnityEditor;
using UnityEngine;

namespace ShatteredForge.EditorTools
{
    public static class LevelContentBootstrapCreator
    {
        private const string RootPath = "Assets/Resources/Levels";
        private const string TiersPath = RootPath + "/Tiers";
        private const string PoolsPath = RootPath + "/Pools";
        private const string DefinitionsPath = RootPath + "/Definitions";
        private const string LootPath = RootPath + "/Loot";
        private const string CatalogPath = RootPath + "/DefaultLevelCatalog.asset";

        [MenuItem("ShatteredForge/Levels/Bootstrap Demo Level Content", priority = 33)]
        public static void BootstrapDemoContent()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(RootPath);
            EnsureFolder(TiersPath);
            EnsureFolder(PoolsPath);
            EnsureFolder(DefinitionsPath);
            EnsureFolder(LootPath);

            var easy = CreateTier("Tier_Easy", "easy", "Easy", new Color(0.6f, 1f, 0.6f), 0, 0.8f, 0.8f, 0.9f, 1.0f, 4, 0, 0);
            var medium = CreateTier("Tier_Medium", "medium", "Medium", Color.white, 10, 1f, 1f, 1f, 1f, 4, 0, 0);
            var hard = CreateTier("Tier_Hard", "hard", "Hard", new Color(1f, 0.85f, 0.5f), 20, 1.4f, 1.3f, 1.1f, 1.05f, 5, 1, 1);
            var nightmare = CreateTier("Tier_Nightmare", "nightmare", "Nightmare", new Color(1f, 0.5f, 0.5f), 30, 2f, 1.7f, 1.2f, 1.1f, 6, 2, 2);

            var emberPool = CreatePool("EnemyPool_EmberMines", new[]
            {
                Entry("grunt", 10, false, false, "ember_mines"),
                Entry("elite", 5, true, false, "ember_mines"),
                Entry("boss", 1, false, true, "ember_mines"),
            });
            var keepPool = CreatePool("EnemyPool_RuinedKeep", new[]
            {
                Entry("grunt", 10, false, false, "ruined_keep"),
                Entry("elite", 4, true, false, "ruined_keep"),
                Entry("boss", 1, false, true, "ruined_keep"),
            });

            var loot = CreateLootTable("LootTable_DefaultBoss");
            EnsureLootRow(loot, "weapon_simple_sword", 2, 1, 1);
            EnsureLootRow(loot, "armor_simple_chest", 2, 1, 1);
            EditorUtility.SetDirty(loot);

            var lvlEasy = CreateLevel("EmberMines_Easy", "level_ember_easy", "Ember Mines - Easy", "Ember Mines", easy, emberPool, emberPool, loot, 3, 5, 4, 0, 10, "weapon_simple_sword");
            var lvlMedium = CreateLevel("EmberMines_Medium", "level_ember_medium", "Ember Mines - Medium", "Ember Mines", medium, emberPool, emberPool, loot, 4, 6, 4, 1, 20, "armor_simple_chest");
            var lvlHard = CreateLevel("RuinedKeep_Hard", "level_keep_hard", "Ruined Keep - Hard", "Ruined Keep", hard, keepPool, keepPool, loot, 5, 7, 5, 1, 35, "weapon_simple_sword");

            var catalog = LoadOrCreateAsset<LevelCatalog>(CatalogPath, () => ScriptableObject.CreateInstance<LevelCatalog>());
            catalog.tiers = new List<LevelTierDefinition> { easy, medium, hard, nightmare };
            catalog.levels = new List<LevelDefinition> { lvlEasy, lvlMedium, lvlHard };
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(catalog);
            Debug.Log($"{nameof(LevelContentBootstrapCreator)}: demo level content bootstrapped at {RootPath}");
        }

        private static LevelTierDefinition CreateTier(
            string assetName,
            string tierId,
            string displayName,
            Color uiColor,
            int sortOrder,
            float hp,
            float dmg,
            float move,
            float atkSpeed,
            int regularOverride,
            int extraElite,
            int extraRolls)
        {
            var path = $"{TiersPath}/{assetName}.asset";
            var tier = LoadOrCreateAsset(path, () => ScriptableObject.CreateInstance<LevelTierDefinition>());
            tier.tierId = tierId;
            tier.displayName = displayName;
            tier.uiColor = uiColor;
            tier.sortOrder = sortOrder;
            tier.enemyHealthMultiplier = hp;
            tier.enemyDamageMultiplier = dmg;
            tier.enemyMoveSpeedMultiplier = move;
            tier.enemyAttackSpeedMultiplier = atkSpeed;
            tier.regularEnemyCountOverride = regularOverride;
            tier.extraEliteSlots = extraElite;
            tier.extraRandomLootRolls = extraRolls;
            EditorUtility.SetDirty(tier);
            return tier;
        }

        private static EnemyPoolDefinition CreatePool(string assetName, EnemyPoolDefinition.Entry[] entries)
        {
            var path = $"{PoolsPath}/{assetName}.asset";
            var pool = LoadOrCreateAsset(path, () => ScriptableObject.CreateInstance<EnemyPoolDefinition>());
            pool.entries = new List<EnemyPoolDefinition.Entry>(entries);
            EditorUtility.SetDirty(pool);
            return pool;
        }

        private static LevelDefinition CreateLevel(
            string assetName,
            string levelId,
            string displayName,
            string biome,
            LevelTierDefinition tier,
            EnemyPoolDefinition regularPool,
            EnemyPoolDefinition bossPool,
            LootTableDefinition lootTable,
            int minRooms,
            int maxRooms,
            int regularEnemiesPerRoom,
            int eliteRoomCount,
            int recommendedPower,
            string guaranteedDropTemplateId)
        {
            var path = $"{DefinitionsPath}/{assetName}.asset";
            var level = LoadOrCreateAsset(path, () => ScriptableObject.CreateInstance<LevelDefinition>());
            level.levelId = levelId;
            level.displayName = displayName;
            level.biome = biome;
            level.tier = tier;
            level.minRooms = minRooms;
            level.maxRooms = maxRooms;
            level.regularEnemiesPerRoom = regularEnemiesPerRoom;
            level.eliteRoomCount = eliteRoomCount;
            level.regularPool = regularPool;
            level.bossPool = bossPool;
            level.requiredTags = new List<string> { biome == "Ruined Keep" ? "ruined_keep" : "ember_mines" };
            level.randomLootTable = lootTable;
            level.randomDropRolls = 2;
            level.recommendedPower = recommendedPower;
            level.guaranteedDropTemplateIds = new List<string> { guaranteedDropTemplateId };
            EditorUtility.SetDirty(level);
            return level;
        }

        private static LootTableDefinition CreateLootTable(string assetName)
        {
            var path = $"{LootPath}/{assetName}.asset";
            return LoadOrCreateAsset(path, () => ScriptableObject.CreateInstance<LootTableDefinition>());
        }

        private static void EnsureLootRow(LootTableDefinition table, string templateId, int weight, int min, int max)
        {
            table.roomClearRows ??= new List<LootTableDefinition.RoomRow>();
            for (var i = 0; i < table.roomClearRows.Count; i++)
            {
                if (table.roomClearRows[i] != null && table.roomClearRows[i].templateId == templateId)
                {
                    return;
                }
            }

            table.roomClearRows.Add(new LootTableDefinition.RoomRow
            {
                templateId = templateId,
                weight = weight,
                minCount = min,
                maxCount = max,
                appliesToRoomTypes = new List<ShatteredForge.Run.RoomType> { ShatteredForge.Run.RoomType.Boss }
            });
        }

        private static EnemyPoolDefinition.Entry Entry(string id, int weight, bool elite, bool boss, params string[] tags)
        {
            return new EnemyPoolDefinition.Entry
            {
                enemyProfileId = id,
                weight = weight,
                isElite = elite,
                isBoss = boss,
                tags = tags != null ? new List<string>(tags) : new List<string>()
            };
        }

        private static T LoadOrCreateAsset<T>(string path, System.Func<T> create) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var created = create();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
