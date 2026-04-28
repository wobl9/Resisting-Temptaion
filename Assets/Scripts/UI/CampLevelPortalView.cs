using System;
using System.Collections.Generic;
using ShatteredForge.Levels;
using UnityEngine;

namespace ShatteredForge.UI
{
    public sealed class CampLevelPortalView : MonoBehaviour
    {
        [Tooltip("If null, loads Resources/Levels/DefaultLevelCatalog.")]
        [SerializeField] private LevelCatalog levelCatalog;

        private Action<string> _onSelectLevel;
        private Action<LevelTierDefinition> _onQuickPlay;
        private Action _onStartProceduralFallback;
        private bool _isOpen;
        private string _title = "Портал уровней";

        public bool IsOpen => _isOpen;
        public LevelCatalog Catalog => levelCatalog;

        public void Configure(
            Action<string> onSelectLevel,
            Action<LevelTierDefinition> onQuickPlay,
            Action onStartProceduralFallback)
        {
            _onSelectLevel = onSelectLevel;
            _onQuickPlay = onQuickPlay;
            _onStartProceduralFallback = onStartProceduralFallback;
        }

        public void SetOpen(bool open)
        {
            _isOpen = open;
        }

        private void Awake()
        {
            if (levelCatalog == null)
            {
                levelCatalog = Resources.Load<LevelCatalog>("Levels/DefaultLevelCatalog");
            }
        }

        private void OnGUI()
        {
            if (!_isOpen)
            {
                return;
            }

            const float width = 740f;
            const float height = 560f;
            var x = (Screen.width - width) * 0.5f;
            var y = (Screen.height - height) * 0.5f;
            GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);

            GUILayout.Label(_title);
            GUILayout.Space(8f);

            if (levelCatalog == null)
            {
                GUILayout.Label("Каталог уровней не найден (Resources/Levels/DefaultLevelCatalog).");
                if (GUILayout.Button("Закрыть", GUILayout.Height(30f)))
                {
                    _isOpen = false;
                }

                GUILayout.EndArea();
                return;
            }

            DrawQuickPlay();
            GUILayout.Space(10f);
            DrawCards();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Старый процедурный акт", GUILayout.Height(30f)))
            {
                _onStartProceduralFallback?.Invoke();
            }

            if (GUILayout.Button("Закрыть", GUILayout.Height(30f)))
            {
                _isOpen = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawQuickPlay()
        {
            GUILayout.Label("Quick Play по тиру");
            var tiers = levelCatalog.tiers ?? new List<LevelTierDefinition>();
            if (tiers.Count == 0)
            {
                GUILayout.Label("Тиры не заданы в каталоге.");
                return;
            }

            tiers.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                return a.sortOrder.CompareTo(b.sortOrder);
            });

            GUILayout.BeginHorizontal();
            for (var i = 0; i < tiers.Count; i++)
            {
                var tier = tiers[i];
                if (tier == null)
                {
                    continue;
                }

                var prev = GUI.color;
                GUI.color = tier.uiColor;
                var label = string.IsNullOrWhiteSpace(tier.displayName) ? tier.name : tier.displayName;
                if (GUILayout.Button(label, GUILayout.Height(28f)))
                {
                    _onQuickPlay?.Invoke(tier);
                }
                GUI.color = prev;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawCards()
        {
            GUILayout.Label("Выбор уровня");
            var levels = levelCatalog.levels;
            if (levels == null || levels.Count == 0)
            {
                GUILayout.Label("В каталоге пока нет уровней.");
                return;
            }

            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                if (level == null)
                {
                    continue;
                }

                GUILayout.BeginVertical(GUI.skin.box);
                var tierName = level.tier != null
                    ? (string.IsNullOrWhiteSpace(level.tier.displayName) ? level.tier.name : level.tier.displayName)
                    : "No tier";
                GUILayout.Label($"{level.displayName}  |  {level.biome}  |  {tierName}");
                GUILayout.Label($"Рекоменд. сила: {level.recommendedPower}  |  Комнат: {level.minRooms}-{level.maxRooms}");
                GUILayout.Label($"Гарант. дропов: {level.guaranteedDropTemplateIds?.Count ?? 0}  |  Рандом-роллов: {level.randomDropRolls}");

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Войти", GUILayout.Width(150f), GUILayout.Height(28f)))
                {
                    _onSelectLevel?.Invoke(level.levelId);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }
    }
}
