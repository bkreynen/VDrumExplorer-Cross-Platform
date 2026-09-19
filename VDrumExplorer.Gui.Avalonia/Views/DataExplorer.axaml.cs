// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using VDrumExplorer.ViewModel.Data;

namespace VDrumExplorer.Gui.Avalonia.Views;

/// <summary>
/// Data Explorer window (Avalonia port). Used for both Kit Explorer and Module Explorer,
/// with the behavior determined by the <see cref="DataExplorerViewModel"/> subclass set as DataContext.
/// Keyboard shortcuts (Ctrl+C/V/Z/Y, Ctrl+K) are declarative <c>KeyBinding</c>s in
/// <c>DataExplorer.axaml</c>, bound to ViewModel commands — no code-behind key handling
/// (see docs/accessibility.md §4). The only code-behind logic is the
/// <see cref="OnDataContextChanged"/> wiring below, which performs the focus mechanics the
/// ViewModel cannot do (moving keyboard focus and scrolling the details pane after a
/// jump-to-field search jump).
/// </summary>
public partial class DataExplorer : Window
{
    private DataExplorerViewModel? viewModel;

    private DataExplorerViewModel ViewModel => (DataExplorerViewModel)DataContext!;

    public DataExplorer()
    {
        InitializeComponent();
        Closing += DataExplorer_Closing;
    }

    /// <summary>
    /// Wires the jump-to-field search events of the current ViewModel to the focus mechanics
    /// (docs/accessibility.md §4): focusing the search box when the search opens (Ctrl+K),
    /// focusing the highlighted result after Down/Up, and focusing + scrolling to the
    /// jumped field's editor after a jump. No key handling happens here — all keys are
    /// declarative <c>KeyBinding</c>s in the axaml.
    /// </summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (viewModel is object)
        {
            viewModel.FieldSearch.FocusSearchRequested -= FieldSearch_FocusSearchRequested;
            viewModel.FieldSearch.FocusResultsRequested -= FieldSearch_FocusResultsRequested;
            viewModel.FieldSearch.SearchJumped -= FieldSearch_SearchJumped;
            viewModel = null;
        }
        if (DataContext is DataExplorerViewModel newViewModel)
        {
            viewModel = newViewModel;
            newViewModel.FieldSearch.FocusSearchRequested += FieldSearch_FocusSearchRequested;
            newViewModel.FieldSearch.FocusResultsRequested += FieldSearch_FocusResultsRequested;
            newViewModel.FieldSearch.SearchJumped += FieldSearch_SearchJumped;
        }
        else
        {
            viewModel = null;
        }
    }

    private bool isClosing;

    private async void DataExplorer_Closing(object? sender, WindowClosingEventArgs e)
    {
        // Allow application/OS-forced shutdown without prompting.
        // Note: Avalonia 12's WindowClosingEventArgs has no IsCancelRequested property;
        // WindowCloseReason.ApplicationShutdown/OSShutdown is the equivalent signal.
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            return;
        }

        // Guard against re-entrancy: if we're already showing the close dialog,
        // don't start another confirmation flow.
        if (isClosing)
        {
            e.Cancel = true;
            return;
        }

        // Null guard: if DataContext isn't set yet, allow close.
        if (DataContext is not DataExplorerViewModel viewModel)
        {
            return;
        }

        if (!viewModel.IsDirty)
        {
            return;
        }

        // Cancel the close first, then show the dialog.
        // If the user confirms, close again programmatically.
        e.Cancel = true;
        isClosing = true;
        try
        {
            var confirm = await viewModel.ConfirmCloseAsync();
            if (confirm)
            {
                // Re-trigger close without the closing handler intercepting
                Closing -= DataExplorer_Closing;
                Close();
            }
        }
        finally
        {
            isClosing = false;
        }
    }

    private void TreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (treeView.SelectedItem is DataTreeNodeViewModel node)
        {
            ViewModel.SelectedNode = node;
        }
    }

    /// <summary>Focuses the search text box when the jump-to-field search opens (Ctrl+K).</summary>
    private void FieldSearch_FocusSearchRequested(object? sender, EventArgs e)
    {
        var box = this.FindControl<TextBox>("fieldSearchBox");
        if (box is object)
        {
            box.Focus();
            box.SelectAll();
        }
    }

    /// <summary>Focuses the highlighted result row after Down/Up in the search results.</summary>
    private void FieldSearch_FocusResultsRequested(object? sender, EventArgs e)
    {
        if (viewModel?.FieldSearch.SelectedResult is object)
        {
            FindResultButton(viewModel.FieldSearch.SelectedResult)?.Focus();
        }
    }

    /// <summary>
    /// After a jump: moves the tree selection to the target node (the ViewModel has already
    /// rebuilt the details pane), then focuses the target field's editor and scrolls it into
    /// view. The editor is the first focusable descendant whose DataContext is the focused
    /// field view model (the per-type editor templates bind the same field VM as their DataContext).
    /// </summary>
    private void FieldSearch_SearchJumped(object? sender, EventArgs e)
    {
        if (viewModel?.SelectedNode is object)
        {
            treeView.SelectedItem = viewModel.SelectedNode;
        }
        UpdateLayout();
        var target = viewModel?.SelectedNodeDetails
            ?.OfType<FieldContainerDataNodeDetailViewModel>()
            .Select(detail => detail.FocusedField)
            .FirstOrDefault(field => field is object);
        if (target is object)
        {
            var editor = this.GetVisualDescendants()
                .OfType<Control>()
                .FirstOrDefault(control => ReferenceEquals(control.DataContext, target) && control.Focusable);
            if (editor is object)
            {
                editor.BringIntoView();
                editor.Focus();
            }
        }
    }

    /// <summary>Finds the result button in the results list whose DataContext is the given result.</summary>
    private Control? FindResultButton(object result) =>
        this.GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(control => ReferenceEquals(control.DataContext, result) && control.Focusable);
}
