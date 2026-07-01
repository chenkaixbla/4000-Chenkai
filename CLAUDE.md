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
   `[MessageBox]`, `[ShowField]`, `[OnValueChanged]`, `[AssetPreview]`, `[Line]`, etc.
   **`[Title]` rule:** only use `[Title]` to group **2+ related fields** under a heading. For a
   single field, or to simply separate things, use `[Line]` (or nothing) — never title a lone
   field. Prefer EditorAttributes (`[Title]`/`[Line]`) over Unity's `[Header]`. For a few orphan
   one-off fields that don't each warrant a heading, gather them under a single catch-all
   `[Title("General")]` group rather than giving each its own title. **Keep `[Title]` text short
   (1–3 words).** For any informative/descriptive text, use `[HelpBox("…", MessageMode.None)]`
   rather than a long title or `[MessageBox]`.

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

### Data source — Catalog_DataSettings  *(implemented)*
- **`Catalog_DataSettings`** ([Catalog/Catalog_DataSettings.cs](Assets/Personal/Scripts/Catalog/Catalog_DataSettings.cs))
  — the project's one settings SO (the Catalog spreadsheet already uses it). Holds a
  `Catalog_DataSource` enum (`Real` / `Testing`) and the two root folders (`Assets/Personal/Data`,
  `Assets/Personal/Testing Data`). **Gathering scans the active root folder filtered by type**
  (`Job_Data`/`ItemsData`/`Idle_Data`/`Monster_Data`), recursing into subfolders — so **subfolders
  are optional dev-only organization, never required**. The per-type `JobsFolder`/`ItemsFolder`/
  `IdlesFolder`/`MonstersFolder` props are just conventional subfolder paths used as the default
  *create* location for new assets. A static `Active` accessor
  (editor: `AssetDatabase`; runtime: `Resources`/`OnEnable` cache) lets any script reach it with
  no scene object, and a static `OnDataSourceChanged` event drives live rebuilds. The **Catalog
  Spreadsheet toolbar** has the Real/Testing dropdown (left of Settings/Stretch/Refresh); the
  **Settings window** edits the two roots. *(This replaced the old `Game_Manager` scene component —
  same rule, moved into the Catalog tooling. Delete any leftover Game_Manager component from the scene.)*
- **RULE — `Catalog_DataSettings` is the single source of truth for jobs/idles/items data.** All
  gathering/loading must go through `Catalog_DataSettings.Active`'s folders (respecting the active
  `dataSource`), never a hard-coded path. Items consumers (inventory/shop) must use
  `Catalog_DataSettings.Active.ItemsFolder` when built.

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
  - Each `UI_Menu_Entry` may also reference one **opener button** GameObject that opens that
    menu. Drag any button object — `ResolveOpenButton()` uses its `UI_Button.button` if present,
    else a native `Button`. The manager auto-wires the `onClick` at runtime — wire navigation
    entirely from this one manager, no per-button setup. (A `GameObject` field is used rather
    than `[TypeFilter]` because dragging a GameObject onto a type-filtered Object field only
    grabs the native `Button`, never the `UI_Button`.)

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
- **`UI_Bar`** ([UI/UI_Bar.cs](Assets/Personal/Scripts/UI/UI_Bar.cs)) — a reusable fill bar
  driven by a **Filled** `Image`'s `fillAmount` (set Image Type → Filled). Holds a `maxValue`,
  a 0-1 `fill` slider (live in edit + play via `OnValidate`), and two optional `TMP_Text`s:
  `valueText` (driven by `UI_Bar` as `current/max`) and `titleText` (**not** driven — left for
  other scripts to fill, e.g. a level label). Drive it with `SetValue(raw)` / `SetFill(0-1)`;
  read `CurrentValue`. **Coloring:** a working `color` field with an inline `SET` button, plus a
  `colorPresets` palette (`UI_Bar_ColorPreset` = label + color) where each row has its own `SET`
  button (custom drawer [UI/Editor/UI_Bar_ColorPresetDrawer.cs](Assets/Personal/Scripts/UI/Editor/UI_Bar_ColorPresetDrawer.cs))
  that applies the swatch to `fillImage.color`; `SetColor(c)` is the runtime API. Used by
  `Idle_Card` (level + timer bars) and `Job_UI` (job display).
- **`Job_UI`** ([Jobs/UI/Job_UI.cs](Assets/Personal/Scripts/Jobs/UI/Job_UI.cs)) — spawns one
  `UI_Button` (`buttonPrefab`) per job under `buttonParent`, setting each button's text to
  `jobName` and image to `jobIcon`. `Build()` **auto-gathers** both job lists (real + testing)
  from `Catalog_DataSettings`'s folders (editor-only, `AssetDatabase`) and spawns whichever matches
  `Catalog_DataSettings.ActiveDataSource`. It builds on `Start` (`buildOnStart`) and **rebuilds itself live**
  when the data source is flipped (subscribes to `Catalog_DataSettings.OnDataSourceChanged`) — no manual
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
- **`Idle_Data_Crafting`** ([Idles/Data/Idle_Data_Crafting.cs](Assets/Personal/Scripts/Idles/Data/Idle_Data_Crafting.cs))
  — `Idle_Data` subclass for crafting idles: `produces` + `required` lists of `Idle_ItemStack`
  (`ItemsData` + amount), plus a `craftingRewards` list of **`Crafting_Reward`** (grant XP or whole
  levels to a specific job **or** idle — `Crafting_RewardTarget` Job/Idle × `Crafting_RewardGrant`
  XP/Level × amount; runtime granting not built yet). `idleKind` is force-locked to `Crafting`
  (read-only, set in `OnEnable`/`OnValidate`). Since it IS an `Idle_Data`, drop the asset straight
  into a job's `idleDatas`. Has its own `Create → Game/Idles/Idle_Data_Crafting` menu.
  - **Catalog:** the spreadsheet's **Recipes tab** lists all `Idle_Data_Crafting` (item-tab-like
    rows: display name, interval, job XP, icon), each with a **`▶ Edit` expander** that reveals the
    **Produces / Required / Rewards** editors (add/remove item stacks and reward rows inline).
- **`Idle_Card_Crafting`** ([Idles/UI/Idle_Card_Crafting.cs](Assets/Personal/Scripts/Idles/UI/Idle_Card_Crafting.cs))
  — special card (an `Idle_Card_Extension`) for crafting jobs. `Idle_Card_ItemSlot` = icon +
  title + a runtime `item` ref. Slot lists: `produces` / `required` (filled from the bound
  `Idle_Data_Crafting`), plus `youHave` / `grants`. **TODO:** `youHave` needs the inventory
  system and `grants` needs reward-granting — neither exists yet, so those two stay empty;
  revisit when those systems land. Drag slot refs in by hand (one row per item).
- **`Idle_UI`** ([Idles/UI/Idle_UI.cs](Assets/Personal/Scripts/Idles/UI/Idle_UI.cs))
  — one per scene. `ShowJob(job)` spawns a card per idle and binds it. **Pools cards**
  (keyed by prefab, pool root created at runtime — no manual pooling setup) so switching
  jobs reuses cards instead of destroy/instantiate. Gets its runtimes from `Idle_Manager`
  (falls back to local non-ticking runtimes if no manager is in the scene). Optional
  `gridLayout` ref: on `ShowJob` its `cellSize`
  is set to the resolved card prefab's authored `RectTransform` size, so different jobs can use
  differently-sized cards (pooling unaffected — keyed by prefab). Has a **custom inspector**
  ([Idles/UI/Editor/Idle_UIEditor.cs](Assets/Personal/Scripts/Idles/UI/Editor/Idle_UIEditor.cs)):
  a styled table auto-listing the active source's jobs (`name` + an `Idle_Card` field, jobs
  disabled on `Job_Manager` are struck through) that edits `jobCards` — same auto-refresh as
  `Job_Manager`. The editor is **UI Toolkit** (`CreateInspectorGUI`) so `Idle_UI`'s own fields
  keep using EditorAttributes (`[Title]`/`[Required]`/`[ReadOnly]`); the table is an `IMGUIContainer`.

**Card-prefab resolution:** `Idle_UI.jobCards` list entry for the job (`{Job_Data, Idle_Card}`,
edited via the table), else `job.idleCardPrefabOverride`, else `Idle_UI.defaultCardPrefab`.

**Wiring:** `Job_UI` has an optional `idleUI` ref; clicking a job button opens the job's menu
*and* calls `Idle_UI.ShowJob(thatJob)`.

**Job Bar (one per canvas):** `Job_UI` owns a single shared `UI_Bar jobBar` (+ optional
`jobBarRoot` GameObject toggled with the view; falls back to the bar's own object). Its
`titleText` shows the viewed job's level, its `valueText`/fill the XP `current/max` (live via
`Job_Runtime.OnUpdated`). A **custom editor table** (mirrors `Job_Manager`'s) lists the active
source's jobs with, per job, a **Menu dropdown** (which `UI_Menu_Manager` menu its button opens;
`(Default)` = `menuToOpen`) and a **Job Bar toggle** (default on) — so different prebuilt views
(idle/combat/crafting) can each show the bar. The bar shows only while the active menu matches the
current job's menu (`Job_UI` subscribes to `UI_Menu_Manager.OnMenuChanged`), so leaving to a
non-job menu (shop/inventory) hides it. The table is edited via `Get/SetJobMenu` +
`Is/SetJobBarEnabled` on `Job_UI` (entries auto-pruned back to defaults).

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
  `AddXP(job, amount)`, `Runtimes`. `Idle_Manager` feeds it job XP from idle cycles. Also holds
  **per-job enable toggles** via a custom inspector
  ([Jobs/Editor/Job_ManagerEditor.cs](Assets/Personal/Scripts/Jobs/Editor/Job_ManagerEditor.cs)):
  the inspector **auto-lists** the **active data source's** jobs (via `Catalog_DataSettings.Active.JobsFolder`, so
  no real/testing duplicates) as `name + enabled` rows — re-gathering on select, on data-source
  change, and on `EditorApplication.projectChanged` (no manual refresh). Toggling persists that
  job's flag via `SetJobEnabled`; untouched jobs default to enabled.
  `Job_UI.Build` skips jobs where `IsJobEnabled(job)` is false (jobs not in the list default to enabled).

> **Scene setup:** add one `Idle_Manager` and one `Job_Manager` to the scene. XP now accrues
> live during play; there is **no save/persistence yet** (resets each play session).

---

## Conventions cheat-sheet
- Unity 6 lookups: `FindFirstObjectByType<T>()` (not the deprecated `FindObjectOfType`).
- Managers expose a static `Instance` when a single scene-wide instance is intended.
- **`Singleton<T>`** ([Core/Singleton.cs](Assets/Personal/Scripts/Core/Singleton.cs)) — opt-in
  base for scene-wide managers: static `Instance`, runtime one-per-scene enforcement, and an
  editor add-time guard that pops a warning dialog (naming the object that already has one) and
  removes the duplicate component. Subclass it; if overriding `Awake`, call `base.Awake()` then
  `if (Instance != this) return;`. **The scene managers extend it** (`Job_Manager`,
  `Idle_Manager`, `UI_Menu_Manager`, `Idle_UI`). *(Data source is no longer a scene manager — it's
  the `Catalog_DataSettings` SO.)*
- **Custom editors use UI Toolkit so EditorAttributes keep working.** EditorAttributes drawers are
  UI Toolkit-based; an IMGUI `OnInspectorGUI` editor can't render them (warns). So `Job_Manager`
  and `Idle_UI` override `CreateInspectorGUI`: normal fields are `PropertyField`s (EditorAttributes
  render, including `[ReadOnly]` for read-outs), and the bespoke list is an `IMGUIContainer`. Never
  write an IMGUI `OnInspectorGUI` editor for a type whose fields use EditorAttributes.
  - **If the target uses `[Button]` or `[ShowInInspector]`** (method/non-serialized rendering),
    inherit `EditorAttributes.Editor.EditorExtension` instead of `Editor` and call
    `base.CreateInspectorGUI()` (then append your table), else those stop rendering — a plain
    `[CustomEditor]` overrides EditorAttributes' global `EditorExtension`. `Job_UIEditor` does this
    (to keep `[Button] Build`); hide the table's backing list with `[HideInInspector]`. `Job_Manager`
    /`Idle_UI` editors use plain `Editor` only because those types have no buttons.
- **Editor table helpers** (`Scripts/Core/Editor/`): `EditorTableGUI` (title bar + column header +
  zebra rows + `StrikeRow()` for disabled rows) and `GameData_Editor.GatherActiveJobs()`
  (active-source jobs, per the Catalog_DataSettings rule). Reused by `Job_ManagerEditor`, `Idle_UIEditor`
  and `Job_UIEditor`.
- Keep public, designer-facing fields tooltipped and grouped with `[Title]`.
