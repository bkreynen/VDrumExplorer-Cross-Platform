// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Threading.Tasks;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Audio;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.LogicalSchema;

namespace VDrumExplorer.ViewModel.Test.Helpers
{
    /// <summary>
    /// Shared configurable <see cref="IViewServices"/> for ViewModel tests. Centralizes the 2-3
    /// inner <c>ConfigurableViewServices</c>/<c>TrackingViewServices</c> duplicates that were
    /// previously copy-pasted across <c>DataExplorerViewModelExtendedTest</c>,
    /// <c>ModuleExplorerViewModelExtendedTest</c> and <c>ExplorerHomeViewModelExtendedTest</c>.
    /// Uses <see cref="VDrumExplorer.ViewModel.Test.Fakes.FakeViewServices"/> callbacks where
    /// possible, but exposes the simple <c>Result</c> properties that existing tests rely on
    /// so migration is a drop-in replacement. Also tracks <c>Show*ExplorerCount</c> for
    /// assertion purposes.
    /// </summary>
    public sealed class ConfigurableViewServices : IViewServices
    {
        public string? OpenFileResult { get; set; }
        public string? SaveFileResult { get; set; }
        public int? CopyKitResult { get; set; }
        public bool CopyKitsResult { get; set; }
        public bool MultiPasteResult { get; set; }
        public Func<MultiPasteViewModel, bool>? MultiPasteCallback { get; set; }
        public bool DataTransferShouldExecute { get; set; }
        public Module? ModuleToReturn { get; set; }
        public Kit? KitToReturn { get; set; }

        public int ShowKitExplorerCount { get; private set; }
        public KitExplorerViewModel? LastKit { get; private set; }
        public int ShowModuleExplorerCount { get; private set; }
        public ModuleExplorerViewModel? LastModule { get; private set; }
        public int ShowAudioExplorerCount { get; private set; }
        public int ShowSchemaExplorerCount { get; private set; }
        public ModuleSchemaViewModel? LastSchemaViewModel { get; private set; }
        public ModuleIdentifier? LastSchemaExplorerIdentifier { get; private set; }
        public bool DataTransferExecuted { get; private set; }

        public Task<string?> ShowOpenFileDialogAsync(string filter) => Task.FromResult(OpenFileResult);
        public Task<string?> ShowSaveFileDialogAsync(string filter) => Task.FromResult(SaveFileResult);
        public Task<int?> ChooseCopyKitTargetAsync(CopyKitViewModel viewModel) => Task.FromResult(CopyKitResult);
        public Task<bool> ChooseCopyKitsTargetAsync(CopyKitsViewModel viewModel) => Task.FromResult(CopyKitsResult);
        public Task<bool> ChooseMultiPasteTargetsAsync(MultiPasteViewModel viewModel)
        {
            if (MultiPasteCallback != null) return Task.FromResult(MultiPasteCallback(viewModel));
            return Task.FromResult(MultiPasteResult);
        }
        public void ShowSchemaExplorer(ModuleSchemaViewModel viewModel)
        {
            ShowSchemaExplorerCount++;
            LastSchemaViewModel = viewModel;
            LastSchemaExplorerIdentifier = viewModel.ModelForTest.Identifier;
        }
        public void ShowKitExplorer(KitExplorerViewModel viewModel)
        {
            ShowKitExplorerCount++;
            LastKit = viewModel;
        }
        public void ShowModuleExplorer(ModuleExplorerViewModel viewModel)
        {
            ShowModuleExplorerCount++;
            LastModule = viewModel;
        }
        public void ShowInstrumentAudioExplorer(InstrumentAudioExplorerViewModel viewModel)
        {
            ShowAudioExplorerCount++;
        }
        public void ShowInstrumentRecorderDialog(InstrumentAudioRecorderViewModel viewModel) { }

        public async Task<T?> ShowDataTransferDialog<T>(DataTransferViewModel<T> viewModel) where T : class
        {
            DataTransferExecuted = true;
            if (typeof(T) == typeof(Module) && ModuleToReturn is T m) return m;
            if (typeof(T) == typeof(Kit) && KitToReturn is T k) return k;
            if (DataTransferShouldExecute)
            {
                try { return await viewModel.TransferAsync().ConfigureAwait(false); } catch { return null; }
            }
            return null;
        }

        public void AddRequerySuggestion(EventHandler handler) { }
        public void RemoveRequerySuggestion(EventHandler handler) { }
    }
}
