// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Gui.Avalonia.ViewServices;
using VDrumExplorer.Gui.Avalonia.Views;
using VDrumExplorer.Gui.Avalonia.Views.Dialogs;
using VDrumExplorer.Gui.Avalonia.Test.Stubs;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.LogicalSchema;
using Xunit;

namespace VDrumExplorer.Gui.Avalonia.Test;

public class InteractionTest
{
    // === AvaloniaViewServices.ParseFilter ===

    [AvaloniaFact]
    public void ParseFilter_SingleExtension_ReturnsOneType()
    {
        var result = AvaloniaViewServices.ParseFilter("VDrum files|*.vdrum");
        Assert.Single(result);
        Assert.Equal("*.vdrum", result[0].Patterns![0]);
    }

    [AvaloniaFact]
    public void ParseFilter_MultipleFilters_ReturnsAllTypes()
    {
        var result = AvaloniaViewServices.ParseFilter("VDrum files|*.vdrum|Kit files|*.vkit");
        Assert.Equal(2, result.Count);
    }

    [AvaloniaFact]
    public void ParseFilter_MultipleExtensionsInOneFilter_ReturnsAllPatterns()
    {
        var result = AvaloniaViewServices.ParseFilter("All files|*.vdrum;*.vkit");
        Assert.Single(result);
        Assert.Equal(2, result[0].Patterns!.Count);
    }

    [AvaloniaFact]
    public void ParseFilter_OddNumberOfParts_DropsLastPart()
    {
        var result = AvaloniaViewServices.ParseFilter("VDrum files|*.vdrum|Extra");
        Assert.Single(result);
    }

    // === Dialog event handlers via reflection ===

    [AvaloniaFact]
    public void CopyKitTargetDialog_Copy_ClosesWindow()
    {
        var module = TestData.LoadTD27Module();
        var kit = module.ExportKit(1);
        var vm = new CopyKitViewModel(module, kit);
        var dialog = new CopyKitTargetDialog { DataContext = vm };
        dialog.Show();
        InvokePrivate(dialog, "Copy", new RoutedEventArgs());
        Assert.False(dialog.IsVisible);
    }

    [AvaloniaFact]
    public void CopyKitTargetDialog_Cancel_ClosesWindow()
    {
        var module = TestData.LoadTD27Module();
        var kit = module.ExportKit(1);
        var vm = new CopyKitViewModel(module, kit);
        var dialog = new CopyKitTargetDialog { DataContext = vm };
        dialog.Show();
        InvokePrivate(dialog, "Cancel", new RoutedEventArgs());
        Assert.False(dialog.IsVisible);
    }

    [AvaloniaFact]
    public void CopyKitsDialog_Copy_ClosesWindow()
    {
        var module = TestData.LoadTD27Module();
        var vm = new CopyKitsViewModel(module);
        var dialog = new CopyKitsDialog { DataContext = vm };
        dialog.Show();
        InvokePrivate(dialog, "Copy", new RoutedEventArgs());
        Assert.False(dialog.IsVisible);
    }

    [AvaloniaFact]
    public void CopyKitsDialog_Cancel_ClosesWindow()
    {
        var module = TestData.LoadTD27Module();
        var vm = new CopyKitsViewModel(module);
        var dialog = new CopyKitsDialog { DataContext = vm };
        dialog.Show();
        InvokePrivate(dialog, "Cancel", new RoutedEventArgs());
        Assert.False(dialog.IsVisible);
    }

    [AvaloniaFact]
    public void MultiPasteDialog_SelectAll_ChecksAllCandidates()
    {
        var vm = CreateMultiPasteViewModel();
        var dialog = new MultiPasteDialog { DataContext = vm };
        dialog.Show();
        InvokePrivate(dialog, "SelectAll", new RoutedEventArgs());
        Assert.All(vm.Candidates, c => Assert.True(c.Checked));
    }

    [AvaloniaFact]
    public void MultiPasteDialog_SelectNone_UnchecksAllCandidates()
    {
        var vm = CreateMultiPasteViewModel();
        foreach (var c in vm.Candidates) c.Checked = true;
        var dialog = new MultiPasteDialog { DataContext = vm };
        dialog.Show();
        InvokePrivate(dialog, "SelectNone", new RoutedEventArgs());
        Assert.All(vm.Candidates, c => Assert.False(c.Checked));
    }

    [AvaloniaFact]
    public void MultiPasteDialog_Paste_ClosesWindow()
    {
        var vm = CreateMultiPasteViewModel();
        var dialog = new MultiPasteDialog { DataContext = vm };
        dialog.Show();
        InvokePrivate(dialog, "Paste", new RoutedEventArgs());
        Assert.False(dialog.IsVisible);
    }

    [AvaloniaFact]
    public void MultiPasteDialog_Cancel_ClosesWindow()
    {
        var vm = CreateMultiPasteViewModel();
        var dialog = new MultiPasteDialog { DataContext = vm };
        dialog.Show();
        InvokePrivate(dialog, "Cancel", new RoutedEventArgs());
        Assert.False(dialog.IsVisible);
    }

    [AvaloniaFact]
    public void DataTransferDialog_HandleClosing_CancelsTransfer()
    {
        var vm = new DataTransferViewModel("Test transfer");
        var dialog = new DataTransferDialog { DataContext = vm };
        dialog.Show();
        var args = CreateWindowClosingEventArgs();
        InvokePrivate(dialog, "HandleClosing", args);
        // Verify cancellation was requested via the protected CancellationTokenSource
        var cts = typeof(DataTransferViewModel)
            .GetProperty("CancellationTokenSource", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(vm) as System.Threading.CancellationTokenSource;
        Assert.NotNull(cts);
        Assert.True(cts!.IsCancellationRequested);
    }

    // === TreeView selection handlers ===

    [AvaloniaFact]
    public void DataExplorer_TreeView_SelectionChanged_UpdatesSelectedNode()
    {
        var kit = TestData.LoadTD27Kit();
        var vm = new KitExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), kit);
        var window = new DataExplorer { DataContext = vm };
        window.Show();

        var treeView = window.FindControl<TreeView>("treeView");
        Assert.NotNull(treeView);
        Assert.NotEmpty(treeView!.Items);

        var node = treeView.Items[0];
        treeView.SelectedItem = node;
        var args = new SelectionChangedEventArgs(
            SelectingItemsControl.SelectionChangedEvent,
            Array.Empty<object>(),
            new[] { node });
        InvokePrivate(window, "TreeView_SelectionChanged", args);

        Assert.NotNull(vm.SelectedNode);
        window.Close();
    }

    [AvaloniaFact]
    public void SchemaExplorer_TreeView_SelectionChanged_UpdatesSelectedNode()
    {
        var schema = ModuleSchema.KnownSchemas[ModuleIdentifier.TD27].Value;
        var vm = new ModuleSchemaViewModel(schema);
        var window = new SchemaExplorer { DataContext = vm };
        window.Show();

        var treeView = window.FindControl<TreeView>("treeView");
        Assert.NotNull(treeView);
        Assert.NotEmpty(treeView!.Items);

        var node = treeView.Items[0];
        treeView.SelectedItem = node;
        var args = new SelectionChangedEventArgs(
            SelectingItemsControl.SelectionChangedEvent,
            Array.Empty<object>(),
            new[] { node });
        InvokePrivate(window, "TreeView_SelectionChanged", args);

        Assert.NotNull(vm.SelectedNode);
        window.Close();
    }

    // === DataExplorer window key bindings (Ctrl+C/V/Z/Y → ViewModel commands) ===
    // The shortcuts are declarative Window.KeyBindings in DataExplorer.axaml; these tests
    // press real keys headlessly and assert the resulting ViewModel state. Dispatch is
    // focus-aware: while a TextBox has focus the window commands are disabled
    // (TextEditorFocused) and the keys fall through to the TextBox's native handling.
    // CopiedSnapshot is backed by a static field, so it is nulled before each test for
    // isolation.

    [AvaloniaFact]
    public void DataExplorer_Shortcut_CtrlC_CopiesNode()
    {
        var kit = TestData.LoadTD27Kit();
        var vm = new KitExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), kit);
        vm.CopiedSnapshot = null;
        var window = new DataExplorer { DataContext = vm };
        window.Show();

        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);

        Assert.NotNull(vm.CopiedSnapshot);
        window.Close();
    }

    [AvaloniaFact]
    public void DataExplorer_Shortcut_CtrlV_PastesNode()
    {
        var kit = TestData.LoadTD27Kit();
        var vm = new KitExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), kit);
        vm.CopiedSnapshot = null;
        var window = new DataExplorer { DataContext = vm };
        window.Show();

        // First copy
        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
        Assert.NotNull(vm.CopiedSnapshot);

        // Then paste
        window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
        Assert.True(vm.CanUndo);
        window.Close();
    }

    [AvaloniaFact]
    public void DataExplorer_Shortcut_CtrlZ_UndoesEdit()
    {
        var kit = TestData.LoadTD27Kit();
        var vm = new KitExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), kit);
        vm.CopiedSnapshot = null;
        var window = new DataExplorer { DataContext = vm };
        window.Show();

        // Copy and paste to create an undo state
        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
        window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
        Assert.True(vm.CanUndo);

        // Undo
        window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
        Assert.False(vm.CanUndo);
        Assert.True(vm.CanRedo);
        window.Close();
    }

    [AvaloniaFact]
    public void DataExplorer_Shortcut_CtrlY_RedoesEdit()
    {
        var kit = TestData.LoadTD27Kit();
        var vm = new KitExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), kit);
        vm.CopiedSnapshot = null;
        var window = new DataExplorer { DataContext = vm };
        window.Show();

        // Copy, paste, undo, then redo
        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
        window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
        window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
        Assert.True(vm.CanRedo);

        window.KeyPressQwerty(PhysicalKey.Y, RawInputModifiers.Control);
        Assert.True(vm.CanUndo);
        Assert.False(vm.CanRedo);
        window.Close();
    }

    // === Focus-aware dispatch: the window commands yield to a focused TextBox ===

    [AvaloniaFact]
    public void DataExplorer_Shortcut_CtrlC_YieldsToFocusedTextBox()
    {
        var kit = TestData.LoadTD27Kit();
        var vm = new KitExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), kit);
        vm.CopiedSnapshot = null;
        var window = new DataExplorer { DataContext = vm };
        window.Show();
        window.UpdateLayout();

        // Focus a TextBox in the details pane (the kit root's "Kit common" fields
        // include string fields rendered as TextBoxes).
        var textBox = window.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
        Assert.NotNull(textBox);
        textBox!.Focus();
        Assert.True(vm.TextEditorFocused, "Focusing a TextBox must set TextEditorFocused.");

        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);

        // The window CopyCommand must have yielded: no node snapshot was copied.
        Assert.Null(vm.CopiedSnapshot);
        window.Close();
    }

    [AvaloniaFact]
    public void DataExplorer_Shortcut_CtrlC_FiresWhenTreeFocused()
    {
        var kit = TestData.LoadTD27Kit();
        var vm = new KitExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), kit);
        vm.CopiedSnapshot = null;
        var window = new DataExplorer { DataContext = vm };
        window.Show();

        // Focus the tree (no TextBox focused): the window command must still win.
        var treeView = window.FindControl<TreeView>("treeView");
        Assert.NotNull(treeView);
        treeView!.Focus();
        Assert.False(vm.TextEditorFocused);

        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);

        Assert.NotNull(vm.CopiedSnapshot);
        window.Close();
    }

    // === Helpers ===

    private static MultiPasteViewModel CreateMultiPasteViewModel()
    {
        var module = TestData.LoadTD27Module();
        var data = module.Data;
        var schemaNode = data.LogicalRoot.SchemaNode;
        var snapshot = new NodeSnapshot(schemaNode, data.CreatePartialSnapshot(schemaNode));
        var candidates = schemaNode.DescendantsAndSelf().Where(snapshot.IsValidForTarget).ToList();
        return new MultiPasteViewModel(snapshot, candidates);
    }

    // Reflection helpers: pass the expected handler parameter types explicitly, because
    // name-only lookup can be ambiguous (e.g. Window itself declares a private
    // HandleClosing(WindowCloseReason), which collides with the dialog's
    // HandleClosing(object?, WindowClosingEventArgs) event handler).

    private static void InvokePrivate(object target, string methodName, RoutedEventArgs e)
    {
        var method = target.GetType().GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(object), e.GetType() });
        Assert.NotNull(method);
        method!.Invoke(target, new object?[] { null, e });
    }

    private static void InvokePrivate(object target, string methodName, SelectionChangedEventArgs e)
    {
        var method = target.GetType().GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(object), e.GetType() });
        Assert.NotNull(method);
        method!.Invoke(target, new object?[] { null, e });
    }

    private static void InvokePrivate(object target, string methodName, WindowClosingEventArgs e)
    {
        var method = target.GetType().GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(object), typeof(WindowClosingEventArgs) });
        Assert.NotNull(method);
        method!.Invoke(target, new object?[] { null, e });
    }

    private static WindowClosingEventArgs CreateWindowClosingEventArgs()
    {
        return (WindowClosingEventArgs)Activator.CreateInstance(
            typeof(WindowClosingEventArgs),
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new object[] { WindowCloseReason.WindowClosing, false },
            null)!;
    }
}
