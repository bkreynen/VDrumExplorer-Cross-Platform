Status: active

# PR #31–#35 verification: visual tests + CI a11y enforcement (2026-09-20)

Context: Phase 0 (PR #30, `a11y/phase0`) merged. Follow-up Phase 1 PRs #31–#35 verified
against the goal: "follow-up PRs have both visual tests and CI that verifies accessibility."

## CI accessibility verification — CONFIRMED

`.github/workflows/ci.yml` runs the Avalonia headless test project
(`VDrumExplorer.Gui.Avalonia.Test.dll`, which contains `A11yScannerTest`) on
ubuntu/macos/windows for every PR. Enforcing-mode scans throw
`A11yEnforcementException` on any Error-severity violation → red CI.
`Scan_EnforceMode_ThrowsOnErrors` proves the throw path itself.

Enforcement coverage grows per PR exactly as views are retrofitted (verified in each
branch's `A11yScannerTest.cs`):

| PR | Branch | Enforcing views added |
|----|--------|----------------------|
| #31 | a11y/phase1-core | ExplorerHome |
| #32 | a11y/phase1-slice2 | + DataExplorer |
| #33 | a11y/phase1-status-wiring | + SchemaExplorer |
| #34 | a11y/phase1-search | + all 5 dialogs (CopyKitTarget, CopyKits, MultiPaste, DataTransfer, ConfirmClose) |
| #35 | a11y/phase1-flat-mode | + DataExplorer flat-mode-ON variant (`Scan_DataExplorer_FlatModeOn_EnforcesInvariants`) |

All 5 PRs: build-and-test green on all 3 OSes, lint green.

## Visual tests — CONFIRMED

The four visual baseline classes (`ExplorerHomeVisualTest`, `DataExplorerVisualTest`,
`SchemaExplorerVisualTest`, `DialogVisualTest`) run in CI on all 3 OSes and stayed green
across all 5 PRs with **zero baseline changes** — legitimately:
- a11y annotations (AutomationProperties.Name/HelpText/AutomationId/LiveSetting,
  HeadingLevel, LandmarkType) are non-visual attached properties.
- Status live-region TextBlocks (PR #33) render zero-height when empty.
- Flat mode (PR #35) defaults off (`isFlatModeEnabled = false`); default render unchanged.

This matches the plan v2 guardrail: "baselines green; re-baselines deliberate and reviewed."

## Non-blocking observations

1. **Flat-mode-ON has no pixel baseline.** It is covered by interaction tests
   (`FlatFieldListInteractionTest`: toggle visibility, edit parity) and the enforcing
   scanner, but the new user-facing visual state has no committed PNG baseline. Optional
   follow-up: add a `data-explorer-flat-mode.png` baseline to `DataExplorerVisualTest`.
2. **PR #34 Coveralls check missing** despite the "Upload coverage to Coveralls" step
   succeeding (`fail-on-error: false` masks service-side check-creation failures). The
   90% coverage-threshold gate passed in-job, so this is observability only, unrelated
   to the a11y/visual goal.
