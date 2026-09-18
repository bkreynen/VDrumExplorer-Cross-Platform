# Accessibility conventions

**Status:** normative for all Phase 1+ PRs.
**Source plan:** `.agents/plans/2026-09-18-1300-accessibility-pivot-plan-v2.md` (see especially the *Guiding principles* and *Interface Decision* sections).
**Applies to:** `VDrumExplorer.Gui.Avalonia` (Avalonia 12.1.1, .NET 10). Property names below are verified against the Avalonia 12 documentation; do not invent API names — check the plan's *Verification steps* list first.
**Automated enforcement:** `A11yScanner` in `VDrumExplorer.Gui.Avalonia.Test/A11y/` (built in parallel with this document; see §8 for how it is used).

---

## 1. Purpose

VDrumExplorer has **one** UI — the Avalonia GUI — used by both blind and sighted drummers. There is no second front-end, no "accessible mode", and no blind-only features. Every accessibility improvement lands in the same views everyone uses; every improvement must also be a reasonable experience for sighted users (and the existing visual-baseline tests enforce exactly that).

The core idea that makes this tractable: **the schema text is the TTS layer.** Every field in the module schemas (`VDrumExplorer.Model/SchemaResources/`) already carries a human-readable `Description` plus typed metadata (min/max, enum value lists, string lengths). Accessible names, help text, and announcements are *derived* from that schema — never hand-written per parameter and never duplicated in the UI layer.

This document defines the conventions every Phase 1+ pull request is reviewed against. It is versioned with the code and changed via PR like any other file.

## 2. Naming rules

### 2.1 Identifier-first

A screen reader announces a control's name as one sentence, so the **unique identifier comes first** and the adjustable aspect follows:

| ✅ Good          | ❌ Bad            | Why                                                     |
|-----------------|-------------------|---------------------------------------------------------|
| `Kick volume`   | `Volume — kick`   | Reader must hear the identifier *before* it can act     |
| `Snare head tuning` | `Tuning (Snare Head)` | Parenthetical context comes too late in speech   |
| `Kit 12, Jazz`  | `Jazz (kit 12)`   | Number-first matches the module's own ordering          |

Context identifiers come from the schema's own repeat lookups and logical tree — pads (`Kick`, `Snare`, `Tom 1`, `Hi-Hat`, …), trigger zones (`Kick Head`, `Snare Rim`, …), kit numbers, and section names (`Kit common`, `Kit ambience`, `Kit Reverb`, `Kit MultiFx`). These are already in the schemas; do not invent new context labels in views.

### 2.2 Never name the control type

Control-type words are redundant — the screen reader already announces the control's role from the automation peer. **Names must not contain "button", "slider", "check box", "combo box", "text box", "list", "tree", "tab", or "menu"** (case-insensitive). The scanner enforces this with a blocklist (§8).

| ✅ Good              | ❌ Bad                        |
|---------------------|-------------------------------|
| `Copy kit`          | `Copy kit button`             |
| `Kick volume`       | `Kick volume slider`          |
| `Sub instrument on/off` | `Sub instrument check box` |

### 2.3 Names are composed from the schema

The accessible `Name` is derived from the schema field's `Name`/`Description` plus its container context:

- **Schema field:** TD-17 `KitCommon` → `"Kit volume"`, `"Kit name"`, `"Pedal HH volume"`; Kick VEdit → `"Tuning"`, `"Muffling"`, `"Snare buzz"`; `KitMidi` → `"Kick note"`.
- **Container context:** TD-17 `pads` lookup (`Kick`, `Snare`, `Tom 1`, …) and logical section descriptions (`Kit common`, `Instruments`, …).

When the context is not already part of the field description, the context identifier is prefixed: a `Tuning` field (-100…100) inside the **Kick** pad's VEdit section becomes **`Kick tuning`**, not `Tuning`. When the schema description already carries the context (`Kit volume`), use it verbatim.

If a view displays a label in a different visual format (e.g. `Kick — Volume` for sighted scanning), the visible label stays, but `AutomationProperties.Name` overrides it with the identifier-first form (`Kick volume`). Screen-reader text is composed, not copied from screen text.

## 3. `AutomationProperties` mapping

Every focusable/interactive control is annotated. The table below is the canonical mapping; the scanner's invariants (§8) mirror it.

| Schema / state source                            | Automation property                          | Example                                            |
|--------------------------------------------------|----------------------------------------------|----------------------------------------------------|
| Field description + container/pad context        | `AutomationProperties.Name`                  | `Kick volume`, `Tempo`, `Kick note`                 |
| Field `Description` + min/max/enum range         | `AutomationProperties.HelpText`              | `Tempo, 20 to 260 BPM`, `Tuning, -100 to 100`, `Muffling: Off, Tape 1, Tape 2, …` |
| Stable structural hook (§3.1)                    | `AutomationProperties.AutomationId`          | `data-explorer.details.kit-common.kit-volume`       |
| Kit-section headers (details pane and flat mode) | `AutomationProperties.HeadingLevel`          | `2` for `Kit common`, `3` for per-pad sections       |
| Major regions (tree pane, details pane, status line, toolbars) | `AutomationProperties.LandmarkType` | `Main` on details pane, `Navigation` on tree        |
| Status line / error line                         | `AutomationProperties.LiveSetting`           | `Polite` (§6)                                       |
| Kit-list / tree rows with state                  | `AutomationProperties.ItemStatus`            | `Kit 12, Jazz, modified`                            |
| Decorative images, separators, spacer elements   | `AutomationProperties.AccessibilityView="Raw"` | — (excluded from the a11y tree)                  |

Notes:

- `HelpText` for range fields is `"<description>, <min> to <max><suffix>"`; enum fields list the allowed values (the full list is fine — screen readers let users re-read it); boolean fields use the schema description alone (`"Sub instrument on/off"`). Volume fields use the module's existing 0–100 formatting; tempo uses BPM.
- `HeadingLevel` uses numeric values 1–6 in axaml (verified: `AutomationProperties.HeadingLevel="2"`). Window title is level 1; kit sections are level 2; per-pad sub-sections are level 3. Heading navigation (H / Shift+H in NVDA/Orca) is a primary traversal, so heading structure must be complete and ordered.
- `LandmarkType` values follow the Avalonia accessibility docs (`Main`, `Navigation`, `Search`, …). Landmarks let screen-reader users jump between the tree, the field areas, and the status line without tabbing through everything.
- Where the per-type field controls (enum/instrument/tempo) cannot express enough through plain properties, add custom `AutomationPeer` subclasses via `OnCreateAutomationPeer()` — this is budgeted in the plan (Phase 1 task 2). The property mapping above still holds for everything else.

### 3.1 `AutomationId` scheme

Pattern: **`<view>.<area>.<control>`**, all segments kebab-case.

- `<view>` — the axaml file name, e.g. `explorer-home`, `data-explorer`, `schema-explorer`, `data-transfer-dialog`.
- `<area>` — a stable region name within the view: `tree`, `details`, `flat-fields`, `kit-list`, `search`, `status`, `toolbar`.
- `<control>` — the semantic control role, or, for schema-driven field controls, the schema-derived field identity.

Examples:

```xml
<!-- Hand-assigned, non-schema controls -->
<Button AutomationProperties.AutomationId="explorer-home.connect-button"
        Content="Connect" />
<TextBox AutomationProperties.AutomationId="data-explorer.search.field-input" />

<!-- Schema-driven field controls: schema identity, not runtime state -->
<TextBox AutomationProperties.AutomationId="data-explorer.details.kit-common.kit-volume"
         AutomationProperties.Name="Kit volume"
         AutomationProperties.HelpText="Kit volume, 0 to 100" />
```

Rules:

- **Never include runtime indices or values in an `AutomationId`** — no kit numbers, trigger indices, or formatted values. The instance context lives in `Name`/`ItemStatus`; the ID is a stable template (`data-explorer.details.kit-common.kit-volume` is valid for every kit's field of that path). Repeated items (tree nodes, kit rows, pad rows) may share one template ID per item role; the scanner treats template-ID repeats as valid.
- IDs are stable across visual refactors — renaming a ViewModel property or restyling a view must not change IDs. If an ID must change, it is a deliberate, reviewed change (like a visual re-baseline).
- IDs are assigned once in axaml and are what the scanner and future FlaUI/Appium suites hang off.

## 4. Keyboard navigation

- **Every action has a keyboard path.** If an operation exists in the UI (kit switch, save, load, copy/paste, note preview, jump-to-section, connection, dialogs), it is reachable and invokable from the keyboard without a pointer. This is a scanner and reviewer invariant.
- **Shortcuts are declarative `KeyBinding`s**, bound to ViewModel `ICommand`s:

  ```xml
  <Window.KeyBindings>
      <KeyBinding Gesture="Ctrl+C" Command="{Binding CopyNodeCommand}" />
      <KeyBinding Gesture="Ctrl+V" Command="{Binding PasteNodeCommand}" />
      <KeyBinding Gesture="Ctrl+Z" Command="{Binding UndoCommand}" />
      <KeyBinding Gesture="Ctrl+K" Command="{Binding OpenFieldSearchCommand}" />
  </Window.KeyBindings>
  ```

- **Never handle keys in code-behind.** The retrofit specifically removes the `KeyDown` handling in `DataExplorer.axaml.cs` (Ctrl+C/V/Z/Y). No Phase 1+ PR may add a new `KeyDown`/`KeyUp` handler for app commands; if a binding needs logic (e.g. copy-node vs copy-kit dispatch), that logic belongs in a ViewModel command, covered by `VDrumExplorer.ViewModel.Test`.
- Actions implemented as methods today (`Undo()`/`Redo()`) are exposed as commands (`UndoCommand`/`RedoCommand`, `CanExecute` from `CanUndo`/`CanRedo`) so `KeyBinding`, the scanner, and UIA can all see them as invokable actions.
- **Visible focus:** every interactive control shows where focus is when focused via keyboard, via `:focus-visible` styling (Avalonia 12 pseudo-class) in `Styles/`. A control whose custom template hides the focus indicator is a defect.

  ```xml
  <Style Selector="Button:focus-visible">
      <Setter Property="BorderBrush" Value="{DynamicResource SystemAccentColor}" />
      <Setter Property="BorderThickness" Value="2" />
  </Style>
  ```

- **Tab order is logical:** reading order == tab order. All interactive controls are reachable by Tab (no focus traps); containers that only group visually are not tab stops. Any `TabIndex` usage must be justified in the PR description.
- Dialogs: initial focus lands on the first interactive control; the dialog closes on the keyboard path that opened it (Esc for cancel).

## 5. Announcement policy (REAPER+OSARA model)

Every operation the app performs is announced through the live-region status line. Silence is a bug.

- **Every successful operation** → a `Polite` live-region update. The user hears the outcome without being interrupted mid-task.
- **Every failure** → an `Assertive` announcement with the **reason** ("Could not save kit: module connection lost"), never a bare "Error" or an empty failure.
- Announcement text is **concise, identifier-first, and includes the new value on commit**:

  | Event                                | Announcement                          |
  |--------------------------------------|---------------------------------------|
  | Field commit                         | `Kick volume set to 80`               |
  | Undo                                 | `Undid: Kick volume restored to 75`   |
  | Kit switch                           | `Switched to kit 12, Jazz`            |
  | Instrument selection                 | `Selected instrument: 808 Kick`       |
  | Long operation start / finish        | `Loading kit…` / `Kit loaded in 12 s` |
  | Failure                              | `Could not switch kit: device not connected` (assertive) |

- The same text appears in the visible status bar — sighted users get a normal status bar; blind users get announcements. One status VM per window feeds both.
- Announcement text is composed from schema `Name`/`Description` like everything else (§2); device-side events (kit change reported by the module) follow the same style once Phase 2 lands them.
- Announce outcomes, not keystrokes: edits announce on commit (explicit action), not per keystroke.

## 6. Live-region policy

Exactly **one** `Polite` status element per window (the status line). Failure announcements go to a separate `Assertive` element that is only populated on error. The scanner enforces this invariant per window.

```xml
<!-- Status area at the bottom of the window layout -->
<TextBlock AutomationProperties.AutomationId="data-explorer.status.message"
           AutomationProperties.LiveSetting="Polite"
           AutomationProperties.LandmarkType="Status"
           Text="{Binding StatusMessage}" />
<TextBlock AutomationProperties.AutomationId="data-explorer.status.error"
           AutomationProperties.LiveSetting="Assertive"
           Text="{Binding ErrorMessage}" />
```

- No view may add a second `Polite` live region (e.g. per-control `LiveSetting` on a field). Progress for long operations updates the one status line.
- The `Assertive` element must stay empty outside failures — assertive announcements interrupt speech, so they are reserved for errors.

## 7. Testing rules

1. **Automated invariants per view.** `A11yScanner` (`VDrumExplorer.Gui.Avalonia.Test/A11y/`) walks the visual tree via `AutomationPeer`s and checks: non-empty identifier-first names without control-type words; `AutomationId` presence; single `Polite` live region per window; headings and landmarks present; decorative elements raw/excluded; no focus traps; logical tab order. Views are **report-only until they are retrofitted**; once a view passes, the scanner flips to **enforcing** for it and stays green in CI.
2. **Visual baselines stay green.** The headless Skia baseline tests (`DataExplorerVisualTest`, `ExplorerHomeVisualTest`, `SchemaExplorerVisualTest`, `DialogVisualTest`) must pass in every PR. Annotation and keyboard work is expected to be visually neutral; any visible change (focus styling, status bar, flat mode) requires a **deliberate, reviewed re-baseline** committed separately or clearly explained in the PR.
3. **Headless interaction tests** for keyboard flows, using the existing `[AvaloniaFact]` pattern: simulated keyboard navigation (kit list → field edit → commit) asserting focus order and status-VM announcement text. Speech itself is not testable headlessly — that is what item 4 is for.
4. **Real screen-reader smoke tests are manual and mandatory per phase.** NVDA (Windows) and Orca (Linux) sessions against the TD-17 v2 (owner hardware), executed per the plan's phase definitions and recorded as checklist results. CI cannot substitute for this; do not merge a phase whose smoke-test checklist is unexecuted.
5. **Coverage and format gates unchanged:** scanner/interaction tests live in the existing test project; `dotnet format --verify-no-changes` clean on every PR.

## 8. Per-module honesty

Accessibility is verified only where it is verified. The plan's support matrix is the contract:

| Module | Accessibility status |
|--------|----------------------|
| **TD-17 v2** | Primary target — verified on hardware every phase (NVDA/Orca smoke tests, maintainer) |
| **TD-27** | Prepared (schema audit, fake-MIDI coverage) — accessibility pending tester verification |
| **TD-07 / TD-50 / TD-50X / AE-01 / AE-10** | Schema-supported; accessibility unverified |

Rules:

- Docs, release notes, and the setup guide carry this status verbatim (Phase 3); **docs never claim more than verification**.
- Schema-level support (a field is readable/writable over MIDI) is never conflated with accessibility verification (a screen-reader user actually completed the flow).
- If a module is untested, the honest label is "unverified", not "should work".

## 9. PR checklist (Phase 1+)

A reviewer runs this list on every PR that touches views or ViewModels. A PR is not approvable with unchecked boxes below the line.

**Views touched**

- [ ] Every focusable/interactive control in touched views has a non-empty `AutomationProperties.Name` following §2 (identifier-first, no control-type words; schema-derived where the control maps to a schema field).
- [ ] `AutomationProperties.HelpText` present on every schema field control, composed from `Description` + range/enum text (§3).
- [ ] `AutomationProperties.AutomationId` present on all testable controls, matching the `<view>.<area>.<control>` kebab-case scheme, with no runtime indices (§3.1).
- [ ] Section headers carry `HeadingLevel` (ordered, no skipped levels in the touched view); major regions carry `LandmarkType` (§3).
- [ ] Decorative elements carry `AutomationProperties.AccessibilityView="Raw"` or are non-focusable (§3).
- [ ] Kit-list/tree rows expose state via `ItemStatus` where it matters (`Kit 12, Jazz, modified`) (§3).

**Keyboard**

- [ ] Every action in the touched views has a keyboard path; no new code-behind `KeyDown`/`KeyUp` handlers; new/changed shortcuts are declarative `KeyBinding`s bound to VM commands (§4).
- [ ] Tab order is logical; no focus traps; visible focus (`:focus-visible`) works on all newly added controls (§4).

**Announcements**

- [ ] Every successful operation outcome flows through the window's single `Polite` status line; failures announce assertively with a reason; no second `Polite` live region added (§5–6).
- [ ] Announcement/`ItemStatus` text is identifier-first and includes new values on commit (§5).

**Tests**

- [ ] Scanner state updated deliberately: untouched views still report-only; retrofitted views are enforcing and green (§7).
- [ ] Visual baseline tests pass; any re-baseline is deliberate and explained in the PR (§7).
- [ ] Keyboard flows in touched views covered by headless interaction tests (§7).
- [ ] Manual NVDA/Orca smoke results recorded if this PR completes a phase's manual-test target (§7).

**Honesty**

- [ ] No doc/README/release-note text claims accessibility beyond what has been verified on hardware (§8).
