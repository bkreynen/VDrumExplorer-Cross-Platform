// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia.Headless.XUnit;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Gui.Avalonia.Test.Stubs;
using VDrumExplorer.Gui.Avalonia.Views;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Data;
using Xunit;

namespace VDrumExplorer.Gui.Avalonia.Test;

/// <summary>
/// Visual acceptance tests for the <see cref="DataExplorer"/> window in both of its
/// modes: the Kit Explorer (a single exported TD-27 kit) and the Module Explorer (the
/// full TD-27 module). The window is rendered headlessly (Skia + Avalonia.Headless,
/// real pixel output), captured to a bitmap and checked in two ways:
/// <list type="number">
///   <item><see cref="KitExplorer_ProducesVisualResult"/> and
///   <see cref="ModuleExplorer_ProducesVisualResult"/>: the render must actually
///   produce visible content - not a blank, transparent or uniform bitmap.</item>
///   <item><see cref="KitExplorer_MatchesBaseline"/> and
///   <see cref="ModuleExplorer_MatchesBaseline"/>: the render must match a committed
///   baseline screenshot within a tolerance. If no baseline exists yet (first run), the
///   current render is saved as the new baseline and the test passes (bootstrap mode).</item>
/// </list>
/// </summary>
public class DataExplorerVisualTest
{
    [AvaloniaFact]
    public void KitExplorer_ProducesVisualResult()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new DataExplorer { DataContext = CreateKitViewModel() });
        VisualTestHelper.AssertProducesVisualResult(bitmap, "kit-explorer.png");
    }

    [AvaloniaFact]
    public void KitExplorer_MatchesBaseline()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new DataExplorer { DataContext = CreateKitViewModel() });
        VisualTestHelper.AssertMatchesBaseline(bitmap, "kit-explorer.png");
    }

    [AvaloniaFact]
    public void ModuleExplorer_ProducesVisualResult()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new DataExplorer { DataContext = CreateModuleViewModel() });
        VisualTestHelper.AssertProducesVisualResult(bitmap, "module-explorer.png");
    }

    [AvaloniaFact]
    public void ModuleExplorer_MatchesBaseline()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new DataExplorer { DataContext = CreateModuleViewModel() });
        VisualTestHelper.AssertMatchesBaseline(bitmap, "module-explorer.png");
    }

    /// <summary>
    /// Creates a <see cref="KitExplorerViewModel"/> for the TD-27 sample kit, backed by
    /// test stubs (no device, no dialogs - keeping the render deterministic).
    /// </summary>
    private static KitExplorerViewModel CreateKitViewModel() =>
        new KitExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), TestData.LoadTD27Kit());

    /// <summary>
    /// Creates a <see cref="ModuleExplorerViewModel"/> for the TD-27 sample module, backed
    /// by test stubs (no device, no dialogs - keeping the render deterministic).
    /// </summary>
    private static ModuleExplorerViewModel CreateModuleViewModel() =>
        new ModuleExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), TestData.LoadTD27Module());
}
