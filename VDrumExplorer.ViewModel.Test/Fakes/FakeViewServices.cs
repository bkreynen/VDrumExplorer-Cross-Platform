// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VDrumExplorer.ViewModel.Audio;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.LogicalSchema;

namespace VDrumExplorer.ViewModel.Test.Fakes
{
    /// <summary>
    /// Minimal implementation of <see cref="IViewServices"/> for testing purposes.
    /// All dialog methods return default/cancelled values; all Show methods are no-ops.
    /// </summary>
    internal sealed class FakeViewServices : IViewServices
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

        public Task<T?> ShowDataTransferDialog<T>(DataTransferViewModel<T> viewModel) where T : class
            => Task.FromResult<T?>(null);

        public void AddRequerySuggestion(EventHandler handler)
        {
        }

        public void RemoveRequerySuggestion(EventHandler handler)
        {
        }
    }
}
