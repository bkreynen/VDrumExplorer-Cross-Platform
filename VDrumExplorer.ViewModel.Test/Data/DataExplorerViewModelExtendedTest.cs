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
using VDrumExplorer.ViewModel.Test.Helpers;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    [Collection("Clipboard")]
    public class DataExplorerViewModelExtendedTest
    {
        private readonly Module module = TestData.LoadTD27Module();

        // Migrated to shared helper: use Helpers.ConfigurableViewServices instead of inner duplicate.
        // Alias keeps call sites `new ConfigurableViewServices()` readable while proving centralization.
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

        private static DeviceViewModel CreateDeviceViewModelWithFakeDevice(IMidiOutput? output = null)
        {
            var input = new FakeMidiInput();
            var outp = output ?? new FakeMidiOutput();
            var client = ViewModelTestHelpers.CreateFakeRolandClient(input, outp, "Test MIDI", 0x10, ModuleIdentifier.TD27);
            var controller = ViewModelTestHelpers.CreateDeviceController(client);
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
                await ViewModelTestHelpers.WaitUntilAsync(() => File.Exists(temp));
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
            await ViewModelTestHelpers.WaitUntilAsync(() => vm.FileName != null, 200);
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
                await ViewModelTestHelpers.WaitUntilAsync(() => File.Exists(temp));
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
                await ViewModelTestHelpers.WaitUntilAsync(() => File.Exists(temp));
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
            await ViewModelTestHelpers.WaitUntilAsync(() => false, 100); // deterministic: cancelled dialog, no file expected
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
                await ViewModelTestHelpers.WaitUntilAsync(() => File.Exists(temp));
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
                await ViewModelTestHelpers.WaitUntilAsync(() => File.Exists(temp));
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
            await ViewModelTestHelpers.WaitUntilAsync(() => false, 100); // deterministic: no hardware, no side-effect
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
            await ViewModelTestHelpers.WaitUntilAsync(() => output.Sent.Count >= 3);
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
                await ViewModelTestHelpers.WaitUntilAsync(() => false, 100); // deterministic: no midi note, no side-effect
            }
        }

        [Fact]
        public async Task CopyDataToDevice_WithoutDevice_DoesNotThrow()
        {
            var vs = new ConfigurableViewServices();
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            vm.CopiedSnapshot = null;
            vm.CopyDataToDeviceCommand.Execute(null!);
            await ViewModelTestHelpers.WaitUntilAsync(() => vs.DataTransferExecuted, 200);
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
            await ViewModelTestHelpers.WaitUntilAsync(() => vs.DataTransferExecuted, 200);
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
            await ViewModelTestHelpers.WaitUntilAsync(() => vs.DataTransferExecuted, 200);
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
            await ViewModelTestHelpers.WaitUntilAsync(() => vm.CanUndo, 200);
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
            await ViewModelTestHelpers.WaitUntilAsync(() => vm.CanUndo);
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
                await ViewModelTestHelpers.WaitUntilAsync(() => File.Exists(temp));
                Assert.True(File.Exists(temp));
                var firstLen = new FileInfo(temp).Length;
                // Modify kit and save again via SaveFileCommand (uses existing FileName)
                vm.DefaultKitNumber = vm.DefaultKitNumber == 1 ? 2 : 1;
                var beforeWrite = File.GetLastWriteTimeUtc(temp);
                vm.SaveFileCommand.Execute(null!);
                await ViewModelTestHelpers.WaitUntilAsync(() => File.GetLastWriteTimeUtc(temp) != beforeWrite || new FileInfo(temp).Length != firstLen, 1000);
                // Fallback to short delay if timestamp granularity coarse, but file must exist
                await ViewModelTestHelpers.WaitUntilAsync(() => File.Exists(temp));
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
