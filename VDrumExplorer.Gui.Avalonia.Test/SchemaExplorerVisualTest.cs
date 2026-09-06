// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia.Headless.XUnit;
using VDrumExplorer.Gui.Avalonia.Views;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel.LogicalSchema;
using Xunit;

namespace VDrumExplorer.Gui.Avalonia.Test;

/// <summary>
/// Visual acceptance tests for the <see cref="SchemaExplorer"/> window, showing the
/// TD-27 module schema. The window is rendered headlessly (Skia + Avalonia.Headless,
/// real pixel output), captured to a bitmap and checked in two ways:
/// <list type="number">
///   <item><see cref="SchemaExplorer_ProducesVisualResult"/>: the render must actually
///   produce visible content - not a blank, transparent or uniform bitmap.</item>
///   <item><see cref="SchemaExplorer_MatchesBaseline"/>: the render must match a committed
///   baseline screenshot within a tolerance. If no baseline exists yet (first run), the
///   current render is saved as the new baseline and the test passes (bootstrap mode).</item>
/// </list>
/// </summary>
public class SchemaExplorerVisualTest
{
    private const string BaselineFileName = "schema-explorer.png";

    [AvaloniaFact]
    public void SchemaExplorer_ProducesVisualResult()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new SchemaExplorer { DataContext = CreateViewModel() });
        VisualTestHelper.AssertProducesVisualResult(bitmap, BaselineFileName);
    }

    [AvaloniaFact]
    public void SchemaExplorer_MatchesBaseline()
    {
        using var bitmap = VisualTestHelper.RenderWindow(
            new SchemaExplorer { DataContext = CreateViewModel() });
        VisualTestHelper.AssertMatchesBaseline(bitmap, BaselineFileName);
    }

    /// <summary>
    /// Creates a <see cref="ModuleSchemaViewModel"/> for the known TD-27 schema.
    /// </summary>
    private static ModuleSchemaViewModel CreateViewModel()
    {
        var schema = ModuleSchema.KnownSchemas[ModuleIdentifier.TD27].Value;
        return new ModuleSchemaViewModel(schema);
    }
}
