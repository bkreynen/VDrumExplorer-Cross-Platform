// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Audio;
using VDrumExplorer.Model.Device;
using VDrumExplorer.Model.Midi;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Home;
using VDrumExplorer.ViewModel.Logging;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Home
{
    public class ExplorerHomeViewModelTest
    {
        private static ExplorerHomeViewModel CreateViewModel(
            IViewServices? viewServices = null,
            LogViewModel? logViewModel = null,
            DeviceViewModel? deviceViewModel = null,
            IAudioDeviceManager? audioDeviceManager = null)
        {
            viewServices ??= new FakeViewServices();
            logViewModel ??= new LogViewModel();
            deviceViewModel ??= new DeviceViewModel();
            audioDeviceManager ??= new FakeAudioDeviceManager();
            return new ExplorerHomeViewModel(viewServices, logViewModel, deviceViewModel, audioDeviceManager);
        }

        private sealed class TrackingViewServices : IViewServices
        {
            public ModuleIdentifier? LastSchemaExplorerIdentifier { get; private set; }
            public ViewModel.LogicalSchema.ModuleSchemaViewModel? LastShownViewModel { get; private set; }
            public int ShowSchemaExplorerCallCount { get; private set; }

            public Task<string?> ShowOpenFileDialogAsync(string filter) => Task.FromResult<string?>(null);
            public Task<string?> ShowSaveFileDialogAsync(string filter) => Task.FromResult<string?>(null);
            public Task<int?> ChooseCopyKitTargetAsync(ViewModel.Dialogs.CopyKitViewModel viewModel) => Task.FromResult<int?>(null);
            public Task<bool> ChooseCopyKitsTargetAsync(ViewModel.Dialogs.CopyKitsViewModel viewModel) => Task.FromResult(false);
            public Task<bool> ChooseMultiPasteTargetsAsync(ViewModel.Dialogs.MultiPasteViewModel viewModel) => Task.FromResult(false);
            public void ShowSchemaExplorer(ViewModel.LogicalSchema.ModuleSchemaViewModel viewModel)
            {
                ShowSchemaExplorerCallCount++;
                LastShownViewModel = viewModel;
                LastSchemaExplorerIdentifier = viewModel.ModelForTest.Identifier;
            }
            public void ShowKitExplorer(ViewModel.Data.KitExplorerViewModel viewModel) { }
            public void ShowModuleExplorer(ViewModel.Data.ModuleExplorerViewModel viewModel) { }
            public void ShowInstrumentAudioExplorer(ViewModel.Audio.InstrumentAudioExplorerViewModel viewModel) { }
            public void ShowInstrumentRecorderDialog(ViewModel.Dialogs.InstrumentAudioRecorderViewModel viewModel) { }
            public Task<T?> ShowDataTransferDialog<T>(ViewModel.Dialogs.DataTransferViewModel<T> viewModel) where T : class => Task.FromResult<T?>(null);
            public void AddRequerySuggestion(EventHandler handler) { }
            public void RemoveRequerySuggestion(EventHandler handler) { }
        }

        private sealed class FakeMidiInput : IMidiInput
        {
            public event EventHandler<MidiMessage>? MessageReceived;
            public void Dispose() { }
        }

        private sealed class FakeMidiOutput : IMidiOutput
        {
            public void Send(MidiMessage message) { }
            public void Dispose() { }
        }

        private static RolandMidiClient CreateRolandMidiClient(IMidiInput input, IMidiOutput output, string inName, string outName, byte id, ModuleIdentifier identifier)
        {
            var type = typeof(RolandMidiClient);
            var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(IMidiInput), typeof(IMidiOutput), typeof(string), typeof(string), typeof(byte), typeof(ModuleIdentifier) }, null);
            if (ctor is null) throw new InvalidOperationException("RolandMidiClient ctor not found");
            return (RolandMidiClient)ctor.Invoke(new object[] { input, output, inName, outName, id, identifier });
        }

        private static DeviceController CreateDeviceController(RolandMidiClient client, ILogger logger)
        {
            var type = typeof(DeviceController);
            var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(RolandMidiClient), typeof(ILogger), typeof(TimeSpan) }, null);
            if (ctor is null) throw new InvalidOperationException("DeviceController ctor not found");
            return (DeviceController)ctor.Invoke(new object[] { client, logger, TimeSpan.FromSeconds(1) });
        }

        internal static DeviceViewModel CreateDeviceViewModelWithFakeDevice(ModuleIdentifier? identifier = null)
        {
            identifier ??= ModuleIdentifier.TD27;
            var input = new FakeMidiInput();
            var output = new FakeMidiOutput();
            var client = CreateRolandMidiClient(input, output, "Test MIDI", "Test MIDI", 0x10, identifier);
            var controller = CreateDeviceController(client, NullLogger.Instance);
            return new DeviceViewModel { ConnectedDevice = controller };
        }

        [Fact]
        public void Constructor_SetsLogAndDeviceViewModels()
        {
            var logVm = new LogViewModel();
            var deviceVm = new DeviceViewModel();
            var vm = CreateViewModel(logViewModel: logVm, deviceViewModel: deviceVm);
            Assert.Same(logVm, vm.LogViewModel);
            Assert.Same(deviceVm, vm.DeviceViewModel);
        }

        [Fact]
        public void Constructor_KnownSchemas_NotEmpty()
        {
            var vm = CreateViewModel();
            Assert.NotEmpty(vm.KnownSchemas);
        }

        [Fact]
        public void Constructor_KnownSchemas_OrderedByName()
        {
            var vm = CreateViewModel();
            var names = vm.KnownSchemas.Select(k => k.DisplayName).ToList();
            var sorted = names.OrderBy(n => n).ToList();
            Assert.Equal(sorted, names);
        }

        [Fact]
        public void Constructor_SelectedSchema_DefaultIsFirst()
        {
            var vm = CreateViewModel();
            Assert.Same(vm.KnownSchemas[0], vm.SelectedSchema);
        }

        [Fact]
        public void SelectedSchema_Set_UpdatesPropertyAndRaisesChanged()
        {
            var vm = CreateViewModel();
            var handler = new List<string>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => handler.Add(e.PropertyName!);
            var second = vm.KnownSchemas[1];
            vm.SelectedSchema = second;
            Assert.Same(second, vm.SelectedSchema);
            Assert.Contains(nameof(ExplorerHomeViewModel.SelectedSchema), handler);
        }

        [Fact]
        public void SelectedSchema_SetSameValue_DoesNotRaise()
        {
            var vm = CreateViewModel();
            var handler = new List<string>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => handler.Add(e.PropertyName!);
            var first = vm.SelectedSchema;
            vm.SelectedSchema = first;
            Assert.Empty(handler);
        }

        [Fact]
        public void Commands_NotNull()
        {
            var vm = CreateViewModel();
            Assert.NotNull(vm.OpenSchemaExplorerCommand);
            Assert.NotNull(vm.LoadKitFromDeviceCommand);
            Assert.NotNull(vm.LoadModuleFromDeviceCommand);
            Assert.NotNull(vm.RecordInstrumentAudioCommand);
            Assert.NotNull(vm.SaveLogCommand);
            Assert.NotNull(vm.LoadFileCommand);
        }

        [Fact]
        public void LoadKitFromDeviceNumber_DefaultIsOne()
        {
            var vm = CreateViewModel();
            Assert.Equal(1, vm.LoadKitFromDeviceNumber);
        }

        [Fact]
        public void LoadKitFromDeviceNumber_SetWithoutDevice_Throws()
        {
            var vm = CreateViewModel();
            // No device connected => ValidateKitNumber with null schema throws
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.LoadKitFromDeviceNumber = 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.LoadKitFromDeviceNumber = 5);
        }

        [Fact]
        public void LoadKitFromDeviceNumber_SetWithDevice_ValidSucceeds()
        {
            var deviceVm = CreateDeviceViewModelWithFakeDevice();
            var vm = CreateViewModel(deviceViewModel: deviceVm);
            // TD-27 has many kits, 1 and 5 should be valid
            vm.LoadKitFromDeviceNumber = 5;
            Assert.Equal(5, vm.LoadKitFromDeviceNumber);
        }

        [Fact]
        public void LoadKitFromDeviceNumber_SetWithDevice_InvalidThrows()
        {
            var deviceVm = CreateDeviceViewModelWithFakeDevice();
            var vm = CreateViewModel(deviceViewModel: deviceVm);
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.LoadKitFromDeviceNumber = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.LoadKitFromDeviceNumber = -1);
            var max = deviceVm.ConnectedDevice!.Schema.Kits;
            Assert.Throws<ArgumentOutOfRangeException>(() => vm.LoadKitFromDeviceNumber = max + 1);
        }

        [Fact]
        public void LoadKitFromDeviceNumber_SetWithDevice_RaisesPropertyChanged()
        {
            var deviceVm = CreateDeviceViewModelWithFakeDevice();
            var vm = CreateViewModel(deviceViewModel: deviceVm);
            var handler = new List<string>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => handler.Add(e.PropertyName!);
            vm.LoadKitFromDeviceNumber = 2;
            Assert.Contains(nameof(ExplorerHomeViewModel.LoadKitFromDeviceNumber), handler);
        }

        [Fact]
        public void OpenSchemaExplorerCommand_Execute_CallsShowSchemaExplorer()
        {
            var tracking = new TrackingViewServices();
            var vm = CreateViewModel(viewServices: tracking);
            vm.OpenSchemaExplorerCommand.Execute(null!);
            Assert.Equal(1, tracking.ShowSchemaExplorerCallCount);
            Assert.NotNull(tracking.LastSchemaExplorerIdentifier);
            Assert.Equal(vm.SelectedSchema.Identifier, tracking.LastSchemaExplorerIdentifier);
        }

        [Fact]
        public void OpenSchemaExplorerCommand_WithDifferentSelectedSchema_UsesSelected()
        {
            var tracking = new TrackingViewServices();
            var vm = CreateViewModel(viewServices: tracking);
            var second = vm.KnownSchemas[1];
            vm.SelectedSchema = second;
            vm.OpenSchemaExplorerCommand.Execute(null!);
            Assert.Equal(second.Identifier, tracking.LastSchemaExplorerIdentifier);
        }

        [Fact]
        public void LoadModuleFromDeviceCommand_WhenNoDevice_DoesNotThrow()
        {
            var vm = CreateViewModel();
            // Should not throw even with no device; it just returns early
            vm.LoadModuleFromDeviceCommand.Execute(null!);
        }

        [Fact]
        public void LoadKitFromDeviceCommand_WhenNoDevice_DoesNotThrow()
        {
            var vm = CreateViewModel();
            vm.LoadKitFromDeviceCommand.Execute(null!);
        }

        [Fact]
        public void SaveLogCommand_NotNullAndCanExecute()
        {
            var vm = CreateViewModel();
            Assert.True(vm.SaveLogCommand.CanExecute(null!));
            // Execute with no file selected (Fake returns null) should not throw
            vm.SaveLogCommand.Execute(null!);
        }

        [Fact]
        public void LoadFileCommand_NotNullAndCanExecute()
        {
            var vm = CreateViewModel();
            Assert.True(vm.LoadFileCommand.CanExecute(null!));
            vm.LoadFileCommand.Execute(null!);
        }

        [Fact]
        public void RecordInstrumentAudioCommand_WhenNoDevice_ThrowsInvalidOperation()
        {
            var vm = CreateViewModel();
            // InstrumentAudioRecorderViewModel ctor throws when no device
            Assert.Throws<InvalidOperationException>(() => vm.RecordInstrumentAudioCommand.Execute(null!));
        }

        [Fact]
        public void RecordInstrumentAudioCommand_WhenDeviceConnected_DoesNotThrowImmediately()
        {
            var deviceVm = CreateDeviceViewModelWithFakeDevice();
            var tracking = new TrackingViewServices();
            var audioManager = new FakeAudioDeviceManager();
            var vm = new ExplorerHomeViewModel(tracking, new LogViewModel(), deviceVm, audioManager);
            // The recorder dialog will be shown; but RecordedAudio is null so no second dialog
            // It should not throw synchronously. The underlying StartRecording is not started until user action.
            // We just verify the command executes without throwing InvalidOperationException.
            var ex = Record.Exception(() => vm.RecordInstrumentAudioCommand.Execute(null!));
            // It may throw due to missing hardware for recording, but construction should succeed now.
            // The command creates view model and shows dialog; if our tracking is no-op it should succeed.
            Assert.Null(ex);
        }
    }
}
