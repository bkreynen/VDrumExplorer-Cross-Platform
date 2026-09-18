Status: active

# Pivot Plan v2: VDrumExplorer-Cross-Platform → Accessible Single-UI Retrofit for Roland V-Drums

**Date:** 2026-09-18 (supersedes `.agents/plans/2026-09-18-1215-accessibility-pivot-plan.md` — v1 kept for history, do not edit)
**Repo:** bkreynen/VDrumExplorer-Cross-Platform (C#/.NET 10, Avalonia, Linux-primary; fork of Jon Skeet's VDrumExplorer)
**Input:** `.agents/research/2026-09-18-1200-accessibility-pivot-research.md` (verified codebase + external research — trusted, not re-verified here)
**Scope:** Planning only. No code changes in this document.

---

## Overview

This plan pivots the app from a sighted-oriented settings editor into an **accessibility interface for Roland drum modules** by **retrofitting the existing Avalonia GUI (`VDrumExplorer.Gui.Avalonia`) into one accessible UI for all users** — blind and sighted alike. The core bet is unchanged from v1: the hard parts (MIDI protocol, schema layer with per-field `Name`/`Description`, MVVM ViewModels, async `DeviceController`) already exist and are tested. What changed is the delivery vehicle: instead of a second front-end, we annotate and rework the **one** UI — `AutomationProperties` everywhere, keyboard shortcuts as real `KeyBinding`s, and in-window accessibility aids (jump-to-field search, flat field-list mode, heading structure, live-region status line) that also improve the sighted experience.

**Owner decisions driving this revision (all binding):**

| # | Decision | Consequence for the plan |
|---|---|---|
| 1 | **No TD-50X (or other) hardware access** — only TD-17 v2 is in hand | TD-50X labeled "experimental, unverified" in docs; no hardware-verification task anywhere in the plan |
| 2 | **ONE UI for all users** — retrofit `VDrumExplorer.Gui.Avalonia`; no separate accessible front-end | v1's option (ii) is overruled; entire Phase 1/2 restructured around retrofit; "one path so that we do not get divergent features" |
| 3 | **No upstream contribution** to Jon Skeet's repo (deliberate fork, major changes, no expectation of contributing back) | Phase 3 upstream-coordination task removed; replaced with community announcement + optional Roland gap report |
| 4 | **TD-17 v2 is the primary target** (owner-owned); TD-27 secondary (tester may be found later) | All Phase 0/1 manual testing targets TD-17 v2; TD-27 prep is Phase 2; other modules documented honestly per the support matrix |
| 5 | **No active recruitment for other-module testers** — community tests other modules and files issues; `VDrumExplorer.Blazor` explicitly out of scope (untouched); linear-navigation sequencing: search first, flat mode second | TD-27 prep stays schema-audit-only (no hardware needed); docs carry honest per-module status; no Blazor tasks anywhere in the plan |

Additionally, the v1 console REPL (option iii) conflicts with the one-path philosophy and is **dropped from the main plan line** (see §Interface Decision for the far-future note).

---

## Guiding principles

1. **One UI, no divergence.** Every accessibility improvement lands in `VDrumExplorer.Gui.Avalonia` itself. There is no second front-end to keep feature-parity with, and no "accessible mode" fork of the views — the same window serves everyone.
2. **Never regress the sighted UX.** The repo already has **visual baseline tests** (`DataExplorerVisualTest`, `ExplorerHomeVisualTest`, `SchemaExplorerVisualTest`, `DialogVisualTest` in `VDrumExplorer.Gui.Avalonia.Test`, using headless Skia frame capture). **Visual baselines stay green** is an explicit guardrail and definition-of-done item in every phase: a11y changes that alter what sighted users see must either be visually neutral or come with deliberately re-baselined, reviewed captures.
3. **ViewModel layer is the shared core.** Retrofit work is views + bindings; business logic stays in `VDrumExplorer.ViewModel`. No logic in code-behind (in fact, retrofit *removes* logic from code-behind — see §Interface Decision).
4. **Schema text is the TTS layer.** Every schema field carries `Name` + `Description` + min/max/enum values — accessible names, `HelpText`, and announcements are derived from this, not hand-written per parameter.
5. **Announce every action** (REAPER+OSARA model): every successful operation produces a live-region status update; every failure produces an assertive announcement.
6. **Blind-user naming rules from day one:** unique identifier first ("Kick volume", not "Volume — kick"); never include control type in the name; decorative elements excluded from the a11y tree.
7. **Test the invariants automatically; test the experience manually.** The headless a11y scanner catches missing names/IDs/structure; real NVDA/Orca sessions with the maintainer (TD-17 v2 on the desk) and later beta users catch what scanners can't.
8. **Honest per-module support.** Verified-where-we-have-hardware (TD-17 v2), prepared-where-we-don't (TD-27), schema-supported-but-unverified elsewhere. No silent claims.

---

## Interface Decision (revised — owner decision 2)

### The decision

**Retrofit `VDrumExplorer.Gui.Avalonia` into the single accessible UI.** No new front-end project, no console REPL on the main line. (A REPL remains a *possible far-future option* — e.g. as a scripting harness if a future need appears — but nothing in this plan builds it, and no phase depends on it.)

### v1's objections to retrofit, addressed honestly — and how the retrofit solves them within the one UI

v1 rejected retrofit on three grounds. Each is real; each has a concrete in-UI answer:

**Objection 1: "Zero `AutomationProperties` today across 11 axaml views."**
Verified: `grep AutomationProperties` across the repo returns **zero hits**. The actual view inventory (verified by glob) is 8 view files + `App.axaml`: `Views/ExplorerHome.axaml`, `Views/DataExplorer.axaml`, `Views/SchemaExplorer.axaml`, and `Views/Dialogs/{DataTransferDialog, CopyKitTargetDialog, MultiPasteDialog, CopyKitsDialog, ConfirmCloseDialog}.axaml` (v1's "11 views" count was approximate — re-verify the full inventory, including any code-constructed controls, during execution).
**Solution:** systematic annotation pass, view by view, driven by schema `Name`/`Description`:
- `AutomationProperties.Name` — identifier-first, composed from schema ("Kick — Volume" → announced "Kick volume" per conventions doc); `HelpText` from schema `Description` + range text ("Tempo, 20 to 260 BPM").
- `AutomationProperties.AutomationId` on all testable controls — stable hooks for the scanner and later FlaUI/Appium.
- `AutomationProperties.HeadingLevel` on kit-section headers, `LandmarkType` on major regions, `LiveSetting` on the status line, `ItemStatus` on kit-list rows.
- Custom `AutomationPeer` subclasses (`OnCreateAutomationPeer()`) where per-type field controls need richer exposure (budget for this — see Risks).
This is mechanical but large — it is the single biggest cost block of the plan (see Effort summary). It is bounded by the scanner: once a view is annotated, the scanner flips to enforcing for it and stays green.

**Objection 2: "Shortcuts hard-coded in code-behind."**
Verified: `DataExplorer.axaml.cs` handles `KeyDown` manually for Ctrl+C (copy node / copy kit), Ctrl+V (paste node / paste kit), Ctrl+Z (undo), Ctrl+Y (redo), with branching logic between `ModuleExplorerViewModel` and node-level commands.
**Solution:** refactor into declarative `KeyBinding`s in `DataExplorer.axaml`, bound to ViewModel commands. Verified command surface: `CopyNodeCommand`, `PasteNodeCommand`, `MultiPasteCommand`, `CopyKitCommand`, `ImportKitFromFileCommand`, `ExportKitCommand` etc. exist as `ICommand`s on the VMs. **Nuance (verify during execution):** undo/redo exist as *methods* (`ViewModel.Undo()`/`ViewModel.Redo()` with `CanUndo`/`CanRedo` properties), not `ICommand`s — the refactor should expose them as commands (e.g. `UndoCommand`/`RedoCommand` wrapping the existing methods, with `CanExecute` from the existing properties) so `KeyBinding` can bind them and the scanner/UIA can see them as invokable actions. This refactor is a net code-quality win independent of a11y: it moves behavior into the testable ViewModel layer, where `VDrumExplorer.ViewModel.Test` already covers it.

**Objection 3: "The tree+details interaction model is hostile to screen readers."**
This is the deepest objection: hundreds of parameters behind a `TreeView` (`treeView` selection in `DataExplorer.axaml.cs`) with no linear path and no announcements. v1 concluded a purpose-built linear UI was cheaper. The one-UI answer is **not** to delete the tree (sighted users know it; it stays) but to add **in-window accessibility aids** — alternative traversals of the *same* ViewModel state, not parallel features:

1. **Jump-to-field/setting search** — a keyboard-invokable search box (e.g. Ctrl+K or a menu command) that fuzzy-matches schema field names across the current kit ("kick vol" → "Kick — Volume"), moves tree selection to the node and focus to the field. This is the screen-reader user's fast path *and* a power-user feature sighted users gain for free. Directly analogous to REAPER/OSARA's action search.
2. **Flat/linear "field list" mode** — an alternative view of the current kit *within the same window* (e.g. a toggle/tab/pane in `DataExplorer.axaml`): all fields of the current kit as a flat, linear list ordered by schema-tree section, each row a named control with `HelpText` ranges. Screen-reader users navigate it with standard reading keys and headings; no tree interaction required. It binds the *same* `DataFieldViewModel` instances as the tree+details pane, so there is exactly one editing path and zero divergence — this is what makes it compatible with the one-UI philosophy rather than a second front-end.
3. **Heading structure over kit sections** — section headers in the details pane (and in flat mode) carry `HeadingLevel`, so screen-reader heading navigation (H / Shift+H in NVDA/Orca) jumps between Kit common, per-instrument sections, etc.
4. **Live-region status line** — a single status element added to the existing layout, `LiveSetting="Polite"` (assertive variant for errors), bound to a status VM. Every `DeviceController` operation outcome flows through it. Sighted users get a normal status bar; blind users get announcements.

Framing that keeps the philosophy honest: **search and the status bar are unambiguous wins for everyone**; flat mode and heading structure are accessibility aids that also give sighted users a faster "spreadsheet view" of a kit. Nothing is blind-only, nothing is sighted-only, and there is one codebase.

### What we give up vs v1's new-front-end (honest accounting)

- The interaction model is constrained by the existing window structure; flat mode and search are *mitigations* for the tree, not a from-scratch linear design. Expect **iteration with beta users** on whether the retrofit's linear navigation is good enough (Risks table).
- Annotation + interaction rework across all views is substantial — likely comparable in dev-days to v1's new front-end for Phase 1/2 (see Effort summary). What we save is the permanent second-front-end maintenance burden, the parity-checklist overhead, and the "which UI do I recommend?" support question. For a solo maintainer, one codebase is the right long-term trade.

### Accessible interaction model (how functions map to a11y — feeds Phase 1/2)

Unchanged in substance from v1 (it was interface-agnostic); restated with retrofit targets:

| Module function (MIDI-controllable) | Accessible interaction (in the retrofitted UI) | Announcement |
|---|---|---|
| Kit browsing/selection | Kit list rows with `ItemStatus` ("Kit 12, Jazz, modified"); Enter or dedicated key switches via Program Change (channel configurable after Phase 0) | Live region: "Switched to kit 12, Jazz" |
| Current-kit parameter read | Fields as named controls in details pane *and* flat mode; `Name` identifier-first from schema; `HelpText` = `Description` + range | Focus announcement includes name + current value + range |
| Parameter edit (enum/bool/string/numeric/instrument/tempo) | Reuse `DataFieldViewModel` formatted-text round-tripping; keyboard editing; commit on explicit action, not per-keystroke | On commit: "Kick volume set to 80" (polite); on failure: assertive + reason |
| Instrument selection | Instrument picker as searchable list (schema instrument names) | "Selected instrument: X" |
| Note preview (`PlayNote`/`Silence`) | Dedicated preview key on focused pad/instrument | "Previewing note 38" |
| Kit/module load & save (`LoadKitAsync`, `SaveDescendants` DT1 writes) | Explicit commands with progress announcement | "Loading kit…", "Kit loaded in 12 s", "Write complete" |
| Device state changes | Status line bound to status VM; Phase 2 adds device-side kit-change event/poll | "Module reports kit changed to 7" |
| Undo/redo | `UndoCommand`/`RedoCommand` (new, wrapping existing methods) via `KeyBinding`; Phase 2 defines device-write semantics | "Undid: Kick volume restored to 75" |
| Fast navigation | Jump-to-field search (Ctrl+K or menu); heading navigation; flat field-list mode | Focus moves; field announced on arrival |
| Onboarding friction (vendor USB driver mode, Receive Exclusive = On) | Spoken setup guide (Phase 3 docs + in-app first-run checklist) | Step-by-step announced instructions |

---

## Per-module support matrix (owner decision 4 — honest status)

| Module | Schema in repo | Accessibility status | Verification path | Docs label |
|---|---|---|---|---|
| **TD-17 v2** | ✅ (v1 + v2 schemas; MIDI Implementation PDFs verified in research) | **Primary target.** All manual NVDA/Orca smoke tests and beta testing run against owner-owned hardware from Phase 1 onward | Maintainer on real hardware, every phase | Fully supported; accessibility verified on hardware |
| **TD-27** | ✅ (MIDI Implementation + Data List PDFs verified in research) | **Secondary.** Schema audit closure + code paths exercised via fake MIDI; no hardware in hand | Future tester (recruit after TD-17 v2 works — Phase 2/3) | Supported; accessibility pending tester verification |
| **TD-07** | ✅ | Schema-supported; **accessibility unverified** | None planned | Experimental (accessibility unverified) |
| **TD-50** | ✅ | Schema-supported; **accessibility unverified** | None planned | Experimental (accessibility unverified) |
| **TD-50X** | ✅ | Schema-supported; **protocol least battle-tested** (Skeet's own alpha notes: initially "may well be entirely broken", later fixed) **and accessibility unverified** | Opportunistic only — if hardware ever appears (owner, beta recruit, or community loaner), run the structured read/write/kit-switch/load pass from v1's Phase 2; otherwise the label stands | **Experimental, unverified** |
| **AE-01 / AE-10** | ✅ (reverse-engineered — no Roland MIDI impl doc exists) | Schema-supported; **accessibility unverified**; protocol itself is reverse-engineered | None planned | Experimental (accessibility unverified) |

Rule: **docs never claim more than verification.** The release notes and setup guide carry this matrix verbatim (Phase 3).

---

## Phases

Sizing assumes a **solo maintainer working part-time**; "dev-days" = focused working days. Phases are sequential, but tasks marked 🔀 are parallelizable within a phase.

---

### Phase 0 — Foundations: scanner baseline, conventions, MIDI de-risking (~2–3 weeks part-time, ~8–10 dev-days)

**Goal:** Build the machinery that proves accessibility stays accessible, quantify the retrofit gap precisely, and de-risk the MIDI layer — all before touching views.

**Tasks:**

1. **A11y invariant scanner (headless, every PR)** — owner requirement (a)
   - New test utility (e.g. `A11yScanner`) in/next to `VDrumExplorer.Gui.Avalonia.Test` (which already uses `Avalonia.Headless.XUnit` `[AvaloniaFact]`): walks the visual tree via `AutomationPeer`s and asserts invariants.
   - **Invariants:**
     - Every focusable/interactive control has a non-empty, meaningful `AutomationProperties.Name` (or visible text content).
     - Names follow identifier-first rule; **fail if name contains control-type words** ("button", "slider", "check box") — small blocklist.
     - `AutomationProperties.AutomationId` present on all testable controls.
     - Exactly one live-region status element (`LiveSetting` Polite) per window; errors use Assertive.
     - Section headers carry `HeadingLevel`; major regions carry `LandmarkType`.
     - Decorative elements excluded from the accessibility view (`AccessibilityView="Raw"` or not focusable).
     - Tab order visits controls in logical order (no focus traps; all interactive controls reachable).
   - **Report-only mode first:** run against the existing GUI as-is → produces the **violation inventory that becomes the retrofit backlog** (per-view counts feed Phase 1/2 task sizing). Enforcing mode is adopted per-view as views are retrofitted (Phase 1/2).
   - **CI coverage-gate decision:** a11y/headless tests count toward coverage (they execute real view code-behind). Verify how the per-project coverage config identifies projects (verify the coverage workflow's project-filter mechanism) before touching the gate; scanner tests live in the existing test project, so gate impact should be minimal — confirm.
2. **A11y conventions doc** — `docs/accessibility.md`: naming rules (identifier-first, no control-type words), `AutomationId` scheme, live-region policy, keyboard-nav rules, how schema `Name`/`Description` map to `AutomationProperties`. Every Phase 1+ PR reviews against it.
3. **Configurable MIDI channel** — remove the hard-coded channel 10: per-connection setting (module schema already knows the module; default stays 10). Touches `DeviceController` construction paths; update `DeviceController` fake-MIDI NUnit tests.
4. 🔀 **Kit-change notification spike (TD-17 v2 first — owner has the hardware)**
   - Today current kit is pull-only. On the TD-17 v2: listen for incoming Program Change / kit-change SysEx on MIDI input while connected; **does the TD-17 v2 transmit Program Change on panel kit-change?** (Open question — the spike answers it empirically.) If yes: event on `IDeviceController` → status VM → live region. If no: document poll fallback (periodic lightweight `GetCurrentKitAsync` name read, announced only on change).
   - Deliverable: written TD-17 v2 finding + event/poll design; implementation lands in Phase 2. TD-27 behavior noted as "verify with tester" (research says TD-17/27/50 behavior differs — check MIDI Implementation docs for TD-27 in parallel).
5. 🔀 **Schema-vs-Roland-docs audit, TD-17 v2 priority** — owner requirement (b)
   - **Priority: TD-17 v2** (primary target): diff the embedded TD-17 v2 JSON schema field coverage against the TD-17 MIDI Implementation + Data List address maps (URLs verified in research, incl. the v2-firmware revision). File gaps (e.g. MFX/master effects, system setup, trigger settings) as issues; close in Phase 2.
   - **Second: TD-27** (secondary target, future tester): same audit so the module is ready when a tester appears.
   - **Lower priority / opportunistic:** TD-07, TD-50, TD-50X, AE-01/AE-10 — audit only as far as needed to write honest docs; TD-50X doc locations still need finding (verify during execution; TD-17/TD-27 URLs already verified).
6. 🔀 **CI plumbing** — headless a11y tests run in the existing 3-OS matrix; decide windows-latest optional job wiring (FlaUI) now, enable in Phase 3.

**Definition of done:**
- `A11yScanner` exists, runs in CI on every PR, and has produced a committed report-only violation inventory for the existing GUI (the retrofit backlog, per view).
- MIDI channel configurable with passing `DeviceController` tests; default behavior unchanged.
- Written TD-17 v2 kit-change finding (transmits PC on panel change: yes/no) + event-vs-poll design decision.
- TD-17 v2 schema gap list exists as issues (TD-27 second; other modules opportunistic).
- **Visual baselines green** (`DataExplorerVisualTest`, `ExplorerHomeVisualTest`, `SchemaExplorerVisualTest`, `DialogVisualTest` untouched and passing — Phase 0 changes no views).
- Coverage gate green; `dotnet format --verify-no-changes` clean.

**Risks:** scanner over `AutomationPeer`s headlessly may not surface every property identically to platform UIA/AT-SPI — cross-check a sample with FlaUI in Phase 3; coverage-gate config may need workflow edits (verify mechanism first).

---

### Phase 1 — Retrofit core views: ExplorerHome + DataExplorer (~5–7 weeks part-time, ~18–24 dev-days)

**Goal:** In the **existing** UI, a blind user with a screen reader can connect to the TD-17 v2, browse kits, switch kits, and read/edit parameters of the current kit — the core loop — while sighted users see (nearly) the same UI they always did.

**Tasks:**

1. **`ExplorerHome.axaml` retrofit** — connection/module selection:
   - `AutomationProperties.Name`/`AutomationId` on all controls (connection fields, module picker, load commands); `HelpText` where the purpose isn't obvious from the name.
   - Announce connection progress/outcomes via the new status line (task 5).
   - Visual baselines for `ExplorerHomeVisualTest` stay green (annotation is visually neutral; if any visible text/label changes, re-baseline deliberately with review).
2. **`DataExplorer.axaml` retrofit — tree + details annotation**:
   - Tree items: accessible names from schema node names; `ItemStatus` where state matters (modified flags).
   - Details pane: section headers get `HeadingLevel`; every field control gets schema-derived `Name` + `HelpText` (range/enum text); per-type `DataFieldViewModel` controls (Enum/Boolean/String/Numeric/Instrument/Tempo) rendered accessibly — custom `AutomationPeer`s where needed (verify which types need them during execution).
   - Instrument picker as searchable list with announced selection.
3. **Keyboard refactor: code-behind → `KeyBinding`s**:
   - Move Ctrl+C/V/Z/Y handling out of `DataExplorer.axaml.cs` `KeyDown` into declarative `KeyBinding`s bound to VM commands (`CopyNodeCommand`, `PasteNodeCommand`, and new `UndoCommand`/`RedoCommand` wrapping the existing `Undo()`/`Redo()` methods with `CanUndo`/`CanRedo` as `CanExecute` — verify exact VM surface during execution).
   - Preserve the copy-kit-vs-copy-node branching semantics (currently in code-behind) — move that logic into the ViewModel layer (e.g. a `CopyCommand` that dispatches on `ModuleExplorerViewModel` + `IsKitRoot`), covered by `VDrumExplorer.ViewModel.Test`.
   - Every action gains a keyboard path: kit switch, save, load, preview note, jump-to-section. Visible focus adorner (`:focus-visible` styling) — verify it doesn't shift visual baselines.
4. **Linear-navigation aids in `DataExplorer`** (the one-UI answer to the tree):
   - **Jump-to-field search** (Ctrl+K or menu command): fuzzy match over schema field names of the current kit; result moves tree selection + focuses the field. Keyboard-first, announced on arrival.
   - **Flat field-list mode**: toggle/pane within the same window listing all current-kit fields linearly by section, bound to the same `DataFieldViewModel` instances (one editing path, zero divergence). Headings per section.
   - **Heading structure** over kit sections in the details pane.
   - Design note: exact UX placement (toggle vs tab vs split pane) is an implementation decision — pick the least visually disruptive option that keeps baselines sane; iterate with beta feedback in Phase 3.
5. **Status line** — single live-region element added to the `DataExplorer` (and `ExplorerHome`) layout, bound to a status VM; every `DeviceController` operation outcome flows through it (polite success, assertive errors). Sighted users see a normal status bar.
6. **Scanner enforcing for touched views** — flip `A11yScanner` to enforcing for `ExplorerHome` and `DataExplorer` once annotated; invariants green in CI from then on.
7. 🔀 **Core-flow headless interaction tests** — extend the existing `[AvaloniaFact]` pattern: simulated keyboard navigation through kit list → field edit → commit, asserting focus order and status-VM announcements (speech itself is Phase 3/manual).
8. 🔀 **TD-17 v2 MIDI gap closure (start)** — begin closing Phase 0 audit gaps for TD-17 v2 (primary target).

**Manual test target (owner, real hardware):** TD-17 v2 + NVDA (Windows) and Orca (Linux) smoke test: connect → list kits → switch kit → read a field → edit a field → hear confirmation. Recorded as a manual-test checklist result. **Start beta-user recruitment now** (Phase 3 task, but lead time is real).

**Definition of done:**
- With a screen reader (maintainer smoke on Linux/Orca + Windows/NVDA against the TD-17 v2): connect → list kits → switch kit → read a field → edit a field → hear confirmation. Checklist recorded.
- `A11yScanner` enforcing and green for `ExplorerHome` + `DataExplorer` on every PR; report-only elsewhere.
- **Visual baselines green** for all four visual test classes (any re-baselines deliberate and reviewed).
- All shortcuts are `KeyBinding`s; `DataExplorer.axaml.cs` contains no key-handling logic; undo/redo exposed as commands with VM tests.
- Jump-to-field search and flat field-list mode exist, keyboard-operable, and covered by headless interaction tests.
- Kit switch, field read, field edit flows covered by headless interaction tests.
- TD-17 v2 schema gaps from the audit either closed or filed with concrete address-map references.
- Coverage gate green; `dotnet format --verify-no-changes` clean.

**Risks:** Avalonia AT-SPI2 quality on Linux is self-reported — real-Orca smoke testing happens *in this phase*, not deferred; per-type field controls may need custom `AutomationPeer`s (budget for it); flat-mode/search UX may need iteration with a real user; retrofitting annotations may surface latent binding/visual quirks — fix forward, keep baselines green.

---

### Phase 2 — Remaining views, device feedback, undo/redo semantics, TD-17 v2 gap closure, TD-27 prep (~4–6 weeks part-time, ~15–20 dev-days)

**Goal:** The retrofitted UI covers everything the app does today, plus device-state feedback the app never had; TD-17 v2 is fully closed out; TD-27 is ready for a tester.

**Tasks:**

1. **Remaining views/dialogs retrofit** — `SchemaExplorer.axaml`, and dialogs `CopyKitTargetDialog`, `CopyKitsDialog`, `MultiPasteDialog`, `DataTransferDialog`, `ConfirmCloseDialog` (verified inventory; re-verify for any code-constructed controls during execution): full annotation, keyboard paths, announcements, scanner enforcing per view. Includes module load (`LoadModuleAsync`), kit load/save, `.vkit`/`.vdrum` import/export, note preview (`PlayNote`/`Silence`).
2. **Device-state feedback** — implement the Phase 0 kit-change design: PC listener where the module transmits (TD-17 v2 answer known by now), poll fallback otherwise; device connect/disconnect events; all surfaced via the live-region status line. Extend `IDeviceController` (and fake-MIDI tests) accordingly.
3. **Undo/redo device-write semantics** — memory-only undo exists; define device-write semantics:
   - Recommended: **undo = re-write previous value via DT1** (`SaveDescendants`/`SetInstrumentAsync` path) for field edits within a session; undo stack records field address + prior value; announce "Undid: X restored to Y".
   - Explicitly out of scope: undoing kit switches (Program Change), undoing bulk loads. Document this.
   - Risk flag: write-timing heuristic (40 ms delay) makes rapid undo/redo sequences fragile — batch/queue writes; verify timing on the TD-17 v2 (real hardware in hand — this is now cheap to test).
4. **MFX / schema-gap closure for TD-17 v2** — close the Phase 0 audit gaps (master effects/MFX, system setup, trigger settings as applicable), exposed as accessible sections (headings + named fields in details pane and flat mode).
5. **Configurable MIDI channel UI** — surface the Phase 0 setting in the connection screen (`ExplorerHome`).
6. 🔀 **TD-27 prep** — close the TD-27 schema audit gaps (Phase 0 second priority) so the module is tester-ready; exercise its code paths via fake MIDI. No hardware claims in docs until a tester verifies.
7. 🔀 **TD-50X opportunistic check** — no hardware (owner decision 1): keep the label "experimental, unverified". If hardware ever appears (community loaner, future beta recruit), run a structured read/write/kit-switch/load pass then. No scheduled task; mitigation is honest labeling (docs, Phase 3).

**Definition of done:**
- Feature parity checklist (all pre-pivot flows vs retrofitted UI) complete and checked off; every flow has keyboard path + announcement + scanner-green view.
- **All 8 views + dialogs scanner-enforcing and green**; report-only mode retired.
- Device-side kit change (event or poll) announced in the UI; `IDeviceController` tests cover it with fake MIDI; TD-17 v2 behavior confirmed on hardware.
- Undo/redo works for field edits including device re-write, with documented scope; tests cover stack behavior and the write-queue; burst undo/redo tested on the TD-17 v2.
- TD-17 v2 audit gaps closed; TD-27 audit gaps closed (tester-ready).
- **Visual baselines green** across all views (re-baselines deliberate and reviewed).
- Coverage gate green across all touched projects.

**Risks:** undo-via-rewrite timing on hardware (interact with the 40 ms heuristic — make the delay configurable/adaptive); parity scope creep — the parity checklist is the guardrail; dialog retrofits may expose `IViewServices` dialog-flow quirks — test via existing dialog VM tests + headless interaction tests.

---

### Phase 3 — Docs, blind-user beta, CI hardening, release (~3–4 weeks part-time + ongoing, ~10–12 dev-days)

**Goal:** Ship to real blind/VI users and make the quality bar durable. No upstream coordination (owner decision 3).

**Tasks:**

1. **Onboarding documentation** — spoken-friendly setup guide: vendor USB driver mode, Receive Exclusive = On, connecting, first kit switch (TD-17 v2 steps first and most detailed; TD-27 next; other modules per the support matrix). Plain-text/HTML accessible docs; in-app first-run checklist with announced steps. **Include the per-module support matrix verbatim** — TD-50X explicitly "experimental, unverified".
2. **Blind-user beta program** — recruit 2–5 blind/VI drummers (channels from research: Sound Without Sight community; vdrums.com; MIDI Association networks). **TD-17 v2 testers first** (owner hardware validates the flow end-to-end before recruiting). **No active recruitment for other modules (owner decision 5):** TD-27 and other-module hardware verification is community-driven — users with those modules test and file issues; the support matrix and docs make each module's verification status explicit so expectations are honest. Structured feedback rounds on: onboarding, kit switching, editing, announcements quality, naming conventions, and specifically **whether the flat-mode/search linear navigation is good enough** (the key retrofit-risk question). Iterate based on findings. (Recruitment starts in Phase 1.)
3. **Manual screen-reader test protocol** — documented NVDA (Windows) + Orca (Linux) test scripts run pre-release; results recorded. Orca has no headless CI story — this stays manual by design.
4. **Optional CI hardening (windows-latest job)** — FlaUI UIA3 tests asserting the same scanner invariants via the real Windows UIA tree (what NVDA consumes); optionally NvdaTestingDriver for spoken-output capture on a couple of golden flows. Mark job `continue-on-error` initially; promote to required once stable.
5. **Community announcement** (replaces v1's upstream-coordination task): release announcement post in Sound Without Sight, vdrums.com, and MIDI Association channels, with the support matrix and honest per-module status. **Optional:** contact Roland with the gap evidence (they engaged accessibility consultants for V-STAGE — Jason Dasent) as a gap report; keep this optional and low-effort — it is advocacy, not a plan dependency.
6. **Release** — tagged release with accessible installer/docs.

**Definition of done:**
- Setup guide published (with support matrix) and reviewed by at least one blind beta user for completeness.
- ≥2 blind/VI beta testers have completed the core loop (connect → switch kit → edit → save) on a TD-17 v2 with feedback logged; top-severity findings fixed. (TD-27 tester feedback is a bonus, not a gate.)
- Manual NVDA + Orca test protocol executed and recorded for the release.
- FlaUI optional CI job running (passing or explicitly `continue-on-error` with known issues filed).
- **Visual baselines green**; scanner enforcing on all views; release published and announced.

**Risks:** beta recruitment/hardware logistics (testers need the right module + USB + OS combo) — mitigate by recruiting across module types early, TD-17 v2 first; feedback volume may exceed solo bandwidth — triage by core-loop impact.

---

## Effort summary (retrofit vs v1's new front-end)

| Phase | Duration (part-time solo) | Focused dev-days | Shippable outcome |
|---|---|---|---|
| 0 | 2–3 weeks | 8–10 | Scanner + retrofit backlog inventory + MIDI de-risking + TD-17 v2 audit |
| 1 | 5–7 weeks | 18–24 | Retrofitted core (ExplorerHome + DataExplorer): accessible core loop, verified on TD-17 v2 |
| 2 | 4–6 weeks | 15–20 | Full parity + device feedback + undo semantics + TD-17 v2 closure + TD-27 prep |
| 3 | 3–4 weeks + ongoing | 10–12 | Beta-validated, documented, released |
| **Total** | **~4–5.5 months part-time** | **~51–66** | |

**Honest comparison with v1 (~48–62 dev-days):** the retrofit is **slightly more expensive in Phase 1/2 execution** — annotating every control across all views, the code-behind→KeyBinding refactor, and building the in-window linear-navigation aids (search + flat mode) inside an existing window structure costs more than designing them fresh. Roughly: v1's new front-end was ~15–20 dev-days for a core loop; the retrofit core is ~18–24. What the retrofit **saves** is structural and permanent: no second front-end to maintain, no parity checklist between two UIs, no "frozen GUI in maintenance mode" drift, no divergent-feature risk (the owner's exact concern), and the visual-baseline tests already protect the sighted UX. For a solo maintainer, one codebase with slightly higher upfront cost beats two codebases with permanent overhead. Net: **~51–66 dev-days vs ~48–62 — a modest premium (~5–10%) bought with eliminated long-term maintenance duplication.**

Each phase still ends in something independently valuable: Phase 0 = durable quality machinery + a quantified retrofit backlog; Phase 1 = a blind user can already do the most important thing on a TD-17 v2 (switch kits, tweak sounds) in the same UI everyone uses; Phase 2 = the retrofitted UI fully replaces the old capability set; Phase 3 = community-validated release.

---

## Risks & mitigations (consolidated)

| Risk | Severity | Mitigation |
|---|---|---|
| **Single-UI retrofit regresses sighted UX** — a11y changes (focus styling, layout additions like status line/flat mode, re-baselining) could break what sighted users see | High | **Visual baseline tests are the guardrail** (`DataExplorerVisualTest`, `ExplorerHomeVisualTest`, `SchemaExplorerVisualTest`, `DialogVisualTest`): "baselines green" is a definition-of-done item in every phase; any re-baseline is deliberate and reviewed; a11y aids framed as universal wins (search, status bar) |
| **Tree+details retrofit may still fall short** — flat mode + search are mitigations, not a from-scratch linear design; interaction model may need iteration | High | Beta users from Phase 1 specifically evaluate linear-navigation adequacy; flat mode binds the same VMs so redesigning it later is cheap; worst case, flat mode can be promoted to the default view within the same window — still one UI |
| **TD-50X protocol maturity** — least battle-tested (Skeet's own release notes); **no hardware access** (owner decision 1) | Medium (given honest labeling) | No verification task; label "experimental, unverified" in docs and support matrix; opportunistic structured pass only if hardware ever appears |
| **Write-timing heuristic (40 ms delay)** — rapid edits/undo sequences may drop or corrupt writes | High | Make delay configurable/adaptive; queue/serialize DT1 writes; test undo/redo bursts on the TD-17 v2 (hardware in hand — cheap to verify now) |
| **Avalonia AT-SPI2 (Linux) quality is self-reported** — Orca experience unproven in the field | High | Real-Orca smoke tests in Phase 1 (not deferred); FlaUI/UIA cross-check in Phase 3; Linux is primary platform so this cannot be skipped |
| **Real screen-reader testing needs real users + hardware** | Medium | Owner has TD-17 v2 (primary target covered); start beta recruitment in Phase 1; maintainer does NVDA/Orca smoke tests meanwhile; manual protocol documented |
| **Solo-maintainer bandwidth** — retrofit + protocol work + beta coordination | Medium | One codebase (no second front-end, no REPL, no parity-between-UIs overhead); phases independently shippable; parity checklist (old flows vs retrofitted UI) prevents scope creep |
| **Coverage gate interaction** — scanner/interaction tests could shift coverage numbers | Low–Medium | Tests live in the existing `VDrumExplorer.Gui.Avalonia.Test` project (no new project → no new gate entry); verify the coverage workflow's project-filter mechanism in Phase 0 anyway |
| **Modules may not transmit kit-change notifications** (pull-only current kit) | Medium | Phase 0 spike on the TD-17 v2 answers empirically; poll fallback announced only on change; TD-27 behavior verified community-driven (issue reports) |
| **MIDI channel hard-coded to 10 in some paths** | Medium | Phase 0 makes it configurable; default unchanged; tests updated |
| **Onboarding friction (vendor driver mode, Receive Exclusive)** — blind users can't navigate module menus to enable these | Medium | Spoken setup guide + in-app checklist (Phase 3); consider detecting "connected but silent" state and announcing the likely cause |
| **No prior art for accessible V-Drums editors** — interaction-model guesses may miss real user needs | Medium | Beta users from Phase 1; iterate; lean on OSARA/REAPER and HISE-thread practitioner rules as proxies |
| **Schema audit may reveal large gaps** (MFX, system, trigger sections) | Low–Medium | Audit quantifies in Phase 0 (TD-17 v2 first); gaps closed per-module by priority; honest docs about coverage per module |

*Removed from v1:* two-front-end maintenance burden (no second front-end), REPL scope risk (no REPL), upstream-relations risk (no upstream coordination).

---

## Open questions

1. **Does the TD-17 v2 transmit Program Change on panel kit-change?** — Phase 0 task 4 answers this empirically on the owner's hardware; determines event-vs-poll design for device feedback.
2. **Linear-navigation sequencing — RESOLVED (owner accepted default):** **search first** (jump-to-field search — smaller to build, universally useful), flat field-list mode second. Both remain Phase 1 deliverables; if bandwidth forces sequencing, search lands first.
3. **Flat-mode UX placement** — toggle vs tab vs split pane within `DataExplorer` (implementation decision in Phase 1; pick the least visually disruptive option that keeps baselines sane).
4. **`VDrumExplorer.Blazor` fate — RESOLVED (owner decision):** the Blazor project is **removed entirely** (owner: "just remove the blazor project, I don't really care about it"). Executed on branch `remove-blazor`: project directory, solution entry, readme/NOTICE attribution mentions, byte-base64 license file, coverage exclusions, and dead dependabot ignore rules all cleaned; build + `dotnet format` verified green.
5. **Coverage-gate mechanics** — how does the CI coverage config enumerate projects? (Phase 0, task 1 — lower risk now that no new project is created, but verify anyway.)
6. **Exact VM command surface for the KeyBinding refactor** — undo/redo are methods today (`Undo()`/`Redo()` + `CanUndo`/`CanRedo`), copy/paste are commands; confirm the full command/method inventory on `DataExplorerViewModel`/`ModuleExplorerViewModel`/`KitExplorerViewModel` when executing Phase 1 task 3.
7. **TD-27 tester — RESOLVED (owner decision 5):** no active recruitment. Other-module hardware verification is community-driven: users with TD-27 (or any other module) test and file issues. The plan only preps TD-27 via the schema audit (no hardware needed); docs carry the honest per-module status so community testers know what to expect.

---

## Verification steps explicitly deferred (do not invent — verify during execution)

- Full axaml view/control inventory including code-constructed controls (research-verified count is 8 view files + App.axaml; v1 said "11" — reconcile during Phase 0 scanner baseline, which will enumerate everything precisely).
- Exact coverage-workflow project-filter mechanism (Phase 0).
- Per-module PC-transmission behavior — TD-17 v2 empirically in Phase 0; TD-27 from docs now, hardware later.
- TD-50X MIDI Implementation / Data List PDF locations (Phase 0 audit, opportunistic; TD-17/TD-27 URLs already verified in research).
- Which `DataFieldViewModel` per-type controls need custom `AutomationPeer`s (Phase 1).
- Whether `:focus-visible` styling or status-line/flat-mode layout shifts any visual baseline (Phase 1 — if so, deliberate re-baseline with review).
- Full undo/redo/copy/paste command/method surface on the explorer VMs (Phase 1 task 3).
