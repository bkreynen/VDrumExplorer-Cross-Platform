// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Threading.Tasks;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Audio;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.LogicalSchema;

namespace VDrumExplorer.Gui.Avalonia.Test.Stubs;

/// <summary>
/// Stub implementation of <see cref="IViewServices"/> for headless tests.
/// The GUI project's <c>AvaloniaViewServices</c> is internal and would open real windows
/// or dialogs, which is undesirable in tests. All dialog methods report "cancelled"
/// (default values) and all "show window" methods are no-ops.
/// </summary>
internal sealed class StubViewServices : IViewServices
{
    public Task<string?> ShowOpenFileDialogAsync(string filter) => Task.FromResult<string?>(null);

    public Task<string?> ShowSaveFileDialogAsync(string filter) => Task.FromResult<string?>(null);

    public Task<int?> ChooseCopyKitTargetAsync(CopyKitViewModel viewModel) => Task.FromResult<int?>(null);

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
