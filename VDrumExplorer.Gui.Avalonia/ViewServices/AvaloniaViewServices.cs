// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using VDrumExplorer.Gui.Avalonia.Views.Dialogs;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Audio;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.LogicalSchema;

namespace VDrumExplorer.Gui.Avalonia.ViewServices;

/// <summary>
/// Avalonia implementation of <see cref="IViewServices"/>.
/// File dialogs use Avalonia's async StorageProvider API, and the
/// <see cref="IViewServices"/> interface defines async methods for file dialogs
/// to avoid sync-over-async deadlocks on the UI thread.
/// Some methods are still stubbed with NotImplementedException for later phases as the corresponding
/// views are ported.
/// </summary>
internal sealed class AvaloniaViewServices : IViewServices
{
    private event EventHandler? RequerySuggested;

    /// <summary>
    /// The main window, used as the parent for dialog boxes.
    /// Set by App.OnFrameworkInitializationCompleted after the main window is created.
    /// </summary>
    public Window? MainWindow { get; set; }

    public async Task<string?> ShowOpenFileDialogAsync(string filter)
    {
        var window = MainWindow;
        if (window is null)
        {
            return null;
        }
        var options = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = ParseFilter(filter),
        };
        var files = await window.StorageProvider.OpenFilePickerAsync(options);
        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    public async Task<string?> ShowSaveFileDialogAsync(string filter)
    {
        var window = MainWindow;
        if (window is null)
        {
            return null;
        }
        var options = new FilePickerSaveOptions
        {
            FileTypeChoices = ParseFilter(filter),
        };
        var file = await window.StorageProvider.SaveFilePickerAsync(options);
        return file?.Path.LocalPath;
    }

    public async Task<int?> ChooseCopyKitTargetAsync(CopyKitViewModel viewModel)
    {
        if (MainWindow is null)
        {
            return null;
        }
        var dialog = new CopyKitTargetDialog { DataContext = viewModel };
        var result = await dialog.ShowDialog<bool?>(MainWindow);
        return result == true ? viewModel.DestinationKitNumber : null;
    }

    public async Task<bool> ChooseCopyKitsTargetAsync(CopyKitsViewModel viewModel)
    {
        if (MainWindow is null)
        {
            return false;
        }
        var dialog = new CopyKitsDialog { DataContext = viewModel };
        var result = await dialog.ShowDialog<bool?>(MainWindow);
        return result == true;
    }

    public async Task<bool> ChooseMultiPasteTargetsAsync(MultiPasteViewModel viewModel)
    {
        if (MainWindow is null)
        {
            return false;
        }
        var dialog = new MultiPasteDialog { DataContext = viewModel };
        var result = await dialog.ShowDialog<bool?>(MainWindow);
        return result == true;
    }

    public void ShowSchemaExplorer(ModuleSchemaViewModel viewModel)
    {
        var window = new Views.SchemaExplorer { DataContext = viewModel };
        window.Show();
    }

    public void ShowKitExplorer(KitExplorerViewModel viewModel) =>
        CreateAndShowDataExplorer(viewModel);

    public void ShowModuleExplorer(ModuleExplorerViewModel viewModel) =>
        CreateAndShowDataExplorer(viewModel);

    /// <summary>
    /// Creates a DataExplorer window with the given view model as its DataContext, and shows it.
    /// Both KitExplorer and ModuleExplorer use the same DataExplorer window, differing only by
    /// the view model subclass provided.
    /// </summary>
    private static void CreateAndShowDataExplorer(DataExplorerViewModel viewModel)
    {
        var window = new Views.DataExplorer { DataContext = viewModel };
        window.Show();
    }

    public void ShowInstrumentAudioExplorer(InstrumentAudioExplorerViewModel viewModel) =>
        throw new NotImplementedException("ShowInstrumentAudioExplorer will be implemented in Phase 3.");

    public void ShowInstrumentRecorderDialog(InstrumentAudioRecorderViewModel viewModel) =>
        throw new NotImplementedException("ShowInstrumentRecorderDialog will be implemented in Phase 3.");

    public async Task<T?> ShowDataTransferDialog<T>(DataTransferViewModel<T> viewModel) where T : class
    {
        if (MainWindow is null)
        {
            return null;
        }
        var dialog = new DataTransferDialog { DataContext = viewModel };
        Task<T>? task = null;

        // The view model sets DialogResult when the transfer completes (success or failure).
        // Close the dialog when that happens, passing the result back to ShowDialog.
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(viewModel.DialogResult))
            {
                dialog.Close(viewModel.DialogResult);
            }
        };

        // Start the transfer only when the dialog is actually shown, to avoid a race
        // condition where the transfer fails before the dialog is displayed (which would
        // cause the Close call above to be ignored).
        dialog.Opened += (sender, args) =>
        {
            if (task is null)
            {
                task = viewModel.TransferAsync();
            }
        };

        var result = await dialog.ShowDialog<bool?>(MainWindow);
        return result == true && task is not null ? await task : null;
    }

    public void AddRequerySuggestion(EventHandler handler) => RequerySuggested += handler;
    public void RemoveRequerySuggestion(EventHandler handler) => RequerySuggested -= handler;

    /// <summary>
    /// Raises the RequerySuggested event, prompting commands to re-evaluate their CanExecute state.
    /// </summary>
    public void RaiseRequerySuggested() => RequerySuggested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Parses a WPF-style file dialog filter string (e.g. "Description|*.ext" or
    /// "Description1|*.ext1|Description2|*.ext2") into Avalonia FilePickerFileType[].
    /// Multiple extensions within one filter are separated by semicolons (e.g. "*.vdrum;*.vkit").
    /// </summary>
    private static List<FilePickerFileType> ParseFilter(string filter)
    {
        var types = new List<FilePickerFileType>();
        var parts = filter.Split('|');
        // WPF filter format is pairs of (description, pattern), so iterate by 2.
        for (int i = 0; i < parts.Length; i += 2)
        {
            if (i + 1 >= parts.Length)
            {
                break;
            }
            var description = parts[i];
            var patterns = parts[i + 1].Split(';');
            types.Add(new FilePickerFileType(description) { Patterns = patterns });
        }
        return types;
    }
}
