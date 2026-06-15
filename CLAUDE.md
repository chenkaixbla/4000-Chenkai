# 4000-Chenkai — Idle Game (Melvor Idle-style)

A Unity idle/incremental game inspired by Melvor Idle: jobs/skills, idle actions,
inventory, shop, and combat. This project is being **rebuilt from the UI up** on top
of a kept data layer. This file is the living brief — update it as the project grows.

- **Engine:** Unity `6000.3.6f1` (Unity 6.3)
- **Inspector toolkit:** [EditorAttributes](Assets/Imported/EditorAttributes) — use its
  attributes for clean inspectors (see below).
- **Platform:** Windows. Shell is PowerShell.

---

## Core philosophy (read before adding code)

The codebase must stay **artist/designer friendly** and easy to return to after time away.
There are many features, so scattering one-off scripts everywhere makes the project hard
to relearn. Follow these principles:

1. **One manager per system.** Each system (menus, inventory, jobs, combat, shop…) is
   driven by a single manager script that *does the work automatically* — spawning,
   wiring, lookups, state. Prefer one smart manager over many tiny coupled scripts.
2. **Automatic where it can be, manual where it should be.** The manager handles
   spawning prefabs, deciding where they go, and attaching/maintaining element scripts.
   The **developer** handles the things that need a human eye: building prefabs, adding
   element scripts to a prefab, and deciding how the UI *looks*.
3. **Don't add a script where a built-in already does the job.** Native Unity
   components (e.g. `Button` and its `onClick`) are used as-is — wire them to a manager's
   public method instead of bolting on a custom component per object. Reserve EditorAttributes
   `[Dropdown]` pickers for fields *on the managers themselves*, where one script serves the
   whole system.
4. **Lean on EditorAttributes** for readable inspectors: `[Title]`, `[Dropdown]`,
   `[ReadOnly]`, `[Required]`, `[Button]`, `[FoldoutGroup]`, `[HorizontalGroup]`,
   `[MessageBox]`, `[ShowField]`, `[OnValueChanged]`, `[AssetPreview]`, etc.

---

## Naming convention (all new scripts)

`Group_Script` or `Group_Subgroup_Script`.

Examples: `UI_Menu_Manager`, `UI_Menu_Button`, `Player_Input`, `Combat_Manager`,
`UI_Confirmation`, `Phone_Apps_Management`. File name matches the class name.

---

## What to KEEP vs what is OLD

### Keep (do not casually change — these are the foundation)
- **`Assets/Personal/Scripts/Catalog/`** — data catalog tooling (settings, item types,
  editor windows). Treat as stable.
- **The three data definitions** (premade `ScriptableObject` lists for the project):
  - `Assets/Personal/Scripts/Items/ItemsData.cs`  ← this is "Item_Data"
  - `Assets/Personal/Scripts/Jobs/Data/Job_Data.cs`
  - `Assets/Personal/Scripts/Idles/Data/Idle_Data.cs`
- **All `ScriptableObject` assets** under `Assets/Personal/Data/` (the actual data lists).

> **Data SOs are now slimmed to generic fields only.** The advanced systems (conditions,
> finish actions, purchase requirements, combat data, cycle costs/rewards) and their types
> (`ConditionRule`, `Item_Amount`, `FinishAction`, `CombatItemDefinition`, `Idle_Instance`,
> etc.) were **deleted**. Current data fields:
> - `ItemsData`: icon, itemType, itemID, displayName, itemDescriptions, price
> - `Job_Data`: jobName, jobIcon, jobCategory, idleDatas, idleCardPrefabOverride (+ helpers)
> - `Idle_Data`: icon, guid, displayName, idleKind, interval, autoRestart,
>   stopWhenCycleCannotRun, idleXPReward, jobXPReward
> The **Catalog spreadsheet** was trimmed to match (removed the idle "Rules" and item
> "Details" columns + their helpers). Re-add advanced fields here when those systems return.

### Old scripts — DELETED
- `Assets/Personal/Old Scripts/` (the previous implementation) has been **removed**. It's
  still recoverable from git history if a reference is ever needed.
- Anything reintroduced must be written fresh in the new structure — do not resurrect old code.

---

## Scripts folder layout

```
Assets/Personal/Scripts/
├─ Catalog/             KEEP — catalog tooling + editor windows (trimmed to generic fields)
├─ Items/ItemsData.cs   KEEP — item data definition ("Item_Data"), slimmed
├─ Jobs/Data/Job_Data.cs    KEEP — job data definition, slimmed
├─ Jobs/UI/Job_UI.cs        NEW — spawns job buttons from the catalog
├─ Idles/Data/Idle_Data.cs  KEEP — idle action data definition, slimmed
├─ Idles/Runtime/           NEW — Idle_Runtime (live idle state)
├─ Idles/UI/                NEW — Idle_Card, Idle_Card_Extension, Idle_UI
└─ UI/                  NEW — UI_Menu_Manager, UI_Button
```

New systems get their own top-level folder under `Scripts/` (e.g. `UI/`, `Inventory/`,
`Combat/`) as they are built.

---

## Systems

### Rewards — shared payout type  *(data definition only)*
- **`Reward`** ([Rewards/Reward.cs](Assets/Personal/Scripts/Rewards/Reward.cs)) — one serializable
  reward entry reused anywhere something pays out. `Reward_Type` enum: `Coins`, `Item` (+amount),
  `XP`, `UnlockJob` (drag a `Job_Data`), `UnlockIdle` (drag an `Idle_Data` — runtime finds its
  owning job, no need to pick the job). `[ShowField]` shows only the fields for the chosen type.
  Each reward also has a `chance` (0–100%, default 100) with a `Rolls()` helper for drop odds.
  > Caveat: `[ShowField]` on a `[Serializable]` type nested in a `List<>` can throw an
  > **editor-only** UI Toolkit binding exception (`BindTree` index out of range) when that asset
  > is in the Inspector during a rebuild (e.g. entering Play). It's a console annoyance only —
  > data, runtime gameplay, and builds are unaffected (EditorAttributes drawers don't ship in
  > builds). Kept by preference; swap to a custom IMGUI drawer if the spam bites.
- **`Reward_Leveled`** wraps a `Reward` with a level trigger (`Reward_Trigger`): `OnEachLevel`
  (every level up) or `AtLevel` (once, when a specific level is reached). `ShouldGrant(level)`
  evaluates it. `Job_Data` and `Idle_Data` hold `List<Reward_Leveled>` (level-up rewards, checked
  against the job's / idle's level); `Monster_Data` holds a plain `List<Reward>` (on defeat).
  Granting/unlock resolution is runtime work, **not built yet**.

### Combat — monster data  *(data definition only)*
- **`Monster_Data`** ([Combat/Data/Monster_Data.cs](Assets/Personal/Scripts/Combat/Data/Monster_Data.cs))
  — enemy data, built to mirror the player's combat model so both fight on the same terms:
  the four levels (`healthLevel`/`strengthLevel`/`defenseLevel` + `attackType` & `attackTypeLevel`)
  whose sum is `CombatLevel` (each caps at 99), plus the non-derived attributes (`speed`,
  `criticalHitRate`, `criticalHitBonus`=200) and a `List<Reward>` (on defeat). Derived stats (max HP, damage
  reduction, damage range) are **not stored** — the future combat system computes them from the
  levels with the same formulas as the player. `Combat_AttackType` enum (`Melee`/`Ranged`/`Magic`)
  is shared by player & monster. No combat runtime/UI yet.
  - **Catalog:** the spreadsheet has a **Monsters tab** (name, combat level, attack type, speed).
    Tabs with a rewards list (Jobs, Idles, Monsters) show a **Rewards column with an `Open` button**
    that selects + pings the SO (rewards are edited in the inspector, not inline). A
    `monstersDataFolder` was added to `Catalog_DataSettings` and its settings window.

### Game — data source  *(implemented; consumers wired later)*
- **`Game_Manager`** ([Game/Game_Manager.cs](Assets/Personal/Scripts/Game/Game_Manager.cs))
  — one per scene (`Instance`). Holds a `Game_DataSource` enum (`Real` / `Testing`) and the
  two root folders (`Assets/Personal/Data`, `Assets/Personal/Testing Data`). Exposes
  `ActiveDataFolder` + `JobsFolder`/`ItemsFolder`/`IdlesFolder` so gathering code reads one
  switch instead of a hard-coded path. **`Job_UI` consumes it** (real vs testing jobs);
  `Idle_UI` follows automatically since idles travel on each `Job_Data`.

> **Testing data:** a full testing set lives in `Assets/Personal/Testing Data/`
> (3 jobs, 14 idles, 24 items — icons blank, everything else filled), mirroring the real
> jobs. Real data stays under `Assets/Personal/Data/`.

### UI — Menu switching  *(implemented)*
The main canvas has buttons scattered around it and one large area where menu panels live.
Exactly one panel is shown at a time (shop, inventory, idle views, combat, etc.).

- **`UI_Menu_Manager`** ([UI/UI_Menu_Manager.cs](Assets/Personal/Scripts/UI/UI_Menu_Manager.cs))
  - One instance per scene, exposed via `UI_Menu_Manager.Instance`.
  - Holds a **manual list of `UI_Menu_Entry`** = `{ string menuName, GameObject panel }`.
    The developer fills this list with each menu panel and a unique name.
  - `Show(name)` activates that panel and deactivates all others (plain `SetActive`).
    `HideAll()`, `HasMenu(name)`, `ActiveMenu`, optional default-on-start.
  - `defaultMenu` uses an EditorAttributes `[Dropdown]` of the menu names (no typos).
  - `GetMenuNames()` is public so future manager-side dropdowns can reuse it.
  - Warns on duplicate menu names.
  - Each `UI_Menu_Entry` may also reference one **opener button** that opens that menu.
    It's a single `[TypeFilter(typeof(Button), typeof(UI_Button))]` Object field — drop in
    either a native `Button` or a `UI_Button`. The manager auto-wires its `onClick` at
    runtime — wire navigation entirely from this one manager, no per-button setup.

**Two ways to open a menu — both valid:**
- Set the entry's `openButton`/`openUIButton` and let the manager wire it (centralized).
- Or wire a button's native `Button.onClick` → `UI_Menu_Manager.Show` and type the name.

**Scene setup (manual, dev side):** add `UI_Menu_Manager` to one GameObject, populate
`menus` with each panel + a unique name + (optionally) its opener button, set a default.

### UI — Buttons & job list  *(implemented)*
- **`UI_Button`** ([UI/UI_Button.cs](Assets/Personal/Scripts/UI/UI_Button.cs)) — a thin
  element script bundling the parts managers fill in code: the `Button`, a `TMP_Text`
  `label`, and an `Image`. Put it on a button prefab; `SetText`/`SetImage` drive it.
  `[Button] Auto Assign` (and `Reset`) grab the three refs from children.
- **`Job_UI`** ([Jobs/UI/Job_UI.cs](Assets/Personal/Scripts/Jobs/UI/Job_UI.cs)) — spawns one
  `UI_Button` (`buttonPrefab`) per job under `buttonParent`, setting each button's text to
  `jobName` and image to `jobIcon`. `Build()` **auto-gathers** both job lists (real + testing)
  from `Game_Manager`'s folders (editor-only, `AssetDatabase`) and spawns whichever matches
  `Game_Manager.dataSource`. It builds on `Start` (`buildOnStart`) and **rebuilds itself live**
  when the data source is flipped (subscribes to `Game_Manager.OnDataSourceChanged`) — no manual
  Refresh/Build. `Job_UI` references the `UI_Menu_Manager` and a
  `[Dropdown]` `menuToOpen` (its menu names); every spawned job button is auto-wired to
  open that menu on click.

> **Text:** the project uses **TextMeshPro** — new UI text fields use `TMP_Text`.

### Idles — cards, view & runtime  *(implemented)*
A job owns idle actions (`Job_Data.idleDatas`). The idle view shows one card per idle for
the selected job. Layers:

- **`Idle_Runtime`** ([Idles/Runtime/Idle_Runtime.cs](Assets/Personal/Scripts/Idles/Runtime/Idle_Runtime.cs))
  — generic, UI-agnostic live state of one idle (`isRunning`, `timer`, `completedCycles`,
  `level`/`currentXP`/`maxXP`, `ownerJobData`) with `OnUpdated`/`OnCycleCompleted` events.
  `Tick()` advances the timer and returns cycles completed; `AddXP()` levels up via
  `XP_Utility`. *Temporary name* — `Idle_Instance` is free now (old one deleted); consolidate
  when convenient. **Economy (costs/rewards) and start conditions are still not implemented.**
- **`Idle_Manager`** ([Idles/Runtime/Idle_Manager.cs](Assets/Personal/Scripts/Idles/Runtime/Idle_Manager.cs))
  — one per scene (`Instance`). **Owns the runtimes** (created per job, persisted) and
  **drives the tick loop**: each frame it ticks every running runtime (so idles progress even
  off-screen) and, per completed cycle, awards **idle XP** to the idle and **job XP** to its
  owning job (via `Job_Manager`). `GetRuntimes(job)`, `StartIdle`/`StopIdle`/`ToggleIdle`,
  `StopAll`.
- **`Idle_Card`** ([Idles/UI/Idle_Card.cs](Assets/Personal/Scripts/Idles/UI/Idle_Card.cs))
  — base card view. `Bind(runtime)`/`Unbind()` (reusable → poolable). Drives the common
  fields (name, icon, info, progress) and **auto-discovers `Idle_Card_Extension` components**
  on its prefab, forwarding bind/refresh/unbind to them. Toggle button raises
  `ToggleRequested` → `Idle_UI` routes it to `Idle_Manager.ToggleIdle`.
- **`Idle_Card_Extension`** ([Idles/UI/Idle_Card_Extension.cs](Assets/Personal/Scripts/Idles/UI/Idle_Card_Extension.cs))
  — abstract base for **special** card visuals. Add a subclass to a special prefab and drag
  in its custom slots (manual, by design). Basic cards need none. This is how special idle
  formatting is added **without bloating scripts or touching `Idle_Runtime`**.
- **`Idle_UI`** ([Idles/UI/Idle_UI.cs](Assets/Personal/Scripts/Idles/UI/Idle_UI.cs))
  — one per scene. `ShowJob(job)` spawns a card per idle and binds it. **Pools cards**
  (keyed by prefab, pool root created at runtime — no manual pooling setup) so switching
  jobs reuses cards instead of destroy/instantiate. Gets its runtimes from `Idle_Manager`
  (falls back to local non-ticking runtimes if no manager is in the scene). Also has an
  optional **Current Job Display** (`jobLevelText` / `jobExperienceText` / `jobLevelBarFill`)
  that `ShowJob` binds to the viewed job's `Job_Runtime` — switch jobs and it updates to that
  job's level/xp (live via `OnUpdated`).

**Card-prefab resolution (per-job + default):** `job.idleCardPrefabOverride` if set, else
`Idle_UI.defaultCardPrefab`. The override field lives on `Job_Data` (content travels with
the catalog, not scene wiring).

**Wiring:** `Job_UI` has an optional `idleUI` ref; clicking a job button opens the menu
*and* calls `Idle_UI.ShowJob(thatJob)`.

### Progression — XP & levels  *(implemented)*
- **`XP_Utility`** ([Progression/XP_Utility.cs](Assets/Personal/Scripts/Progression/XP_Utility.cs))
  — shared level curve `GetMaxXPForLevel(level)` (RuneScape/Melvor-style, recovered from the
  old project) + `AddXP(ref level, ref currentXP, ref maxXP, amount)` that levels up and rolls
  over the remainder. **Both idles and jobs use the same curve.**
- **`Job_Runtime`** ([Jobs/Runtime/Job_Runtime.cs](Assets/Personal/Scripts/Jobs/Runtime/Job_Runtime.cs))
  — a job's live `level`/`currentXP`/`maxXP` + `OnUpdated`, `AddXP`, `GetNormalizedLevelProgress`.
  `Job_Data.maxLevel` (default 100) exists as a field but is **not wired to cap leveling yet**.
- **`Job_Manager`** ([Jobs/Runtime/Job_Manager.cs](Assets/Personal/Scripts/Jobs/Runtime/Job_Manager.cs))
  — one per scene (`Instance`). Owns a `Job_Runtime` per job (lazy, persisted). `GetRuntime(job)`,
  `AddXP(job, amount)`, `Runtimes`. `Idle_Manager` feeds it job XP from idle cycles.

> **Scene setup:** add one `Idle_Manager` and one `Job_Manager` to the scene. XP now accrues
> live during play; there is **no save/persistence yet** (resets each play session).

---

## Conventions cheat-sheet
- Unity 6 lookups: `FindFirstObjectByType<T>()` (not the deprecated `FindObjectOfType`).
- Managers expose a static `Instance` when a single scene-wide instance is intended.
- Keep public, designer-facing fields tooltipped and grouped with `[Title]`.
