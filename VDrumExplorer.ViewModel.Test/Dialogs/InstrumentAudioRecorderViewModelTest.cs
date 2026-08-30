// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Audio;
using VDrumExplorer.Model.Device;
using VDrumExplorer.Model.Midi;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Dialogs
{
    public class InstrumentAudioRecorderViewModelTest
    {
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

        private static DeviceViewModel CreateDeviceViewModelWithFakeDevice(ModuleIdentifier? identifier = null, string midiName = "Test MIDI")
        {
            identifier ??= ModuleIdentifier.TD27;
            var input = new FakeMidiInput();
            var output = new FakeMidiOutput();
            var client = CreateRolandMidiClient(input, output, midiName, midiName, 0x10, identifier);
            var controller = CreateDeviceController(client, NullLogger.Instance);
            return new DeviceViewModel { ConnectedDevice = controller };
        }

        private static InstrumentAudioRecorderViewModel CreateViewModel(
            DeviceViewModel? deviceViewModel = null,
            IViewServices? viewServices = null,
            IAudioDeviceManager? audioDeviceManager = null,
            ILogger? logger = null)
        {
            deviceViewModel ??= CreateDeviceViewModelWithFakeDevice();
            viewServices ??= new FakeViewServices();
            audioDeviceManager ??= new FakeAudioDeviceManager();
            logger ??= NullLogger.Instance;
            return new InstrumentAudioRecorderViewModel(viewServices, logger, deviceViewModel, audioDeviceManager);
        }

        [Fact]
        public void Constructor_ThrowsWhenNoDevice()
        {
            var deviceVm = new DeviceViewModel { ConnectedDevice = null };
            Assert.Throws<InvalidOperationException>(() =>
                new InstrumentAudioRecorderViewModel(new FakeViewServices(), NullLogger.Instance, deviceVm, new FakeAudioDeviceManager()));
        }

        [Fact]
        public void Constructor_WithDevice_SetsTitleAndProperties()
        {
            var deviceVm = CreateDeviceViewModelWithFakeDevice();
            var vm = CreateViewModel(deviceViewModel: deviceVm);
            Assert.Contains(deviceVm.ConnectedDevice!.Schema.Identifier.Name, vm.Title);
            Assert.Contains("Instrument Audio Recorder", vm.Title);
            Assert.NotNull(vm.Settings);
            Assert.NotNull(vm.Progress);
            Assert.NotNull(vm.StartRecordingCommand);
            Assert.NotNull(vm.CancelCommand);
        }

        [Fact]
        public void Constructor_Initially_RecordedAudioIsNull()
        {
            var vm = CreateViewModel();
            Assert.Null(vm.RecordedAudio);
        }

        [Fact]
        public void Constructor_Initially_SettingsEnabledTrue_ProgressEnabledFalse()
        {
            var vm = CreateViewModel();
            Assert.True(vm.SettingsEnabled);
            Assert.False(vm.ProgressEnabled);
        }

        [Fact]
        public void Constructor_StartRecordingCommand_InitiallyDisabled()
        {
            var vm = CreateViewModel();
            // No OutputFile set, so disabled. Even if input device is guessed, OutputFile is null.
            Assert.False(vm.StartRecordingCommand.Enabled);
        }

        [Fact]
        public void Constructor_CancelCommand_InitiallyDisabled()
        {
            var vm = CreateViewModel();
            Assert.False(vm.CancelCommand.Enabled);
        }

        [Fact]
        public void Cancel_WhenNotRecording_DoesNotThrow()
        {
            var vm = CreateViewModel();
            var ex = Record.Exception(() => vm.Cancel());
            Assert.Null(ex);
            Assert.False(vm.CancelCommand.Enabled);
        }

        [Fact]
        public void StartRecordingCommand_BecomesEnabledAfterSettingOutputFileAndInputDevice()
        {
            var audioManager = new FakeAudioDeviceManager(
                new List<IAudioInput> { new FakeAudioInput("Input1") },
                new List<IAudioOutput>());
            var vm = CreateViewModel(audioDeviceManager: audioManager);
            // Initially disabled
            Assert.False(vm.StartRecordingCommand.Enabled);
            // Set the required properties
            vm.Settings.OutputFile = "/tmp/out.bin";
            vm.Settings.SelectedInputDevice = audioManager.GetInputs()[0];
            // PropertyChanged from Settings should have updated button status
            Assert.True(vm.StartRecordingCommand.Enabled);
        }

        [Fact]
        public void StartRecordingCommand_DisabledWhenOutputFileCleared()
        {
            var audioManager = new FakeAudioDeviceManager(
                new List<IAudioInput> { new FakeAudioInput("Input1") },
                new List<IAudioOutput>());
            var vm = CreateViewModel(audioDeviceManager: audioManager);
            vm.Settings.OutputFile = "/tmp/out.bin";
            vm.Settings.SelectedInputDevice = audioManager.GetInputs()[0];
            Assert.True(vm.StartRecordingCommand.Enabled);
            vm.Settings.OutputFile = null;
            Assert.False(vm.StartRecordingCommand.Enabled);
        }

        [Fact]
        public void StartRecordingCommand_DisabledWhenInputDeviceCleared()
        {
            var audioManager = new FakeAudioDeviceManager(
                new List<IAudioInput> { new FakeAudioInput("Input1") },
                new List<IAudioOutput>());
            var vm = CreateViewModel(audioDeviceManager: audioManager);
            vm.Settings.OutputFile = "/tmp/out.bin";
            vm.Settings.SelectedInputDevice = audioManager.GetInputs()[0];
            Assert.True(vm.StartRecordingCommand.Enabled);
            vm.Settings.SelectedInputDevice = null;
            Assert.False(vm.StartRecordingCommand.Enabled);
        }

        [Fact]
        public void Settings_PropertyChanged_UpdatesButtonStatus()
        {
            var audioManager = new FakeAudioDeviceManager(
                new List<IAudioInput> { new FakeAudioInput("A"), new FakeAudioInput("B") },
                new List<IAudioOutput>());
            var vm = CreateViewModel(audioDeviceManager: audioManager);
            var handler = new List<string>();
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => handler.Add(e.PropertyName!);

            vm.Settings.OutputFile = "/tmp/out.bin";
            // Should have raised SettingsEnabled/ProgressEnabled via UpdateButtonStatus? Actually UpdateButtonStatus raises those.
            // At minimum StartRecordingCommand.Enabled should reflect new state after setting both required fields
            vm.Settings.SelectedInputDevice = audioManager.GetInputs()[0];
            Assert.True(vm.StartRecordingCommand.Enabled);
        }

        [Fact]
        public void Progress_NotNullAndInitialState()
        {
            var vm = CreateViewModel();
            Assert.NotNull(vm.Progress);
            Assert.Equal("Progress", vm.Progress.CurrentInstrumentRecording);
            Assert.Equal(0, vm.Progress.TotalInstruments);
            Assert.Equal(0, vm.Progress.CompletedInstruments);
        }

        [Fact]
        public void Settings_HasExpectedDefaults()
        {
            var vm = CreateViewModel();
            Assert.NotNull(vm.Settings.InstrumentGroups);
            Assert.NotEmpty(vm.Settings.InstrumentGroups);
            Assert.Equal("(All)", vm.Settings.SelectedInstrumentGroup);
        }

        [Fact]
        public void Title_ContainsSchemaName()
        {
            var identifier = ModuleIdentifier.TD27;
            var deviceVm = CreateDeviceViewModelWithFakeDevice(identifier);
            var vm = CreateViewModel(deviceViewModel: deviceVm);
            Assert.Contains(identifier.Name, vm.Title);
        }

        [Fact]
        public void SettingsEnabled_ReflectsCancelCommandState()
        {
            var vm = CreateViewModel();
            // Initially no cancellation => SettingsEnabled true
            Assert.True(vm.SettingsEnabled);
            Assert.False(vm.ProgressEnabled);
            Assert.False(vm.IsRecording);
            // Use internal accessor instead of reflection
            vm.cancellationTokenSource = new System.Threading.CancellationTokenSource();
            vm.UpdateButtonStatus();
            Assert.True(vm.IsRecording);
            Assert.False(vm.SettingsEnabled);
            Assert.True(vm.ProgressEnabled);
            Assert.True(vm.CancelCommand.Enabled);
            Assert.False(vm.StartRecordingCommand.Enabled);
            // Clean up: reset
            vm.cancellationTokenSource = null;
            vm.UpdateButtonStatus();
            Assert.False(vm.IsRecording);
            Assert.True(vm.SettingsEnabled);
        }

        [Fact]
        public void Constructor_SetsLoggerAndDeviceAndSchema()
        {
            var deviceVm = CreateDeviceViewModelWithFakeDevice(ModuleIdentifier.TD17, "MyMIDI");
            var vm = CreateViewModel(deviceViewModel: deviceVm);
            Assert.DoesNotContain("User samples", vm.Settings.InstrumentGroups);
            // Verify schema is TD-17 via title
            Assert.Contains("TD-17", vm.Title);
        }
    }
}
