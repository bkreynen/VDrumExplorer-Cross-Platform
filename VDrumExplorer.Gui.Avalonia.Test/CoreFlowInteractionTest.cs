// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using VDrumExplorer.ViewModel.Audio;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.LogicalSchema;
using VDrumExplorer.ViewModel.Status;
using Xunit;

namespace VDrumExplorer.Gui.Avalonia.Test;

/// <summary>
/// Headless core-flow interaction tests (Phase 1 task 7, docs/accessibility.md §7 item 3):
/// the core accessibility loops are driven end-to-end through the real <see cref="DataExplorer"/>
/// view with simulated key presses (<c>KeyPressQwerty</c> for key gestures,
/// <c>KeyTextInput</c> for typed text) and real control interaction — not
/// reflection into private handlers. Covered flows:
/// <list type="bullet">
/// <item>Kit list → field edit → commit: select a tree node, focus a field editor, type a
/// value, assert the field VM and dirty state update.</item>
/// <item>Undo via keyboard: an applied edit reverted by Ctrl+Z (KeyBinding → UndoCommand).</item>
/// <item>Copy/paste via keyboard: Ctrl+C sets <see cref="DataExplorerViewModel.CopiedSnapshot"/>,
/// Ctrl+V applies it.</item>
/// <item>Jump-to-field search via keyboard: Ctrl+K opens search, typing queries, Enter jumps,
/// focus lands on the target field's editor and the status line announces the jump.</item>
/// <item>Focus order sanity: Tab moves keyboard focus between interactive controls.</item>
/// <item>Status announcements (docs/accessibility.md §5) on the kit copy/paste paths of a
/// Module Explorer.</item>
/// </list>
/// </summary>
/// <remarks>
/// <see cref="DataExplorerViewModel.CopiedSnapshot"/> is backed by a static field shared across
/// windows; it is nulled before each test that touches the clipboard for isolation (same pattern
/// as <see cref="InteractionTest"/>).
/// </remarks>
public class CoreFlowInteractionTest
{
    // === 1. Kit list → field edit → commit ===

    [AvaloniaFact]
    public void DataExplorer_FieldEditViaKeyboard_UpdatesFieldValueAndDirtyState()
    {
        var vm = CreateKitExplorerViewModel();
        vm.CopiedSnapshot = null;
        var window = CreateWindow(vm);

        // Select the kit root in the real tree, which rebuilds the details pane.
        var treeView = window.FindControl<TreeView>("treeView");
        Assert.NotNull(treeView);
        treeView!.SelectedItem = vm.Root[0];
        Assert.Same(vm.Root[0], vm.SelectedNode);
        window.UpdateLayout();

        // Focus the string editor of the "Kit name" field and edit it via the keyboard.
        // The field VM must be one of the details-pane instances the view actually binds
        // (vm.SelectedNodeDetails), not a fresh CreateDetails() copy — otherwise no
        // editor control's DataContext matches it (same pattern as
        // FlatFieldListInteractionTest).
        var field = Assert.IsType<EditableStringDataFieldViewModel>(
            vm.SelectedNodeDetails!
                .OfType<FieldContainerDataNodeDetailViewModel>()
                .Where(container => container.Description == "Kit common")
                .SelectMany(container => container.Fields)
                .First(f => f.Description == "Kit name"));
        var editor = FindEditors(window, field).OfType<TextBox>().FirstOrDefault();
        Assert.NotNull(editor);
        editor!.Focus();
        Assert.Same(editor, GetFocusedElement(window));

        // Edit via the keyboard: select all, then type the new name. KeyDown
        // dispatches the Ctrl+A gesture; typed characters go through
        // <c>KeyTextInput</c> — Avalonia.Headless's KeyPress only dispatches
        // KeyDown/KeyUp and never inserts text (see the KeyTextInput documentation).
        window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control); // select all
        window.KeyTextInput("deep");

        Assert.Equal("deep", field.Text);
        Assert.Equal("deep", field.Model.Text);
        Assert.True(vm.IsDirty);
        window.Close();
    }

    // === 2. Undo via keyboard ===

    [AvaloniaFact]
    public void DataExplorer_UndoViaKeyboard_RevertsAppliedEdit()
    {
        var vm = CreateKitExplorerViewModel();
        vm.CopiedSnapshot = null;
        var window = CreateWindow(vm);

        // Make an edit via the keyboard: copy one trigger's settings and paste them
        // into another (a real data change that also creates an undo entry).
        var (source, target) = GetPasteTargetPair(vm);
        var sourceVolume = NumericField(source, "Main instrument", "Volume").Value;
        var targetVolume = NumericField(target, "Main instrument", "Volume");
        var originalVolume = targetVolume.Value;
        Assert.NotEqual(sourceVolume, originalVolume); // the pair provably differs

        // Make an edit via the keyboard: copy one trigger's settings and paste them
        // into another (a real data change that also creates an undo entry).
        SelectNode(window, vm, source);
        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
        SelectNode(window, vm, target);
        window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);

        Assert.Equal(sourceVolume, targetVolume.Value); // paste applied
        Assert.True(vm.CanUndo);

        // Ctrl+Z must revert the edit to its previous state.
        window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
        Assert.Equal(originalVolume, targetVolume.Value);
        Assert.False(vm.CanUndo);
        Assert.True(vm.CanRedo);
        window.Close();
    }

    // === 3. Copy/paste via keyboard ===

    [AvaloniaFact]
    public void DataExplorer_CopyPasteViaKeyboard_AppliesCopiedSnapshot()
    {
        var vm = CreateKitExplorerViewModel();
        vm.CopiedSnapshot = null;
        var window = CreateWindow(vm);

        var (source, target) = GetPasteTargetPair(vm);
        SelectNode(window, vm, source);
        var sourceVolume = NumericField(source, "Main instrument", "Volume").Value;

        // Ctrl+C copies the node settings into the clipboard snapshot.
        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
        Assert.NotNull(vm.CopiedSnapshot);
        Assert.Equal(source.Model.SchemaNode.Path, vm.CopiedSnapshot!.Path);

        // Ctrl+V applies the snapshot to the selected target node.
        SelectNode(window, vm, target);
        Assert.NotEqual(sourceVolume, NumericField(target, "Main instrument", "Volume").Value);
        window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
        Assert.Equal(sourceVolume, NumericField(target, "Main instrument", "Volume").Value);
        Assert.True(vm.CanUndo); // the paste is undoable
        window.Close();
    }

    // === 4. Jump-to-field search via keyboard ===

    [AvaloniaFact]
    public void DataExplorer_FieldSearchJumpViaKeyboard_MovesSelectionFocusAndAnnounces()
    {
        var vm = CreateKitExplorerViewModel();
        vm.CopiedSnapshot = null;
        var window = CreateWindow(vm);

        // Ctrl+K opens the search; the view focuses the search box.
        window.KeyPressQwerty(PhysicalKey.K, RawInputModifiers.Control);
        Assert.True(vm.FieldSearch.IsOpen);
        window.UpdateLayout();
        var searchBox = window.FindControl<TextBox>("fieldSearchBox");
        Assert.NotNull(searchBox);
        Assert.Same(searchBox, GetFocusedElement(window));

        // Type the query into the search box (KeyTextInput — KeyPress dispatches
        // KeyDown/KeyUp only and never inserts text), then press Enter to jump.
        window.KeyTextInput("kit name");
        Assert.Equal("kit name", vm.FieldSearch.Query);
        Assert.NotEmpty(vm.FieldSearch.Results);
        var result = vm.FieldSearch.Results[0];
        Assert.Same(result, vm.FieldSearch.SelectedResult);

        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        // The tree selection moved to the result's node, the details pane shows the
        // target field flagged as focused, and keyboard focus sits on its editor.
        Assert.Same(result.Node, vm.SelectedNode);
        var container = Assert.IsType<FieldContainerDataNodeDetailViewModel>(
            vm.SelectedNodeDetails!
                .OfType<FieldContainerDataNodeDetailViewModel>()
                .First(c => c.FocusedField is object));
        Assert.Same(result.Field.ModelForTest, container.FocusedField!.ModelForTest);
        var focused = Assert.IsAssignableFrom<Control>(GetFocusedElement(window));
        Assert.Same(container.FocusedField, focused.DataContext);
        Assert.Equal($"Jumped to {result.DisplayText}", vm.Status.Message);
        Assert.Equal("", vm.Status.ErrorMessage);
        window.Close();
    }

    // === 5. Focus order sanity ===

    [AvaloniaFact]
    public void DataExplorer_Tab_MovesFocusBetweenInteractiveControls()
    {
        var vm = CreateKitExplorerViewModel();
        var window = CreateWindow(vm);

        // Tab through a few controls: focus must be reachable, land on interactive
        // (focusable) controls, and keep moving. No exact order is asserted — the
        // scanner owns the tab-order invariant (docs/accessibility.md §7).
        var visited = new List<object?>();
        for (int i = 0; i < 4; i++)
        {
            window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
            var focused = GetFocusedElement(window);
            if (focused is object)
            {
                var control = Assert.IsAssignableFrom<Control>(focused);
                Assert.True(control.Focusable, $"Focused control after {i + 1} Tabs should be focusable");
            }
            visited.Add(focused);
        }
        Assert.Contains(visited.Skip(1), f => f is object && !ReferenceEquals(f, visited[0]));
        window.Close();
    }

    // === 6. Status announcements on kit operations ===

    [AvaloniaFact]
    public void DataExplorer_KitCopyPasteViaKeyboard_AnnouncesOnStatus()
    {
        var module = TestData.LoadTD27Module();
        var vm = new ModuleExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), module);
        var window = new DataExplorer { DataContext = vm };
        window.Show();

        // Select a kit root; Ctrl+C copies the whole kit, Ctrl+V pastes it into the
        // selected kit's slot — each announced on the polite live region. Kit 1 is
        // not present in the TD-27 module tree (kit roots are nested); the ViewModel
        // tests use kit 3.
        var kitRoot = FindKitRoot(vm.Root[0], 3);
        vm.SelectedNode = kitRoot;

        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
        Assert.Equal("Copied kit 3", vm.Status.Message);
        Assert.Equal("", vm.Status.ErrorMessage);
        Assert.True(vm.HasCopiedKit);

        window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
        Assert.Equal("Pasted kit into kit 3", vm.Status.Message);
        Assert.Equal("", vm.Status.ErrorMessage);
        Assert.True(vm.CanUndo);
        window.Close();
    }

    [AvaloniaFact]
    public void DataExplorer_CopyKitViaDialog_AnnouncesTargetSlot()
    {
        var module = TestData.LoadTD27Module();
        // The kit-copy dialog needs a view services that completes the target chooser
        // with a destination slot; the plain StubViewServices reports "cancelled".
        var vm = new ModuleExplorerViewModel(
            new KitDialogViewServices(), NullLogger.Instance, new DeviceViewModel(), module);
        var window = new DataExplorer { DataContext = vm };
        window.Show();

        var kitRoot = FindKitRoot(vm.Root[0], 3);
        vm.CopyKitCommand.Execute(kitRoot);

        Assert.Equal("Copied kit to slot 5", vm.Status.Message);
        Assert.Equal("", vm.Status.ErrorMessage);
        Assert.True(vm.CanUndo); // the kit copy is undoable
        window.Close();
    }

    // === Helpers ===

    /// <summary>
    /// Creates a KitExplorerViewModel for the embedded TD-27 kit with the plain stub
    /// view services (all dialogs cancelled).
    /// </summary>
    private static KitExplorerViewModel CreateKitExplorerViewModel()
    {
        var kit = TestData.LoadTD27Kit();
        return new KitExplorerViewModel(
            new StubViewServices(), NullLogger.Instance, new DeviceViewModel(), kit);
    }

    private static DataExplorer CreateWindow(DataExplorerViewModel vm)
    {
        var window = new DataExplorer { DataContext = vm };
        window.Show();
        return window;
    }

    /// <summary>
    /// Selects a node by assigning it to the real TreeView (the view's SelectionChanged
    /// handler updates the ViewModel), asserting the round-trip took effect.
    /// </summary>
    private static void SelectNode(DataExplorer window, DataExplorerViewModel vm, DataTreeNodeViewModel node)
    {
        var treeView = window.FindControl<TreeView>("treeView");
        Assert.NotNull(treeView);
        treeView!.SelectedItem = node;
        Assert.Same(node, vm.SelectedNode);
        window.UpdateLayout();
    }

    /// <summary>
    /// Finds a pair of distinct tree nodes with structurally compatible paths
    /// (paste-valid targets) whose "Main instrument — Volume" values differ, so a
    /// paste provably changes data and an undo provably reverts it. Deterministic for
    /// the fixed embedded TD-27 sample: two trigger containers.
    /// </summary>
    private static (DataTreeNodeViewModel Source, DataTreeNodeViewModel Target) GetPasteTargetPair(
        DataExplorerViewModel vm)
    {
        var triggers = vm.Root[0].Children.First(child => child.Title == "Triggers");
        foreach (var source in triggers.Children)
        {
            foreach (var target in triggers.Children.Where(t => !ReferenceEquals(t, source)))
            {
                if (NumericField(source, "Main instrument", "Volume").Value
                    != NumericField(target, "Main instrument", "Volume").Value)
                {
                    return (source, target);
                }
            }
        }
        throw new Xunit.Sdk.XunitException("No differing paste-target pair found in the kit tree");
    }

    private static EditableNumericDataFieldViewModel NumericField(
        DataTreeNodeViewModel node, string containerDescription, string fieldDescription) =>
        Assert.IsType<EditableNumericDataFieldViewModel>(
            FindField(node, containerDescription, fieldDescription));

    /// <summary>
    /// Finds the kit-root node for the given kit number anywhere in the module tree
    /// (kit roots are nested inside schema containers, not direct children of the root,
    /// so a depth-first search like the ViewModel tests' <c>FindAllKitRoots</c> is needed).
    /// </summary>
    private static DataTreeNodeViewModel FindKitRoot(DataTreeNodeViewModel node, int kitNumber)
    {
        var found = new List<int>();
        var result = TryFindKitRoot(node, kitNumber, found);
        if (result is object)
        {
            return result;
        }
        throw new Xunit.Sdk.XunitException($"No kit root for kit {kitNumber} found in the module tree; kit roots found: [{string.Join(",", found)}]");
    }

    private static DataTreeNodeViewModel? TryFindKitRoot(DataTreeNodeViewModel node, int kitNumber, List<int> found)
    {
        if (node.IsKitRoot)
        {
            found.Add(node.KitNumber ?? -1);
            if (node.KitNumber == kitNumber)
            {
                return node;
            }
        }
        foreach (var child in node.Children)
        {
            var result = TryFindKitRoot(child, kitNumber, found);
            if (result is object)
            {
                return result;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds a field view model by container and field description from the node's
    /// details (fresh instances reading the current model data).
    /// </summary>
    private static DataFieldViewModel FindField(
        DataTreeNodeViewModel node, string containerDescription, string fieldDescription)
    {
        var field = node.CreateDetails()
            .OfType<FieldContainerDataNodeDetailViewModel>()
            .Where(container => container.Description == containerDescription)
            .SelectMany(container => container.Fields)
            .FirstOrDefault(field => field.Description == fieldDescription);
        Assert.NotNull(field);
        return field!;
    }

    /// <summary>All focusable controls whose DataContext is the given field view model
    /// (the per-type editor templates bind the field VM as their DataContext).</summary>
    private static IEnumerable<Control> FindEditors(DataExplorer window, object field) =>
        window.GetVisualDescendants()
            .OfType<Control>()
            .Where(control => ReferenceEquals(control.DataContext, field) && control.Focusable);

    private static object? GetFocusedElement(DataExplorer window) =>
        window.FocusManager?.GetFocusedElement();

    /// <summary>
    /// View services whose kit-copy-target chooser completes with slot 5 (used by the
    /// kit-copy dialog path, which the plain <see cref="StubViewServices"/> cancels).
    /// Everything else behaves like the plain stub.
    /// </summary>
    private sealed class KitDialogViewServices : IViewServices
    {
        public Task<string?> ShowOpenFileDialogAsync(string filter) => Task.FromResult<string?>(null);

        public Task<string?> ShowSaveFileDialogAsync(string filter) => Task.FromResult<string?>(null);

        public Task<int?> ChooseCopyKitTargetAsync(CopyKitViewModel viewModel) => Task.FromResult<int?>(5);

        public Task<bool> ChooseCopyKitsTargetAsync(CopyKitsViewModel viewModel) => Task.FromResult(false);

        public Task<bool> ChooseMultiPasteTargetsAsync(MultiPasteViewModel viewModel) => Task.FromResult(false);

        public void ShowSchemaExplorer(ModuleSchemaViewModel viewModel)
        {
        }

        public void ShowKitExplorer(KitExplorerViewModel viewModel)
        {
        }

        public void ShowModuleExplorer(ModuleExplorerViewModel viewModel)
        {
        }

        public void ShowInstrumentAudioExplorer(InstrumentAudioExplorerViewModel viewModel)
        {
        }

        public void ShowInstrumentRecorderDialog(InstrumentAudioRecorderViewModel viewModel)
        {
        }

        public Task<T?> ShowDataTransferDialog<T>(DataTransferViewModel<T> viewModel) where T : class =>
            Task.FromResult<T?>(null);

        public Task<bool> ConfirmCloseAsync() => Task.FromResult(true);

        public void AddRequerySuggestion(EventHandler handler)
        {
        }

        public void RemoveRequerySuggestion(EventHandler handler)
        {
        }
    }
}
