// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia.Controls;
using Avalonia.Input;
using VDrumExplorer.ViewModel.Data;

namespace VDrumExplorer.Gui.Avalonia.Views;

/// <summary>
/// Data Explorer window (Avalonia port). Used for both Kit Explorer and Module Explorer,
/// with the behavior determined by the <see cref="DataExplorerViewModel"/> subclass set as DataContext.
/// </summary>
public partial class DataExplorer : Window
{
    private DataExplorerViewModel ViewModel => (DataExplorerViewModel)DataContext!;

    public DataExplorer()
    {
        InitializeComponent();
        KeyDown += DataExplorer_KeyDown;
    }

    private void TreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (treeView.SelectedItem is DataTreeNodeViewModel node)
        {
            ViewModel.SelectedNode = node;
        }
    }

    private void DataExplorer_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && e.KeyModifiers == KeyModifiers.Control)
        {
            // Ctrl+C: Copy the selected node.
            // If the selected node is a kit (in Module Explorer), copy the whole kit.
            // Otherwise, copy the node's settings (like a tom's parameters).
            if (ViewModel.SelectedNode is DataTreeNodeViewModel node)
            {
                if (ViewModel is ModuleExplorerViewModel moduleVm && node.IsKitRoot)
                {
                    moduleVm.CopySelectedKitToClipboard();
                }
                else
                {
                    ViewModel.CopyNodeCommand.Execute(null!);
                }
                e.Handled = true;
            }
        }
        else if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
        {
            // Ctrl+V: Paste.
            // If a kit was copied (in Module Explorer), paste the kit into the selected kit slot.
            // Otherwise, paste the node's settings into the selected node.
            if (ViewModel is ModuleExplorerViewModel moduleVm && moduleVm.HasCopiedKit)
            {
                moduleVm.PasteKitFromClipboard();
                e.Handled = true;
            }
            else if (ViewModel.CopiedSnapshot is not null && ViewModel.PasteNodeCommand.Enabled)
            {
                ViewModel.PasteNodeCommand.Execute(null!);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Z && e.KeyModifiers == KeyModifiers.Control)
        {
            // Ctrl+Z: Undo the last edit operation.
            if (ViewModel.CanUndo)
            {
                ViewModel.Undo();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Y && e.KeyModifiers == KeyModifiers.Control)
        {
            // Ctrl+Y: Redo the last undone operation.
            if (ViewModel.CanRedo)
            {
                ViewModel.Redo();
                e.Handled = true;
            }
        }
    }
}
