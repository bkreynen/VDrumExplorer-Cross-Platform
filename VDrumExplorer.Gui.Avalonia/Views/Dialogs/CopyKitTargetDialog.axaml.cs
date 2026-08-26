// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VDrumExplorer.Gui.Avalonia.Views.Dialogs;

/// <summary>
/// Modal dialog for selecting a destination kit number when copying a kit.
/// Avalonia port of the WPF CopyKitTargetDialog.
/// </summary>
public partial class CopyKitTargetDialog : Window
{
    public CopyKitTargetDialog()
    {
        InitializeComponent();
    }

    private void Copy(object? sender, RoutedEventArgs e) => Close(true);

    private void Cancel(object? sender, RoutedEventArgs e) => Close(false);
}
