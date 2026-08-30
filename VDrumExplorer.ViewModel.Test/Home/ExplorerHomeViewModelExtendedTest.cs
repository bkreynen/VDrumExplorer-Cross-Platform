using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Audio;
using VDrumExplorer.Model.Device;
using VDrumExplorer.Model.Midi;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.Home;
using VDrumExplorer.ViewModel.Logging;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;
using static VDrumExplorer.Proto.ModelExtensions;

namespace VDrumExplorer.ViewModel.Test.Home
{
    public class ExplorerHomeViewModelExtendedTest
    {
        private sealed class TrackingViewServices : IViewServices
        {
            public string? OpenFileResult { get; set; }
            public string? SaveFileResult { get; set; }
            public Module? ModuleToReturn { get; set; }
            public Kit? KitToReturn { get; set; }
            public int ShowKitExplorerCount { get; private set; }
            public int ShowModuleExplorerCount { get; private set; }
            public int ShowAudioExplorerCount { get; private set; }
            public bool DataTransferExecuted { get; private set; }
            public Task<string?> ShowOpenFileDialogAsync(string filter) => Task.FromResult(OpenFileResult);
            public Task<string?> ShowSaveFileDialogAsync(string filter) => Task.FromResult(SaveFileResult);
            public Task<int?> ChooseCopyKitTargetAsync(CopyKitViewModel viewModel) => Task.FromResult<int?>(null);
            public Task<bool> ChooseCopyKitsTargetAsync(CopyKitsViewModel viewModel) => Task.FromResult(false);
            public Task<bool> ChooseMultiPasteTargetsAsync(MultiPasteViewModel viewModel) => Task.FromResult(false);
            public void ShowSchemaExplorer(ViewModel.LogicalSchema.ModuleSchemaViewModel viewModel) { }
            public void ShowKitExplorer(ViewModel.Data.KitExplorerViewModel viewModel) { ShowKitExplorerCount++; }
            public void ShowModuleExplorer(ViewModel.Data.ModuleExplorerViewModel viewModel) { ShowModuleExplorerCount++; }
            public void ShowInstrumentAudioExplorer(ViewModel.Audio.InstrumentAudioExplorerViewModel viewModel) { ShowAudioExplorerCount++; }
            public void ShowInstrumentRecorderDialog(InstrumentAudioRecorderViewModel viewModel) { }
            public async Task<T?> ShowDataTransferDialog<T>(DataTransferViewModel<T> viewModel) where T : class
            {
                DataTransferExecuted = true;
                if (typeof(T) == typeof(Module) && ModuleToReturn is T m) return m;
                if (typeof(T) == typeof(Kit) && KitToReturn is T k) return k;
                // Execute transfer if we can? For coverage of LoadModuleFromDevice path where transfer returns module
                try { return await viewModel.TransferAsync(); } catch { return null; }
            }
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
        private static RolandMidiClient CreateRolandMidiClient(IMidiInput input, IMidiOutput output, string name, byte id, ModuleIdentifier identifier)
        {
            var t = typeof(RolandMidiClient);
            var ctor = t.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
                new[] { typeof(IMidiInput), typeof(IMidiOutput), typeof(string), typeof(string), typeof(byte), typeof(ModuleIdentifier) }, null)!;
            return (RolandMidiClient)ctor.Invoke(new object[] { input, output, name, name, id, identifier });
        }
        private static DeviceController CreateDeviceController(RolandMidiClient client)
        {
            var t = typeof(DeviceController);
            var ctor = t.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
                new[] { typeof(RolandMidiClient), typeof(ILogger), typeof(TimeSpan) }, null)!;
            return (DeviceController)ctor.Invoke(new object[] { client, NullLogger.Instance, TimeSpan.FromSeconds(1) });
        }
        private static DeviceViewModel CreateDeviceViewModelWithFakeDevice()
        {
            var input = new FakeMidiInput();
            var output = new FakeMidiOutput();
            var client = CreateRolandMidiClient(input, output, "Test MIDI", 0x10, ModuleIdentifier.TD27);
            var controller = CreateDeviceController(client);
            return new DeviceViewModel { ConnectedDevice = controller };
        }

        private static ExplorerHomeViewModel CreateVm(IViewServices vs, DeviceViewModel? dvm = null, IAudioDeviceManager? adm = null, LogViewModel? lvm = null)
        {
            dvm ??= new DeviceViewModel();
            adm ??= new FakeAudioDeviceManager();
            lvm ??= new LogViewModel();
            return new ExplorerHomeViewModel(vs, lvm, dvm, adm);
        }

        private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 1000)
        {
            for (int i = 0; i < timeoutMs / 20; i++)
            {
                if (condition()) return;
                await Task.Delay(20);
            }
        }

        [Fact]
        public async Task LoadFile_Cancelled_DoesNotShowExplorer()
        {
            var vs = new TrackingViewServices { OpenFileResult = null };
            var vm = CreateVm(vs);
            vm.LoadFileCommand.Execute(null!);
            await Task.Delay(100);
            Assert.Equal(0, vs.ShowKitExplorerCount);
            Assert.Equal(0, vs.ShowModuleExplorerCount);
        }

        [Fact]
        public async Task LoadFile_WithInvalidPath_DoesNotThrow()
        {
            var vs = new TrackingViewServices { OpenFileResult = "/no/such/file.vdrum" };
            var lvm = new LogViewModel();
            var vm = CreateVm(vs, lvm: lvm);
            vm.LoadFileCommand.Execute(null!);
            await WaitUntilAsync(() => lvm.LogEntries.Count > 0);
            Assert.Contains(lvm.LogEntries, e => e.Level == LogLevel.Error);
            Assert.Equal(0, vs.ShowKitExplorerCount + vs.ShowModuleExplorerCount + vs.ShowAudioExplorerCount);
        }

        [Fact]
        public async Task LoadFile_WithKitFile_ShowsKitExplorer()
        {
            var module = TestData.LoadTD27Module();
            var kit = module.ExportKit(1);
            var temp = Path.Combine(Path.GetTempPath(), $"loadkit_{Guid.NewGuid():N}.vkit");
            using (var s = File.Create(temp)) kit.Save(s);
            var vs = new TrackingViewServices { OpenFileResult = temp };
            var vm = CreateVm(vs);
            try
            {
                vm.LoadFileCommand.Execute(null!);
                await WaitUntilAsync(() => vs.ShowKitExplorerCount > 0);
                Assert.Equal(1, vs.ShowKitExplorerCount);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        [Fact]
        public async Task LoadFile_WithModuleFile_ShowsModuleExplorer()
        {
            var module = TestData.LoadTD27Module();
            var temp = Path.Combine(Path.GetTempPath(), $"loadmod_{Guid.NewGuid():N}.vdrum");
            using (var s = File.Create(temp)) module.Save(s);
            var vs = new TrackingViewServices { OpenFileResult = temp };
            var vm = CreateVm(vs);
            try
            {
                vm.LoadFileCommand.Execute(null!);
                await WaitUntilAsync(() => vs.ShowModuleExplorerCount > 0);
                Assert.Equal(1, vs.ShowModuleExplorerCount);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        [Fact]
        public async Task LoadFile_WithAudioFile_ShowsAudioExplorer()
        {
            // Create a minimal ModuleAudio file via proto
            var schema = TestData.LoadTD27Schema();
            var audio = new ModuleAudio(schema, new AudioFormat(44100, 1, 16), TimeSpan.FromSeconds(1), new List<InstrumentAudio>());
            var temp = Path.Combine(Path.GetTempPath(), $"loadaud_{Guid.NewGuid():N}.vaudio");
            using (var s = File.Create(temp)) audio.Save(s);
            var vs = new TrackingViewServices { OpenFileResult = temp };
            var fakeAudio = new FakeAudioDeviceManager();
            var vm = CreateVm(vs, adm: fakeAudio);
            try
            {
                vm.LoadFileCommand.Execute(null!);
                await Task.Delay(200);
                Assert.Equal(1, vs.ShowAudioExplorerCount);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        [Fact]
        public async Task LoadModuleFromDevice_NoDevice_DoesNotExecuteTransfer()
        {
            var vs = new TrackingViewServices();
            var vm = CreateVm(vs);
            vm.LoadModuleFromDeviceCommand.Execute(null!);
            await Task.Delay(100);
            Assert.False(vs.DataTransferExecuted);
        }

        [Fact]
        public async Task LoadModuleFromDevice_WithDevice_TransfersAndShowsExplorer()
        {
            var module = TestData.LoadTD27Module();
            var vs = new TrackingViewServices { ModuleToReturn = module };
            var dvm = CreateDeviceViewModelWithFakeDevice();
            var vm = CreateVm(vs, dvm);
            vm.LoadModuleFromDeviceCommand.Execute(null!);
            await Task.Delay(200);
            Assert.True(vs.DataTransferExecuted);
            Assert.Equal(1, vs.ShowModuleExplorerCount);
        }

        [Fact]
        public async Task LoadKitFromDevice_NoDevice_DoesNotExecuteTransfer()
        {
            var vs = new TrackingViewServices();
            var vm = CreateVm(vs);
            vm.LoadKitFromDeviceCommand.Execute(null!);
            await Task.Delay(100);
            Assert.False(vs.DataTransferExecuted);
        }

        [Fact]
        public async Task LoadKitFromDevice_WithDevice_ShowsExplorer()
        {
            var module = TestData.LoadTD27Module();
            var kit = module.ExportKit(1);
            var vs = new TrackingViewServices { KitToReturn = kit };
            var dvm = CreateDeviceViewModelWithFakeDevice();
            // Set kit number valid
            var vm = CreateVm(vs, dvm);
            vm.LoadKitFromDeviceNumber = 1; // now valid with device
            vm.LoadKitFromDeviceCommand.Execute(null!);
            await Task.Delay(200);
            Assert.True(vs.DataTransferExecuted);
            Assert.Equal(1, vs.ShowKitExplorerCount);
        }

        [Fact]
        public async Task SaveLog_Cancelled_DoesNotThrow()
        {
            var vs = new TrackingViewServices { SaveFileResult = null };
            var lvm = new LogViewModel();
            var vm = CreateVm(vs, lvm: lvm);
            vm.SaveLogCommand.Execute(null!);
            await Task.Delay(100);
        }

        [Fact]
        public async Task SaveLog_WithFile_WritesFile()
        {
            var temp = Path.Combine(Path.GetTempPath(), $"log_{Guid.NewGuid():N}.json");
            var vs = new TrackingViewServices { SaveFileResult = temp };
            var lvm = new LogViewModel();
            lvm.Logger.LogInformation("test entry");
            var vm = CreateVm(vs, lvm: lvm);
            try
            {
                vm.SaveLogCommand.Execute(null!);
                await Task.Delay(150);
                Assert.True(File.Exists(temp));
                var text = File.ReadAllText(temp);
                Assert.Contains("test entry", text);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        [Fact]
        public async Task LoadModuleFromDevice_WithDeviceButNullResult_ShowsNothing()
        {
            var vs = new TrackingViewServices { ModuleToReturn = null };
            // Override to return null without TransferAsync success
            var dvm = CreateDeviceViewModelWithFakeDevice();
            // Make vs that returns null via base Transfer that will fail because SaveDescendants needs hardware timeouts
            // So we keep our tracking that tries TransferAsync but will likely throw/timeout and return null
            // Instead use a custom vs that returns null directly
            var customVs = new TrackingViewServices();
            // We need to set ModuleToReturn null and also make DataTransferExecuted still true but ShowModuleExplorerCount 0
            // By default Tracking returns null via TransferAsync attempt which may timeout; simpler: create vs that just returns null
            var quickVs = new QuickNullViewServices();
            var vm = CreateVm(quickVs, dvm);
            vm.LoadModuleFromDeviceCommand.Execute(null!);
            await Task.Delay(200);
            Assert.Equal(0, quickVs.ShowModuleExplorerCount);
        }

        private sealed class QuickNullViewServices : IViewServices
        {
            public int ShowModuleExplorerCount { get; private set; }
            public Task<string?> ShowOpenFileDialogAsync(string filter) => Task.FromResult<string?>(null);
            public Task<string?> ShowSaveFileDialogAsync(string filter) => Task.FromResult<string?>(null);
            public Task<int?> ChooseCopyKitTargetAsync(CopyKitViewModel viewModel) => Task.FromResult<int?>(null);
            public Task<bool> ChooseCopyKitsTargetAsync(CopyKitsViewModel viewModel) => Task.FromResult(false);
            public Task<bool> ChooseMultiPasteTargetsAsync(MultiPasteViewModel viewModel) => Task.FromResult(false);
            public void ShowSchemaExplorer(ViewModel.LogicalSchema.ModuleSchemaViewModel viewModel) { }
            public void ShowKitExplorer(ViewModel.Data.KitExplorerViewModel viewModel) { }
            public void ShowModuleExplorer(ViewModel.Data.ModuleExplorerViewModel viewModel) { ShowModuleExplorerCount++; }
            public void ShowInstrumentAudioExplorer(ViewModel.Audio.InstrumentAudioExplorerViewModel viewModel) { }
            public void ShowInstrumentRecorderDialog(InstrumentAudioRecorderViewModel viewModel) { }
            public Task<T?> ShowDataTransferDialog<T>(DataTransferViewModel<T> viewModel) where T : class => Task.FromResult<T?>(null);
            public void AddRequerySuggestion(EventHandler handler) { }
            public void RemoveRequerySuggestion(EventHandler handler) { }
        }
    }
}
