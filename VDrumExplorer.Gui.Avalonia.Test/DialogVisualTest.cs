// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Linq;
using Avalonia.Headless.XUnit;
using VDrumExplorer.Gui.Avalonia.Views.Dialogs;
using VDrumExplorer.Model.Schema.Logical;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Dialogs;
using Xunit;

namespace VDrumExplorer.Gui.Avalonia.Test;

/// <summary>
/// Visual acceptance tests for the four dialog windows: <see cref="CopyKitTargetDialog"/>,
/// <see cref="CopyKitsDialog"/>, <see cref="MultiPasteDialog"/> and <see cref="DataTransferDialog"/>.
/// Each dialog is rendered headlessly (Skia + Avalonia.Headless, real pixel output), captured
/// to a bitmap and checked in two ways:
/// <list type="number">
///   <item>A "ProducesVisualResult" test: the render must actually produce visible content -
///   not a blank, transparent or uniform bitmap.</item>
///   <item>A "MatchesBaseline" test: the render must match a committed baseline screenshot
///   within a tolerance. If no baseline exists yet (first run), the current render is saved
///   as the new baseline and the test passes (bootstrap mode).</item>
/// </list>
/// </summary>
public class DialogVisualTest
{
    [AvaloniaFact]
    public void CopyKitTargetDialog_ProducesVisualResult()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new CopyKitTargetDialog { DataContext = CreateCopyKitViewModel() });
        VisualTestHelper.AssertProducesVisualResult(bitmap, "copy-kit-target-dialog.png");
    }

    [AvaloniaFact]
    public void CopyKitTargetDialog_MatchesBaseline()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new CopyKitTargetDialog { DataContext = CreateCopyKitViewModel() });
        VisualTestHelper.AssertMatchesBaseline(bitmap, "copy-kit-target-dialog.png");
    }

    [AvaloniaFact]
    public void CopyKitsDialog_ProducesVisualResult()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new CopyKitsDialog { DataContext = CreateCopyKitsViewModel() });
        VisualTestHelper.AssertProducesVisualResult(bitmap, "copy-kits-dialog.png");
    }

    [AvaloniaFact]
    public void CopyKitsDialog_MatchesBaseline()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new CopyKitsDialog { DataContext = CreateCopyKitsViewModel() });
        VisualTestHelper.AssertMatchesBaseline(bitmap, "copy-kits-dialog.png");
    }

    [AvaloniaFact]
    public void MultiPasteDialog_ProducesVisualResult()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new MultiPasteDialog { DataContext = CreateMultiPasteViewModel() });
        VisualTestHelper.AssertProducesVisualResult(bitmap, "multi-paste-dialog.png");
    }

    [AvaloniaFact]
    public void MultiPasteDialog_MatchesBaseline()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new MultiPasteDialog { DataContext = CreateMultiPasteViewModel() });
        VisualTestHelper.AssertMatchesBaseline(bitmap, "multi-paste-dialog.png");
    }

    [AvaloniaFact]
    public void DataTransferDialog_ProducesVisualResult()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new DataTransferDialog { DataContext = CreateDataTransferViewModel() });
        VisualTestHelper.AssertProducesVisualResult(bitmap, "data-transfer-dialog.png");
    }

    [AvaloniaFact]
    public void DataTransferDialog_MatchesBaseline()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new DataTransferDialog { DataContext = CreateDataTransferViewModel() });
        VisualTestHelper.AssertMatchesBaseline(bitmap, "data-transfer-dialog.png");
    }

    /// <summary>
    /// Creates a <see cref="CopyKitViewModel"/> for copying kit 1 of the TD-27 sample module.
    /// </summary>
    private static CopyKitViewModel CreateCopyKitViewModel()
    {
        var module = TestData.LoadTD27Module();
        return new CopyKitViewModel(module, module.ExportKit(1));
    }

    /// <summary>
    /// Creates a <see cref="CopyKitsViewModel"/> for the TD-27 sample module.
    /// </summary>
    private static CopyKitsViewModel CreateCopyKitsViewModel() =>
        new CopyKitsViewModel(TestData.LoadTD27Module());

    /// <summary>
    /// Creates a <see cref="MultiPasteViewModel"/> with a snapshot of the TD-27 module root
    /// and all valid paste target candidates (the root itself, as no other node shares its
    /// variable-stripped path).
    /// </summary>
    private static MultiPasteViewModel CreateMultiPasteViewModel()
    {
        var module = TestData.LoadTD27Module();
        var data = module.Data;
        var schemaNode = data.LogicalRoot.SchemaNode;
        var snapshot = new NodeSnapshot(schemaNode, data.CreatePartialSnapshot(schemaNode));
        var candidates = schemaNode.DescendantsAndSelf().Where(snapshot.IsValidForTarget).ToList();
        return new MultiPasteViewModel(snapshot, candidates);
    }

    /// <summary>
    /// Creates a <see cref="DataTransferViewModel"/> in its initial state (0 of 0 items
    /// transferred), which is sufficient for a deterministic screenshot.
    /// </summary>
    private static DataTransferViewModel CreateDataTransferViewModel() =>
        new DataTransferViewModel("Loading module data");
}
