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
using Xunit;

namespace VDrumExplorer.ViewModel.Test
{
    public class DelegateCommandTest
    {
        [Fact]
        public void CommandBase_Enabled_SetToSameValue_DoesNotFireCanExecuteChanged()
        {
            var command = new DelegateCommand(() => { }, true);
            var handler = new CanExecuteChangedHandler();
            command.CanExecuteChanged += handler.OnCanExecuteChanged;
            command.Enabled = true; // Same value, no event expected
            Assert.Equal(0, handler.CallCount);
        }

        [Fact]
        public void CommandBase_Enabled_SetToDifferentValue_FiresCanExecuteChanged()
        {
            var command = new DelegateCommand(() => { }, true);
            var handler = new CanExecuteChangedHandler();
            command.CanExecuteChanged += handler.OnCanExecuteChanged;
            command.Enabled = false;
            Assert.Equal(1, handler.CallCount);
            Assert.Same(command, handler.Sender);
        }

        [Fact]
        public void CommandBase_NotImplemented_ThrowsWhenExecuted()
        {
            Assert.Throws<NotImplementedException>(() => CommandBase.NotImplemented.Execute(null));
        }

        [Fact]
        public void CommandBase_NotImplemented_IsNotEnabled()
        {
            Assert.False(CommandBase.NotImplemented.Enabled);
        }

        [Fact]
        public void CommandBase_NotImplemented_CanExecuteReturnsEnabled()
        {
            Assert.False(CommandBase.NotImplemented.CanExecute(null));
        }

        [Fact]
        public void DelegateCommand_ExecutesAction()
        {
            bool executed = false;
            var command = new DelegateCommand(() => executed = true, true);
            command.Execute(null);
            Assert.True(executed);
        }

        [Fact]
        public void DelegateCommand_CanExecute_ReturnsEnabledValue()
        {
            var enabledCommand = new DelegateCommand(() => { }, true);
            var disabledCommand = new DelegateCommand(() => { }, false);
            Assert.True(enabledCommand.CanExecute(null));
            Assert.False(disabledCommand.CanExecute(null));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DelegateCommand_Enabled_PropertyMatchesConstructorArgument(bool enabled)
        {
            var command = new DelegateCommand(() => { }, enabled);
            Assert.Equal(enabled, command.Enabled);
        }

        [Fact]
        public void DelegateCommand_Enabled_CanBeChangedAfterConstruction()
        {
            var command = new DelegateCommand(() => { }, false);
            Assert.False(command.CanExecute(null));
            command.Enabled = true;
            Assert.True(command.CanExecute(null));
        }

        [Fact]
        public void DelegateCommand_T_ExecutesActionWithCastedParameter()
        {
            int received = 0;
            var command = new DelegateCommand<int>(x => received = x, true);
            command.Execute(42);
            Assert.Equal(42, received);
        }

        [Fact]
        public void DelegateCommand_T_CanExecute_ReturnsEnabledValue()
        {
            var command = new DelegateCommand<int>(x => { }, true);
            Assert.True(command.CanExecute(42));
        }

        [Fact]
        public void ConditionallyEnabledDelegateCommand_CanExecute_WithMatchingParameterType_ReturnsTrue()
        {
            var viewServices = new DummyViewServices();
            var command = new ConditionallyEnabledDelegateCommand<int>(viewServices, x => { }, x => x > 0);
            Assert.True(command.CanExecute(5));
        }

        [Fact]
        public void ConditionallyEnabledDelegateCommand_CanExecute_WithMatchingParameterType_ReturnsFalseWhenPredicateIsFalse()
        {
            var viewServices = new DummyViewServices();
            var command = new ConditionallyEnabledDelegateCommand<int>(viewServices, x => { }, x => x > 0);
            Assert.False(command.CanExecute(-1));
        }

        [Fact]
        public void ConditionallyEnabledDelegateCommand_CanExecute_WithNonMatchingParameterType_ReturnsFalse()
        {
            var viewServices = new DummyViewServices();
            var command = new ConditionallyEnabledDelegateCommand<int>(viewServices, x => { }, x => true);
            // Pass a string instead of an int — CanExecute should return false.
            Assert.False(command.CanExecute("not an int"));
        }

        [Fact]
        public void ConditionallyEnabledDelegateCommand_CanExecute_WithNullParameter_ReturnsFalse()
        {
            var viewServices = new DummyViewServices();
            var command = new ConditionallyEnabledDelegateCommand<int>(viewServices, x => { }, x => true);
            Assert.False(command.CanExecute(null));
        }

        [Fact]
        public void ConditionallyEnabledDelegateCommand_Execute_CallsActionWithCorrectParameter()
        {
            var viewServices = new DummyViewServices();
            int received = 0;
            var command = new ConditionallyEnabledDelegateCommand<int>(viewServices, x => received = x, x => true);
            command.Execute(99);
            Assert.Equal(99, received);
        }

        [Fact]
        public void ConditionallyEnabledDelegateCommand_CanExecuteChanged_AddRemove_DelegatesToViewServices()
        {
            var viewServices = new DummyViewServices();
            var command = new ConditionallyEnabledDelegateCommand<int>(viewServices, x => { }, x => true);
            var handler = new CanExecuteChangedHandler();
            command.CanExecuteChanged += handler.OnCanExecuteChanged;
            Assert.Equal(1, viewServices.AddCount);
            command.CanExecuteChanged -= handler.OnCanExecuteChanged;
            Assert.Equal(1, viewServices.RemoveCount);
        }

        /// <summary>
        /// Minimal IViewServices implementation for testing ConditionallyEnabledDelegateCommand.
        /// </summary>
        private sealed class DummyViewServices : IViewServices
        {
            internal int AddCount;
            internal int RemoveCount;

            public void AddRequerySuggestion(EventHandler handler) => AddCount++;
            public void RemoveRequerySuggestion(EventHandler handler) => RemoveCount++;

            public Task<string?> ShowOpenFileDialogAsync(string filter) => throw new NotImplementedException();
            public Task<string?> ShowSaveFileDialogAsync(string filter) => throw new NotImplementedException();
            public Task<int?> ChooseCopyKitTargetAsync(CopyKitViewModel viewModel) => throw new NotImplementedException();
            public Task<bool> ChooseCopyKitsTargetAsync(CopyKitsViewModel viewModel) => throw new NotImplementedException();
            public Task<bool> ChooseMultiPasteTargetsAsync(MultiPasteViewModel viewModel) => throw new NotImplementedException();
            public void ShowSchemaExplorer(ModuleSchemaViewModel viewModel) => throw new NotImplementedException();
            public void ShowKitExplorer(KitExplorerViewModel viewModel) => throw new NotImplementedException();
            public void ShowModuleExplorer(ModuleExplorerViewModel viewModel) => throw new NotImplementedException();
            public void ShowInstrumentAudioExplorer(InstrumentAudioExplorerViewModel viewModel) => throw new NotImplementedException();
            public void ShowInstrumentRecorderDialog(InstrumentAudioRecorderViewModel viewModel) => throw new NotImplementedException();
            public Task<T?> ShowDataTransferDialog<T>(DataTransferViewModel<T> viewModel) where T : class => throw new NotImplementedException();
        }

        private sealed class CanExecuteChangedHandler
        {
            internal int CallCount;
            internal object? Sender;

            public void OnCanExecuteChanged(object? sender, EventArgs e)
            {
                CallCount++;
                Sender = sender;
            }
        }
    }
}
