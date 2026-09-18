// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.ViewModel;
using AutomationLandmarkType = Avalonia.Automation.Peers.AutomationLandmarkType;
using VDrumExplorer.Gui.Avalonia.Test.A11y;
using VDrumExplorer.Gui.Avalonia.Test.Stubs;
using VDrumExplorer.Gui.Avalonia.Views;
using VDrumExplorer.Gui.Avalonia.Views.Dialogs;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.Home;
using VDrumExplorer.ViewModel.LogicalSchema;
using VDrumExplorer.ViewModel.Logging;
using Xunit;

namespace VDrumExplorer.Gui.Avalonia.Test;

/// <summary>
/// Phase 0 accessibility scanner tests (report-only mode).
/// <para>
/// Every existing view is instantiated with the same view-model construction used by the
/// visual tests, shown headlessly, and scanned by <see cref="A11yScanner"/>. The scan
/// produces the violation inventory committed at
/// <c>VDrumExplorer.Gui.Avalonia.Test/A11yInventory/report.md</c>, which is the Phase 1/2
/// retrofit backlog.
/// </para>
/// <para>
/// These tests are REPORT-ONLY by design: they assert that the scanner runs and produces
/// counts for each view, not that the views are accessible. To regenerate the committed
/// report, rebuild and run the test project:
/// <code>dotnet build VDrumExplorer.Gui.Avalonia.Test -c Release &amp;&amp;
/// dotnet VDrumExplorer.Gui.Avalonia.Test/bin/Release/net10.0/VDrumExplorer.Gui.Avalonia.Test.dll</code>
/// then commit the regenerated <c>A11yInventory/report.md</c>.
/// </para>
/// <para>
/// Positive-control tests prove the scanner itself works: a fully annotated synthetic
/// window must produce zero violations, and windows with missing names, blocklisted
/// names and missing IDs must be caught. Enforcing mode (used in Phase 1 once views are
/// annotated) is exercised via <see cref="A11yEnforcementException"/>.
/// </para>
/// </summary>
public class A11yScannerTest
{
    // === Per-view inventory scans (report-only; these pass regardless of violation counts) ===

    [AvaloniaFact]
    public void Scan_ExplorerHome_ProducesInventory()
    {
        var result = ScanView(CreateExplorerHome);
        Assert.True(result.ControlsScanned > 0, "No controls scanned.");
        Assert.True(result.InteractiveControls > 0, "No interactive controls found in ExplorerHome.");
    }

    [AvaloniaFact]
    public void Scan_DataExplorer_ProducesInventory()
    {
        var result = ScanView(CreateDataExplorer);
        Assert.True(result.ControlsScanned > 0, "No controls scanned.");
        Assert.True(result.InteractiveControls > 0, "No interactive controls found in DataExplorer.");
    }

    [AvaloniaFact]
    public void Scan_SchemaExplorer_ProducesInventory()
    {
        var result = ScanView(CreateSchemaExplorer);
        Assert.True(result.ControlsScanned > 0, "No controls scanned.");
        Assert.True(result.InteractiveControls > 0, "No interactive controls found in SchemaExplorer.");
    }

    [AvaloniaFact]
    public void Scan_CopyKitTargetDialog_ProducesInventory()
    {
        var result = ScanView(CreateCopyKitTargetDialog);
        Assert.True(result.ControlsScanned > 0, "No controls scanned.");
        Assert.True(result.InteractiveControls > 0, "No interactive controls found in CopyKitTargetDialog.");
    }

    [AvaloniaFact]
    public void Scan_CopyKitsDialog_ProducesInventory()
    {
        var result = ScanView(CreateCopyKitsDialog);
        Assert.True(result.ControlsScanned > 0, "No controls scanned.");
        Assert.True(result.InteractiveControls > 0, "No interactive controls found in CopyKitsDialog.");
    }

    [AvaloniaFact]
    public void Scan_MultiPasteDialog_ProducesInventory()
    {
        var result = ScanView(CreateMultiPasteDialog);
        Assert.True(result.ControlsScanned > 0, "No controls scanned.");
        Assert.True(result.InteractiveControls > 0, "No interactive controls found in MultiPasteDialog.");
    }

    [AvaloniaFact]
    public void Scan_DataTransferDialog_ProducesInventory()
    {
        var result = ScanView(CreateDataTransferDialog);
        Assert.True(result.ControlsScanned > 0, "No controls scanned.");
        Assert.True(result.InteractiveControls > 0, "No interactive controls found in DataTransferDialog.");
    }

    [AvaloniaFact]
    public void Scan_ConfirmCloseDialog_ProducesInventory()
    {
        var result = ScanView(() => new ConfirmCloseDialog());
        Assert.True(result.ControlsScanned > 0, "No controls scanned.");
        Assert.True(result.InteractiveControls > 0, "No interactive controls found in ConfirmCloseDialog.");
    }

    /// <summary>
    /// Scans all eight views and writes the violation inventory report to
    /// <c>VDrumExplorer.Gui.Avalonia.Test/A11yInventory/report.md</c> (committed to the
    /// repository as the retrofit backlog; see the class comment for regeneration).
    /// </summary>
    [AvaloniaFact]
    public void Scan_AllViews_WritesInventoryReport()
    {
        var views = new (string Name, Func<Window> Create)[]
        {
            ("ExplorerHome", CreateExplorerHome),
            ("DataExplorer", CreateDataExplorer),
            ("SchemaExplorer", CreateSchemaExplorer),
            ("CopyKitTargetDialog", CreateCopyKitTargetDialog),
            ("CopyKitsDialog", CreateCopyKitsDialog),
            ("MultiPasteDialog", CreateMultiPasteDialog),
            ("DataTransferDialog", CreateDataTransferDialog),
            ("ConfirmCloseDialog", () => new ConfirmCloseDialog()),
        };

        var results = new List<A11yScanResult>();
        foreach (var (name, create) in views)
        {
            results.Add(ScanView(create));
        }

        string reportPath = WriteInventoryReport(results);
        string report = File.ReadAllText(reportPath);

        Assert.True(File.Exists(reportPath), $"Inventory report was not written to {reportPath}.");
        foreach (var view in views)
        {
            Assert.Contains(view.Name, report);
        }
        Assert.Contains("TOTAL", report);
    }

    // === Positive controls (prove the scanner catches what it should) ===

    [AvaloniaFact]
    public void Scan_CompliantWindow_ReportsZeroViolations()
    {
        var panel = new StackPanel();
        AutomationProperties.SetLandmarkType(panel, AutomationLandmarkType.Main);

        var header = new TextBlock { Text = "Kick settings" };
        AutomationProperties.SetHeadingLevel(header, 1);

        var statusLine = new TextBlock { Text = "Ready" };
        AutomationProperties.SetLiveSetting(statusLine, AutomationLiveSetting.Polite);
        AutomationProperties.SetAutomationId(statusLine, "statusLine");
        AutomationProperties.SetName(statusLine, "Status");

        var button = new Button { Content = "Kick volume" };
        AutomationProperties.SetAutomationId(button, "btnKickVolume");

        var textBox = new TextBox();
        AutomationProperties.SetName(textBox, "Kit name");
        AutomationProperties.SetAutomationId(textBox, "txtKitName");

        panel.Children.Add(header);
        panel.Children.Add(button);
        panel.Children.Add(textBox);
        panel.Children.Add(statusLine);

        var window = new Window { Content = panel };
        var result = ScanView(() => window);

        Assert.Empty(result.Violations);
    }

    [AvaloniaFact]
    public void Scan_MissingName_CatchesInteractiveControl()
    {
        var button = new Button();
        AutomationProperties.SetAutomationId(button, "btnUnnamed");

        var window = new Window { Content = button };
        var result = ScanView(() => window);

        var violation = Assert.Single(result.Violations, v => v.Rule == "MISSING_NAME" && v.ControlType == "Button");
        Assert.Equal(A11ySeverity.Error, violation.Severity);
    }

    [AvaloniaFact]
    public void Scan_BlocklistedName_CatchesControlTypeWord()
    {
        var button = new Button { Content = "Kick volume button" };
        AutomationProperties.SetAutomationId(button, "btnKick");

        var window = new Window { Content = button };
        var result = ScanView(() => window);

        Assert.Contains(result.Violations, v => v.Rule == "NAME_CONTAINS_TYPE_WORD" && v.ControlType == "Button");
    }

    [AvaloniaFact]
    public void Scan_EnforceMode_ThrowsOnErrors()
    {
        var button = new Button();
        AutomationProperties.SetAutomationId(button, "btnUnnamed");

        var window = new Window { Content = button };
        var exception = Assert.Throws<A11yEnforcementException>(() => ScanView(() => window, enforce: true));
        Assert.Contains("MISSING_NAME", exception.Message);
    }

    // === View factories (mirroring the visual tests' view-model construction) ===

    private static Window CreateExplorerHome() => new ExplorerHome
    {
        DataContext = new ExplorerHomeViewModel(
            new StubViewServices(),
            new LogViewModel(),
            new DeviceViewModel(),
            new StubAudioDeviceManager()),
    };

    private static Window CreateDataExplorer() => new DataExplorer
    {
        DataContext = new KitExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), TestData.LoadTD27Kit()),
    };

    private static Window CreateSchemaExplorer() => new SchemaExplorer
    {
        DataContext = CreateModuleSchemaViewModel(),
    };

    private static ModuleSchemaViewModel CreateModuleSchemaViewModel()
    {
        var schema = ModuleSchema.KnownSchemas[ModuleIdentifier.TD27].Value;
        return new ModuleSchemaViewModel(schema);
    }

    private static Window CreateCopyKitTargetDialog()
    {
        var module = TestData.LoadTD27Module();
        return new CopyKitTargetDialog
        {
            DataContext = new CopyKitViewModel(module, module.ExportKit(1)),
        };
    }

    private static Window CreateCopyKitsDialog() => new CopyKitsDialog
    {
        DataContext = new CopyKitsViewModel(TestData.LoadTD27Module()),
    };

    private static Window CreateMultiPasteDialog() => new MultiPasteDialog
    {
        DataContext = CreateMultiPasteViewModel(),
    };

    private static MultiPasteViewModel CreateMultiPasteViewModel()
    {
        var module = TestData.LoadTD27Module();
        var data = module.Data;
        var schemaNode = data.LogicalRoot.SchemaNode;
        var snapshot = new NodeSnapshot(schemaNode, data.CreatePartialSnapshot(schemaNode));
        var candidates = schemaNode.DescendantsAndSelf().Where(snapshot.IsValidForTarget).ToList();
        return new MultiPasteViewModel(snapshot, candidates);
    }

    private static Window CreateDataTransferDialog() => new DataTransferDialog
    {
        DataContext = new DataTransferViewModel("Loading module data"),
    };

    // === Scan + report helpers ===

    /// <summary>
    /// Shows a view headlessly, scans it with <see cref="A11yScanner"/> (report-only mode by
    /// default; enforcing mode when <paramref name="enforce"/> is true) and closes it again.
    /// </summary>
    private static A11yScanResult ScanView(Func<Window> createView, bool enforce = false)
    {
        var window = createView();
        try
        {
            window.Show();
            window.UpdateLayout();
            return A11yScanner.Scan(window, window.GetType().Name, new A11yScanOptions { Enforce = enforce });
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Writes the inventory report into the source project directory (found by walking up
    /// from the test output directory, mirroring <see cref="VisualTestHelper"/>'s baseline
    /// lookup) so the committed copy stays authoritative. Returns the path written.
    /// </summary>
    private static string WriteInventoryReport(IReadOnlyList<A11yScanResult> results)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "VDrumExplorer.Gui.Avalonia.Test.csproj")))
        {
            directory = directory.Parent!;
        }
        string root = directory?.FullName ?? AppContext.BaseDirectory;
        string inventoryDirectory = Path.Combine(root, "A11yInventory");
        Directory.CreateDirectory(inventoryDirectory);
        string path = Path.Combine(inventoryDirectory, "report.md");

        var sb = new StringBuilder();
        sb.AppendLine("# A11y violation inventory (Phase 0, report-only)");
        sb.AppendLine();
        sb.AppendLine("Generated by `A11yScannerTest.Scan_AllViews_WritesInventoryReport` — the scanner is in");
        sb.AppendLine("report-only mode: these counts are the retrofit backlog for Phase 1/2, NOT failures.");
        sb.AppendLine("Regenerate by rebuilding and running the test project, then commit the result.");
        sb.AppendLine();
        sb.AppendLine("| View | Controls | Interactive | Errors | Warnings | Info |");
        sb.AppendLine("|------|----------|-------------|--------|----------|------|");
        int totalControls = 0, totalInteractive = 0, totalErrors = 0, totalWarnings = 0, totalInfo = 0;
        foreach (var result in results)
        {
            int errors = result.Count(A11ySeverity.Error);
            int warnings = result.Count(A11ySeverity.Warning);
            int info = result.Count(A11ySeverity.Info);
            sb.AppendLine($"| {result.ViewName} | {result.ControlsScanned} | {result.InteractiveControls} | {errors} | {warnings} | {info} |");
            totalControls += result.ControlsScanned;
            totalInteractive += result.InteractiveControls;
            totalErrors += errors;
            totalWarnings += warnings;
            totalInfo += info;
        }
        sb.AppendLine($"| **TOTAL** | {totalControls} | {totalInteractive} | {totalErrors} | {totalWarnings} | {totalInfo} |");
        sb.AppendLine();
        foreach (var result in results)
        {
            sb.AppendLine(A11yScanner.ToMarkdown(result));
        }

        File.WriteAllText(path, sb.ToString());
        return path;
    }
}
