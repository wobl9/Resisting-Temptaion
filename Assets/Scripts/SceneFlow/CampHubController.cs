using System;
using ShatteredForge.Combat;
using ShatteredForge.Core;
using ShatteredForge.Input;
using ShatteredForge.Items;
using ShatteredForge.Menu;
using ShatteredForge.Progression;
using ShatteredForge.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// Expedition camp (RU: «лагерь», scene <see cref="SceneNames.CampHub"/>): profile/account, placeholders, dungeon entry.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class CampHubController : MonoBehaviour
    {
        private enum NearKind
        {
            None,
            Shop,
            Forge,
            Alchemy,
            Dungeon
        }

        [Header("Scene routing")]
        [SerializeField] private string gameplaySceneName = "GameplayScene";

        [Header("Profile storage")]
        [SerializeField] private ProfileStorageMode profileStorageMode = ProfileStorageMode.Local;
        [SerializeField] private string remoteProfileStorageBaseUrl = "";
        [SerializeField] private string remoteProfileStorageAuthBearer = "";

        [Header("Interaction")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private Transform shopAnchor;
        [SerializeField] private Transform forgeAnchor;
        [SerializeField] private Transform alchemyAnchor;
        [SerializeField] private Transform dungeonAnchor;
        [SerializeField] [Min(0.5f)] private float interactRadius = 2.2f;

        [Header("Items")]
        [Tooltip("If null, loads Resources/Items/DefaultItemCatalog.")]
        [SerializeField] private ItemCatalog itemCatalog;

        [Tooltip("If null, loads Resources/Items/DefaultVendorCatalog.")]
        [SerializeField] private VendorCatalog vendorCatalog;

        [Tooltip("If null, loads Resources/Items/DefaultCraftingRecipeCatalog.")]
        [SerializeField] private CraftingRecipeCatalog craftingRecipeCatalog;

        private IProfileStorage _profilesService;
        private string _profileId;
        private ProfileData _profile;
        private AccountState _account;

        private NearKind _near;
        private string _status = string.Empty;
        private CampHubEconomyDraw.EconomyPanelKind _economyPanel = CampHubEconomyDraw.EconomyPanelKind.None;
        private VendorCatalog _vendorCatalogRuntime;
        private CraftingRecipeCatalog _craftingRecipeCatalogRuntime;
        private PlayerInventoryPanel _inventoryPanel;
        private CampCharacterSheetPanel _characterSheet;
        private CampPauseMenuView _pauseMenuView;
        private bool _pauseMenuOpen;
        private bool _pauseSettingsOpen;
        private float _savedTimeScale = 1f;
        private float _masterVolume = 1f;
        private bool _fullscreen = true;
        private Resolution[] _resolutions;
        private int _resolutionIndex;

        private void Awake()
        {
            ItemCatalogRuntime.Current = itemCatalog != null
                ? itemCatalog
                : Resources.Load<ItemCatalog>("Items/DefaultItemCatalog");
            ItemStatBonusCatalogRuntime.Current = Resources.Load<ItemStatBonusCatalog>("Items/DefaultItemStatBonusCatalog");
            if (ItemCatalogRuntime.Current == null)
            {
                Debug.LogWarning(
                    $"{nameof(CampHubController)}: ItemCatalog not assigned and Resources.Load(\"Items/DefaultItemCatalog\") failed. Item names / camp slots may be limited.");
            }

            _vendorCatalogRuntime = vendorCatalog != null
                ? vendorCatalog
                : Resources.Load<VendorCatalog>("Items/DefaultVendorCatalog");
            var loadedRecipes = craftingRecipeCatalog != null
                ? craftingRecipeCatalog
                : Resources.Load<CraftingRecipeCatalog>("Items/DefaultCraftingRecipeCatalog");
            if (loadedRecipes != null && loadedRecipes.recipes != null && loadedRecipes.recipes.Count > 0)
            {
                _craftingRecipeCatalogRuntime = loadedRecipes;
            }
            else
            {
                if (loadedRecipes == null)
                {
                    Debug.LogWarning(
                        $"{nameof(CampHubController)}: CraftingRecipeCatalog not in Resources and not assigned; using baked default recipes.");
                }
                else
                {
                    Debug.LogWarning(
                        $"{nameof(CampHubController)}: CraftingRecipeCatalog has no recipes (check asset); using baked default recipes.");
                }

                _craftingRecipeCatalogRuntime = CraftingRecipeCatalog.CreateWithDefaultRecipes();
            }

            _profilesService = ProfileStorageFactory.Create(
                profileStorageMode,
                remoteProfileStorageBaseUrl,
                remoteProfileStorageAuthBearer);

            _profileId = PlayerPrefs.GetString(MenuSessionPrefs.ActiveProfileIdKey, string.Empty);
            if (string.IsNullOrEmpty(_profileId) || !_profilesService.TryLoadProfile(_profileId, out _profile))
            {
                _profile = null;
                _profileId = string.Empty;
                _account = BuildInitialAccount();
                CharacterPaperDoll.EnsureList(_account);
                CharacterStatsService.RecalculateForCamp(_account);
                _status = "Лагерь (демо, без профиля)";
            }
            else
            {
                _account = LoadOrCreateAccount(_profile);
                CharacterPaperDoll.EnsureList(_account);
                CharacterStatsService.RecalculateForCamp(_account);
                SyncLegacyResourcesFromProfile(_profile, _account);
                ProfileAccountGoldMigration.ApplyMissingGoldFieldOnce(_profile, _account, PersistAccountIfNeeded);
                _status = string.IsNullOrEmpty(_profile.profileName)
                    ? "Лагерь"
                    : $"Лагерь | {_profile.profileName}";
            }

            // Before other scripts' Awake (e.g. SimplePlayerController): ensure CC + mesh exist.
            EnsureCampAvatar();
            CampHubLandmarkDresser.Dress(shopAnchor, forgeAnchor, alchemyAnchor, dungeonAnchor);
            CampHubAvatarPrototypeVisuals.EnsureOn(playerBody != null ? playerBody.gameObject : null);

            // Stale "resume expedition" from a prior session/editor run must not hijack GameplayScene Awake
            // (otherwise PlayableLoopDemo restores an old run instead of honouring dungeon entry from camp).
            MenuSessionWriter.ClearResumeIntent();

            _inventoryPanel = GetComponent<PlayerInventoryPanel>();
            if (_inventoryPanel == null)
            {
                _inventoryPanel = gameObject.AddComponent<PlayerInventoryPanel>();
            }

            _inventoryPanel.BindCamp(
                _account,
                PersistAccountIfNeeded,
                _ => RefreshCampLookCapture());

            _characterSheet = GetComponent<CampCharacterSheetPanel>();
            if (_characterSheet == null)
            {
                _characterSheet = gameObject.AddComponent<CampCharacterSheetPanel>();
            }

            _characterSheet.Bind(_account, PersistAccountIfNeeded, RefreshCampLookCapture);

            _masterVolume = AudioListener.volume;
            _fullscreen = Screen.fullScreen;
            _resolutions = Screen.resolutions;
            _resolutionIndex = FindCurrentResolutionIndex();
            EnsurePauseMenuView();
        }

        private void RefreshCampLookCapture()
        {
            var rig = FindAnyObjectByType<CampHubCameraRig>();
            if (rig == null)
            {
                return;
            }

            var suppressed = (_characterSheet != null && _characterSheet.IsOpen) ||
                             (_inventoryPanel != null && _inventoryPanel.IsOpen) ||
                             _economyPanel != CampHubEconomyDraw.EconomyPanelKind.None ||
                             _pauseMenuOpen;
            rig.SetLookFromUiSuppressed(suppressed);
        }

        private void Update()
        {
            UpdateNearKind();
            if (_pauseMenuOpen)
            {
                if (DemoInput.KeyDown(Key.Escape))
                {
                    SetPauseMenuOpen(false);
                }
                return;
            }

            if (_characterSheet != null && _characterSheet.IsOpen && DemoInput.KeyDown(Key.Escape))
            {
                _characterSheet.SetOpen(false);
                return;
            }

            if (_inventoryPanel != null && _inventoryPanel.IsOpen && DemoInput.KeyDown(Key.Escape))
            {
                _inventoryPanel.SetOpen(false);
                return;
            }

            if (DemoInput.KeyDown(Key.Escape))
            {
                if (_economyPanel != CampHubEconomyDraw.EconomyPanelKind.None)
                {
                    SetEconomyPanel(CampHubEconomyDraw.EconomyPanelKind.None);
                }
                else
                {
                    SetPauseMenuOpen(true);
                }
                return;
            }

            if (DemoInput.KeyDown(Key.E))
            {
                TryInteract();
            }

            if (DemoInput.KeyDown(Key.C))
            {
                _characterSheet?.ToggleStats();
                return;
            }

            if (DemoInput.KeyDown(Key.B))
            {
                _characterSheet?.ToggleInventory();
                return;
            }
        }

        private void OnGUI()
        {
            const int pad = 12;
            GUI.Label(new Rect(pad, pad, Screen.width - pad * 2, 24), _status);

            var hintY = pad + 28;
            if (_near != NearKind.None)
            {
                var label = _near switch
                {
                    NearKind.Shop => "Торговец — E",
                    NearKind.Forge => "Кузница — E",
                    NearKind.Alchemy => "Алхимия — E",
                    NearKind.Dungeon => "Dungeon — E to enter",
                    _ => string.Empty
                };
                GUI.Label(new Rect(pad, hintY, Screen.width - pad * 2, 24), label);
                hintY += 26;
            }
            else
            {
                GUI.Label(new Rect(pad, hintY, Screen.width - pad * 2, 24), "WASD / стрелки — движение | Мышь — обзор (герой поворачивается) | C — слоты и снабжение | Tab — снабжение | Esc — курсор | Подойти к меткам | E — действие");
                hintY += 26;
            }

            if (_economyPanel != CampHubEconomyDraw.EconomyPanelKind.None)
            {
                if (CampHubEconomyDraw.DrawEconomyPanel(
                        _economyPanel,
                        _account,
                        ItemCatalogRuntime.Current,
                        _vendorCatalogRuntime,
                        _craftingRecipeCatalogRuntime,
                        PersistAccountIfNeeded))
                {
                    SetEconomyPanel(CampHubEconomyDraw.EconomyPanelKind.None);
                }
            }

        }

        /// <summary>
        /// Guarantees a visible capsule + <see cref="CharacterController"/> + <see cref="SimplePlayerController"/> (same stack as dungeon).
        /// </summary>
        private void EnsureCampAvatar()
        {
            if (playerBody == null)
            {
                var found = FindAnyObjectByType<SimplePlayerController>();
                if (found != null)
                {
                    playerBody = found.transform;
                }
            }

            if (playerBody == null)
            {
                Debug.LogWarning($"{nameof(CampHubController)}: spawning default camp avatar (no Player in scene).");
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Player";
                go.tag = "Player";
                go.transform.SetPositionAndRotation(new Vector3(0f, 0.12f, 0f), Quaternion.identity);
                Object.Destroy(go.GetComponent<CapsuleCollider>());
                var cc = go.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.4f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                go.AddComponent<SimplePlayerController>();
                playerBody = go.transform;
                go.GetComponent<SimplePlayerController>()?.ConfigureForCampHub();
                return;
            }

            var root = playerBody.gameObject;
            var ccExisting = root.GetComponent<CharacterController>();
            if (ccExisting == null)
            {
                ccExisting = root.AddComponent<CharacterController>();
                ccExisting.height = 1.8f;
                ccExisting.radius = 0.4f;
                ccExisting.center = new Vector3(0f, 0.9f, 0f);
            }

            if (root.GetComponent<SimplePlayerController>() == null)
            {
                root.AddComponent<SimplePlayerController>();
            }

            var pos = root.transform.position;
            if (pos.y < 0.08f)
            {
                root.transform.position = new Vector3(pos.x, 0.12f, pos.z);
            }

            if (root.GetComponentInChildren<MeshRenderer>(true) == null)
            {
                var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                vis.name = "AvatarVisual";
                vis.transform.SetParent(root.transform, false);
                vis.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                Object.Destroy(vis.GetComponent<CapsuleCollider>());
            }

            root.GetComponent<SimplePlayerController>()?.ConfigureForCampHub();
        }

        private void SetEconomyPanel(CampHubEconomyDraw.EconomyPanelKind panel)
        {
            if (panel != CampHubEconomyDraw.EconomyPanelKind.None && _inventoryPanel != null && _inventoryPanel.IsOpen)
            {
                _inventoryPanel.SetOpen(false);
            }

            if (panel != CampHubEconomyDraw.EconomyPanelKind.None && _characterSheet != null && _characterSheet.IsOpen)
            {
                _characterSheet.SetOpen(false);
            }

            _economyPanel = panel;
            RefreshCampLookCapture();
        }

        private void SetPauseMenuOpen(bool open)
        {
            if (_pauseMenuOpen == open)
            {
                return;
            }

            _pauseMenuOpen = open;
            if (open)
            {
                _pauseSettingsOpen = false;
                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                _pauseSettingsOpen = false;
                Time.timeScale = Mathf.Approximately(_savedTimeScale, 0f) ? 1f : _savedTimeScale;
            }

            if (_pauseMenuView != null)
            {
                _pauseMenuView.SetOpen(open);
                if (open)
                {
                    _pauseMenuView.ShowMainPage();
                }
            }

            RefreshCampLookCapture();
        }

        private void EnsurePauseMenuView()
        {
            if (_pauseMenuView != null)
            {
                return;
            }

            var fromResources = Resources.Load<CampPauseMenuView>(CampPauseMenuView.DefaultViewResourcesPath);
            if (fromResources != null)
            {
                _pauseMenuView = Instantiate(fromResources, transform);
                _pauseMenuView.name = "CampPauseMenuUi";
            }
            else
            {
                var holder = new GameObject("CampPauseMenuUi");
                holder.transform.SetParent(transform, false);
                _pauseMenuView = holder.AddComponent<CampPauseMenuView>();
            }

            _pauseMenuView.EnsureBuilt();
            _pauseMenuView.SetOpen(false);
            _pauseMenuView.Bind(
                onContinue: () => SetPauseMenuOpen(false),
                onOpenSettings: OpenPauseSettings,
                onExitToMainMenu: TryExitToMainMenu,
                onVolumeChanged: ApplyMasterVolume,
                onToggleFullscreen: ToggleFullscreen,
                onNextResolution: CycleResolution,
                onBackFromSettings: BackFromPauseSettings);
        }

        private void OpenPauseSettings()
        {
            if (!_pauseMenuOpen || _pauseMenuView == null)
            {
                return;
            }

            _pauseSettingsOpen = true;
            _pauseMenuView.ShowSettingsPage(_masterVolume, _fullscreen, GetResolutionLabel(_resolutionIndex));
        }

        private void BackFromPauseSettings()
        {
            if (!_pauseMenuOpen || _pauseMenuView == null)
            {
                return;
            }

            _pauseSettingsOpen = false;
            _pauseMenuView.ShowMainPage();
        }

        private void ApplyMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            AudioListener.volume = _masterVolume;
            if (_pauseMenuOpen && _pauseSettingsOpen && _pauseMenuView != null)
            {
                _pauseMenuView.ShowSettingsPage(_masterVolume, _fullscreen, GetResolutionLabel(_resolutionIndex));
            }
        }

        private void ToggleFullscreen()
        {
            _fullscreen = !_fullscreen;
            Screen.fullScreen = _fullscreen;
            if (_pauseMenuOpen && _pauseSettingsOpen && _pauseMenuView != null)
            {
                _pauseMenuView.ShowSettingsPage(_masterVolume, _fullscreen, GetResolutionLabel(_resolutionIndex));
            }
        }

        private void CycleResolution()
        {
            if (_resolutions == null || _resolutions.Length == 0)
            {
                return;
            }

            _resolutionIndex = (_resolutionIndex + 1) % _resolutions.Length;
            var resolution = _resolutions[_resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, _fullscreen);
            if (_pauseMenuOpen && _pauseSettingsOpen && _pauseMenuView != null)
            {
                _pauseMenuView.ShowSettingsPage(_masterVolume, _fullscreen, GetResolutionLabel(_resolutionIndex));
            }
        }

        private void TryExitToMainMenu()
        {
            if (SceneNavigation.IsBusy)
            {
                return;
            }

            Time.timeScale = 1f;
            MenuSessionWriter.ClearResumeIntent();
            MenuSessionWriter.SetPendingDungeonEntry(false);
            SceneNavigation.GoTo(SceneNames.DefaultMenu);
        }

        private int FindCurrentResolutionIndex()
        {
            if (_resolutions == null || _resolutions.Length == 0)
            {
                return 0;
            }

            for (var i = 0; i < _resolutions.Length; i++)
            {
                var resolution = _resolutions[i];
                if (resolution.width == Screen.currentResolution.width &&
                    resolution.height == Screen.currentResolution.height)
                {
                    return i;
                }
            }

            return _resolutions.Length - 1;
        }

        private string GetResolutionLabel(int index)
        {
            if (_resolutions == null || _resolutions.Length == 0)
            {
                return "N/A";
            }

            var resolution = _resolutions[Mathf.Clamp(index, 0, _resolutions.Length - 1)];
            return $"{resolution.width} x {resolution.height} @ {Mathf.RoundToInt((float)resolution.refreshRateRatio.value)}Hz";
        }

        private void UpdateNearKind()
        {
            if (playerBody == null)
            {
                _near = NearKind.None;
                return;
            }

            var p = playerBody.position;
            var r2 = interactRadius * interactRadius;
            _near = NearKind.None;
            var bestSq = r2 + 0.0001f;

            void Consider(Transform anchor, NearKind kind)
            {
                if (anchor == null)
                {
                    return;
                }

                var d = anchor.position - p;
                d.y = 0f;
                var sq = d.sqrMagnitude;
                if (sq > r2 || sq >= bestSq)
                {
                    return;
                }

                bestSq = sq;
                _near = kind;
            }

            Consider(shopAnchor, NearKind.Shop);
            Consider(forgeAnchor, NearKind.Forge);
            Consider(alchemyAnchor, NearKind.Alchemy);
            Consider(dungeonAnchor, NearKind.Dungeon);
        }

        private void TryInteract()
        {
            if (_characterSheet != null && _characterSheet.IsOpen)
            {
                _characterSheet.SetOpen(false);
                return;
            }

            if (_inventoryPanel != null && _inventoryPanel.IsOpen)
            {
                _inventoryPanel.SetOpen(false);
                return;
            }

            if (_economyPanel != CampHubEconomyDraw.EconomyPanelKind.None)
            {
                SetEconomyPanel(CampHubEconomyDraw.EconomyPanelKind.None);
                return;
            }

            switch (_near)
            {
                case NearKind.Shop:
                    SetEconomyPanel(CampHubEconomyDraw.EconomyPanelKind.Shop);
                    break;
                case NearKind.Forge:
                    SetEconomyPanel(CampHubEconomyDraw.EconomyPanelKind.Forge);
                    break;
                case NearKind.Alchemy:
                    SetEconomyPanel(CampHubEconomyDraw.EconomyPanelKind.Alchemy);
                    break;
                case NearKind.Dungeon:
                    EnterDungeon();
                    break;
            }
        }

        private void EnterDungeon()
        {
            if (SceneNavigation.IsBusy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                _status = "Gameplay scene name is empty.";
                return;
            }

            PersistAccountIfNeeded();
            PendingCampDungeonRequest.Set();
            MenuSessionWriter.SetPendingDungeonEntry(true);
            SceneNavigation.GoTo(gameplaySceneName.Trim());
        }

        private void OnDestroy()
        {
            if (_pauseMenuOpen && Mathf.Approximately(Time.timeScale, 0f))
            {
                Time.timeScale = 1f;
            }
        }

        private void PersistAccountIfNeeded()
        {
            if (_profile == null)
            {
                return;
            }

            _profile.forgeDust = _account.forgeDust;
            _profile.emberCore = _account.emberCore;
            _profile.sigilToken = _account.sigilToken;
            _profile.insuranceSeal = _account.insuranceSeal;
            _profile.gold = _account.gold;
            _profile.accountJson = JsonUtility.ToJson(_account);
            _profilesService.SaveProfile(_profile);
        }

        private static AccountState BuildInitialAccount()
        {
            var account = new AccountState
            {
                gold = AccountEconomy.StarterGoldPurse,
                forgeDust = 2500,
                emberCore = 5,
                sigilToken = 20,
                insuranceSeal = 1,
                primaryStats = CharacterPrimaryStats.CreateDefault()
            };

            account.stash.Add(ItemInstanceFactory.Create("weapon_simple_sword"));
            account.stash.Add(ItemInstanceFactory.Create("armor_simple_chest"));

            AccountEconomy.AppendStarterCraftMaterials(account);
            return account;
        }

        private static AccountState LoadOrCreateAccount(ProfileData profile)
        {
            if (!string.IsNullOrWhiteSpace(profile.accountJson))
            {
                try
                {
                    var acc = JsonUtility.FromJson<AccountState>(profile.accountJson) ?? BuildInitialAccount();
                    CharacterPaperDoll.EnsureList(acc);
                    CharacterStatsService.RecalculateForCamp(acc);
                    return acc;
                }
                catch
                {
                    return BuildInitialAccount();
                }
            }

            return BuildInitialAccount();
        }

        private static void SyncLegacyResourcesFromProfile(ProfileData profile, AccountState account)
        {
            if (profile == null || account == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.accountJson))
            {
                account.forgeDust = profile.forgeDust;
                account.emberCore = profile.emberCore;
                account.sigilToken = profile.sigilToken;
                account.insuranceSeal = profile.insuranceSeal;
                account.gold = profile.gold;
            }
        }
    }
}
