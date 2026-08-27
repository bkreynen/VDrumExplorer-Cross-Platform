// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Json;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;
using static VDrumExplorer.Proto.ModelExtensions;

namespace VDrumExplorer.ViewModel.Test.Data
{
    public class KitExplorerViewModelTest
    {
        private readonly Module module = TestData.LoadTD27Module();
        private readonly Kit kit;
        private readonly KitExplorerViewModel viewModel;

        public KitExplorerViewModelTest()
        {
            kit = module.ExportKit(1);
            viewModel = new KitExplorerViewModel(new FakeViewServices(), NullLogger.Instance, new DeviceViewModel(), kit);
        }

        [Fact]
        public void Kit_IsSet()
        {
            Assert.Same(kit, viewModel.Kit);
        }

        [Fact]
        public void DefaultKitNumber_MatchesKitDefault()
        {
            Assert.Equal(kit.DefaultKitNumber, viewModel.DefaultKitNumber);
        }

        [Fact]
        public void DefaultKitNumber_SetValidValue_UpdatesProperty()
        {
            viewModel.DefaultKitNumber = 5;
            Assert.Equal(5, viewModel.DefaultKitNumber);
            Assert.Equal(5, kit.DefaultKitNumber);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void DefaultKitNumber_InvalidValue_ThrowsArgumentOutOfRangeException(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => viewModel.DefaultKitNumber = value);
        }

        [Fact]
        public void DefaultKitNumber_AboveMax_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => viewModel.DefaultKitNumber = module.Schema.Kits + 1);
        }

        [Fact]
        public void KitCopyTargetNumber_DefaultMatchesKitDefault()
        {
            Assert.Equal(kit.DefaultKitNumber, viewModel.KitCopyTargetNumber);
        }

        [Fact]
        public void KitCopyTargetNumber_SetValidValue_UpdatesProperty()
        {
            viewModel.KitCopyTargetNumber = 3;
            Assert.Equal(3, viewModel.KitCopyTargetNumber);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void KitCopyTargetNumber_InvalidValue_ThrowsArgumentOutOfRangeException(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => viewModel.KitCopyTargetNumber = value);
        }

        [Fact]
        public void KitCopyTargetNumber_AboveMax_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => viewModel.KitCopyTargetNumber = module.Schema.Kits + 1);
        }

        [Fact]
        public void IsKitExplorer_ReturnsTrue()
        {
            Assert.True(viewModel.IsKitExplorer);
        }

        [Fact]
        public void IsModuleExplorer_ReturnsFalse()
        {
            Assert.False(viewModel.IsModuleExplorer);
        }

        [Fact]
        public void SaveFileFilter_IsKitFiles()
        {
            Assert.Equal("Kit files|*.vkit", viewModel.SaveFileFilter);
        }

        [Fact]
        public void Title_WithoutFileName_ContainsExplorerName()
        {
            Assert.Contains("Kit Explorer", viewModel.Title);
        }

        [Fact]
        public void Title_WithFileName_ContainsFileName()
        {
            viewModel.FileName = "test.vkit";
            Assert.Contains("test.vkit", viewModel.Title);
        }

        [Fact]
        public void FileName_SetValue_UpdatesTitle()
        {
            var originalTitle = viewModel.Title;
            viewModel.FileName = "myfile.vkit";
            Assert.NotEqual(originalTitle, viewModel.Title);
            Assert.Contains("myfile.vkit", viewModel.Title);
        }

        [Fact]
        public void Root_NotNull()
        {
            Assert.NotNull(viewModel.Root);
            Assert.Single(viewModel.Root);
        }

        [Fact]
        public void SelectedNode_DefaultIsRootNode()
        {
            Assert.Same(viewModel.Root[0], viewModel.SelectedNode);
        }

        [Fact]
        public void SelectedNodeDetails_NotNull()
        {
            Assert.NotNull(viewModel.SelectedNodeDetails);
        }

        [Fact]
        public void EditCommand_NotNull()
        {
            Assert.NotNull(viewModel.EditCommand);
        }

        [Fact]
        public void CommitCommand_NotNull()
        {
            Assert.NotNull(viewModel.CommitCommand);
        }

        [Fact]
        public void CancelEditCommand_NotNull()
        {
            Assert.NotNull(viewModel.CancelEditCommand);
        }

        [Fact]
        public void PlayNoteCommand_NotNull()
        {
            Assert.NotNull(viewModel.PlayNoteCommand);
        }

        [Fact]
        public void CopyNodeCommand_NotNull()
        {
            Assert.NotNull(viewModel.CopyNodeCommand);
        }

        [Fact]
        public void PasteNodeCommand_NotNull()
        {
            Assert.NotNull(viewModel.PasteNodeCommand);
        }

        [Fact]
        public void MultiPasteCommand_NotNull()
        {
            Assert.NotNull(viewModel.MultiPasteCommand);
        }

        [Fact]
        public void SaveFileCommand_NotNull()
        {
            Assert.NotNull(viewModel.SaveFileCommand);
        }

        [Fact]
        public void SaveFileAsCommand_NotNull()
        {
            Assert.NotNull(viewModel.SaveFileAsCommand);
        }

        [Fact]
        public void ExportJsonCommand_NotNull()
        {
            Assert.NotNull(viewModel.ExportJsonCommand);
        }

        [Fact]
        public void CopyDataToDeviceCommand_NotNull()
        {
            Assert.NotNull(viewModel.CopyDataToDeviceCommand);
        }

        [Fact]
        public void CopyToTemporaryStudioSetCommand_NotNull()
        {
            Assert.NotNull(viewModel.CopyToTemporaryStudioSetCommand);
        }

        [Fact]
        public void CopyMultipleKitsCommand_IsNotImplemented()
        {
            Assert.Same(CommandBase.NotImplemented, viewModel.CopyMultipleKitsCommand);
        }

        [Fact]
        public void OpenCopyInKitExplorerCommand_IsNotImplemented()
        {
            Assert.Same(CommandBase.NotImplemented, viewModel.OpenCopyInKitExplorerCommand);
        }

        [Fact]
        public void CopyKitCommand_IsNotImplemented()
        {
            Assert.Same(CommandBase.NotImplemented, viewModel.CopyKitCommand);
        }

        [Fact]
        public void ImportKitFromFileCommand_IsNotImplemented()
        {
            Assert.Same(CommandBase.NotImplemented, viewModel.ImportKitFromFileCommand);
        }

        [Fact]
        public void ExportKitCommand_IsNotImplemented()
        {
            Assert.Same(CommandBase.NotImplemented, viewModel.ExportKitCommand);
        }

        [Fact]
        public void ConvertCommand_NotNull()
        {
            Assert.NotNull(viewModel.ConvertCommand);
        }

        [Fact]
        public void ReadOnly_InitiallyTrue()
        {
            Assert.True(viewModel.ReadOnly);
        }

        [Fact]
        public void EditCommand_InitiallyEnabled()
        {
            Assert.True(viewModel.EditCommand.Enabled);
        }

        [Fact]
        public void CommitCommand_InitiallyDisabled()
        {
            Assert.False(viewModel.CommitCommand.Enabled);
        }

        [Fact]
        public void CancelEditCommand_InitiallyDisabled()
        {
            Assert.False(viewModel.CancelEditCommand.Enabled);
        }

        [Fact]
        public void EnterEditMode_EnablesCommitAndCancel()
        {
            viewModel.EditCommand.Execute(null!);
            Assert.False(viewModel.ReadOnly);
            Assert.False(viewModel.EditCommand.Enabled);
            Assert.True(viewModel.CommitCommand.Enabled);
            Assert.True(viewModel.CancelEditCommand.Enabled);
        }

        [Fact]
        public void CommitEdit_ReenablesEditCommand()
        {
            viewModel.EditCommand.Execute(null!);
            viewModel.CommitCommand.Execute(null!);
            Assert.True(viewModel.ReadOnly);
            Assert.True(viewModel.EditCommand.Enabled);
            Assert.False(viewModel.CommitCommand.Enabled);
            Assert.False(viewModel.CancelEditCommand.Enabled);
        }

        [Fact]
        public void CancelEdit_ReenablesEditCommand()
        {
            viewModel.EditCommand.Execute(null!);
            viewModel.CancelEditCommand.Execute(null!);
            Assert.True(viewModel.ReadOnly);
            Assert.True(viewModel.EditCommand.Enabled);
            Assert.False(viewModel.CommitCommand.Enabled);
            Assert.False(viewModel.CancelEditCommand.Enabled);
        }

        [Fact]
        public void MidiChannels_Contains1Through16()
        {
            Assert.Equal(16, viewModel.MidiChannels.Count);
            for (int i = 0; i < 16; i++)
            {
                Assert.Equal(i + 1, viewModel.MidiChannels[i]);
            }
        }

        [Fact]
        public void SelectedMidiChannel_DefaultIs10()
        {
            Assert.Equal(10, viewModel.SelectedMidiChannel);
        }

        [Fact]
        public void Attack_DefaultIs80()
        {
            Assert.Equal(80, viewModel.Attack);
        }

        [Fact]
        public void MinAttack_Is1()
        {
            Assert.Equal(1, viewModel.MinAttack);
        }

        [Fact]
        public void MaxAttack_Is127()
        {
            Assert.Equal(127, viewModel.MaxAttack);
        }

        [Fact]
        public void CopyDataTitle_ForKitExplorer_ReturnsCopyKit()
        {
            Assert.Equal("Copy Kit", viewModel.CopyDataTitle);
        }

        [Fact]
        public void SaveToStream_WritesValidData()
        {
            using var stream = new MemoryStream();
            // SaveToStream is protected, but we can test via SaveFileCommand
            // which calls it internally. However, SaveFileCommand uses ViewServices
            // to get a file name. Instead, test via Kit.Save directly.
            kit.Save(stream);
            Assert.True(stream.Length > 0);
        }

        [Fact]
        public void FormatAsJson_ReturnsNonEmptyString()
        {
            // FormatAsJson is protected, but Kit.ToJson is the underlying call
            var json = kit.ToJson();
            Assert.NotEmpty(json);
        }

        [Fact]
        public void HasConvertibleModuleIdentifiers_MayBeEmpty()
        {
            // TD27 may or may not have convertible identifiers depending on schema configuration
            // Just verify the property doesn't throw
            _ = viewModel.HasConvertibleModuleIdentifiers;
        }

        [Fact]
        public void ConvertibleModuleIdentifiers_NotNull()
        {
            Assert.NotNull(viewModel.ConvertibleModuleIdentifiers);
        }

        [Fact]
        public void CanUndo_InitiallyFalse()
        {
            Assert.False(viewModel.CanUndo);
        }

        [Fact]
        public void CanRedo_InitiallyFalse()
        {
            Assert.False(viewModel.CanRedo);
        }

        [Fact]
        public void Undo_WithEmptyStack_DoesNothing()
        {
            viewModel.Undo();
            Assert.False(viewModel.CanUndo);
            Assert.False(viewModel.CanRedo);
        }

        [Fact]
        public void Redo_WithEmptyStack_DoesNothing()
        {
            viewModel.Redo();
            Assert.False(viewModel.CanUndo);
            Assert.False(viewModel.CanRedo);
        }
    }
}
