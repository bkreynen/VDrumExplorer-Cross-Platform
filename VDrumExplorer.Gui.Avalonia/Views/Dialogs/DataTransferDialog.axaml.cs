// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Avalonia.Controls;
using Avalonia.Interactivity;
using VDrumExplorer.ViewModel.Dialogs;

namespace VDrumExplorer.Gui.Avalonia.Views.Dialogs;

/// <summary>
/// Progress dialog for data transfer operations (e.g. loading/saving kit or module data).
/// Avalonia port of the WPF DataTransferDialog. The transfer is started by
/// <see cref="ViewServices.AvaloniaViewServices.ShowDataTransferDialog{T}"/> when the
/// dialog's <see cref="Window.Opened"/> event fires, and the dialog is closed when the
/// view model's <see cref="DataTransferViewModel.DialogResult"/> property is set.
/// </summary>
public partial class DataTransferDialog : Window
{
    public DataTransferDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// When the dialog is closed (via the X button or programmatically), cancel the
    /// transfer. This mirrors the WPF behavior. CancellationTokenSource.Cancel() is
    /// safe to call multiple times, so this is harmless when the dialog closes normally
    /// after the transfer completes.
    /// </summary>
    private void HandleClosing(object? sender, WindowClosingEventArgs e) =>
        ((DataTransferViewModel)DataContext!).CancelCommand.Execute(null);
}
