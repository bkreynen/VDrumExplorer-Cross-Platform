// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Gui.Avalonia.Test.Stubs;
using VDrumExplorer.Gui.Avalonia.Views;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Data;
using Xunit;

namespace VDrumExplorer.Gui.Avalonia.Test;

/// <summary>
/// Headless interaction tests for the flat field-list mode (Phase 1 task 4,
/// docs/accessibility.md §4): toggling swaps the visible area (flat list on → details
/// pane hidden), editing through a flat-list editor mutates the <em>same</em> field view
/// model the details pane binds (zero divergence), and toggling back shows the edit in
/// the details pane. The mode is toggled via the view model command, exactly as the
/// File-menu item does. Editing is driven headlessly with <c>KeyTextInput</c> for
/// typed text (KeyPress only dispatches KeyDown/KeyUp) plus a Ctrl+A select-all
/// gesture via <c>KeyPressQwerty</c>.
/// </summary>
public class FlatFieldListInteractionTest
{
    private const string FlatAreaAutomationId = "data-explorer.flat-fields";

    [AvaloniaFact]
    public void Toggle_FlatListVisible_AndDetailsPaneHidden()
    {
        var (window, vm) = CreateWindow();

        // Default: details pane visible, flat list absent from the tree (sections empty
        // means the flat ItemsControl has no content; the ScrollViewer itself is hidden).
        var flatArea = FindFlatArea(window);
        Assert.NotNull(flatArea);
        Assert.False(flatArea!.IsVisible);
        Assert.Empty(vm.FlatFields.Sections);

        vm.FlatFields.ToggleFlatModeCommand.Execute(null!);
        window.UpdateLayout();

        Assert.True(vm.FlatFields.IsFlatModeEnabled);
        Assert.True(flatArea.IsVisible);
        Assert.NotEmpty(vm.FlatFields.Sections);
        window.Close();
    }

    [AvaloniaFact]
    public void Toggle_Off_DetailsPaneVisibleAgain()
    {
        var (window, vm) = CreateWindow();
        vm.FlatFields.ToggleFlatModeCommand.Execute(null!);
        window.UpdateLayout();
        var flatArea = FindFlatArea(window)!;
        Assert.True(flatArea.IsVisible);

        vm.FlatFields.ToggleFlatModeCommand.Execute(null!);
        window.UpdateLayout();

        Assert.False(flatArea.IsVisible);
        Assert.True(vm.FlatFields.IsTreeModeVisible);
        Assert.Empty(vm.FlatFields.Sections);
        window.Close();
    }

    [AvaloniaFact]
    public void FlatMode_EditFieldViaFlatEditor_SameFieldVmUpdated_AndDetailsPaneReflectsIt()
    {
        var (window, vm) = CreateWindow();

        // The field VM that BOTH areas bind: the details pane container of the selected
        // root node (identity asserted explicitly — zero divergence).
        var field = Assert.IsType<EditableStringDataFieldViewModel>(
            vm.SelectedNodeDetails!
                .OfType<FieldContainerDataNodeDetailViewModel>()
                .SelectMany(c => c.Fields)
                .First(f => f is EditableStringDataFieldViewModel && f.Description == "Kit name"));
        var detailsContainer = vm.SelectedNodeDetails!
            .OfType<FieldContainerDataNodeDetailViewModel>()
            .First(c => c.Fields.Contains(field));

        // Toggle flat mode on and find the Kit name editor inside the flat area.
        vm.FlatFields.ToggleFlatModeCommand.Execute(null!);
        window.UpdateLayout();
        var flatArea = FindFlatArea(window)!;
        Assert.True(flatArea.IsVisible);

        var flatEditor = Assert.IsType<TextBox>(
            flatArea.GetVisualDescendants()
                .OfType<TextBox>()
                .First(box => ReferenceEquals(box.DataContext, field)));
        flatEditor.Focus();
        Assert.Same(flatEditor, window.FocusManager?.GetFocusedElement());

        // Edit via the keyboard: select all (KeyDown dispatches the Ctrl+A gesture),
        // then type the new name. Avalonia.Headless's KeyPress only dispatches
        // KeyDown/KeyUp and never inserts characters, so typed text goes through
        // <c>KeyTextInput</c> (see the KeyTextInput documentation).
        window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control);
        window.KeyTextInput("deep");

        // The SAME field view model the details pane binds was updated, and the edit
        // marks the document dirty.
        Assert.Equal("deep", field.Text);
        Assert.Equal("deep", field.Model.Text);
        Assert.Same(field, detailsContainer.Fields.First(f => f.Description == "Kit name"));
        Assert.True(vm.IsDirty);

        // The AutomationId of the flat editor uses the flat-fields prefix.
        Assert.Equal("data-explorer.flat-fields.string-value",
            AutomationProperties.GetAutomationId(flatEditor));

        // Toggle back to tree mode: the details pane shows the edited value (the same
        // field view model is rebound, so no stale copy is shown).
        vm.FlatFields.ToggleFlatModeCommand.Execute(null!);
        window.UpdateLayout();
        Assert.False(flatArea.IsVisible);

        var detailsEditor = Assert.IsType<TextBox>(
            window.GetVisualDescendants()
                .OfType<TextBox>()
                .First(box => ReferenceEquals(box.DataContext, field)));
        Assert.Equal("deep", detailsEditor.Text);
        window.Close();
    }

    /// <summary>
    /// Creates a headless DataExplorer window with a KitExplorerViewModel for the
    /// embedded TD-27 kit.
    /// </summary>
    private static (DataExplorer Window, KitExplorerViewModel Vm) CreateWindow()
    {
        var vm = new KitExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), TestData.LoadTD27Kit());
        var window = new DataExplorer { DataContext = vm };
        window.Show();
        window.UpdateLayout();
        return (window, vm);
    }

    /// <summary>The ScrollViewer hosting the flat field list (identified by its
    /// AutomationId, since the details pane ScrollViewer shares Grid.Row).</summary>
    private static ScrollViewer? FindFlatArea(DataExplorer window) =>
        window.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault(sv => AutomationProperties.GetAutomationId(sv) == FlatAreaAutomationId);
}
