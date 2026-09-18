// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace VDrumExplorer.Gui.Avalonia.Test.A11y;

/// <summary>
/// Severity of a scanner violation.
/// </summary>
public enum A11ySeverity
{
    /// <summary>Structural violation that will fail enforcing mode (missing name/ID, live-region policy).</summary>
    Error = 0,

    /// <summary>Naming-quality violation (control-type words in names, duplicate IDs, decorative noise).</summary>
    Warning = 1,

    /// <summary>Report-only structural gap (heading levels, landmarks, tab-order heuristic).</summary>
    Info = 2,
}

/// <summary>
/// A single violation found by the scanner.
/// </summary>
/// <param name="Rule">Stable rule identifier (used as the report/backlog key).</param>
/// <param name="Severity">Severity of the violation.</param>
/// <param name="ControlType">Runtime type of the offending control.</param>
/// <param name="ControlName">Best-effort identifier for the control (AutomationId, name, or text).</param>
/// <param name="Message">Human-readable description of the violation.</param>
public sealed record A11yViolation(string Rule, A11ySeverity Severity, string ControlType, string ControlName, string Message)
{
    public override string ToString() =>
        $"{Severity}: [{Rule}] {ControlType} '{ControlName}': {Message}";
}

/// <summary>
/// Options controlling the scanner.
/// </summary>
public sealed record A11yScanOptions
{
    /// <summary>
    /// False (default) = report-only mode: violations are collected but never thrown.
    /// True = enforcing mode: <see cref="A11yScanner.Scan"/> throws
    /// <see cref="A11yEnforcementException"/> if any Error-severity violation is found.
    /// Adopted per-view as views are retrofitted (Phase 1/2).
    /// </summary>
    public bool Enforce { get; init; }
}

/// <summary>
/// Result of scanning one rooted control/window.
/// </summary>
public sealed class A11yScanResult
{
    /// <summary>Name given to the scan (usually the view type name).</summary>
    public required string ViewName { get; init; }

    /// <summary>All violations found, in traversal order.</summary>
    public required ImmutableArray<A11yViolation> Violations { get; init; }

    /// <summary>Total number of Avalonia <see cref="Control"/>s visited in the visual tree.</summary>
    public required int ControlsScanned { get; init; }

    /// <summary>Number of interactive (focusable/known interactive type) controls found.</summary>
    public required int InteractiveControls { get; init; }

    /// <summary>Number of violations by severity.</summary>
    public int Count(A11ySeverity severity) => Violations.Count(v => v.Severity == severity);

    /// <summary>Number of violations per rule, ordered by rule name.</summary>
    public IReadOnlyList<(string Rule, int Count)> CountsByRule() =>
        Violations
            .GroupBy(v => v.Rule)
            .Select(g => (Rule: g.Key, Count: g.Count()))
            .OrderBy(x => x.Rule, StringComparer.Ordinal)
            .ToList();
}

/// <summary>
/// Thrown by <see cref="A11yScanner.Scan"/> in enforcing mode when Error-severity
/// violations are found.
/// </summary>
public sealed class A11yEnforcementException : Exception
{
    public A11yEnforcementException(string viewName, IReadOnlyList<A11yViolation> violations)
        : base($"A11y enforcement failed for '{viewName}' ({violations.Count} error(s)):\n" +
               string.Join("\n", violations.Select(v => "  " + v)))
    {
        Violations = violations;
    }

    public IReadOnlyList<A11yViolation> Violations { get; }
}

/// <summary>
/// Headless accessibility invariant scanner for Avalonia views.
/// Walks the visual tree of a rooted window/control and checks a11y invariants using the
/// <see cref="AutomationProperties"/> attached-property getters directly (reliable in the
/// headless test platform, unlike instantiating platform automation peers).
/// Phase 0 runs in report-only mode; the violation inventory becomes the retrofit backlog.
/// </summary>
public static class A11yScanner
{
    /// <summary>
    /// Names that must not appear in accessible names (blind-user rule: never announce the
    /// control type; the screen reader already knows it from the control type role).
    /// Matched case-insensitively as whole words.
    /// </summary>
    private static readonly string[] TypeWordBlocklist =
    [
        "button", "slider", "check box", "checkbox", "combo box", "menu item",
        "text box", "textbox", "label", "list box",
    ];

    /// <summary>Compiled word-boundary pattern per blocklist entry (built once, statically).</summary>
    private static readonly ImmutableArray<Regex> TypeWordPatterns =
        TypeWordBlocklist
            .Select(word => new Regex(
                $@"(?i)\b{Regex.Escape(word)}\b"))
            .ToImmutableArray();

    /// <summary>
    /// Control types treated as interactive/testable even if their <c>Focusable</c> property
    /// has not yet been resolved (e.g. the control is not attached to a themed window).
    /// </summary>
    private static readonly Type[] KnownInteractiveTypes =
    [
        typeof(Button),
        typeof(TextBox),
        typeof(ComboBox),
        typeof(CheckBox),
        typeof(Slider),
        typeof(MenuItem),
        typeof(TreeView),
        typeof(TreeViewItem),
        typeof(ListBox),
        typeof(ListBoxItem),
        typeof(ToggleButton),
        typeof(RadioButton),
        typeof(NumericUpDown),
        typeof(DatePicker),
        typeof(CalendarDatePicker),
        typeof(TimePicker),
        typeof(TabItem),
        typeof(ComboBoxItem),
    ];

    /// <summary>
    /// Types whose string-typed content property counts as a visible accessible name
    /// when no <c>AutomationProperties.Name</c> is set.
    /// </summary>
    private static readonly Type[] NameSourceTypeHierarchy =
    [
        typeof(TextBlock),
        typeof(ContentControl),
        typeof(HeaderedContentControl),
    ];

    /// <summary>
    /// Item-container types: their name may be derived from the first visible text in
    /// their subtree, because the header is generated from an item template.
    /// </summary>
    private static readonly Type[] ItemContainerTypes =
    [
        typeof(TreeViewItem),
        typeof(ListBoxItem),
        typeof(ComboBoxItem),
        typeof(MenuItem),
        typeof(TabItem),
    ];

    /// <summary>
    /// Container types that act as section/major-region candidates for the report-only
    /// heading/landmark statistics.
    /// </summary>
    private static readonly Type[] RegionCandidateTypes =
    [
        typeof(HeaderedContentControl),
        typeof(TabControl),
        typeof(Expander),
        typeof(UserControl),
    ];

    /// <summary>
    /// Types that may act as decorative (announced-but-empty) elements.
    /// </summary>
    private static readonly Type[] DecorativeCandidateTypes =
    [
        typeof(TextBlock),
        typeof(Image),
        typeof(Separator),
    ];

    /// <summary>
    /// Scans the visual tree rooted at <paramref name="root"/> (typically a shown
    /// <see cref="Window"/>) and returns every violation found.
    /// </summary>
    /// <param name="root">Rooted control to scan.</param>
    /// <param name="viewName">Name of the view being scanned, used in reports/exceptions.</param>
    /// <param name="enforce">
    /// False (default) = report-only mode. True = enforcing mode: throws
    /// <see cref="A11yEnforcementException"/> if any Error-severity violation is found.
    /// </param>
    public static A11yScanResult Scan(Control root, string viewName, bool enforce = false) =>
        Scan(root, viewName, new A11yScanOptions { Enforce = enforce });

    /// <summary>
    /// Scans the visual tree rooted at <paramref name="root"/> (typically a shown
    /// <see cref="Window"/>) and returns every violation found.
    /// </summary>
    public static A11yScanResult Scan(Control root, string viewName, A11yScanOptions options)
    {
        var violations = ImmutableArray.CreateBuilder<A11yViolation>();
        var controls = new List<Control>();
        var automationIds = new Dictionary<string, List<Control>>(StringComparer.Ordinal);
        int politeLiveRegions = 0;
        int assertiveLiveRegions = 0;

        // Walk the visual tree depth-first, in deterministic order.
        controls.Add(root);
        Visit(root);
        foreach (var descendant in root.GetVisualDescendants().OfType<Control>())
        {
            controls.Add(descendant);
            Visit(descendant);
        }

        // Live-region policy: exactly one status element per window.
        RegisterDuplicates();
        if (politeLiveRegions + assertiveLiveRegions == 0)
        {
            violations.Add(new A11yViolation(
                "LIVE_REGION_MISSING", A11ySeverity.Error, root.GetType().Name, Describe(root),
                "No element sets AutomationProperties.LiveSetting; the window has no live-region status line."));
        }
        if (politeLiveRegions > 1)
        {
            violations.Add(new A11yViolation(
                "MULTIPLE_POLITE_LIVE_REGIONS", A11ySeverity.Error, root.GetType().Name, Describe(root),
                $"Found {politeLiveRegions} Polite live regions; exactly one is expected per window."));
        }
        if (assertiveLiveRegions > 1)
        {
            violations.Add(new A11yViolation(
                "MULTIPLE_ASSERTIVE_LIVE_REGIONS", A11ySeverity.Warning, root.GetType().Name, Describe(root),
                $"Found {assertiveLiveRegions} Assertive live regions; errors should share one assertive channel."));
        }

        var result = new A11yScanResult
        {
            ViewName = viewName,
            Violations = violations.ToImmutable(),
            ControlsScanned = controls.Count,
            InteractiveControls = controls.Count(IsInteractive),
        };

        if (options.Enforce && result.Count(A11ySeverity.Error) > 0)
        {
            throw new A11yEnforcementException(
                viewName, result.Violations.Where(v => v.Severity == A11ySeverity.Error).ToList());
        }

        return result;

        void Visit(Control control)
        {
            if (control is not StyledElement styled)
            {
                return;
            }

            var type = control.GetType();

            // --- AutomationId presence + duplicate detection (interactive controls only).
            if (IsInteractive(control))
            {
                var id = AutomationProperties.GetAutomationId(styled);
                if (string.IsNullOrWhiteSpace(id))
                {
                    violations.Add(new A11yViolation(
                        "MISSING_AUTOMATION_ID", A11ySeverity.Error, type.Name, Describe(control),
                        "Interactive control has no AutomationProperties.AutomationId."));
                }
                else
                {
                    if (!automationIds.TryGetValue(id, out var owners))
                    {
                        owners = new List<Control>();
                        automationIds[id] = owners;
                    }
                    owners.Add(control);
                }

                // --- Accessible name: AutomationProperties.Name or visible text content.
                string? attachedName = AutomationProperties.GetName(styled);
                bool hasAttachedName = !string.IsNullOrWhiteSpace(attachedName);
                string? derivedText = GetDerivedText(control);
                string name = hasAttachedName ? attachedName! : derivedText ?? "";

                if (!hasAttachedName && derivedText is null)
                {
                    violations.Add(new A11yViolation(
                        "MISSING_NAME", A11ySeverity.Error, type.Name, Describe(control),
                        "Interactive control has no AutomationProperties.Name and no visible text content."));
                }

                // --- Naming convention: no control-type words in the name.
                foreach (var (word, pattern) in TypeWordBlocklist.Zip(TypeWordPatterns))
                {
                    if (name.Length > 0 && pattern.IsMatch(name))
                    {
                        violations.Add(new A11yViolation(
                            "NAME_CONTAINS_TYPE_WORD", A11ySeverity.Error, type.Name, Describe(control),
                            $"Name '{name}' contains the control-type word '{word}'."));
                    }
                }

                // --- Tab-order heuristic (report-only): a focusable control with IsTabStop=false
                // is not keyboard-reachable via Tab.
                if (control.Focusable && !control.IsTabStop)
                {
                    violations.Add(new A11yViolation(
                        "TAB_ORDER_UNREACHABLE", A11ySeverity.Info, type.Name, Describe(control),
                        "Control is focusable but IsTabStop is false; not reachable via Tab."));
                }
            }

            // --- Decorative elements: must be excluded from the accessibility view.
            if (IsDecorativeCandidate(control, styled))
            {
                var view = AutomationProperties.GetAccessibilityView(styled);
                if (view == AccessibilityView.Default)
                {
                    violations.Add(new A11yViolation(
                        "DECORATIVE_IN_ACCESSIBILITY_VIEW", A11ySeverity.Info, type.Name, Describe(control),
                        "Decorative (non-interactive, unnamed) element is not excluded from the accessibility view."));
                }
            }

            // --- Section headers: report-only count of headered regions missing HeadingLevel.
            if (RegionCandidateTypes.Any(t => t.IsAssignableFrom(type)) &&
                AutomationProperties.GetHeadingLevel(styled) == 0)
            {
                violations.Add(new A11yViolation(
                    "MISSING_HEADING_LEVEL", A11ySeverity.Info, type.Name, Describe(control),
                    "Section-header candidate has no AutomationProperties.HeadingLevel (report-only)."));
            }

            // --- Major regions: report-only count of region candidates missing LandmarkType.
            if (RegionCandidateTypes.Any(t => t.IsAssignableFrom(type)) &&
                AutomationProperties.GetLandmarkType(styled) is null)
            {
                violations.Add(new A11yViolation(
                    "MISSING_LANDMARK", A11ySeverity.Info, type.Name, Describe(control),
                    "Major-region candidate has no AutomationProperties.LandmarkType (report-only)."));
            }

            // --- Live regions: count them for the window-level policy check.
            var liveSetting = AutomationProperties.GetLiveSetting(styled);
            switch (liveSetting)
            {
                case AutomationLiveSetting.Polite:
                    politeLiveRegions++;
                    break;
                case AutomationLiveSetting.Assertive:
                    assertiveLiveRegions++;
                    break;
            }
        }

        void RegisterDuplicates()
        {
            foreach (var (id, owners) in automationIds)
            {
                if (owners.Count > 1)
                {
                    violations.Add(new A11yViolation(
                        "DUPLICATE_AUTOMATION_ID", A11ySeverity.Warning,
                        owners[0].GetType().Name, id,
                        $"AutomationId '{id}' is used by {owners.Count} controls."));
                }
            }
        }
    }

    /// <summary>Checks whether a control is interactive (focusable or a known interactive type).</summary>
    private static bool IsInteractive(Control control)
    {
        if (control.Focusable)
        {
            return true;
        }
        var type = control.GetType();
        return KnownInteractiveTypes.Any(t => t.IsAssignableFrom(type));
    }

    /// <summary>
    /// Returns visible text content usable as an accessible name, or null if the control
    /// exposes no string content of its own. Editable value controls (TextBox, ComboBox,
    /// Slider, NumericUpDown) deliberately return null: their value is not a name, so they
    /// require an <c>AutomationProperties.Name</c> (that is the retrofit work).
    /// </summary>
    private static string? GetDerivedText(Control control)
    {
        var type = control.GetType();

        // Item containers derive their name from the first text in their (templated) subtree.
        if (ItemContainerTypes.Any(t => t.IsAssignableFrom(type)))
        {
            var itemText = control.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(tb => tb.Text)
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
            return itemText is null ? null : itemText;
        }

        if (NameSourceTypeHierarchy.Any(t => t.IsAssignableFrom(type)))
        {
            switch (control)
            {
                case TextBlock textBlock:
                    return textBlock.Text;
                case HeaderedContentControl headered:
                    return headered.Header as string;
                case ContentControl content:
                    return content.Content as string;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether the element is a decorative candidate: a TextBlock/Image/Separator
    /// that is not interactive and carries no name or text. Template-generated elements
    /// (<see cref="Visual.TemplatedParent"/> is set, e.g. the watermark inside a TextBox
    /// theme) are control-authored, not view-authored, and are never flagged.
    /// </summary>
    private static bool IsDecorativeCandidate(Control control, StyledElement styled)
    {
        if (IsInteractive(control) || control.TemplatedParent is not null)
        {
            return false;
        }
        var type = control.GetType();
        if (!DecorativeCandidateTypes.Any(t => t.IsAssignableFrom(type)))
        {
            return false;
        }
        if (!string.IsNullOrWhiteSpace(AutomationProperties.GetName(styled)))
        {
            return false;
        }
        return GetOwnText(control) is null or "";
    }

    /// <summary>Returns only the control's own string content (not item-template subtree text).</summary>
    private static string? GetOwnText(Control control)
    {
        switch (control)
        {
            case TextBlock textBlock:
                return textBlock.Text;
            case HeaderedContentControl headered:
                return headered.Header as string;
            case ContentControl content:
                return content.Content as string;
            default:
                return null;
        }
    }

    /// <summary>Best-effort identifier used in violation descriptions.</summary>
    private static string Describe(StyledElement element)
    {
        if (AutomationProperties.GetAutomationId(element) is { } id)
        {
            return id;
        }
        return (element as Control) is { } control ? GetOwnText(control) ?? "" : "";
    }

    /// <summary>
    /// Renders a scan result as a markdown section (used by the report writer and tests).
    /// </summary>
    public static string ToMarkdown(A11yScanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"### {result.ViewName}");
        sb.AppendLine();
        sb.AppendLine($"- Controls scanned: {result.ControlsScanned}");
        sb.AppendLine($"- Interactive controls: {result.InteractiveControls}");
        sb.AppendLine($"- Violations: {result.Violations.Length} " +
                      $"(errors {result.Count(A11ySeverity.Error)}, warnings {result.Count(A11ySeverity.Warning)}, info {result.Count(A11ySeverity.Info)})");
        sb.AppendLine();
        foreach (var (rule, count) in result.CountsByRule())
        {
            sb.AppendLine($"- `{rule}`: {count}");
        }
        sb.AppendLine();
        if (result.Violations.Length > 0)
        {
            sb.AppendLine("<details><summary>Violation details</summary>");
            sb.AppendLine();
            foreach (var violation in result.Violations)
            {
                sb.AppendLine($"- {violation}");
            }
            sb.AppendLine();
            sb.AppendLine("</details>");
        }
        return sb.ToString();
    }
}
