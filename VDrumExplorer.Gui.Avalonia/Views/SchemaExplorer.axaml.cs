// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia.Controls;
using VDrumExplorer.ViewModel.LogicalSchema;

namespace VDrumExplorer.Gui.Avalonia.Views;

/// <summary>
/// Window for exploring a module schema as a tree with details.
/// Ported from the WPF SchemaExplorer window.
/// </summary>
public partial class SchemaExplorer : Window
{
    public SchemaExplorer()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles tree view selection changes by updating the <see cref="ModuleSchemaViewModel.SelectedNode"/>
    /// property on the data context. Avalonia uses <see cref="SelectionChangedEventArgs"/> with
    /// <see cref="SelectionChangedEventArgs.AddedItems"/> instead of WPF's RoutedPropertyChangedEventArgs.
    /// </summary>
    private void TreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ModuleSchemaViewModel viewModel && treeView.SelectedItem is TreeNodeViewModel node)
        {
            viewModel.SelectedNode = node;
        }
    }
}
