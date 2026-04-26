using System;
using ShatteredForge.Combat;
using ShatteredForge.Core;
using ShatteredForge.Input;
using ShatteredForge.Menu;
using ShatteredForge.Progression;
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

        private IProfileStorage _profilesService;
        private string _profileId;
        private ProfileData _profile;
        private AccountState _account;

        private NearKind _near;
        private string _status = string.Empty;
        private bool _stubPanelOpen;
        private NearKind _stubPanelKind;

        private void Awake()
        {
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
                _status = "Лагерь (демо, без профиля)";
            }
            else
            {
                _account = LoadOrCreateAccount(_profile);
                SyncLegacyResourcesFromProfile(_profile, _account);
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
        }

        private void Update()
        {
            UpdateNearKind();
            if (DemoInput.KeyDown(Key.Escape))
            {
                if (_stubPanelOpen)
                {
                    SetStubPanelOpen(false);
                }
                else
                {
                    FindFirstObjectByType<CampHubCameraRig>()?.ToggleCursorLock();
                }
            }

            if (DemoInput.KeyDown(Key.E))
            {
                TryInteract();
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
                    NearKind.Shop => "Shop (stub) — E",
                    NearKind.Forge => "Forge (stub) — E",
                    NearKind.Alchemy => "Alchemy (stub) — E",
                    NearKind.Dungeon => "Dungeon — E to enter",
                    _ => string.Empty
                };
                GUI.Label(new Rect(pad, hintY, Screen.width - pad * 2, 24), label);
                hintY += 26;
            }
            else
            {
                GUI.Label(new Rect(pad, hintY, Screen.width - pad * 2, 24), "WASD / стрелки — движение | Мышь — обзор (герой поворачивается) | Esc — курсор | Подойти к меткам | E — действие");
                hintY += 26;
            }

            if (_stubPanelOpen)
            {
                var w = 420f;
                var h = 160f;
                var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
                GUI.Box(r, GUIContent.none);
                GUILayout.BeginArea(new Rect(r.x + 16f, r.y + 12f, w - 32f, h - 24f));
                var title = _stubPanelKind switch
                {
                    NearKind.Shop => "Shop",
                    NearKind.Forge => "Forge",
                    NearKind.Alchemy => "Alchemy lab",
                    _ => "Stub"
                };
                GUILayout.Label(title, new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold });
                GUILayout.Space(8f);
                GUILayout.Label("Coming soon.");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close (E)", GUILayout.Height(32f)))
                {
                    SetStubPanelOpen(false);
                }

                GUILayout.EndArea();
            }
        }

        /// <summary>
        /// Guarantees a visible capsule + <see cref="CharacterController"/> + <see cref="SimplePlayerController"/> (same stack as dungeon).
        /// </summary>
        private void EnsureCampAvatar()
        {
            if (playerBody == null)
            {
                var found = FindFirstObjectByType<SimplePlayerController>();
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

        private void SetStubPanelOpen(bool open)
        {
            _stubPanelOpen = open;
            FindFirstObjectByType<CampHubCameraRig>()?.SetLookFromUiSuppressed(open);
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
            if (_stubPanelOpen)
            {
                SetStubPanelOpen(false);
                return;
            }

            switch (_near)
            {
                case NearKind.Shop:
                case NearKind.Forge:
                case NearKind.Alchemy:
                    _stubPanelKind = _near;
                    SetStubPanelOpen(true);
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
            _profile.accountJson = JsonUtility.ToJson(_account);
            _profilesService.SaveProfile(_profile);
        }

        private static AccountState BuildInitialAccount()
        {
            var account = new AccountState
            {
                forgeDust = 2500,
                emberCore = 5,
                sigilToken = 20,
                insuranceSeal = 1
            };

            account.stash.Add(new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = "weapon_sword_t1",
                rarity = "Rare",
                enhanceLevel = 5
            });
            account.stash.Add(new ItemInstance
            {
                id = Guid.NewGuid().ToString(),
                templateId = "armor_chest_t1",
                rarity = "Magic",
                enhanceLevel = 3
            });

            return account;
        }

        private static AccountState LoadOrCreateAccount(ProfileData profile)
        {
            if (!string.IsNullOrWhiteSpace(profile.accountJson))
            {
                try
                {
                    return JsonUtility.FromJson<AccountState>(profile.accountJson) ?? BuildInitialAccount();
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
            }
        }
    }
}
