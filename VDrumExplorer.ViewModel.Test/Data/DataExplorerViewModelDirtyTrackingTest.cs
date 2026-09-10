// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Data.Fields;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    [Collection("Clipboard")]
    public class DataExplorerViewModelDirtyTrackingTest
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

        // === IsDirty ===

        [Fact]
        public void IsDirty_InitiallyFalse()
        {
            var vm = CreateKitExplorer();
            Assert.False(vm.IsDirty);
        }

        [Fact]
        public void Title_DoesNotContainAsterisk_WhenClean()
        {
            var vm = CreateKitExplorer();
            Assert.DoesNotContain(" *", vm.Title);
        }

        [Fact]
        public void Title_ContainsAsterisk_WhenDirty()
        {
            var vm = CreateKitExplorer();
            vm.MarkDirty();
            Assert.Contains(" *", vm.Title);
        }

        [Fact]
        public void Title_RemovesAsterisk_WhenCleanedAfterDirty()
        {
            var vm = CreateKitExplorer();
            vm.MarkDirty();
            Assert.Contains(" *", vm.Title);
            vm.MarkClean();
            Assert.DoesNotContain(" *", vm.Title);
        }

        [Fact]
        public void IsDirty_RaisesPropertyChanged_WhenChanged()
        {
            var vm = CreateKitExplorer();
            var changed = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changed.Add(e.PropertyName);
            vm.MarkDirty();
            Assert.Contains(nameof(DataExplorerViewModel.IsDirty), changed);
        }

        [Fact]
        public void IsDirty_RaisesTitlePropertyChanged_WhenChanged()
        {
            var vm = CreateKitExplorer();
            var changed = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changed.Add(e.PropertyName);
            vm.MarkDirty();
            Assert.Contains(nameof(DataExplorerViewModel.Title), changed);
        }

        // === MarkDirty / MarkClean ===

        [Fact]
        public void MarkDirty_SetsIsDirtyTrue()
        {
            var vm = CreateKitExplorer();
            vm.MarkDirty();
            Assert.True(vm.IsDirty);
        }

        [Fact]
        public void MarkClean_SetsIsDirtyFalse()
        {
            var vm = CreateKitExplorer();
            vm.MarkDirty();
            vm.MarkClean();
            Assert.False(vm.IsDirty);
        }

        [Fact]
        public void MarkDirty_Idempotent_DoesNotRaiseRepeatedly()
        {
            var vm = CreateKitExplorer();
            var changed = new List<string?>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => changed.Add(e.PropertyName);
            vm.MarkDirty();
            var firstCount = changed.Count(c => c == nameof(DataExplorerViewModel.IsDirty));
            vm.MarkDirty();
            var secondCount = changed.Count(c => c == nameof(DataExplorerViewModel.IsDirty));
            Assert.Equal(1, firstCount);
            Assert.Equal(1, secondCount);
        }

        // === RevertAllCommand ===

        [Fact]
        public void RevertAllCommand_InitiallyDisabled()
        {
            var vm = CreateKitExplorer();
            Assert.False(vm.RevertAllCommand.Enabled);
        }

        [Fact]
        public void RevertAllCommand_EnabledAfterMarkDirty()
        {
            var vm = CreateKitExplorer();
            vm.MarkDirty();
            Assert.True(vm.RevertAllCommand.Enabled);
        }

        [Fact]
        public void RevertAllCommand_DisabledAfterMarkClean()
        {
            var vm = CreateKitExplorer();
            vm.MarkDirty();
            Assert.True(vm.RevertAllCommand.Enabled);
            vm.MarkClean();
            Assert.False(vm.RevertAllCommand.Enabled);
        }

        [Fact]
        public void RevertAllCommand_NotNull()
        {
            var vm = CreateKitExplorer();
            Assert.NotNull(vm.RevertAllCommand);
        }

        // === RevertAll ===

        [Fact]
        public void RevertAll_ClearsIsDirty()
        {
            var vm = CreateKitExplorer();
            // Modify data via paste to make it dirty
            var kitRoots = FindAllKitRoots(CreateModuleExplorer().Root[0]);
            // Use a simpler approach: just mark dirty and revert
            vm.MarkDirty();
            Assert.True(vm.IsDirty);
            vm.RevertAllCommand.Execute(null!);
            Assert.False(vm.IsDirty);
        }

        [Fact]
        public void RevertAll_DisablesRevertAllCommand()
        {
            var vm = CreateKitExplorer();
            vm.MarkDirty();
            Assert.True(vm.RevertAllCommand.Enabled);
            vm.RevertAllCommand.Execute(null!);
            Assert.False(vm.RevertAllCommand.Enabled);
        }

        [Fact]
        public void RevertAll_IsUndoable()
        {
            var vm = CreateKitExplorer();
            vm.MarkDirty();
            Assert.False(vm.CanUndo);
            vm.RevertAllCommand.Execute(null!);
            Assert.True(vm.CanUndo);
        }

        [Fact]
        public void RevertAll_ThenUndo_MarksDirtyAgain()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            // Perform a real data modification via paste
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteNodeCommand.Execute(null!);
            Assert.True(vm.IsDirty);
            // Revert all: data goes back to clean, IsDirty should be false
            vm.RevertAllCommand.Execute(null!);
            Assert.False(vm.IsDirty);
            // Undo the revert: data goes back to modified, IsDirty should be true
            vm.Undo();
            Assert.True(vm.IsDirty);
        }

        [Fact]
        public void RevertAll_ThenUndo_EnablesRevertAllCommand()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteNodeCommand.Execute(null!);
            Assert.True(vm.RevertAllCommand.Enabled);
            vm.RevertAllCommand.Execute(null!);
            Assert.False(vm.RevertAllCommand.Enabled);
            vm.Undo();
            Assert.True(vm.RevertAllCommand.Enabled);
        }

        // === Undo/Redo dirty tracking ===

        [Fact]
        public void Undo_AfterPaste_MarksDirty()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            Assert.False(vm.IsDirty);
            vm.PasteNodeCommand.Execute(null!);
            Assert.True(vm.IsDirty);
        }

        [Fact]
        public void Undo_AfterPaste_RestoresCleanState()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteNodeCommand.Execute(null!);
            Assert.True(vm.IsDirty);
            vm.Undo();
            Assert.False(vm.IsDirty);
        }

        [Fact]
        public void Undo_AfterPaste_DisablesRevertAllCommand()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteNodeCommand.Execute(null!);
            Assert.True(vm.RevertAllCommand.Enabled);
            vm.Undo();
            Assert.False(vm.RevertAllCommand.Enabled);
        }

        [Fact]
        public void Redo_AfterUndo_RestoresDirtyState()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteNodeCommand.Execute(null!);
            vm.Undo();
            Assert.False(vm.IsDirty);
            vm.Redo();
            Assert.True(vm.IsDirty);
        }

        [Fact]
        public void Redo_AfterUndo_EnablesRevertAllCommand()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            vm.PasteNodeCommand.Execute(null!);
            vm.Undo();
            Assert.False(vm.IsDirty);
            vm.Redo();
            Assert.True(vm.RevertAllCommand.Enabled);
        }

        // === ConfirmCloseAsync ===

        [Fact]
        public async Task ConfirmCloseAsync_DelegatesToViewServices()
        {
            var viewServices = new FakeViewServices();
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(viewServices, NullLogger.Instance, new DeviceViewModel(), kit);
            // FakeViewServices.ConfirmCloseAsync returns true
            var result = await vm.ConfirmCloseAsync();
            Assert.True(result);
        }

        // === Field change triggers MarkDirty ===

        [Fact]
        public void FieldChange_ThroughPaste_MarksDirty()
        {
            var vm = CreateModuleExplorer();
            var kitRoots = FindAllKitRoots(vm.Root[0]);
            vm.SelectedNode = kitRoots[0];
            vm.CopyNodeCommand.Execute(null!);
            vm.SelectedNode = kitRoots[1];
            Assert.False(vm.IsDirty);
            vm.PasteNodeCommand.Execute(null!);
            Assert.True(vm.IsDirty);
        }
    }
}
