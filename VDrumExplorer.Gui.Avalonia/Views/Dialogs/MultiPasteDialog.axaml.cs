// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia.Controls;
using Avalonia.Interactivity;
using VDrumExplorer.ViewModel.Dialogs;

namespace VDrumExplorer.Gui.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for selecting multiple paste targets via checkboxes.
/// Avalonia port of the WPF MultiPasteDialog.
/// </summary>
public partial class MultiPasteDialog : Window
{
    public MultiPasteDialog()
    {
        InitializeComponent();
    }

    private void Cancel(object? sender, RoutedEventArgs e) => Close(false);

    private void Paste(object? sender, RoutedEventArgs e) => Close(true);

    private void SelectAll(object? sender, RoutedEventArgs e) =>
        SetCheckedForAllCandidates(true);

    private void SelectNone(object? sender, RoutedEventArgs e) =>
        SetCheckedForAllCandidates(false);

    private void SetCheckedForAllCandidates(bool value)
    {
        var vm = (MultiPasteViewModel) DataContext!;
        foreach (var item in vm.Candidates)
        {
            item.Checked = value;
        }
    }
}
