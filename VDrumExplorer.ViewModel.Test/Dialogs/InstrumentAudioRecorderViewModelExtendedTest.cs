using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
    public class InstrumentAudioRecorderViewModelExtendedTest
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
        private sealed class TrackingMidiOutput : IMidiOutput
        {
            public List<MidiMessage> Sent { get; } = new();
            public void Send(MidiMessage message) => Sent.Add(message);
            public void Dispose() { }
        }
        private static RolandMidiClient CreateRolandMidiClient(IMidiInput input, IMidiOutput output, string name, byte id, ModuleIdentifier identifier)
        {
            var t = typeof(RolandMidiClient);
            var ctor = t.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
                new[] { typeof(IMidiInput), typeof(IMidiOutput), typeof(string), typeof(string), typeof(byte), typeof(ModuleIdentifier) }, null)!;
            return (RolandMidiClient)ctor.Invoke(new object[] { input, output, name, name, id, identifier });
        }
        private static DeviceController CreateDeviceController(RolandMidiClient client, TimeSpan timeout)
        {
            var t = typeof(DeviceController);
            var ctor = t.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
                new[] { typeof(RolandMidiClient), typeof(Microsoft.Extensions.Logging.ILogger), typeof(TimeSpan) }, null)!;
            return (DeviceController)ctor.Invoke(new object[] { client, NullLogger.Instance, timeout });
        }
        private static DeviceViewModel CreateDeviceViewModelWithFakeDevice(string midiName = "Test MIDI", IMidiOutput? output = null, TimeSpan? timeout = null)
        {
            var input = new FakeMidiInput();
            var outp = output ?? new FakeMidiOutput();
            var client = CreateRolandMidiClient(input, outp, midiName, 0x10, ModuleIdentifier.TD27);
            var controller = CreateDeviceController(client, timeout ?? TimeSpan.FromSeconds(1));
            return new DeviceViewModel { ConnectedDevice = controller };
        }

        [Fact]
        public void StartRecordingCommand_InitiallyDisabled_AndEnabledAfterSetup()
        {
            var audioManager = new FakeAudioDeviceManager(
                new List<IAudioInput> { new FakeAudioInput("Input1") },
                new List<IAudioOutput>());
            var vm = new InstrumentAudioRecorderViewModel(new FakeViewServices(), NullLogger.Instance, CreateDeviceViewModelWithFakeDevice(), audioManager);
            Assert.False(vm.StartRecordingCommand.Enabled);
            vm.Settings.OutputFile = Path.Combine(Path.GetTempPath(), $"out_{Guid.NewGuid():N}.vaudio");
            vm.Settings.SelectedInputDevice = audioManager.GetInputs()[0];
            Assert.True(vm.StartRecordingCommand.Enabled);
        }

        [Fact]
        public async Task StartRecording_WithoutInputDevice_ReturnsNull()
        {
            var dvm = CreateDeviceViewModelWithFakeDevice();
            var vm = new InstrumentAudioRecorderViewModel(new FakeViewServices(), NullLogger.Instance, dvm, new FakeAudioDeviceManager());
            vm.Settings.SelectedInputDevice = null;
            var result = await vm.StartRecording(CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task StartRecording_CancelledToken_ThrowsOrReturnsNull()
        {
            // Use a device with short timeout, but cancellation before even GetCurrentKit
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var dvm = CreateDeviceViewModelWithFakeDevice();
            var audioInput = new FakeAudioInput("TestInput");
            var audioManager = new FakeAudioDeviceManager(
                new List<IAudioInput> { audioInput },
                new List<IAudioOutput>());
            var vm = new InstrumentAudioRecorderViewModel(new FakeViewServices(), NullLogger.Instance, dvm, audioManager);
            vm.Settings.OutputFile = Path.Combine(Path.GetTempPath(), $"cancel_{Guid.NewGuid():N}.vaudio");
            vm.Settings.SelectedInputDevice = audioInput;
            // GetCurrentKitAsync will be cancelled; StartRecording does not catch OCE around GetCurrentKit, so it propagates
            try
            {
                var result = await vm.StartRecording(cts.Token);
                Assert.Null(result);
            }
            catch (OperationCanceledException ex)
            {
                // Expected when token already cancelled — verify exception type (TaskCanceledException is subtype)
                Assert.IsAssignableFrom<OperationCanceledException>(ex);
            }
        }

        [Fact]
        public void Cancel_WhenNotRecording_DoesNotThrowAndKeepsCommandsDisabled()
        {
            var vm = new InstrumentAudioRecorderViewModel(new FakeViewServices(), NullLogger.Instance, CreateDeviceViewModelWithFakeDevice(), new FakeAudioDeviceManager());
            vm.Cancel();
            Assert.False(vm.CancelCommand.Enabled);
        }

        [Fact]
        public async Task StartRecordingAsyncVoid_ExecutesAndHandlesException()
        {
            // Trigger the async void StartRecording() via command, with OutputFile set but no real hardware.
            // It should not throw synchronously; internal try/catch will handle timeout and set Progress.
            var audioInput = new FakeAudioInput("In");
            var audioManager = new FakeAudioDeviceManager(
                new List<IAudioInput> { audioInput },
                new List<IAudioOutput>());
            var dvm = CreateDeviceViewModelWithFakeDevice(timeout: TimeSpan.FromMilliseconds(100));
            var vm = new InstrumentAudioRecorderViewModel(new FakeViewServices(), NullLogger.Instance, dvm, audioManager);
            var temp = Path.Combine(Path.GetTempPath(), $"rec_{Guid.NewGuid():N}.vaudio");
            vm.Settings.OutputFile = temp;
            vm.Settings.SelectedInputDevice = audioInput;
            vm.Settings.RecordingTime = 0.1m;
            vm.Settings.RecordToPlayDelay = 0;
            try
            {
                // Execute async void via command
                Assert.True(vm.StartRecordingCommand.Enabled);
                vm.StartRecordingCommand.Execute(null!);
                // Give it time to run: it will hit GetCurrentKit timeout after 100ms, then catch and finish
                await Task.Delay(600);
                // After completion, RecordedAudio should be null (failed), and commands reset
                Assert.Null(vm.RecordedAudio);
                Assert.False(vm.CancelCommand.Enabled);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        [Fact]
        public async Task SelectOutputFile_WithDialogResult_SetsOutputFile()
        {
            var temp = Path.Combine(Path.GetTempPath(), $"sel_{Guid.NewGuid():N}.vaudio");
            var vs = new SelectFileViewServices(temp);
            var schema = TestData.LoadTD27Schema();
            var vm = new InstrumentAudioRecorderSettingsViewModel(vs, new FakeAudioDeviceManager(), schema, "Test MIDI");
            Assert.Null(vm.OutputFile);
            vm.SelectOutputFileCommand.Execute(null!);
            await Task.Delay(100);
            Assert.Equal(temp, vm.OutputFile);
        }

        [Fact]
        public async Task SelectOutputFile_Cancelled_KeepsNull()
        {
            var vs = new SelectFileViewServices(null);
            var schema = TestData.LoadTD27Schema();
            var vm = new InstrumentAudioRecorderSettingsViewModel(vs, new FakeAudioDeviceManager(), schema, "Test MIDI");
            vm.SelectOutputFileCommand.Execute(null!);
            await Task.Delay(100);
            Assert.Null(vm.OutputFile);
        }

        private sealed class SelectFileViewServices : IViewServices
        {
            private readonly string? result;
            public SelectFileViewServices(string? result) => this.result = result;
            public Task<string?> ShowOpenFileDialogAsync(string filter) => Task.FromResult<string?>(null);
            public Task<string?> ShowSaveFileDialogAsync(string filter) => Task.FromResult(result);
            public Task<int?> ChooseCopyKitTargetAsync(CopyKitViewModel viewModel) => Task.FromResult<int?>(null);
            public Task<bool> ChooseCopyKitsTargetAsync(CopyKitsViewModel viewModel) => Task.FromResult(false);
            public Task<bool> ChooseMultiPasteTargetsAsync(MultiPasteViewModel viewModel) => Task.FromResult(false);
            public void ShowSchemaExplorer(ViewModel.LogicalSchema.ModuleSchemaViewModel viewModel) { }
            public void ShowKitExplorer(ViewModel.Data.KitExplorerViewModel viewModel) { }
            public void ShowModuleExplorer(ViewModel.Data.ModuleExplorerViewModel viewModel) { }
            public void ShowInstrumentAudioExplorer(ViewModel.Audio.InstrumentAudioExplorerViewModel viewModel) { }
            public void ShowInstrumentRecorderDialog(InstrumentAudioRecorderViewModel viewModel) { }
            public Task<T?> ShowDataTransferDialog<T>(DataTransferViewModel<T> viewModel) where T : class => Task.FromResult<T?>(null);
            public void AddRequerySuggestion(EventHandler handler) { }
            public void RemoveRequerySuggestion(EventHandler handler) { }
        }
    }
}
