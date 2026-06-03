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

> Note: the kept data scripts still reference older types (e.g. `Item_Amount`,
> `ConditionRule`, `CombatItemDefinition`, `Idle_Instance`). Those live in `Old Scripts`
> for now so everything keeps compiling. As systems are rebuilt, these dependencies will
> be reworked and pointed at new code.

### Old (temporary — slated for deletion)
- **`Assets/Personal/Old Scripts/`** — the previous, messy implementation.
  It is kept only so the project compiles during the rebuild. **Do not build new features
  on top of it.** It will be bulk-deleted later.
- **Deleting unused old scripts requires the user's permission** (ask before removing).

---

## Scripts folder layout

```
Assets/Personal/
├─ Old Scripts/          TEMP — previous implementation, to be deleted (outside Scripts/)
└─ Scripts/
   ├─ Catalog/           KEEP — catalog tooling + editor windows
   ├─ Items/ItemsData.cs KEEP — item data definition ("Item_Data")
   ├─ Jobs/Data/Job_Data.cs   KEEP — job data definition
   ├─ Idles/Data/Idle_Data.cs KEEP — idle action data definition
   └─ UI/                NEW — rebuilt UI systems live here
```

New systems get their own top-level folder under `Scripts/` (e.g. `UI/`, `Inventory/`,
`Combat/`) as they are built.

---

## Systems

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
  `jobName` and image to `jobIcon`. The job list is **gathered from the Catalog**
  (`Job_Data` assets in the catalog's jobs folder) via `Refresh Jobs From Catalog`
  (editor-only, uses `AssetDatabase`) into a serialized list, then spawned at runtime
  (`buildOnStart` / `[Button] Build`). Editor gathers, runtime spawns — designer presses
  one button, the manager does the rest. `Job_UI` references the `UI_Menu_Manager` and a
  `[Dropdown]` `menuToOpen` (its menu names); every spawned job button is auto-wired to
  open that menu on click.

> **Text:** the project uses **TextMeshPro** — new UI text fields use `TMP_Text`.

---

## Conventions cheat-sheet
- Unity 6 lookups: `FindFirstObjectByType<T>()` (not the deprecated `FindObjectOfType`).
- Managers expose a static `Instance` when a single scene-wide instance is intended.
- Keep public, designer-facing fields tooltipped and grouped with `[Title]`.
