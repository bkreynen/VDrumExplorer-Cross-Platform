Status: active

# Pivot Plan: VDrumExplorer-Cross-Platform → Accessibility Interface for Roland V-Drums

**Date:** 2026-09-18
**Repo:** bkreynen/VDrumExplorer-Cross-Platform (C#/.NET 10, Avalonia, Linux-primary; fork of Jon Skeet's VDrumExplorer)
**Input:** `.agents/research/2026-09-18-1200-accessibility-pivot-research.md` (verified codebase facts + external research — trusted, not re-verified here)
**Scope:** Planning only. No code changes in this document.

---

## Overview

This plan pivots the app from a personal sighted-oriented settings editor into an **accessibility interface for Roland drum modules** (TD-17, TD-27, TD-50X, AE-01/AE-10) for blind/visually-impaired users, while preserving the existing "easy settings adjustment" functionality. The core bet: the hard parts (MIDI protocol, schema layer with per-field `Name`/`Description`, framework-agnostic MVVM ViewModels, async `DeviceController`) already exist and are tested — the pivot is primarily a **new accessible front-end + a11y test infrastructure + MIDI coverage verification**, not a rewrite.

**Recommended interface (owner requirement c):** a **new dedicated accessible Avalonia front-end** with linear/flattened navigation, reusing the ViewModel layer wholesale, alongside the untouched existing GUI for sighted users — plus a lightweight **interactive console REPL** as a fast-complement/test-harness. Rationale in §Interface Decision below.

---

## Guiding principles

1. **Never regress the existing app.** The current Avalonia GUI keeps working for sighted users throughout the pivot. All new work is additive.
2. **ViewModel layer is the shared core.** Any new front-end (Avalonia accessible UI, console REPL) implements `IViewServices` and binds existing ViewModels. No business logic in views.
3. **Schema text is the TTS layer.** Every schema field already carries `Name` + `Description` + min/max/enum values — accessible announcements are derived from this, not hand-written per parameter.
4. **Announce every action** (REAPER+OSARA model): every successful operation produces a live-region status update; every failure produces an assertive announcement.
5. **Blind-user naming rules from day one:** unique identifier first ("Kick volume", not "Volume — kick"); never include control type in the name; decorative elements excluded from the a11y tree.
6. **Test the invariants automatically; test the experience manually.** Headless scanners catch missing names/IDs/structure; real NVDA/Orca sessions with real users catch what scanners can't.

---

## Interface Decision (owner requirement c)

### Options evaluated

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| (i) Retrofit existing Avalonia GUI with `AutomationProperties` + keyboard nav | One codebase; sighted UX unchanged | **Zero a11y annotations today** across 11 axaml views; shortcuts hard-coded in code-behind; the tree+details interaction model itself is hostile to screen readers (hundreds of parameters behind a TreeView; no linear path; no live regions). Retrofit = annotating every control *and* reworking the interaction model — the worst of both costs. | Rejected as primary |
| (ii) New dedicated accessible Avalonia UI (linear/flattened navigation) | Reuses all ViewModels + `IViewServices`; interaction model designed for screen readers from line one (linear sections, headings/landmarks, live regions, keyboard-first); existing GUI untouched; Avalonia a11y stack (UIA/NSAccessibility/AT-SPI2) is documented and mature; headless test pattern already proven in `VDrumExplorer.Gui.Avalonia.Test` | Second front-end to maintain (mitigated: views only, logic stays in shared ViewModels); solo-maintainer bandwidth | **Recommended primary** |
| (iii) Interactive console/TTS REPL reusing ViewModel layer | Cheapest to ship; terminal screen readers (NVDA console, Orca terminal) work inherently; doubles as a headless test harness and power-user tool; `VDrumExplorer.Console` already has one-shot commands (`list-kits`, `show-kit`) to build on | Less discoverable; no rich widgets (lists with ItemStatus, live regions); TTS announcement logic would duplicate what live regions do in the GUI; Windows console screen-reader experience is weaker than GUI | **Recommended complement** (Phase 2), not primary |
| (iv) Blazor web UI | ARIA ecosystem mature; cross-platform | Web MIDI API: Chrome-only, SysEx permission friction, poor fit for Roland vendor-driver USB mode; ViewModel reuse weaker (different binding model); hosting/story adds maintenance; blind users' desktop screen readers work best with native apps | Rejected |

### Recommendation

**Option (ii) as the primary deliverable, with option (iii) as a Phase-2 complement.**

Rationale:
- The existing GUI's problem is not missing annotations — it's the **interaction model** (tree+details with no linear traversal, no announcements). A retrofit would require re-architecting the views anyway, at which point a purpose-built accessible front-end on the same ViewModels is cheaper and cleaner.
- Blind users' actual workflows (from practitioner research: navigate by first-word identifiers at high speech rates, jump via headings, hear feedback for every action) map directly onto a **linear, sectioned, keyboard-first** UI: Kit list → Current kit sections (headings) → fields as named controls with `HelpText` ranges → live-region status line.
- Solo-maintainer cost is bounded: the ViewModel layer (`VDrumExplorer.ViewModel`, `DataFieldViewModel` per-type VMs) and device layer (`IDeviceController`) are shared; the new front-end is views + one `IViewServices` implementation. The existing GUI freezes into maintenance mode (bug-fixes only).
- **Old UX preserved:** the existing Avalonia GUI remains fully functional for sighted "easy settings adjustment"; the accessible UI also supports the same editing flows (kit/instrument/parameter editing) so functionality is duplicated, not lost. The console REPL additionally preserves a scriptable path (`show-kit` JSON already exists).

### Accessible interaction model (how functions map to a11y — feeds Phase 1/2)

| Module function (MIDI-controllable) | Accessible interaction | Announcement |
|---|---|---|
| Kit browsing/selection | List with `ItemStatus` per row ("Kit 12, Jazz, modified"); Enter or dedicated key switches via Program Change (channel 10 today — configurable in Phase 0) | Live region: "Switched to kit 12, Jazz" |
| Current-kit parameter read | Fields rendered as named controls; `Name` = "Section — Field" identifier-first from schema `Name`; `HelpText` = schema `Description` + range ("Tempo, 20 to 260 BPM") | Focus announcement includes name + current value + range |
| Parameter edit (enum/bool/string/numeric/instrument/tempo) | Reuse `DataFieldViewModel` formatted-text round-tripping; keyboard editing (combo/checkbox/text/numeric stepper); commit on explicit action, not per-keystroke | On commit: "Kick volume set to 80" (live region, polite); on failure: assertive + reason |
| Instrument selection | Instrument picker as searchable list (schema instrument names) | "Selected instrument: X" |
| Note preview (`PlayNote`/`Silence`) | Dedicated preview key on focused pad/instrument | "Previewing note 38" |
| Kit/module load & save (`LoadKitAsync`, `SaveDescendants` DT1 writes) | Explicit commands with progress announcement | "Loading kit…", "Kit loaded in 12 s", "Write complete" |
| Device state changes | Live-region status line bound to a status VM (Phase 2 adds device-side events) | "Module reports kit changed to 7" |
| Undo/redo | Memory-only today; Phase 2 defines semantics (see Risks) | "Undid: Kick volume restored to 75" |
| Onboarding friction (vendor USB driver mode, Receive Exclusive = On) | Spoken setup guide (docs + in-app first-run checklist) — Phase 3 | Step-by-step announced instructions |

---

## Phases

Sizing assumes a **solo maintainer working part-time**; "dev-days" = focused working days. Phases are sequential (each depends on the prior), but tasks marked 🔀 are parallelizable within a phase.

---

### Phase 0 — Foundations: a11y test infra + MIDI verification kickoff (~2–3 weeks part-time, ~8–10 dev-days)

**Goal:** Before building accessible UI, build the machinery that proves it stays accessible, and de-risk the MIDI layer.

**Tasks:**

1. **A11y invariant scanner (headless, every PR)** — owner requirement (a)
   - New test utility (e.g. `A11yScanner`) in/next to `VDrumExplorer.Gui.Avalonia.Test` (which already uses `Avalonia.Headless.XUnit` `[AvaloniaFact]`): walks the visual tree via `AutomationPeer`s and asserts invariants.
   - **Invariants to assert:**
     - Every focusable/interactive control has a non-empty, meaningful `AutomationProperties.Name` (or visible text content).
     - Names follow identifier-first rule; **fail if name contains control-type words** ("button", "slider", "check box") — enforceable via a small blocklist.
     - `AutomationProperties.AutomationId` present on all testable controls (stable hooks for FlaUI/Appium later).
     - Exactly one live-region status element exists (`LiveSetting` Polite) per window; errors use Assertive.
     - Section headers carry `HeadingLevel`; major regions carry `LandmarkType`.
     - Decorative elements excluded from the accessibility view (`AccessibilityView="Raw"` or not focusable).
     - Tab order visits controls in logical order (no focus traps; all interactive controls reachable).
   - Scanner runs against the **existing GUI first** in "report-only" mode (produces a violation inventory — this quantifies the retrofit gap and seeds Phase 1 naming conventions), and in **enforcing** mode against the new accessible UI from Phase 1 onward.
   - **CI coverage-gate decision:** a11y/headless tests **do count toward coverage** (they execute real view code-behind and raise the number). To avoid tanking the 90% gate: add the new accessible-UI project to the coverage set in its first PR **with its actual measured coverage as the recorded baseline**, then ratchet. Verify how the per-project coverage config identifies projects (plan step: verify the coverage workflow's project filter mechanism) before merging the new project into the gate.
2. **A11y conventions doc** — short `docs/accessibility.md` (or `.agents/` doc promoted later): naming rules, `AutomationId` scheme, live-region policy, keyboard-nav rules. Every Phase 1+ PR reviews against it.
3. **Configurable MIDI channel** — remove the hard-coded channel 10: add a per-connection setting (module schema already knows the module; default stays 10). Touches `DeviceController` construction paths; update `DeviceController` fake-MIDI NUnit tests.
4. **Program Change listener spike (device-side kit-change notification)** — owner requirement (b)
   - Today current kit is pull-only. Spike: listen for incoming Program Change / kit-change SysEx on the MIDI input while connected; if the module transmits on panel kit-change (verify per module — TD-17/27/50 behavior differs; check MIDI Implementation docs), surface an event on `IDeviceController` → status VM → live region.
   - If modules don't transmit panel changes (likely for some), document the fallback: periodic lightweight poll (`GetCurrentKitAsync` name read) at a sane interval, announced only on change.
   - Deliverable: written finding per module + event/poll design; implementation lands in Phase 2.
5. **Schema-vs-docs audit kickoff** — owner requirement (b)
   - For each schema (AE-01, AE-10, TD-07, TD-17 v1/v2, TD-27, TD-50, TD-50X): diff embedded JSON schema field coverage against the Roland MIDI Implementation + Data List address maps (TD-17 and TD-27 PDFs verified in research; locate TD-50X equivalents — verify). Produce a per-module gap list (e.g. master effects/MFX sections, system setup, trigger settings) filed as issues. Gaps close in Phase 2.
6. 🔀 **CI plumbing** — ensure headless a11y tests run in the existing 3-OS matrix; decide windows-latest optional job wiring (FlaUI) now, enable in Phase 3.

**Definition of done:**
- `A11yScanner` exists, runs in CI on every PR, and produces a violation report for the existing GUI (report-only) — report committed as a baseline artifact.
- MIDI channel is configurable with passing `DeviceController` tests; default behavior unchanged.
- Written per-module finding: does the module notify kit changes? Event vs poll design decided.
- Per-module schema gap list exists as issues (at minimum for TD-17 v2, TD-27, TD-50X).
- Coverage gate still green; `dotnet format --verify-no-changes` clean.

**Risks:** scanner over `AutomationPeer`s headlessly may not surface every property identically to platform UIA/AT-SPI — mitigate by cross-checking a sample with FlaUI in Phase 3; coverage-gate config may need workflow edits (verify mechanism first).

---

### Phase 1 — Accessible core flows: the new accessible Avalonia front-end (~4–6 weeks part-time, ~15–20 dev-days)

**Goal:** A blind user, with a screen reader running, can connect to a module, browse kits, switch kits, and read/edit parameters of the current kit — the core loop.

**Tasks:**

1. **New project skeleton** — e.g. `VDrumExplorer.Gui.Avalonia.Accessible` (name TBD): Avalonia app reusing `VDrumExplorer.ViewModel`; implements `IViewServices` (dialogs). Existing GUI untouched.
2. **Linear/flattened layout** (replaces tree+details):
   - Connection/module selection screen → Kit list (rows with `ItemStatus`: number, name, modified flags) → Current-kit view: flat sections as `HeadingLevel` headings (Kit common, per-instrument sections, etc. derived from schema tree), fields as named controls.
   - Keyboard-first: full Tab traversal, `HotKey`/`KeyBinding` for every action (switch kit, save, load, preview note, jump-to-section via heading navigation); visible focus adorner.
   - Naming: identifier-first from schema (`Name` composed as "Section — Field" per conventions doc); `HelpText` from schema `Description` + min/max/enum text; no control-type words in names.
3. **Kit browsing & switching** — bind kit list VM; switch via `SetCurrentKitAsync` (Program Change); live-region announcement on success/failure.
4. **Field reading & editing** — render `DataFieldViewModel` per-type VMs (Enum/Boolean/String/Numeric/Instrument/Tempo) as accessible controls; commit-on-explicit-action; announcements per the interaction model table; instrument picker as searchable list.
5. **Status line** — single live-region element bound to a status VM; every `DeviceController` operation outcome flows through it (polite for success, assertive for errors).
6. **A11y scanner enforcing** — flip scanner to enforcing mode for the new project; every interactive control named/ID'd; invariants green in CI.
7. 🔀 **Core-flow headless interaction tests** — extend the existing `[AvaloniaFact]` pattern: simulated keyboard navigation through kit list → field edit → commit, asserting focus order and status announcements (as VM state; speech itself is Phase 3/manual).
8. 🔀 **MIDI gap closure (start)** — begin closing Phase 0 audit gaps for **TD-17 v2 and TD-27 first** (most likely beta hardware; TD-50X later).

**Definition of done:**
- New accessible front-end builds on all 3 OS matrix legs; existing GUI unchanged (its tests still pass).
- With a screen reader (manual smoke on Linux/Orca + Windows/NVDA by the maintainer): connect → list kits → switch kit → read a field → edit a field → hear confirmation. Recorded as a manual-test checklist result.
- A11y scanner enforcing and green for the new project on every PR; coverage gate green with recorded baseline.
- Kit switch, field read, field edit flows covered by headless interaction tests.
- TD-17 v2 + TD-27 schema gaps from the audit either closed or filed with concrete address-map references.

**Risks:** Avalonia AT-SPI2 quality on Linux is self-reported by Avalonia — early real-Orca smoke testing is mandatory in this phase, not deferred; per-type field controls may need custom `AutomationPeer`s (budget for it); schema-tree → flat-section mapping may need iteration with a real user (get one beta contact early — see Phase 3 recruitment, start now).

---

### Phase 2 — Expanded coverage: all flows, device feedback, undo/redo, REPL (~4–6 weeks part-time, ~15–20 dev-days)

**Goal:** The accessible UI covers everything the old GUI does, plus device-state feedback the old GUI never had.

**Tasks:**

1. **Full flow coverage** — port remaining flows to the accessible UI: module load (`LoadModuleAsync`), kit load/save, `.vkit`/`.vdrum` import/export, note preview (`PlayNote`/`Silence`), any remaining dialogs (enumerate from the 11 existing axaml views — verify full list during implementation). All with announcements + keyboard paths + scanner invariants.
2. **Device-state feedback** — implement the Phase 0 kit-change design: PC listener where supported, poll fallback otherwise; device connect/disconnect events; all surfaced via the live-region status line. Extend `IDeviceController` (and fake-MIDI tests) accordingly.
3. **Undo/redo semantics** — memory-only undo exists; define device-write semantics:
   - Recommended: **undo = re-write previous value via DT1** (`SaveDescendants`/`SetInstrumentAsync` path) for field edits within a session; undo stack records field address + prior value; announce "Undid: X restored to Y".
   - Explicitly out of scope: undoing kit switches (Program Change), undoing bulk loads. Document this.
   - Risk flag: write-timing heuristic (40 ms delay) makes rapid undo/redo sequences fragile — batch/queue writes; verify timing behavior on real hardware.
4. **MFX / master effects access** — close schema-tree gaps for master effects/MFX per the audit (TD-27/TD-50X priority), exposed as accessible sections.
5. **Configurable MIDI channel UI** — surface the Phase 0 setting in the accessible UI's connection screen.
6. **Console REPL (option iii complement)** — extend `VDrumExplorer.Console` with an interactive REPL reusing ViewModels: commands like `kits`, `use <n>`, `get <field>`, `set <field> <value>`, `preview <note>`, `status`. Terminal-screen-reader friendly; doubles as a headless harness for scripting tests. Keep it thin — no logic duplication.
7. 🔀 **TD-50X protocol verification** — least battle-tested per Skeet's own notes: structured test pass against a real TD-50X (or a owner-community loaner/beta tester) covering read, write, kit switch, load. File and fix gaps. If no hardware access, mark TD-50X "experimental, unverified" in docs — do not silently claim support.

**Definition of done:**
- Feature parity checklist (old GUI flows vs accessible UI) complete and checked off; every flow has keyboard path + announcement + scanner-green.
- Device-side kit change (event or poll) announced in the accessible UI; `IDeviceController` tests cover it with fake MIDI.
- Undo/redo works for field edits including device re-write, with documented scope; tests cover stack behavior and the write-queue.
- REPL shipped: can connect, list/switch kits, get/set a field, all via terminal; documented.
- TD-50X verification report written (verified-ok or explicitly-experimental).
- Coverage gate green across all touched projects.

**Risks:** undo-via-rewrite on real hardware needs careful timing (interact with the 40 ms heuristic — consider making the delay configurable/adaptive); parity scope creep — the parity checklist is the guardrail; REPL is a scope-cut candidate if bandwidth tight (defer to Phase 3 without blocking anything).

---

### Phase 3 — Polish: docs, beta testing with blind users, CI hardening, upstream (~3–4 weeks part-time + ongoing, ~10–12 dev-days)

**Goal:** Ship it to real blind/VI users and make the quality bar durable.

**Tasks:**

1. **Onboarding documentation** — spoken-friendly setup guide: vendor USB driver mode, Receive Exclusive = On, connecting, first kit switch (per-module steps; TD-17 first). Plain-text/HTML accessible docs; also in-app first-run checklist with announced steps.
2. **Blind-user beta program** — recruit 2–5 blind/VI drummers (channels from research: Sound Without Sight community; vdrums.com; MIDI Association networks). Structured feedback rounds on: onboarding, kit switching, editing, announcements quality, naming conventions. Iterate Phase 1/2 output based on findings. **Start recruitment in Phase 1** — lead time is real.
3. **Manual screen-reader test protocol** — documented NVDA (Windows) + Orca (Linux) test scripts run pre-release; results recorded. Orca has no headless CI story — this stays manual by design.
4. **Optional CI hardening (windows-latest job)** — FlaUI UIA3 tests asserting the same invariants via the real Windows UIA tree (what NVDA consumes); optionally NvdaTestingDriver for spoken-output capture on a couple of golden flows. Mark job `continue-on-error` initially; promote to required once stable.
5. **Upstream coordination** — open a discussion/issue on Jon Skeet's VDrumExplorer (and/or upstream the fork's protocol fixes): share the a11y findings, schema audit results, TD-50X verification. Position the accessible front-end as a contribution-friendly sibling, not a hostile fork. Also consider contacting Roland (they engaged consultants for V-STAGE — Jason Dasent) with the gap evidence.
6. **Release** — tagged release with accessible installer/docs; announcement post in the communities above.

**Definition of done:**
- Setup guide published and reviewed by at least one blind beta user for completeness.
- ≥2 blind/VI beta testers have completed the core loop (connect → switch kit → edit → save) with feedback logged; top-severity findings fixed.
- Manual NVDA + Orca test protocol executed and recorded for the release.
- FlaUI optional CI job running (passing or explicitly `continue-on-error` with known issues filed).
- Upstream issue/discussion opened with Skeet; release published and announced.

**Risks:** beta recruitment/hardware logistics (modules are physical; testers need the right module + USB + OS combo) — mitigate by recruiting across module types early; feedback volume may exceed solo bandwidth — triage by core-loop impact.

---

## Effort summary

| Phase | Duration (part-time solo) | Focused dev-days | Shippable outcome |
|---|---|---|---|
| 0 | 2–3 weeks | 8–10 | A11y test infra + MIDI de-risking + audit baseline |
| 1 | 4–6 weeks | 15–20 | Accessible core loop usable by a screen-reader user |
| 2 | 4–6 weeks | 15–20 | Full parity + device feedback + undo + REPL |
| 3 | 3–4 weeks + ongoing | 10–12 | Beta-validated, documented, released |
| **Total** | **~3.5–5 months part-time** | **~48–62** | |

Each phase ends in something independently valuable: Phase 0 = durable quality machinery; Phase 1 = a blind user can already do the most important thing (switch kits, tweak sounds); Phase 2 = replaces the old GUI for real use; Phase 3 = community-validated release.

---

## Risks & mitigations (consolidated)

| Risk | Severity | Mitigation |
|---|---|---|
| **TD-50X protocol maturity** — least battle-tested (Skeet's own release notes) | High | Dedicated verification task in Phase 2; if unverified, label "experimental" honestly; prioritize TD-17/TD-27 for beta |
| **Write-timing heuristic (40 ms delay)** — rapid edits/undo sequences may drop or corrupt writes | High | Make delay configurable/adaptive; queue/serialize DT1 writes; test undo/redo bursts on real hardware in Phase 2 |
| **Avalonia AT-SPI2 (Linux) quality is self-reported** — Orca experience unproven in the field | High | Early real-Orca smoke tests in Phase 1 (not deferred); FlaUI/UIA cross-check in Phase 3; Linux is primary platform so this cannot be skipped |
| **Real screen-reader testing needs real users + hardware** | High | Start beta recruitment in Phase 1; maintainer does NVDA/Orca smoke tests meanwhile; manual protocol documented |
| **Solo-maintainer bandwidth** — two front-ends + REPL + protocol work | Medium | Existing GUI frozen to maintenance mode; REPL is a cut candidate; parity checklist prevents scope creep; phases independently shippable |
| **Coverage gate interaction** — new UI project could tank the 90% gate | Medium | A11y tests count toward coverage (they execute view code); record measured baseline on first inclusion, then ratchet; verify coverage workflow's project-filter mechanism in Phase 0 |
| **Modules may not transmit kit-change notifications** (pull-only current kit) | Medium | Phase 0 spike decides event vs poll per module; poll fallback announced only on change |
| **MIDI channel hard-coded to 10 in some paths** | Medium | Phase 0 makes it configurable; default unchanged; tests updated |
| **Onboarding friction (vendor driver mode, Receive Exclusive)** — blind users can't navigate module menus to enable these | Medium | Spoken setup guide + in-app checklist (Phase 3); consider detecting "connected but silent" state and announcing the likely cause |
| **No prior art for accessible V-Drums editors** — interaction-model guesses may miss real user needs | Medium | Beta users from Phase 1; iterate; lean on OSARA/REAPER and HISE-thread practitioner rules as proxies |
| **Schema audit may reveal large gaps** (MFX, system, trigger sections) | Low–Medium | Audit quantifies in Phase 0; gaps closed per-module by priority; honest docs about coverage per module |

---

## Open questions

1. **Coverage gate mechanics** — how does the CI coverage config enumerate projects (solution-wide vs explicit list)? Determines how the new accessible-UI project joins the 90% gate. (Phase 0, task 1.)
2. **Full axaml view/dialog inventory** — the 11 views are known to exist but not individually enumerated in the research; Phase 2 task 1 starts with "verify full list" to build the parity checklist.
3. **TD-50X hardware access** — does the owner (or a beta recruit) have a TD-50X? Determines whether Phase 2 verification is "test" or "mark experimental".
4. **Do any supported modules transmit Program Change on panel kit-change?** — Phase 0 spike answers this per module; affects the device-feedback design.
5. **REPL priority** — owner preference: ship in Phase 2 as planned, or defer if bandwidth is tight? (Cut candidate; nothing depends on it.)
6. **Upstream posture** — does the owner want to pursue contribution/merge back toward Skeet's repo long-term, or stay a friendly fork? Affects how much Phase 3 invests in upstream coordination.

---

## Verification steps explicitly deferred (do not invent — verify during execution)

- Exact coverage-workflow project filter mechanism (Phase 0).
- Full list of the 11 axaml views and their flows (Phase 2 parity checklist).
- Per-module PC-transmission behavior (Phase 0 spike).
- TD-50X MIDI Implementation / Data List PDF locations (Phase 0 audit; TD-17/TD-27 URLs already verified in research).
- Whether `VDrumExplorer.Blazor` contains anything worth salvaging (deprioritized — option (iv) rejected; a quick inventory is optional).
