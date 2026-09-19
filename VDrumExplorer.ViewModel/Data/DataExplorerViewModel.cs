// Copyright 2020 Jon Skeet. All rights reserved.
// This file was modified from the original at https://github.com/jskeet/DemoCode/tree/master/Drums
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Data;
using VDrumExplorer.Model.Data.Logical;
using VDrumExplorer.Utility;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.Home;
using VDrumExplorer.ViewModel.Status;

namespace VDrumExplorer.ViewModel.Data
{
    public abstract class DataExplorerViewModel : ViewModelBase<ModuleData>
    {
        protected ILogger Logger { get; }
        protected IViewServices ViewServices { get; }
        public DeviceViewModel DeviceViewModel { get; }
        /// <summary>
        /// Status line view model backing this window's live-region status line
        /// (polite success announcements / assertive error announcements, see
        /// docs/accessibility.md §5-§6). Operation outcomes will flow through it
        /// in a later task; it is exposed here so the view can bind to it.
        /// </summary>
        public StatusViewModel Status { get; } = new StatusViewModel();
        private readonly ModuleData data;
        private readonly bool IsMatchingDeviceConnected;

        public DelegateCommand RevertAllCommand { get; }
        public DelegateCommand PlayNoteCommand { get; }
        public DelegateCommand CopyNodeCommand { get; }
        public DelegateCommand PasteNodeCommand { get; }
        public DelegateCommand MultiPasteCommand { get; }
        /// <summary>
        /// Dispatching copy command for the Ctrl+C shortcut: in the Module Explorer a
        /// selected kit root copies the whole kit, otherwise the node's settings are
        /// copied (the branching that used to live in the view's KeyDown handler).
        /// </summary>
        public DelegateCommand CopyCommand { get; }
        /// <summary>
        /// Dispatching paste command for the Ctrl+V shortcut: if a kit was copied
        /// (Module Explorer), paste the kit into the selected kit's slot; otherwise
        /// paste the node's settings when a valid snapshot is available.
        /// </summary>
        public DelegateCommand PasteCommand { get; }
        /// <summary>
        /// Undo command (wraps <see cref="Undo"/>) for the Ctrl+Z shortcut.
        /// CanExecute tracks <see cref="CanUndo"/>.
        /// </summary>
        public DelegateCommand UndoCommand { get; }
        /// <summary>
        /// Redo command (wraps <see cref="Redo"/>) for the Ctrl+Y shortcut.
        /// CanExecute tracks <see cref="CanRedo"/>.
        /// </summary>
        public DelegateCommand RedoCommand { get; }
        // There are app commands of course, but it's not clear how we bind them.
        public DelegateCommand SaveFileCommand { get; }
        public DelegateCommand SaveFileAsCommand { get; }
        public DelegateCommand ExportJsonCommand { get; }
        public CommandBase CopyDataToDeviceCommand { get; }
        public virtual ICommand CopyToTemporaryStudioSetCommand => CommandBase.NotImplemented;
        public virtual ICommand CopyMultipleKitsCommand => CommandBase.NotImplemented;

        public ICommand ConvertCommand { get; }

        public void SaveHandler(object sender, EventArgs e)
        {
        }

        private string? fileName;
        public string? FileName
        {
            get => fileName;
            set
            {
                if (SetProperty(ref fileName, value))
                {
                    RaisePropertyChanged(nameof(Title));
                }
            }
        }

        public abstract ICommand OpenCopyInKitExplorerCommand { get; }
        public abstract ICommand CopyKitCommand { get; }
        public abstract ICommand ImportKitFromFileCommand { get; }
        public abstract ICommand ExportKitCommand { get; }

        protected abstract string ExplorerName { get; }
        public abstract string SaveFileFilter { get; }
        protected abstract void SaveToStream(Stream stream);
        protected abstract string FormatAsJson();
        // Ugly, but simple.
        public bool IsKitExplorer => this is KitExplorerViewModel;
        public bool IsModuleExplorer => !IsKitExplorer;
        public bool IsAerophoneKitExplorer => IsKitExplorer && Model.Schema.Identifier.Equals(ModuleIdentifier.AE10);

        public string CopyDataTitle => IsKitExplorer ? "Copy Kit" : "Copy Data";

        private string SchemaIdentifierDisplayName => $"{Model.Schema.Identifier.Name} rev 0x{Model.Schema.Identifier.SoftwareRevision:x}";

        public string Title => FileName is null
            ? $"{ExplorerName} ({SchemaIdentifierDisplayName}){(IsDirty ? " *" : "")}"
            : $"{ExplorerName} ({SchemaIdentifierDisplayName}){(IsDirty ? " *" : "")} - {fileName}";

        public IReadOnlyList<int> MidiChannels { get; } = Enumerable.Range(1, 16).ToList();
        public IReadOnlyList<ModuleIdentifierViewModel> ConvertibleModuleIdentifiers { get; }
        public bool HasConvertibleModuleIdentifiers => ConvertibleModuleIdentifiers.Any();

        private int selectedMidiChannel = 10;
        public int SelectedMidiChannel
        {
            get => selectedMidiChannel;
            // No validation, as we assume this is in a drop-down for now.
            set => SetProperty(ref selectedMidiChannel, value);
        }

        public int MinAttack => 1;
        public int MaxAttack => 127;

        private int attack = 80;
        public int Attack
        {
            get => attack;
            set => SetProperty(ref attack, value);
        }

        // Static so the clipboard is shared across all DataExplorer windows, allowing
        // copy/paste of node settings (e.g. a tom's parameters) from one window to another.
        private static NodeSnapshot? copiedSnapshot;
        public NodeSnapshot? CopiedSnapshot
        {
            get => copiedSnapshot;
            set
            {
                if (SetProperty(ref copiedSnapshot, value))
                {
                    PasteNodeCommand.Enabled = IsPasteNodeCommandValid;
                    MultiPasteCommand.Enabled = value is not null;
                    UpdatePasteCommandEnabled();
                }
            }
        }

        private bool IsPasteNodeCommandValid => copiedSnapshot?.IsValidForTarget(SelectedNode?.Model.SchemaNode) ?? false;

        /// <summary>
        /// Updates <see cref="PasteCommand.Enabled"/> to reflect everything that can make
        /// a paste possible: a copied kit (Module Explorer) or a node snapshot valid for
        /// the selected node. Must be called whenever either input changes.
        /// </summary>
        protected void UpdatePasteCommandEnabled() =>
            PasteCommand.Enabled = (this is ModuleExplorerViewModel moduleVm && moduleVm.HasCopiedKit)
                || IsPasteNodeCommandValid;

        /// <summary>
        /// Snapshot of the data as of the last save (or load). Used as the baseline
        /// for the "Revert all" command.
        /// </summary>
        private ModuleDataSnapshot? cleanSnapshot;

        private bool isDirty;
        /// <summary>
        /// Indicates whether the data has been modified since the last save (or load).
        /// </summary>
        public bool IsDirty
        {
            get => isDirty;
            private set
            {
                if (SetProperty(ref isDirty, value))
                {
                    RaisePropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// Marks the data as dirty (modified since the last save).
        /// Called by detail view models when field values change.
        /// </summary>
        public void MarkDirty()
        {
            IsDirty = true;
            RevertAllCommand.Enabled = true;
        }

        /// <summary>
        /// Marks the data as clean, recording the current state as the baseline
        /// for future "Revert all" operations.
        /// </summary>
        public void MarkClean()
        {
            cleanSnapshot = data.CreateSnapshot();
            IsDirty = false;
            RevertAllCommand.Enabled = false;
        }

        /// <summary>
        /// Recomputes <see cref="IsDirty"/> by comparing the current data against the clean snapshot.
        /// Called after undo/redo operations that may restore the data to its clean state.
        /// </summary>
        private void UpdateDirtyState()
        {
            if (cleanSnapshot is null)
            {
                return;
            }
            var current = data.CreateSnapshot();
            var dirty = !SnapshotsEqual(current, cleanSnapshot);
            IsDirty = dirty;
            RevertAllCommand.Enabled = dirty;
        }

        /// <summary>
        /// Compares two snapshots for equality by checking that they have the same segments
        /// with the same data at the same addresses.
        /// </summary>
        private static bool SnapshotsEqual(ModuleDataSnapshot a, ModuleDataSnapshot b)
        {
            var aSegments = a.Segments.ToList();
            var bSegments = b.Segments.ToList();
            if (aSegments.Count != bSegments.Count)
            {
                return false;
            }
            for (int i = 0; i < aSegments.Count; i++)
            {
                if (!aSegments[i].Address.Equals(bSegments[i].Address))
                {
                    return false;
                }
                var aData = aSegments[i].CopyData();
                var bData = bSegments[i].CopyData();
                if (!aData.SequenceEqual(bData))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Asks the user to confirm closing the explorer when there are unsaved changes.
        /// Exposed publicly so the view's Closing handler can prompt without
        /// reaching into the protected <see cref="ViewServices"/> property.
        /// </summary>
        public Task<bool> ConfirmCloseAsync() => ViewServices.ConfirmCloseAsync();

        public DataExplorerViewModel(IViewServices viewServices, ILogger logger, DeviceViewModel deviceViewModel, ModuleData data) : base(data)
        {
            Logger = logger;
            this.DeviceViewModel = deviceViewModel;
            this.ViewServices = viewServices;
            this.data = data;
            // TODO: t update this (and the results) if the device is plugged in later.
            IsMatchingDeviceConnected = deviceViewModel?.ConnectedDevice?.Schema.Identifier == data.Schema.Identifier;
            cleanSnapshot = data.CreateSnapshot();
            RevertAllCommand = new DelegateCommand(RevertAll, false);
            PlayNoteCommand = new DelegateCommand(PlayNote, IsMatchingDeviceConnected);
            SaveFileCommand = new DelegateCommand(SaveFile, true);
            SaveFileAsCommand = new DelegateCommand(SaveFileAs, true);
            ExportJsonCommand = new DelegateCommand(ExportJson, true);
            CopyDataToDeviceCommand = new DelegateCommand(CopyDataToDevice, IsMatchingDeviceConnected);
            CopyNodeCommand = new DelegateCommand(CopyNode, true);
            PasteNodeCommand = new DelegateCommand(PasteNode, true);
            MultiPasteCommand = new DelegateCommand(MultiPaste, false);
            CopyCommand = new DelegateCommand(CopySelected, true);
            PasteCommand = new DelegateCommand(PasteSelected, false);
            UndoCommand = new DelegateCommand(Undo, false);
            RedoCommand = new DelegateCommand(Redo, false);
            Root = SingleItemCollection.Of(new DataTreeNodeViewModel(data.LogicalRoot, this));
            SelectedNode = Root[0];

            ConvertCommand = new DelegateCommand<ModuleIdentifierViewModel>(ConvertToAlternativeSchema, true);
            var moduleId = data.Schema.Identifier;
            ConvertibleModuleIdentifiers = ModuleSchema.KnownSchemas.Keys
                .Where(key => key.Name == moduleId.Name)
                .Except(new[] { moduleId })
                .Select(id => new ModuleIdentifierViewModel(id, true))
                .ToList();
        }

        private void SaveFileAs() => SaveFileImpl(null);

        private void SaveFile() => SaveFileImpl(FileName);

        private async void SaveFileImpl(string? defaultFileName)
        {
            string? fileName = defaultFileName;
            if (fileName is null)
            {
                fileName = await ViewServices.ShowSaveFileDialogAsync(SaveFileFilter);
                if (fileName is null)
                {
                    return;
                }
                FileName = fileName;
            }
            using (var stream = File.Create(fileName))
            {
                SaveToStream(stream);
            }
            MarkClean();
        }

        private async void ExportJson()
        {
            var fileName = await ViewServices.ShowSaveFileDialogAsync(FileFilters.JsonFiles);
            if (fileName is null)
            {
                return;
            }
            var json = FormatAsJson();
            File.WriteAllText(fileName, json);
        }

        private readonly Stack<ModuleDataSnapshot> undoStack = new();
        private readonly Stack<ModuleDataSnapshot> redoStack = new();

        /// <summary>
        /// Maximum number of undo operations to remember.
        /// </summary>
        private const int MaxUndoStack = 50;

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        /// <summary>
        /// Takes a snapshot of the current data state and pushes it to the undo stack.
        /// Call this BEFORE any edit operation so the previous state can be restored.
        /// </summary>
        protected void PushUndoState()
        {
            undoStack.Push(data.CreateSnapshot());
            if (undoStack.Count > MaxUndoStack)
            {
                // Remove the oldest entry by converting to a temporary list, removing the first element, and rebuilding.
                var items = undoStack.ToList();
                items.RemoveAt(items.Count - 1); // Remove oldest (last in the list from ToList on a stack)
                undoStack.Clear();
                foreach (var item in items)
                {
                    undoStack.Push(item);
                }
            }
            redoStack.Clear();
            RaisePropertyChanged(nameof(CanUndo));
            RaisePropertyChanged(nameof(CanRedo));
            UpdateUndoRedoCommandEnabled();
            // All undoable operations modify the data, so they make it dirty.
            MarkDirty();
            RevertAllCommand.Enabled = true;
        }

        /// <summary>
        /// Reverts all data to the state it had when it was last saved (or loaded).
        /// The revert itself is undoable via the undo stack.
        /// </summary>
        private void RevertAll()
        {
            if (cleanSnapshot is null)
            {
                return;
            }
            PushUndoState();
            data.LoadSnapshot(cleanSnapshot, NullLogger.Instance);
            MarkClean();
            // Refresh the details panel to reflect the reverted data.
            if (SelectedNode is DataTreeNodeViewModel node)
            {
                SelectedNodeDetails = node.CreateDetails();
            }
        }

        /// <summary>
        /// Undoes the last edit operation by restoring the previous data state.
        /// </summary>
        public void Undo()
        {
            if (undoStack.Count == 0)
            {
                return;
            }
            redoStack.Push(data.CreateSnapshot());
            var previous = undoStack.Pop();
            data.LoadSnapshot(previous, NullLogger.Instance);
            RaisePropertyChanged(nameof(CanUndo));
            RaisePropertyChanged(nameof(CanRedo));
            UpdateUndoRedoCommandEnabled();
            UpdateDirtyState();
            // Refresh the details panel to reflect the restored data.
            if (SelectedNode is DataTreeNodeViewModel node)
            {
                SelectedNodeDetails = node.CreateDetails();
            }
        }

        /// <summary>
        /// Redoes the last undone operation by restoring the next data state.
        /// </summary>
        public void Redo()
        {
            if (redoStack.Count == 0)
            {
                return;
            }
            undoStack.Push(data.CreateSnapshot());
            var next = redoStack.Pop();
            data.LoadSnapshot(next, NullLogger.Instance);
            RaisePropertyChanged(nameof(CanUndo));
            RaisePropertyChanged(nameof(CanRedo));
            UpdateUndoRedoCommandEnabled();
            UpdateDirtyState();
            if (SelectedNode is DataTreeNodeViewModel node)
            {
                SelectedNodeDetails = node.CreateDetails();
            }
        }

        /// <summary>
        /// Ctrl+C dispatch: in the Module Explorer, a selected kit root copies the whole
        /// kit to the kit clipboard; otherwise copies the node's settings (like a tom's
        /// parameters). This branching used to live in the view's KeyDown handler.
        /// </summary>
        private void CopySelected()
        {
            if (this is ModuleExplorerViewModel moduleVm && SelectedNode?.IsKitRoot == true)
            {
                moduleVm.CopySelectedKitToClipboard();
            }
            else
            {
                CopyNodeCommand.Execute(null!);
            }
        }

        /// <summary>
        /// Ctrl+V dispatch: if a kit was copied (Module Explorer), paste the kit into the
        /// selected kit's slot; otherwise paste the node's settings into the selected
        /// node when a valid snapshot is available.
        /// </summary>
        private void PasteSelected()
        {
            if (this is ModuleExplorerViewModel moduleVm && moduleVm.HasCopiedKit)
            {
                moduleVm.PasteKitFromClipboard();
            }
            else if (CopiedSnapshot is not null && PasteNodeCommand.Enabled)
            {
                PasteNodeCommand.Execute(null!);
            }
        }

        /// <summary>
        /// Updates <see cref="UndoCommand"/>/<see cref="RedoCommand"/> enabled state from
        /// <see cref="CanUndo"/>/<see cref="CanRedo"/>. Must be called whenever either
        /// undo stack changes, so <see cref="UndoCommand.CanExecute"/> stays accurate.
        /// </summary>
        private void UpdateUndoRedoCommandEnabled()
        {
            UndoCommand.Enabled = CanUndo;
            RedoCommand.Enabled = CanRedo;
        }

        public SingleItemCollection<DataTreeNodeViewModel> Root { get; }

        private DataTreeNodeViewModel? selectedNode;
        public DataTreeNodeViewModel? SelectedNode
        {
            get => selectedNode;
            set
            {
                if (SetProperty(ref selectedNode, value))
                {
                    PlayNoteCommand.Enabled = IsMatchingDeviceConnected && SelectedNode?.MidiNotePath is object;
                    SelectedNodeDetails = selectedNode?.CreateDetails();
                    CopyNodeCommand.Enabled = selectedNode is object;
                    CopyCommand.Enabled = selectedNode is object;
                    PasteNodeCommand.Enabled = IsPasteNodeCommandValid;
                    UpdatePasteCommandEnabled();
                }
            }
        }

        private IReadOnlyList<IDataNodeDetailViewModel>? selectedNodeDetails;
        public IReadOnlyList<IDataNodeDetailViewModel>? SelectedNodeDetails
        {
            get => selectedNodeDetails;
            set => SetProperty(ref selectedNodeDetails, value);
        }

        private async void PlayNote()
        {
            var device = DeviceViewModel.ConnectedDevice;
            if (device is null)
            {
                return;
            }
            var midiNote = SelectedNode?.GetMidiNote();
            if (midiNote is null)
            {
                return;
            }

            // Switch the TD-17 to the kit that the selected node belongs to,
            // so the note plays with the correct kit's instrument settings.
            if (SelectedNode is DataTreeNodeViewModel node && node.KitNumber is int kitNumber)
            {
                try
                {
                    await device.SetCurrentKitAsync(kitNumber, CancellationToken.None);
                    // Give the TD-17 a moment to process the kit switch before sending the note.
                    await Task.Delay(100, CancellationToken.None);
                }
                catch
                {
                    // If we can't switch kits, play the note anyway with whatever kit is active.
                }
            }

            device.PlayNote(SelectedMidiChannel, midiNote.Value, Attack);
        }

        protected async void CopyDataToDevice(DataTreeNode? node, ModuleAddress? targetAddress)
        {
            var device = DeviceViewModel.ConnectedDevice;
            if (device is null || node is null)
            {
                return;
            }
            // We may or may not really need to bring up the dialog box, but it's simplest to always do that.
            var viewModel = new DataTransferViewModel<string>(Logger, "Copying data to device", "Copying {0}",
                async (progress, token) => { await device.SaveDescendants(node, targetAddress, progress, token); return ""; });
            await ViewServices.ShowDataTransferDialog(viewModel);
        }

        protected abstract void CopyDataToDevice();

        private void CopyNode()
        {
            var sourceNode = SelectedNode?.Model.SchemaNode;
            if (sourceNode is null)
            {
                return;
            }
            var snapshot = Model.CreatePartialSnapshot(sourceNode);
            CopiedSnapshot = new NodeSnapshot(sourceNode, snapshot);
        }

        private void PasteNode()
        {
            var targetNode = SelectedNode?.Model.SchemaNode;
            if (CopiedSnapshot?.IsValidForTarget(targetNode) != true)
            {
                return;
            }
            PushUndoState();
            var relocated = CopiedSnapshot.Data.Relocated(CopiedSnapshot.SourceNode, targetNode!);
            Model.LoadPartialSnapshot(relocated, Logger);
        }

        private async void MultiPaste()
        {
            if (CopiedSnapshot is not NodeSnapshot snapshot)
            {
                return;
            }
            var candidates = Root.Single().Model.SchemaNode.DescendantsAndSelf().Where(snapshot.IsValidForTarget).ToList();
            var vm = new MultiPasteViewModel(snapshot, candidates);
            if (await ViewServices.ChooseMultiPasteTargetsAsync(vm))
            {
                PushUndoState();
                foreach (var candidate in vm.Candidates.Where(c => c.Checked))
                {
                    var relocated = CopiedSnapshot.Data.Relocated(CopiedSnapshot.SourceNode, candidate.Candidate);
                    Model.LoadPartialSnapshot(relocated, Logger);
                }
            }
        }

        private void ConvertToAlternativeSchema(ModuleIdentifierViewModel targetId) =>
            ConvertToAlternativeSchema(ModuleSchema.KnownSchemas[targetId.Identifier].Value);

        protected abstract void ConvertToAlternativeSchema(ModuleSchema schema);
    }
}
