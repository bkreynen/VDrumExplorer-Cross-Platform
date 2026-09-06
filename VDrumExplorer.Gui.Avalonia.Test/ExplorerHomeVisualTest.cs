// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using VDrumExplorer.Gui.Avalonia.Test.Stubs;
using VDrumExplorer.Gui.Avalonia.Views;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Home;
using VDrumExplorer.ViewModel.Logging;
using Xunit;

namespace VDrumExplorer.Gui.Avalonia.Test;

/// <summary>
/// Visual acceptance tests for the main <see cref="ExplorerHome"/> window.
/// The window is rendered headlessly (Skia + Avalonia.Headless, real pixel output),
/// captured to a bitmap and checked in two ways:
/// <list type="number">
///   <item><see cref="ExplorerHome_ProducesVisualResult"/>: the render must actually
///   produce visible content - not a blank, transparent or uniform bitmap.</item>
///   <item><see cref="ExplorerHome_MatchesBaseline"/>: the render must match a committed
///   baseline screenshot within a tolerance. If no baseline exists yet (first run), the
///   current render is saved as the new baseline and the test passes (bootstrap mode).</item>
/// </list>
/// </summary>
public class ExplorerHomeVisualTest
{
    private const string BaselineFileName = "explorer-home.png";

    [AvaloniaFact]
    public void ExplorerHome_ProducesVisualResult()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new ExplorerHome { DataContext = CreateViewModel() });
        VisualTestHelper.AssertProducesVisualResult(bitmap, BaselineFileName);
    }

    [AvaloniaFact]
    public void ExplorerHome_MatchesBaseline()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new ExplorerHome { DataContext = CreateViewModel() });
        VisualTestHelper.AssertMatchesBaseline(bitmap, BaselineFileName);
    }

    /// <summary>
    /// Creates an <see cref="ExplorerHomeViewModel"/> backed by test stubs (no device,
    /// no log entries - keeping the render deterministic).
    /// </summary>
    private static ExplorerHomeViewModel CreateViewModel() =>
        new ExplorerHomeViewModel(
            new StubViewServices(),
            new LogViewModel(),
            new DeviceViewModel(),
            new StubAudioDeviceManager());
}
