Status: active

# Research: Accessibility Pivot for VDrumExplorer-Cross-Platform

**Date:** 2026-09-18
**Scope:** (1) Automated a11y testing for Avalonia/.NET in CI, (2) accessible UI patterns for device-configuration apps, (3) Roland V-Drum accessibility gap + MIDI-controllable functions.
**Method:** Web search + Avalonia docs + Jon Skeet's VDrumExplorer docs. Research only — no code changes.

---

## (a) Recommended automated a11y testing approach (Avalonia + .NET + GitHub Actions Linux)

### Avalonia accessibility support — state of the art (verified, current docs)

Avalonia has **built-in, documented accessibility support** via automation peers (WPF/UWP-style), exposed to platform APIs: **UIA on Windows, NSAccessibility on macOS, AT-SPI2 on Linux**. Docs claim "Full support" on all three desktop platforms (last updated Apr 2026).

Key APIs (all documented at https://docs.avaloniaui.net/docs/app-development/accessibility):
- `AutomationProperties.Name` / `HelpText` / `LabeledBy` — accessible naming
- `AutomationProperties.AutomationId` — stable, non-localized IDs for UI automation testing
- `AutomationProperties.LiveSetting` (`Polite`/`Assertive`) — live-region announcements for dynamic status text (directly relevant: "Kit loaded", "Write complete")
- `AutomationProperties.HeadingLevel`, `LandmarkType` — structural navigation
- `AutomationProperties.ItemStatus`, `ItemType`, `AcceleratorKey`, `AccessKey`, `ControlTypeOverride`, `AccessibilityView`
- Custom `AutomationPeer` subclasses via `OnCreateAutomationPeer()` for custom controls
- Data-validation errors are exposed automatically through automation peers
- Linux: AT-SPI2 tree is exposed over D-Bus automatically when a session bus + accessibility service are present — no app-side config needed. Orca can discover/interact with standard controls (https://docs.avaloniaui.net/docs/platform-specific-guides/linux#accessibility).

**Caveat (flagged):** "Full support" is Avalonia's own claim. Historical context: accessibility was a long-running gap (issue #585 opened 2016, work started ~2020). Quality on Linux/AT-SPI specifically is less battle-tested than Windows/UIA; real-screen-reader verification is still essential. I could not independently verify AT-SPI2 peer coverage for every control type.

### Testing layers — what runs where

Avalonia's own testing docs (https://docs.avaloniaui.net/docs/testing) define a layered strategy and explicitly list **Appium UI tests as the layer that covers "accessibility"**:

| Layer | Tooling | Runs on GH Actions Linux runner? | Maturity |
|---|---|---|---|
| Unit tests (VMs, MIDI protocol logic) | xUnit/NUnit plain | ✅ Yes, trivially | Mature |
| **Headless tests** | `Avalonia.Headless.XUnit` / `.NUnit` — full control tree, layout, binding, simulated keyboard/mouse, no display server | ✅ Yes — designed for CI, no X server needed | Mature, first-party |
| Visual regression | Headless + Skia frame capture | ✅ Yes | Mature, first-party |
| **Appium UI tests** | Launch real app window, drive via **platform accessibility tree** | ⚠️ Partially — needs X (Xvfb works on Linux runners); Avalonia itself uses Appium internally on Windows/macOS | First-party documented; Linux/AT-SPI driver setup is the fiddly part |
| Windows UIA tests | **FlaUI** (FlaUI/FlaUI, MIT, active; UIA3 wrapper) or WinAppDriver | ❌ Needs `windows-latest` runner (works there; FlaUI is used in CI on headless Windows runners) | FlaUI: mature/community; WinAppDriver: semi-abandoned by MS (community-maintained since ~2023) |
| Linux AT-SPI tests | **dogtail** (Python, AT-SPI; active — 1.0.x releases 2025–2026, 2.0 in dev) or raw **pyatspi2** | ⚠️ Yes with effort — dogtail's new `HermeticSession` boots a private D-Bus + a11y bus + bare mutter with a virtual monitor in a CI container with no graphics session | dogtail: active but GNOME-centric, Python-only; pyatspi2 "has no CI" per GNOME devs |
| Screen-reader-in-the-loop | **NvdaTestingDriver** (.NET, drives NVDA via its remote/controller client, captures spoken output as text) | ❌ Windows only (NVDA is Windows software) — would need `windows-latest` | Niche but real; BrowserStack has an alpha NVDA-automation product (web-focused) |
| Orca headless | **No established headless-CI story.** GNOME's own docs note "Orca has no CI" (it has a manual test suite). dogtail can assert on the AT-SPI tree (what Orca consumes) but not on Orca's speech output | — | Gap — treat Orca testing as manual |

### Concrete recommendation for this stack

1. **CI (ubuntu-latest, headless, every PR):**
   - Unit tests for the MIDI/protocol layer (pure logic, no UI).
   - `Avalonia.Headless.XUnit` tests that walk the visual tree and **assert a11y invariants**: every interactive control has a non-empty `AutomationProperties.Name` (or text content), `AutomationId` present on testable controls, tab order sane, live-region status element exists. This is the "axe-core equivalent" for this stack — there is no off-the-shelf axe for Avalonia; a small custom invariant-scanner over `AutomationPeer`s is the realistic option. (Inference, not an off-the-shelf tool — flagged.)
   - Optionally Appium with Xvfb driving the real app over AT-SPI for a few smoke flows (kit list navigation, settings edit). Budget extra setup time; Avalonia's Appium docs are Windows/macOS-focused.
2. **CI (windows-latest, optional job):** FlaUI UIA3 tests — same assertions via UIA, plus it validates the platform blind Windows users (NVDA) actually consume. FlaUI is the most mature .NET-native option.
3. **Manual, pre-release, with real screen readers:** NVDA on Windows (can be semi-automated with NvdaTestingDriver on a Windows runner if desired), Orca on Linux (manual only — no headless Orca CI exists). Recruit blind/VI tester feedback early (Sound Without Sight community, see (b)).

**Honest bottom line:** No axe-core equivalent exists for Avalonia desktop. The realistic automated layer is (i) headless tree-scanning for a11y metadata invariants + (ii) Appium/FlaUI driving the platform accessibility tree. Real screen-reader validation stays manual, especially Orca.

---

## (b) Accessible UI patterns for a settings/kit-management interface

### From Avalonia docs (directly implementable)
- **Keyboard-first:** everything reachable via Tab/Shift+Tab; `HotKey`/`KeyBinding` for all pointer-only actions; visible `FocusAdorner` (`:focus-visible` styling); `KeyboardNavigation.TabNavigation` / XYFocus modes.
- **Live regions:** `AutomationProperties.LiveSetting="Polite"` on a status TextBlock bound to e.g. `StatusMessage` — announces "Kit 12 loaded", "Write to module complete" without focus change. `Assertive` for errors.
- **Structure:** `HeadingLevel` on section headers, `LandmarkType` (Form/Main/Navigation/Search) on regions — lets screen-reader users jump between areas instead of linear-tabbing through hundreds of parameters.
- **Naming discipline:** explicit `Name` for icon-only controls; `LabeledBy` for field/label pairs; `HelpText` for units/ranges ("Tempo, 20 to 260 BPM").
- **ItemStatus** on list items (e.g. kit list rows: "Kit 12, Jazz, favorite, modified").

### From the blind-musician community (verified practitioner guidance)
The HISE forum thread on accessible plugin UIs (https://forum.hise.audio/topic/7426/, blind users + devs, 2025) gives concrete rules that map 1:1 to this app:
- **Name elements with the unique identifier first** — "Kick volume", not "Volume — kick". Blind users navigate at very high speech rates and scan by first word.
- **Don't include control type in the name** ("button", "slider") — the screen reader announces the role itself.
- **Exclude decorative elements from the a11y tree** (Avalonia: `AccessibilityView="Raw"`).
- Keyboard ninjas: every operation via shortcut or menu; announce feedback for every action (OSARA/REAPER model — https://reaperaccessible.fr).

### Prior art — music hardware accessibility (strong signal this pivot is timely)
- **NYU Tandon SynthAccess / MIDItoSpeech** (MIDI.org, Jun 2026): listens to MIDI parameter changes and speaks "parameter name + value"; community JSON device files mapping CC/NRPN/PC to human-readable labels. Exactly the architecture this app could adopt for live module feedback. https://midi.org/nyu-tandons-synthaccess-project-points-toward-a-more-accessible-future-for-synthesizers
- **MIDI Association MASSIG — "MIDI Assistive Text"** (Jul 2026): proposed standard for devices to send text metadata over MIDI for screen readers/braille. Industry momentum: Ableton Move screen-reader support, NI Accessibility Helper/NKS, Arturia TTS, Softube Console 1, Roland V-STAGE accessibility suite. https://midi.org/assistive-text-in-midi-a-new-path-toward-more-accessible-music-technology
- **Roland itself just entered this space** (Aug 2026): Future Design Lab + blind musician Jason Dasent produced screen-reader guides, audio walkthroughs, described video for the **V-STAGE keyboard** — proof Roland now treats this as a product concern, but for a keyboard, **not for V-Drums modules**. https://mixdownmag.com.au/news/roland-builds-an-accessibility-guide-for-the-v-stage-keyboard ; interview: https://articles.roland.com/designing-access-an-interview-with-jason-dasent/
- **REAPER + OSARA** is the canonical case study of community-built accessibility transforming a DAW for blind users (keyboard-first + spoken feedback on every action).
- **Ableton Move "Move Everything"** hack added screen-reader support — first standalone groovebox accessible to blind users (CDM, Feb 2026). https://cdm.link/tag/blind
- **No prior art found specifically for Roland V-Drum module editors being made accessible.** Jon Skeet's VDrumExplorer is a sighted-oriented WPF tree UI; nobody appears to have built an a11y-first V-Drums front-end. **This appears to be a genuine gap** (inference from absence of search results — flagged as such).

---

## (c) Roland V-Drum accessibility gap + MIDI-controllable functions

### Accessibility gap — confirmed
- **No TD-series module (TD-17, TD-27, TD-50X) has any documented accessibility or screen-reader feature.** Roland's accessibility statement covers only their *website* (https://www.roland.com/us/accessibility). Module manuals (TD-27/TD-50 Reference, Data List, MIDI Implementation — all on static.roland.com) describe purely visual operation: small LCDs, cursor buttons, soft keys, dial.
- The modules are menu-driven with deep hierarchies (e.g. TD-27 note map: `[KIT EDIT] → [F5 OTHER] → KIT MIDI → [F1 NOTE]`) — exactly the interaction model blind users cannot access.
- **Community evidence is thin but the broader pattern is well documented:** blind musicians report being locked out of screen/menu-driven music hardware generally (KVR 2026 industry piece; FMOD forum threads; Sound Without Sight community). I did **not** find a specific vdrums.com forum thread from a blind drummer — searches surfaced nothing module-specific. **Flagged: the "blind drummers complaining" claim is inferred from the general music-tech accessibility literature, not from a located first-person V-Drums thread.** The strongest adjacent fact: Roland's own accessibility work (V-STAGE, Aug 2026) exists precisely because "screens, menus, LEDs, soft buttons" exclude blind players — and V-Drums modules were not included in that effort.
- Notable irony found in a vdrums.com thread: the TD-27 has "the most flexibility of current Roland modules... minimal UI controls, dreaded [menus]" — deep functionality behind a visual menu system (https://www.vdrums.com/forum/general/products/1228952-td-30-vs-td-27).

### MIDI-controllable functions (what the app can do)

**Publicly documented by Roland (verified):**
- **TD-17 MIDI Implementation** (the doc Jon Skeet calls "absolutely vital"): https://static.roland.com/assets/media/pdf/TD-17_MIDI_Imple_eng01_W.pdf (+ v2-firmware revision `..._eng04_W.pdf`)
- **TD-27 MIDI Implementation** (v1.01, 52 pp): https://static.roland.com/assets/media/pdf/ — also on ManualsLib/kraftmusic mirrors. Documents Note On/Off, Control Change, Program Change, and Roland SysEx.
- **TD-27 Data List** (parameter/address tables): https://static.roland.com/assets/media/pdf/TD-27_Data_List_eng01_W.pdf
- TD-50 equivalents exist (Reference, Data List, MIDI Implementation — via ManualsLib/Roland).

**Function coverage:**
1. **Kit switching via Program Change** — documented; standard Roland bank/PC behavior. ✅
2. **SysEx parameter read/write** — Roland's standard **RQ1 (read request) / DT1 (data set)** universal model-ID messages with address maps published in the Data List. This is what VDrumExplorer uses to read/write "almost all the data in a module" — kit parameters, instruments, MFX, trigger settings, system setup. ✅ Documented, not reverse-engineered, for TD-17/27/50.
3. **Note playback / note mapping** — Note On receive per-pad note numbers (TD-27 default map published by Roland support: https://support.roland.com/hc/en-us/articles/4407474950811). Enables remote "play note" preview. ✅
4. **Data backup/transfer** — bulk dumps via SysEx (VDrumExplorer loads full module data in ~3 min on TD-17; kit-level .vkit / module-level .vdrum files). SD-card backup is the other documented route. ✅
5. **Real-time messages** — CC (modulation, foot controller/pedal hi-hat), channel messages per the implementation chart. ✅

**Practical requirements (from Skeet's docs):** USB driver mode must be **"vendor"** (not factory-default "generic") and **"Receive Exclusive" = On** in module MIDI settings — a real onboarding friction point an accessibility app must handle with spoken guidance.

**Reverse-engineered portions:** Skeet states TD-17/27/50 support rests on Roland's published docs; the **Aerophone AE-01/Mini** support was reverse-engineered (no MIDI impl doc). For TD-series, the schema work is doc-based with community bug-fixes (schema corrections across alpha releases). TD-50X support was initially shaky ("may well be entirely broken; I can't test it personally", alpha08–alpha11) and later fixed — **flagged: TD-50X protocol coverage is the least battle-tested**.

**Cross-check with this repo:** the current app already implements read/write parameters + Program Change kit switching over MIDI (per repo description), i.e., the transport layer exists; the pivot is a UI/UX problem, not a protocol problem.

---

## Summary table of key sources

| Claim | Source |
|---|---|
| Avalonia a11y model, AutomationProperties, platform table | https://docs.avaloniaui.net/docs/app-development/accessibility |
| Avalonia Linux AT-SPI2/Orca/Accerciser testing | https://docs.avaloniaui.net/docs/platform-specific-guides/linux#accessibility |
| Avalonia testing layers (headless/Appium covers a11y) | https://docs.avaloniaui.net/docs/testing |
| Headless xUnit CI setup | https://docs.avaloniaui.net/docs/testing/headless-xunit |
| FlaUI (.NET UIA automation) | https://github.com/FlaUI/FlaUI |
| dogtail AT-SPI testing incl. HermeticSession for CI containers | https://pypi.org/project/dogtail/ |
| "Orca has no CI" (GNOME a11y stack doc) | https://gnome.pages.gitlab.gnome.org/at-spi2-core/devel-docs/atspi-python-stack.html |
| NvdaTestingDriver (NVDA-in-the-loop .NET tests) | https://github.com/kastwey/nvda-testing-driver |
| Blind-user UI naming rules (HISE thread) | https://forum.hise.audio/topic/7426/ |
| NYU SynthAccess / MIDItoSpeech | https://midi.org/nyu-tandons-synthaccess-project-points-toward-a-more-accessible-future-for-synthesizers |
| MIDI Assistive Text (MASSIG) | https://midi.org/assistive-text-in-midi-a-new-path-toward-more-accessible-music-technology |
| Roland V-STAGE accessibility suite (Aug 2026) | https://mixdownmag.com.au/news/roland-builds-an-accessibility-guide-for-the-v-stage-keyboard ; https://articles.roland.com/designing-access-an-interview-with-jason-dasent/ |
| Roland website-only accessibility statement | https://www.roland.com/us/accessibility |
| TD-17 MIDI Implementation (Skeet's vital doc) | https://static.roland.com/assets/media/pdf/TD-17_MIDI_Imple_eng01_W.pdf |
| TD-27 MIDI Implementation | https://www.manualslib.com/manual/1984110/Roland-Td-27.html (Roland PDF: TD-27_MIDI_Imple on static.roland.com) |
| TD-27 Data List (address maps) | https://static.roland.com/assets/media/pdf/TD-27_Data_List_eng01_W.pdf |
| TD-27 default MIDI note map | https://support.roland.com/hc/en-us/articles/4407474950811 |
| VDrumExplorer protocol usage, vendor-driver requirement, TD-50X caveats | https://jskeet.github.io/DemoCode/Drums/ |
| Music-tech accessibility industry state (2026) | https://www.kvraudio.com/music-technology-accessibility-how-the-industry-is-opening-up-to-disabled-musicians |

## Uncertainty / unverified items (explicit flags)
1. **No first-person blind-drummer V-Drums thread located** — the accessibility gap is confirmed by absence of any Roland accessibility features in module docs + the general music-hardware exclusion literature, not by a specific community complaint thread.
2. **Avalonia AT-SPI2 quality on Linux** — "Full support" is Avalonia's self-reported status; I did not find independent third-party validation of Orca+ Avalonia in the field. Budget manual Orca testing.
3. **No axe-core equivalent for Avalonia exists** — the recommended headless invariant-scanner is a build-it-yourself approach (inference from tooling landscape, not a found tool).
4. **TD-50X SysEx coverage** — least-tested per Skeet's own release notes; verify against a real TD-50X before promising support.
5. **Appium-on-Linux for Avalonia** — documented as Avalonia's internal approach (Windows/macOS); Linux/Xvfb/AT-SPI driver setup is plausible but unverified in detail.
