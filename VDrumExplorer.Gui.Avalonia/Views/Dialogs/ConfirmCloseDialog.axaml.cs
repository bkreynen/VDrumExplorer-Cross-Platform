// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VDrumExplorer.Gui.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog shown when the user attempts to close a data explorer window with unsaved changes.
/// Returns true (close without saving) or false (cancel) via ShowDialog.
/// </summary>
public partial class ConfirmCloseDialog : Window
{
    public ConfirmCloseDialog()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close(true);
    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(false);
}
