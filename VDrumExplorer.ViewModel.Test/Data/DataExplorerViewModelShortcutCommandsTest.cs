// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    /// <summary>
    /// Tests for the dispatching shortcut commands added for the declarative
    /// <c>Window.KeyBindings</c> in DataExplorer.axaml (Ctrl+C/V/Z/Y):
    /// <see cref="DataExplorerViewModel.CopyCommand"/>,
    /// <see cref="DataExplorerViewModel.PasteCommand"/>,
    /// <see cref="DataExplorerViewModel.UndoCommand"/> and
    /// <see cref="DataExplorerViewModel.RedoCommand"/>.
    /// <para>
    /// These commands replaced the KeyDown handler in the view's code-behind, so their
    /// dispatch behavior (kit copy/paste in the Module Explorer vs node copy/paste
    /// elsewhere) is what the shortcuts now exercise end to end.
    /// </para>
    /// </summary>
    [Collection("Clipboard")]
    public class DataExplorerViewModelShortcutCommandsTest
    {
        private readonly Module module = TestData.LoadTD27Module();

        private KitExplorerViewModel CreateKitExplorer()
        {
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(new FakeViewServices(), NullLogger.Instance, new DeviceViewModel(), kit);
            vm.CopiedSnapshot = null;
            return vm;
        }

        private ModuleExplorerViewModel CreateModuleExplorer()
        {
            var vm = new ModuleExplorerViewModel(new FakeViewServices(), NullLogger.Instance, new DeviceViewModel(), module);
            vm.CopiedSnapshot = null;
            return vm;
        }

        private static List<DataTreeNodeViewModel> FindAllKitRoots(DataTreeNodeViewModel node)
        {
            var result = new List<DataTreeNodeViewModel>();
            FindRecursive(node, result);
            return result;
            static void FindRecursive(DataTreeNodeViewModel current, List<DataTreeNodeViewModel> acc)
            {
                if (current.IsKitRoot) acc.Add(current);
                foreach (var child in current.Children) FindRecursive(child, acc);
            }
        }

        // === CopyCommand dispatch ===

        [Fact]
        public void CopyCommand_KitExplorer_CopiesNodeSnapshot()
        {
            var vm = CreateKitExplorer();
            Assert.Null(vm.CopiedSnapshot);

            vm.CopyCommand.Execute(null!);

            // Kit Explorer has no kit clipboard: non-kit-root dispatch falls through to
            // the node copy path (the branch that used to live in the KeyDown handler).
            Assert.NotNull(vm.CopiedSnapshot);
        }

        [Fact]
        public void CopyCommand_ModuleExplorer_KitRootSelected_CopiesKitToClipboard()
        {
            var vm = CreateModuleExplorer();
            vm.SelectedNode = FindAllKitRoots(vm.Root[0])[0];

            vm.CopyCommand.Execute(null!);

            Assert.True(vm.HasCopiedKit);
            // PasteCommand must become enabled as soon as a kit is on the kit clipboard.
            Assert.True(vm.PasteCommand.Enabled);
        }

        [Fact]
        public void CopyCommand_ModuleExplorer_NonKitRootSelected_CopiesNodeSnapshot()
        {
            var vm = CreateModuleExplorer();
            // Module root is not a kit root.
            vm.SelectedNode = vm.Root[0];

            vm.CopyCommand.Execute(null!);

            Assert.False(vm.HasCopiedKit);
            Assert.NotNull(vm.CopiedSnapshot);
        }

        [Fact]
        public void CopyCommand_EnabledTracksSelectedNode()
        {
            var vm = CreateKitExplorer();
            Assert.True(vm.CopyCommand.Enabled);
            vm.SelectedNode = null;
            Assert.False(vm.CopyCommand.Enabled);
            vm.SelectedNode = vm.Root[0];
            Assert.True(vm.CopyCommand.Enabled);
        }

        // === PasteCommand dispatch ===

        [Fact]
        public void PasteCommand_InitiallyDisabled_AndNoOpWithoutCopiedData()
        {
            var vm = CreateKitExplorer();
            Assert.Null(vm.CopiedSnapshot);
            Assert.False(vm.PasteCommand.Enabled);

            vm.PasteCommand.Execute(null!);

            // Nothing copied, nothing selected to paste into: no undo state pushed.
            Assert.False(vm.CanUndo);
            Assert.False(vm.CanRedo);
        }

        [Fact]
        public void PasteCommand_ModuleExplorer_AfterNodeCopy_PastesNodeAndEnablesUndo()
        {
            var vm = CreateModuleExplorer();
            // Module root is not a kit root, so CopyCommand takes the node-copy dispatch.
            vm.SelectedNode = vm.Root[0];
            vm.CopyCommand.Execute(null!);
            Assert.False(vm.HasCopiedKit);
            Assert.NotNull(vm.CopiedSnapshot);

            // The snapshot's target is still selected: paste is enabled and pushes undo state.
            Assert.True(vm.PasteCommand.Enabled);
            vm.PasteCommand.Execute(null!);
            Assert.True(vm.CanUndo);
        }

        [Fact]
        public void PasteCommand_ModuleExplorer_AfterKitCopy_PastesKitAndEnablesUndo()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            Assert.True(kitRoots.Count >= 2);
            vm.SelectedNode = kitRoots[0];
            vm.CopyCommand.Execute(null!); // kit-root dispatch: whole kit to clipboard

            vm.SelectedNode = kitRoots[1];
            Assert.True(vm.PasteCommand.Enabled);
            vm.PasteCommand.Execute(null!);

            Assert.True(vm.CanUndo);
        }

        [Fact]
        public void PasteCommand_ModuleExplorer_PasteDoesNotClearKitClipboard()
        {
            var vm = CreateModuleExplorer();
            vm.SelectedNode = FindAllKitRoots(vm.Root[0])[0];
            vm.CopyCommand.Execute(null!);
            Assert.True(vm.HasCopiedKit);

            vm.PasteCommand.Execute(null!);

            // The kit clipboard is not consumed on paste (matches the previous
            // KeyDown-handler behavior: repeated Ctrl+V keeps pasting).
            Assert.True(vm.HasCopiedKit);
            Assert.True(vm.PasteCommand.Enabled);
        }

        // === UndoCommand / RedoCommand ===

        [Fact]
        public void UndoAndRedoCommands_InitiallyDisabled()
        {
            var vm = CreateKitExplorer();
            Assert.False(vm.CanUndo);
            Assert.False(vm.CanRedo);
            Assert.False(vm.UndoCommand.Enabled);
            Assert.False(vm.RedoCommand.Enabled);
            Assert.False(vm.UndoCommand.CanExecute(null!));
            Assert.False(vm.RedoCommand.CanExecute(null!));
        }

        [Fact]
        public void UndoCommand_EnabledAfterPaste_UndoCommandExecuteUndoes()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteCommand.Execute(null!);

            Assert.True(vm.UndoCommand.Enabled);
            Assert.False(vm.RedoCommand.Enabled);

            vm.UndoCommand.Execute(null!);

            Assert.False(vm.UndoCommand.Enabled);
            Assert.True(vm.RedoCommand.Enabled);
        }

        [Fact]
        public void RedoCommand_AfterUndoCommand_RestoresRedoDisabled()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteCommand.Execute(null!);
            vm.UndoCommand.Execute(null!);

            Assert.True(vm.RedoCommand.Enabled);

            vm.RedoCommand.Execute(null!);

            Assert.True(vm.UndoCommand.Enabled);
            Assert.False(vm.RedoCommand.Enabled);
        }

        [Fact]
        public void UndoCommand_CanExecuteChanged_FiresOnUndoStackChanges()
        {
            var vm = CreateModuleExplorer();
            var fired = new List<bool>();
            vm.UndoCommand.CanExecuteChanged += (s, e) => fired.Add(((VDrumExplorer.ViewModel.CommandBase)s!).Enabled);

            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteCommand.Execute(null!); // undo stack push: UndoCommand false -> true

            Assert.Equal(new[] { true }, fired);

            vm.UndoCommand.Execute(null!); // undo: UndoCommand true -> false

            Assert.Equal(new[] { true, false }, fired);
        }

        [Fact]
        public void RedoCommand_CanExecuteChanged_FiresOnUndoAndRedo()
        {
            var vm = CreateModuleExplorer();
            var fired = new List<bool>();
            vm.RedoCommand.CanExecuteChanged += (s, e) => fired.Add(((VDrumExplorer.ViewModel.CommandBase)s!).Enabled);

            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteCommand.Execute(null!); // stack push clears redo: no change (already false)
            vm.UndoCommand.Execute(null!);  // redo becomes possible: false -> true

            Assert.Equal(new[] { true }, fired);

            vm.RedoCommand.Execute(null!); // redo consumed: true -> false

            Assert.Equal(new[] { true, false }, fired);
        }
    }
}
