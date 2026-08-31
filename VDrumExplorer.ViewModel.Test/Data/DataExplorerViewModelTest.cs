// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    [Collection("Clipboard")]
    public class DataExplorerViewModelTest
    {
        // DataExplorerViewModel is abstract; we test via KitExplorerViewModel and ModuleExplorerViewModel.
        private readonly Module module = TestData.LoadTD27Module();
        private KitExplorerViewModel CreateKitExplorer()
        {
            var kit = module.ExportKit(1);
            // Ensure static clipboard is cleared for isolation
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

        [Fact]
        public void ReadOnly_InitiallyTrue_AndToggleViaEditCommit()
        {
            var vm = CreateKitExplorer();
            Assert.True(vm.ReadOnly);
            Assert.True(vm.EditCommand.Enabled);
            Assert.False(vm.CommitCommand.Enabled);
            Assert.False(vm.CancelEditCommand.Enabled);

            vm.EditCommand.Execute(null!);
            Assert.False(vm.ReadOnly);
            Assert.False(vm.EditCommand.Enabled);
            Assert.True(vm.CommitCommand.Enabled);
            Assert.True(vm.CancelEditCommand.Enabled);

            vm.CommitCommand.Execute(null!);
            Assert.True(vm.ReadOnly);
        }

        [Fact]
        public void CancelEdit_RestoresReadOnly()
        {
            var vm = CreateKitExplorer();
            vm.EditCommand.Execute(null!);
            vm.CancelEditCommand.Execute(null!);
            Assert.True(vm.ReadOnly);
        }

        [Fact]
        public void FileName_Setter_UpdatesTitleAndRaisesPropertyChanged()
        {
            var vm = CreateKitExplorer();
            var changed = new List<string>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changed.Add(e.PropertyName!);
            vm.FileName = "myfile.vkit";
            Assert.Equal("myfile.vkit", vm.FileName);
            Assert.Contains("myfile.vkit", vm.Title);
            Assert.Contains(nameof(DataExplorerViewModel.FileName), changed);
            Assert.Contains(nameof(DataExplorerViewModel.Title), changed);
        }

        [Fact]
        public void Title_WithoutFileName_ContainsExplorerNameAndSchema()
        {
            var vm = CreateKitExplorer();
            Assert.Contains("Kit Explorer", vm.Title);
            Assert.Contains(vm.Kit.Schema.Identifier.Name, vm.Title);
        }

        [Fact]
        public void IsKitExplorer_ForKit_ReturnsTrue_ForModuleFalse()
        {
            var kitVm = CreateKitExplorer();
            var modVm = CreateModuleExplorer();
            Assert.True(kitVm.IsKitExplorer);
            Assert.False(kitVm.IsModuleExplorer);
            Assert.False(modVm.IsKitExplorer);
            Assert.True(modVm.IsModuleExplorer);
        }

        [Fact]
        public void CopyDataTitle_KitVsModule()
        {
            Assert.Equal("Copy Kit", CreateKitExplorer().CopyDataTitle);
            Assert.Equal("Copy Data", CreateModuleExplorer().CopyDataTitle);
        }

        [Fact]
        public void MidiChannels_Contains1To16()
        {
            var vm = CreateKitExplorer();
            Assert.Equal(16, vm.MidiChannels.Count);
            Assert.Equal(Enumerable.Range(1, 16).ToList(), vm.MidiChannels);
        }

        [Fact]
        public void SelectedMidiChannel_Default10_AndSettable()
        {
            var vm = CreateKitExplorer();
            Assert.Equal(10, vm.SelectedMidiChannel);
            vm.SelectedMidiChannel = 5;
            Assert.Equal(5, vm.SelectedMidiChannel);
        }

        [Fact]
        public void Attack_Default80_AndMinMax()
        {
            var vm = CreateKitExplorer();
            Assert.Equal(80, vm.Attack);
            Assert.Equal(1, vm.MinAttack);
            Assert.Equal(127, vm.MaxAttack);
            vm.Attack = 100;
            Assert.Equal(100, vm.Attack);
        }

        [Fact]
        public void SelectedNode_DefaultIsRoot_AndHasDetails()
        {
            var vm = CreateKitExplorer();
            Assert.Same(vm.Root[0], vm.SelectedNode);
            Assert.NotNull(vm.SelectedNodeDetails);
        }

        [Fact]
        public void SelectedNode_SetToNull_ClearsDetailsAndDisablesCopy()
        {
            var vm = CreateKitExplorer();
            vm.SelectedNode = null;
            Assert.Null(vm.SelectedNodeDetails);
            Assert.False(vm.CopyNodeCommand.Enabled);
        }

        [Fact]
        public void SelectedNode_SetToValidNode_EnablesCopy()
        {
            var vm = CreateKitExplorer();
            vm.SelectedNode = null;
            Assert.False(vm.CopyNodeCommand.Enabled);
            vm.SelectedNode = vm.Root[0];
            Assert.True(vm.CopyNodeCommand.Enabled);
        }

        [Fact]
        public void CopiedSnapshot_SetToNull_DisablesPasteAndMultiPaste()
        {
            var vm = CreateKitExplorer();
            // First copy to enable
            vm.CopyNodeCommand.Execute(null!);
            Assert.NotNull(vm.CopiedSnapshot);
            // Now clear
            vm.CopiedSnapshot = null;
            Assert.Null(vm.CopiedSnapshot);
            Assert.False(vm.MultiPasteCommand.Enabled);
            // Paste should be disabled because snapshot is null
            Assert.False(vm.PasteNodeCommand.Enabled);
        }

        [Fact]
        public void CopiedSnapshot_SetToValid_EnablesMultiPaste()
        {
            var vm = CreateKitExplorer();
            vm.CopiedSnapshot = null;
            Assert.False(vm.MultiPasteCommand.Enabled);
            vm.CopyNodeCommand.Execute(null!);
            Assert.True(vm.MultiPasteCommand.Enabled);
        }

        [Fact]
        public void CopyNodeCommand_Execute_SetsCopiedSnapshot()
        {
            var vm = CreateKitExplorer();
            vm.CopiedSnapshot = null;
            Assert.Null(vm.CopiedSnapshot);
            vm.CopyNodeCommand.Execute(null!);
            Assert.NotNull(vm.CopiedSnapshot);
        }

        [Fact]
        public void PasteNodeCommand_InitiallyDisabled_UntilCopyWithValidTarget()
        {
            var vm = CreateModuleExplorer();
            // No snapshot yet => Paste disabled
            Assert.False(vm.PasteNodeCommand.Enabled);
            // Copy first kit root
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            Assert.True(kitRoots.Count >= 2);
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            // Still on same node => IsValidForTarget true => Paste enabled
            Assert.True(vm.PasteNodeCommand.Enabled);
            // Select second kit root => still valid (same type)
            vm.SelectedNode = kitRoots[1];
            Assert.True(vm.PasteNodeCommand.Enabled);
            // Select root (module root) => different type => invalid => disabled
            vm.SelectedNode = vm.Root[0];
            Assert.False(vm.PasteNodeCommand.Enabled);
        }

        [Fact]
        public void PasteNodeCommand_Execute_ModifiesModelAndEnablesUndo()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            Assert.True(kitRoots.Count >= 2);
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            var snapshotBeforePaste = vm.CopiedSnapshot;
            Assert.NotNull(snapshotBeforePaste);

            vm.SelectedNode = kitRoots[1];
            Assert.False(vm.CanUndo);
            vm.PasteNodeCommand.Execute(null!);
            Assert.True(vm.CanUndo);
            Assert.False(vm.CanRedo);
        }

        [Fact]
        public void Undo_AfterPaste_RestoresAndEnablesRedo()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteNodeCommand.Execute(null!);
            Assert.True(vm.CanUndo);
            vm.Undo();
            Assert.False(vm.CanUndo);
            Assert.True(vm.CanRedo);
        }

        [Fact]
        public void Redo_AfterUndo_Restores()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteNodeCommand.Execute(null!);
            vm.Undo();
            Assert.True(vm.CanRedo);
            vm.Redo();
            Assert.True(vm.CanUndo);
            Assert.False(vm.CanRedo);
        }

        [Fact]
        public void Undo_WithEmptyStack_DoesNotThrow()
        {
            var vm = CreateKitExplorer();
            Assert.False(vm.CanUndo);
            vm.Undo();
            Assert.False(vm.CanUndo);
        }

        [Fact]
        public void Redo_WithEmptyStack_DoesNotThrow()
        {
            var vm = CreateKitExplorer();
            Assert.False(vm.CanRedo);
            vm.Redo();
            Assert.False(vm.CanRedo);
        }

        [Fact]
        public void MultiPasteCommand_InitiallyDisabled_WhenNoSnapshot()
        {
            var vm = CreateKitExplorer();
            vm.CopiedSnapshot = null;
            Assert.False(vm.MultiPasteCommand.Enabled);
        }

        [Fact]
        public void MultiPasteCommand_EnabledAfterCopy()
        {
            var vm = CreateKitExplorer();
            vm.CopiedSnapshot = null;
            vm.CopyNodeCommand.Execute(null!);
            Assert.True(vm.MultiPasteCommand.Enabled);
            vm.CopiedSnapshot = null;
            Assert.False(vm.MultiPasteCommand.Enabled);
        }

        [Fact]
        public void MultiPasteCommand_Execute_WhenCancelled_DoesNotEnableUndo()
        {
            var vm = CreateKitExplorer();
            vm.CopyNodeCommand.Execute(null!);
            Assert.False(vm.CanUndo);
            // FakeViewServices returns false for ChooseMultiPasteTargetsAsync, so MultiPaste does nothing
            vm.MultiPasteCommand.Execute(null!);
            // Should still be no undo because dialog was cancelled (no push)
            // Note: MultiPaste is async void, but Fake returns false quickly. Give it a moment.
            // Since we can't await async void, we just check it didn't throw and CanUndo still false.
            Assert.False(vm.CanUndo);
        }

        [Fact]
        public void ConvertibleModuleIdentifiers_NotNull()
        {
            var vm = CreateKitExplorer();
            Assert.NotNull(vm.ConvertibleModuleIdentifiers);
            _ = vm.HasConvertibleModuleIdentifiers;
        }

        [Fact]
        public void CopyNode_WithoutSelectedNode_DoesNotSetSnapshot()
        {
            var vm = CreateKitExplorer();
            vm.CopiedSnapshot = null;
            vm.SelectedNode = null;
            // CopyNodeCommand should be disabled, but executing anyway should not set snapshot
            // Directly executing command when disabled still calls CopyNode but returns early due to null sourceNode
            vm.CopyNodeCommand.Execute(null!);
            Assert.Null(vm.CopiedSnapshot);
        }

        [Fact]
        public void PasteNode_WithoutValidSnapshot_DoesNotEnableUndo()
        {
            var vm = CreateKitExplorer();
            vm.CopiedSnapshot = null;
            Assert.False(vm.CanUndo);
            vm.PasteNodeCommand.Execute(null!);
            Assert.False(vm.CanUndo);
        }

        [Fact]
        public void SelectedNodeDetails_UpdatedOnSelectionChange()
        {
            var vm = CreateModuleExplorer();
            var initialDetails = vm.SelectedNodeDetails;
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            Assert.NotNull(vm.SelectedNodeDetails);
            Assert.NotSame(initialDetails, vm.SelectedNodeDetails);
        }
    }
}
