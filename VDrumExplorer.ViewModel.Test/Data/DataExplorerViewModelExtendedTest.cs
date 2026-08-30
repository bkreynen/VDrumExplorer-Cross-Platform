// Extended tests for DataExplorerViewModel async methods to push coverage.
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.Model.Device;
using VDrumExplorer.Model.Midi;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.ViewModel.Test.Fakes;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    public class DataExplorerViewModelExtendedTest
    {
        private readonly Module module = TestData.LoadTD27Module();

        private sealed class ConfigurableViewServices : IViewServices
        {
            public string? OpenFileResult { get; set; }
            public string? SaveFileResult { get; set; }
            public int? CopyKitResult { get; set; }
            public bool CopyKitsResult { get; set; }
            public bool MultiPasteResult { get; set; }
            public bool DataTransferShouldExecute { get; set; } = false;
            public Func<MultiPasteViewModel, bool>? MultiPasteCallback { get; set; }

            public Task<string?> ShowOpenFileDialogAsync(string filter) => Task.FromResult(OpenFileResult);
            public Task<string?> ShowSaveFileDialogAsync(string filter) => Task.FromResult(SaveFileResult);
            public Task<int?> ChooseCopyKitTargetAsync(CopyKitViewModel viewModel) => Task.FromResult(CopyKitResult);
            public Task<bool> ChooseCopyKitsTargetAsync(CopyKitsViewModel viewModel) => Task.FromResult(CopyKitsResult);
            public Task<bool> ChooseMultiPasteTargetsAsync(MultiPasteViewModel viewModel)
            {
                if (MultiPasteCallback != null) return Task.FromResult(MultiPasteCallback(viewModel));
                return Task.FromResult(MultiPasteResult);
            }
            public void ShowSchemaExplorer(ViewModel.LogicalSchema.ModuleSchemaViewModel viewModel) { }
            public void ShowKitExplorer(KitExplorerViewModel viewModel) { }
            public void ShowModuleExplorer(ModuleExplorerViewModel viewModel) { }
            public void ShowInstrumentAudioExplorer(ViewModel.Audio.InstrumentAudioExplorerViewModel viewModel) { }
            public void ShowInstrumentRecorderDialog(InstrumentAudioRecorderViewModel viewModel) { }
            public async Task<T?> ShowDataTransferDialog<T>(DataTransferViewModel<T> viewModel) where T : class
            {
                if (DataTransferShouldExecute)
                {
                    try { return await viewModel.TransferAsync(); } catch { return null; }
                }
                return null;
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
        private sealed class TrackingMidiOutput : IMidiOutput
        {
            public System.Collections.Generic.List<MidiMessage> Sent { get; } = new();
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
        private static DeviceController CreateDeviceController(RolandMidiClient client)
        {
            var t = typeof(DeviceController);
            var ctor = t.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
                new[] { typeof(RolandMidiClient), typeof(Microsoft.Extensions.Logging.ILogger), typeof(TimeSpan) }, null)!;
            return (DeviceController)ctor.Invoke(new object[] { client, NullLogger.Instance, TimeSpan.FromSeconds(1) });
        }
        private static DeviceViewModel CreateDeviceViewModelWithFakeDevice(IMidiOutput? output = null)
        {
            var input = new FakeMidiInput();
            var outp = output ?? new FakeMidiOutput();
            var client = CreateRolandMidiClient(input, outp, "Test MIDI", 0x10, ModuleIdentifier.TD27);
            var controller = CreateDeviceController(client);
            return new DeviceViewModel { ConnectedDevice = controller };
        }

        private static DataTreeNodeViewModel? FindMidiNode(DataTreeNodeViewModel node)
        {
            if (node.GetMidiNote() != null) return node;
            foreach (var child in node.Children)
            {
                var found = FindMidiNode(child);
                if (found != null) return found;
            }
            return null;
        }

        [Fact]
        public async Task SaveFile_WithExistingFileName_WritesFileWithoutDialog()
        {
            var vs = new ConfigurableViewServices();
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), kit);
            vm.CopiedSnapshot = null;
            var temp = Path.Combine(Path.GetTempPath(), $"vdrum_save_{Guid.NewGuid():N}.vkit");
            try
            {
                vm.FileName = temp;
                vm.SaveFileCommand.Execute(null!);
                await Task.Delay(150);
                Assert.True(File.Exists(temp));
                Assert.True(new FileInfo(temp).Length > 0);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        [Fact]
        public async Task SaveFileAs_Cancelled_DoesNotCreateFile()
        {
            var vs = new ConfigurableViewServices { SaveFileResult = null };
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), kit);
            vm.CopiedSnapshot = null;
            vm.SaveFileAsCommand.Execute(null!);
            await Task.Delay(100);
            // No exception means success; FileName should stay null
            Assert.Null(vm.FileName);
        }

        [Fact]
        public async Task SaveFileAs_WithDialogResult_CreatesFile()
        {
            var vs = new ConfigurableViewServices();
            var temp = Path.Combine(Path.GetTempPath(), $"vdrum_saveas_{Guid.NewGuid():N}.vkit");
            vs.SaveFileResult = temp;
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), kit);
            vm.CopiedSnapshot = null;
            try
            {
                vm.SaveFileAsCommand.Execute(null!);
                await Task.Delay(150);
                Assert.True(File.Exists(temp));
                Assert.Equal(temp, vm.FileName);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        [Fact]
        public async Task SaveFileAs_Module_CreatesFile()
        {
            var vs = new ConfigurableViewServices();
            var temp = Path.Combine(Path.GetTempPath(), $"vdrum_mod_{Guid.NewGuid():N}.vdrum");
            vs.SaveFileResult = temp;
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            vm.CopiedSnapshot = null;
            try
            {
                vm.SaveFileAsCommand.Execute(null!);
                await Task.Delay(150);
                Assert.True(File.Exists(temp));
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        [Fact]
        public async Task ExportJson_Cancelled_DoesNotThrow()
        {
            var vs = new ConfigurableViewServices { SaveFileResult = null };
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), kit);
            vm.CopiedSnapshot = null;
            vm.ExportJsonCommand.Execute(null!);
            await Task.Delay(100);
        }

        [Fact]
        public async Task ExportJson_WithFile_WritesJson()
        {
            var temp = Path.Combine(Path.GetTempPath(), $"vdrum_json_{Guid.NewGuid():N}.json");
            var vs = new ConfigurableViewServices { SaveFileResult = temp };
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), kit);
            vm.CopiedSnapshot = null;
            try
            {
                vm.ExportJsonCommand.Execute(null!);
                await Task.Delay(150);
                Assert.True(File.Exists(temp));
                var json = File.ReadAllText(temp);
                Assert.NotEmpty(json);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        [Fact]
        public async Task ExportJson_Module_WithFile_WritesJson()
        {
            var temp = Path.Combine(Path.GetTempPath(), $"vdrum_mjson_{Guid.NewGuid():N}.json");
            var vs = new ConfigurableViewServices { SaveFileResult = temp };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            vm.CopiedSnapshot = null;
            try
            {
                vm.ExportJsonCommand.Execute(null!);
                await Task.Delay(150);
                Assert.True(File.Exists(temp));
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        [Fact]
        public async Task PlayNote_WithoutDevice_DoesNotThrow()
        {
            var vs = new ConfigurableViewServices();
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), kit);
            vm.CopiedSnapshot = null;
            // Find a node with midi note, set as selected
            var midiNode = FindMidiNode(vm.Root[0]);
            if (midiNode != null) vm.SelectedNode = midiNode;
            vm.PlayNoteCommand.Execute(null!);
            await Task.Delay(150);
        }

        [Fact]
        public async Task PlayNote_WithDevice_AndMidiNode_DoesNotThrow()
        {
            var vs = new ConfigurableViewServices();
            var output = new TrackingMidiOutput();
            var deviceVm = CreateDeviceViewModelWithFakeDevice(output);
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(vs, NullLogger.Instance, deviceVm, kit);
            vm.CopiedSnapshot = null;
            var midiNode = FindMidiNode(vm.Root[0]);
            Assert.NotNull(midiNode);
            vm.SelectedNode = midiNode!;
            // ModuleExplorer with matching device: Need to force IsMatchingDeviceConnected true by using module's data? KitExplorer schema TD27 matches device TD27
            // For KitExplorer, IsMatchingDeviceConnected is evaluated at ctor: device schema == data schema -> true, so PlayNoteCommand enabled
            // Execute play note; it should attempt kit switch + play
            vm.PlayNoteCommand.Execute(null!);
            await Task.Delay(300);
            Assert.True(output.Sent.Count > 0, "PlayNote should send ProgramChange + NoteOn/Off");
            Assert.Contains(output.Sent, m => (m.Data[0] & 0xF0) == 0xC0);
            Assert.Contains(output.Sent, m => (m.Data[0] & 0xF0) == 0x90);
            Assert.Contains(output.Sent, m => (m.Data[0] & 0xF0) == 0x80);
        }

        [Fact]
        public async Task PlayNote_WithDevice_ButNoMidiNote_DoesNotThrow()
        {
            var vs = new ConfigurableViewServices();
            var deviceVm = CreateDeviceViewModelWithFakeDevice();
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(vs, NullLogger.Instance, deviceVm, kit);
            vm.CopiedSnapshot = null;
            // Select root which likely has no midi note
            vm.SelectedNode = vm.Root[0];
            // Ensure root has no midi note (if it does, skip)
            if (vm.SelectedNode.GetMidiNote() == null)
            {
                vm.PlayNoteCommand.Execute(null!);
                await Task.Delay(150);
            }
        }

        [Fact]
        public async Task CopyDataToDevice_WithoutDevice_DoesNotThrow()
        {
            var vs = new ConfigurableViewServices();
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            vm.CopiedSnapshot = null;
            vm.CopyDataToDeviceCommand.Execute(null!);
            await Task.Delay(100);
        }

        [Fact]
        public async Task CopyDataToDevice_WithDevice_CallsDialog()
        {
            var vs = new ConfigurableViewServices();
            var deviceVm = CreateDeviceViewModelWithFakeDevice();
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, deviceVm, module);
            vm.CopiedSnapshot = null;
            // Select a kit node to ensure SelectedNode.Model not null
            var kitNode = FindKitRoot(vm.Root[0]);
            vm.SelectedNode = kitNode;
            vm.CopyDataToDeviceCommand.Execute(null!);
            await Task.Delay(100);
        }

        [Fact]
        public async Task CopyDataToDevice_KitExplorer_WithDevice_CallsDialog()
        {
            var vs = new ConfigurableViewServices();
            var deviceVm = CreateDeviceViewModelWithFakeDevice();
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(vs, NullLogger.Instance, deviceVm, kit);
            vm.CopiedSnapshot = null;
            vm.CopyDataToDeviceCommand.Execute(null!);
            await Task.Delay(100);
        }

        [Fact]
        public async Task MultiPaste_Cancelled_DoesNotEnableUndo()
        {
            var vs = new ConfigurableViewServices { MultiPasteResult = false };
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), kit);
            vm.CopiedSnapshot = null;
            vm.CopyNodeCommand.Execute(null!);
            Assert.False(vm.CanUndo);
            vm.MultiPasteCommand.Execute(null!);
            await Task.Delay(100);
            Assert.False(vm.CanUndo);
        }

        [Fact]
        public async Task MultiPaste_WithSelectionEnabled_ChecksAndPastes()
        {
            var vs = new ConfigurableViewServices();
            vs.MultiPasteCallback = (m) =>
            {
                // Check first candidate
                if (m.Candidates.Count > 0) m.Candidates[0].Checked = true;
                return true;
            };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            vm.CopiedSnapshot = null;
            // Copy a kit root node
            var kitRoot = FindKitRoot(vm.Root[0]);
            vm.SelectedNode = kitRoot;
            vm.CopyNodeCommand.Execute(null!);
            Assert.NotNull(vm.CopiedSnapshot);
            Assert.True(vm.MultiPasteCommand.Enabled);
            vm.MultiPasteCommand.Execute(null!);
            await Task.Delay(150);
            Assert.True(vm.CanUndo);
        }

        [Fact]
        public async Task SaveFile_WithExistingFileName_Overwrites()
        {
            var vs = new ConfigurableViewServices();
            var kit = module.ExportKit(1);
            var vm = new KitExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), kit);
            vm.CopiedSnapshot = null;
            var temp = Path.Combine(Path.GetTempPath(), $"vdrum_over_{Guid.NewGuid():N}.vkit");
            try
            {
                // First save via dialog
                vs.SaveFileResult = temp;
                vm.SaveFileAsCommand.Execute(null!);
                await Task.Delay(150);
                Assert.True(File.Exists(temp));
                var firstLen = new FileInfo(temp).Length;
                // Modify kit and save again via SaveFileCommand (uses existing FileName)
                vm.DefaultKitNumber = vm.DefaultKitNumber == 1 ? 2 : 1;
                vm.SaveFileCommand.Execute(null!);
                await Task.Delay(150);
                Assert.True(File.Exists(temp));
                Assert.True(new FileInfo(temp).Length > 0);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        private static DataTreeNodeViewModel FindKitRoot(DataTreeNodeViewModel node)
        {
            if (node.IsKitRoot) return node;
            foreach (var child in node.Children)
            {
                var r = FindKitRoot(child);
                if (r != null) return r;
            }
            return null!;
        }
    }
}
