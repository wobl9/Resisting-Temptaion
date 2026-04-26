# Profile persistence (save system)

**Purpose:** single reference for how player profiles and expedition checkpoints are stored, which code owns what, and how we will add a server later.

**Current shipping default:** only **local** persistence is used. `Remote` storage exists in code for a future backend; leave `ProfileStorageMode.Local` in scenes unless you intentionally test HTTP.

---

## High-level architecture

```text
MainMenuController / PlayableLoopDemo
        │
        ▼
ProfileStorageFactory.Create(mode, baseUrl, token)
        │
        ├── ProfileStorageMode.Local  → LocalJsonProfileStorage (disk JSON)
        │
        └── ProfileStorageMode.Remote → RemoteProfileStorage (HTTP + local mirror)
```

- **Contract:** `IProfileStorage` in `Assets/Scripts/Menu/IProfileStorage.cs` — all menu and gameplay code should depend on this interface, not on a concrete storage class.
- **Models:** `ProfileSummary`, `ProfileIndexData`, `ProfileData` in `Assets/Scripts/Menu/ProfilePersistenceModels.cs`.
- **Factory:** `Assets/Scripts/Menu/ProfileStorageFactory.cs` — chooses implementation. If mode is `Remote` but `remoteBaseUrl` is empty/whitespace, the factory **falls back to local** (safe default).

---

## Local disk layout

Root: `Application.persistentDataPath/ShatteredForge/`

| Path | Role |
|------|------|
| `profiles_index.json` | Active profile id + list of profile summaries (names, timestamps). |
| `Profiles/profile_{profileId}.json` | Full `ProfileData` for one slot (meta, currencies, `accountJson`, expedition fields, `expeditionRunJson`). |

Serialization: `UnityEngine.JsonUtility` + `File.ReadAllText` / `File.WriteAllText` inside `LocalJsonProfileStorage`.

---

## Session handoff (not the same as profile files)

Between menu scene and gameplay scene we still use **PlayerPrefs** (fast, scene-agnostic):

| Key | Constant | Meaning |
|-----|----------|---------|
| `sf.active_profile_id` | `MenuSessionPrefs.ActiveProfileIdKey` | Which profile id the next gameplay load should use. |
| `sf.resume_expedition` | `MenuSessionPrefs.ResumeExpeditionKey` | `1` if user chose Continue (resume expedition), cleared after read. |

Writers: `MenuSessionWriter`, and `MainMenuController` when deleting a profile / syncing prefs.

**Note:** a future logged-in server account will still be able to use these prefs for “last selected profile id” in the client; authoritative profile list may live on the server.

---

## Revision / concurrency

`ProfileData.profileRevision` increments on each successful **local** `SaveProfile` (`LocalJsonProfileStorage`). `RemoteProfileStorage` bumps revision once per save, PUTs to the server, then mirrors the same snapshot to disk via `PersistProfileSnapshot` (no double increment).

Use this field later for `If-Match` / optimistic concurrency on the backend.

---

## Remote storage (future)

Implementation: `Assets/Scripts/Menu/RemoteProfileStorage.cs`.

Expected HTTP layout relative to **base URL** (no trailing slash required; factory normalizes):

| Method | Relative path | Body |
|--------|---------------|------|
| GET | `index` | — → JSON matching `ProfileIndexData` |
| PUT | `index` | JSON `ProfileIndexData` |
| GET | `profiles/{profileId}` | — → JSON `ProfileData` |
| PUT | `profiles/{profileId}` | JSON `ProfileData` |
| DELETE | `profiles/{profileId}` | — |

Optional header: `Authorization: Bearer <token>` from `remoteProfileStorageAuthBearer` on `MainMenuController` / `PlayableLoopDemo`.

Network calls are **synchronous** today (`SendWebRequest` with a completion wait). When we wire a real server, prefer async (`async`/`UniTask`) or a save queue without blocking the main thread; the `IProfileStorage` surface can stay sync initially and be wrapped.

---

## Inspector defaults (scenes)

On `MainMenuController` and `PlayableLoopDemo`:

- `profileStorageMode` = **Local**
- `remoteProfileStorageBaseUrl` = empty
- `remoteProfileStorageAuthBearer` = empty

Keep it that way until a backend is ready.

---

## Related docs

- Start menu behavior and glossary: `Docs/ShatteredForge/Menu_logic_contract.md`
- Progress tracking: `Docs/ShatteredForge/PROGRESS.md`
