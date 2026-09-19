// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia.Controls;
using VDrumExplorer.ViewModel.Data;

namespace VDrumExplorer.Gui.Avalonia.Views;

/// <summary>
/// Data Explorer window (Avalonia port). Used for both Kit Explorer and Module Explorer,
/// with the behavior determined by the <see cref="DataExplorerViewModel"/> subclass set as DataContext.
/// Keyboard shortcuts (Ctrl+C/V/Z/Y) are declarative <c>KeyBinding</c>s in
/// <c>DataExplorer.axaml</c>, bound to ViewModel commands — no code-behind key handling
/// (see docs/accessibility.md §4).
/// </summary>
public partial class DataExplorer : Window
{
    private DataExplorerViewModel ViewModel => (DataExplorerViewModel)DataContext!;

    public DataExplorer()
    {
        InitializeComponent();
        Closing += DataExplorer_Closing;
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
}
