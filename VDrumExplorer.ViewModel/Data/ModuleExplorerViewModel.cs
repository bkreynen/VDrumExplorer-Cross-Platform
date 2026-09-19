// Copyright 2020 Jon Skeet. All rights reserved.
// This file was modified from the original at https://github.com/jskeet/DemoCode/tree/master/Drums
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Json;
using VDrumExplorer.Proto;
using VDrumExplorer.ViewModel.Dialogs;

namespace VDrumExplorer.ViewModel.Data
{
    public class ModuleExplorerViewModel : DataExplorerViewModel
    {
        public Module Module { get; }

        public ModuleExplorerViewModel(IViewServices viewServices, ILogger logger, DeviceViewModel deviceViewModel, Module module)
            : base(viewServices, logger, deviceViewModel, module.Data)
        {
            Module = module;
            OpenCopyInKitExplorerCommand = new ConditionallyEnabledDelegateCommand<DataTreeNodeViewModel>(viewServices, OpenCopyInKitExplorer, IsKitNode);
            CopyKitCommand = new ConditionallyEnabledDelegateCommand<DataTreeNodeViewModel>(viewServices, CopyKit, IsKitNode);
            ImportKitFromFileCommand = new ConditionallyEnabledDelegateCommand<DataTreeNodeViewModel>(viewServices, ImportKitFromFile, IsKitNode);
            ExportKitCommand = new ConditionallyEnabledDelegateCommand<DataTreeNodeViewModel>(viewServices, ExportKit, IsKitNode);
            CopyMultipleKitsCommand = new DelegateCommand(CopyMultipleKits, true);

            bool IsKitNode(DataTreeNodeViewModel node) => node?.IsKitRoot is true;
        }

        protected override string ExplorerName => "Module Explorer";
        public override string SaveFileFilter => FileFilters.ModuleFiles;

        protected override void SaveToStream(Stream stream) => Module.Save(stream);

        public override ICommand OpenCopyInKitExplorerCommand { get; }
        public override ICommand CopyKitCommand { get; }
        public override ICommand ImportKitFromFileCommand { get; }
        public override ICommand ExportKitCommand { get; }
        public override ICommand CopyMultipleKitsCommand { get; }

        private void OpenCopyInKitExplorer(DataTreeNodeViewModel kitNode)
        {
            if (kitNode.KitNumber is not int kitNumber)
            {
                return;
            }
            var kit = Module.ExportKit(kitNumber);
            var viewModel = new KitExplorerViewModel(ViewServices, Logger, DeviceViewModel, kit);
            ViewServices.ShowKitExplorer(viewModel);
        }

        private async void CopyKit(DataTreeNodeViewModel kitNode)
        {
            if (kitNode.KitNumber is not int kitNumber)
            {
                return;
            }
            var kit = Module.ExportKit(kitNumber);
            var viewModel = new CopyKitViewModel(Module, kit);
            var destinationKitNumber = await ViewServices.ChooseCopyKitTargetAsync(viewModel);
            if (destinationKitNumber is int destination)
            {
                PushUndoState();
                Module.ImportKit(kit, destination);
                Status.SetMessage($"Copied kit to slot {destination}");
            }
        }

        /// <summary>
        /// Copies a range of kits (sourceFrom..sourceTo) to a destination range
        /// starting at destinationFrom, in a single operation with one undo entry.
        /// </summary>
        private async void CopyMultipleKits()
        {
            var viewModel = new CopyKitsViewModel(Module);
            if (await ViewServices.ChooseCopyKitsTargetAsync(viewModel))
            {
                PushUndoState();
                int count = viewModel.CopyCount;
                for (int i = 0; i < count; i++)
                {
                    int sourceKit = viewModel.SourceFrom + i;
                    int destKit = viewModel.DestinationFrom + i;
                    // Export before importing so overlapping ranges copy correctly.
                    var kit = Module.ExportKit(sourceKit);
                    Module.ImportKit(kit, destKit);
                }
                Status.SetMessage($"Copied {count} kits to kit {viewModel.DestinationFrom}");
            }
        }

        private async void ImportKitFromFile(DataTreeNodeViewModel kitNode)
        {
            if (kitNode.KitNumber is not int kitNumber)
            {
                return;
            }
            string? file = await ViewServices.ShowOpenFileDialogAsync(FileFilters.KitFiles);
            if (file is null)
            {
                return;
            }
            object loaded;
            try
            {
                loaded = ProtoIo.LoadModel(file, Logger);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading {file}", ex);
                Status.SetError($"Could not load kit file: {ex.Message}");
                return;
            }
            if (!(loaded is Kit kit))
            {
                Logger.LogError("Loaded file was not a kit");
                Status.SetError("Could not import kit: loaded file was not a kit");
                return;
            }

            if (!kit.Schema.Identifier.Equals(Module.Schema.Identifier))
            {
                Logger.LogError($"Kit was from {kit.Schema.Identifier.Name}; this module is {Module.Schema.Identifier.Name}");
                Status.SetError($"Could not import kit: kit was from {kit.Schema.Identifier.Name}; this module is {Module.Schema.Identifier.Name}");
                return;
            }
            PushUndoState();
            Module.ImportKit(kit, kitNumber);
            Status.SetMessage($"Imported kit {kitNumber} from {Path.GetFileName(file)}");
        }

        private async void ExportKit(DataTreeNodeViewModel kitNode)
        {
            if (kitNode.KitNumber is not int kitNumber)
            {
                return;
            }

            var kit = Module.ExportKit(kitNumber);
            var file = await ViewServices.ShowSaveFileDialogAsync(FileFilters.KitFiles);
            if (file is null)
            {
                return;
            }
            try
            {
                using (var stream = File.Create(file))
                {
                    kit.Save(stream);
                }
                Status.SetMessage($"Exported kit {kitNumber} to {Path.GetFileName(file)}");
            }
            catch (Exception ex)
            {
                Status.SetError($"Could not export kit {kitNumber}: {ex.Message}");
            }
        }

        // Internal clipboard for Ctrl+C/Ctrl+V kit copy.
        private Kit? copiedKit;

        /// <summary>
        /// Whether a kit has been copied to the internal clipboard (for Ctrl+V paste).
        /// </summary>
        public bool HasCopiedKit => copiedKit is not null;

        /// <summary>
        /// Copies the currently selected kit to the internal clipboard.
        /// Called by <see cref="DataExplorerViewModel.CopyCommand"/> (Ctrl+C) when a kit
        /// root is selected in the Module Explorer.
        /// </summary>
        public void CopySelectedKitToClipboard()
        {
            if (SelectedNode is DataTreeNodeViewModel node && node.IsKitRoot && node.KitNumber is int kitNumber)
            {
                copiedKit = Module.ExportKit(kitNumber);
                RaisePropertyChanged(nameof(HasCopiedKit));
                UpdatePasteCommandEnabled();
                Status.SetMessage($"Copied kit {kitNumber}");
            }
        }

        /// <summary>
        /// Pastes the kit from the internal clipboard directly into the currently selected kit's slot.
        /// Called by <see cref="DataExplorerViewModel.PasteCommand"/> (Ctrl+V) when a kit is on the
        /// kit clipboard. No dialog — just paste.
        /// </summary>
        public void PasteKitFromClipboard()
        {
            if (copiedKit is null)
            {
                return;
            }
            // Paste into the currently selected kit's slot.
            if (SelectedNode is DataTreeNodeViewModel node && node.IsKitRoot && node.KitNumber is int destination)
            {
                PushUndoState();
                Module.ImportKit(copiedKit, destination);
                Status.SetMessage($"Pasted kit into kit {destination}");
            }
        }

        protected override void CopyDataToDevice() => CopyDataToDevice(SelectedNode?.Model, null);

        protected override void ConvertToAlternativeSchema(ModuleSchema schema)
        {
            var converted = new Module(Module.Data.ConvertToSchema(schema, Logger));
            ViewServices.ShowModuleExplorer(new ModuleExplorerViewModel(ViewServices, Logger, DeviceViewModel, converted));
        }

        protected override string FormatAsJson() => Module.ToJson();
    }
}
