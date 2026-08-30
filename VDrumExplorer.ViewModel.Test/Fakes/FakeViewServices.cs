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
        /// <summary>
        /// Optional callbacks to configure success paths. When set, the corresponding dialog method delegates to the callback.
        /// When null, the default cancel/empty behaviour is preserved. This centralizes configurability so per-test
        /// ConfigurableViewServices duplicates are no longer needed for simple success-path coverage.
        /// </summary>
        public Func<string, Task<string?>>? OpenFileFunc { get; set; }
        public Func<string, Task<string?>>? SaveFileFunc { get; set; }
        public Func<CopyKitViewModel, Task<int?>>? ChooseCopyKitFunc { get; set; }
        public Func<CopyKitsViewModel, Task<bool>>? ChooseCopyKitsFunc { get; set; }
        public Func<MultiPasteViewModel, Task<bool>>? ChooseMultiPasteFunc { get; set; }
        /// <summary>
        /// Generic data-transfer handler. When set, invoked with the viewModel boxed as object; should return boxed result or null.
        /// </summary>
        public Func<object, Task<object?>>? DataTransferFunc { get; set; }

        public Task<string?> ShowOpenFileDialogAsync(string filter) =>
            OpenFileFunc is not null ? OpenFileFunc(filter) : Task.FromResult<string?>(null);

        public Task<string?> ShowSaveFileDialogAsync(string filter) =>
            SaveFileFunc is not null ? SaveFileFunc(filter) : Task.FromResult<string?>(null);

        public Task<int?> ChooseCopyKitTargetAsync(CopyKitViewModel viewModel) =>
            ChooseCopyKitFunc is not null ? ChooseCopyKitFunc(viewModel) : Task.FromResult<int?>(null);

        public Task<bool> ChooseCopyKitsTargetAsync(CopyKitsViewModel viewModel) =>
            ChooseCopyKitsFunc is not null ? ChooseCopyKitsFunc(viewModel) : Task.FromResult(false);

        public Task<bool> ChooseMultiPasteTargetsAsync(MultiPasteViewModel viewModel) =>
            ChooseMultiPasteFunc is not null ? ChooseMultiPasteFunc(viewModel) : Task.FromResult(false);

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

        public async Task<T?> ShowDataTransferDialog<T>(DataTransferViewModel<T> viewModel) where T : class
        {
            if (DataTransferFunc is not null)
            {
                var result = await DataTransferFunc(viewModel).ConfigureAwait(false);
                return result as T;
            }
            return null;
        }

        public void AddRequerySuggestion(EventHandler handler)
        {
        }

        public void RemoveRequerySuggestion(EventHandler handler)
        {
        }
    }
}
