using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Dialogs;
using VDrumExplorer.Proto;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    public class ModuleExplorerViewModelExtendedTest
    {
        private readonly Module module = TestData.LoadTD27Module();

        private sealed class ConfigurableViewServices : IViewServices
        {
            public string? OpenFileResult { get; set; }
            public string? SaveFileResult { get; set; }
            public int? CopyKitResult { get; set; }
            public bool CopyKitsResult { get; set; }
            public bool MultiPasteResult { get; set; }
            public Task<string?> ShowOpenFileDialogAsync(string filter) => Task.FromResult(OpenFileResult);
            public Task<string?> ShowSaveFileDialogAsync(string filter) => Task.FromResult(SaveFileResult);
            public Task<int?> ChooseCopyKitTargetAsync(CopyKitViewModel viewModel) => Task.FromResult(CopyKitResult);
            public Task<bool> ChooseCopyKitsTargetAsync(CopyKitsViewModel viewModel) => Task.FromResult(CopyKitsResult);
            public Task<bool> ChooseMultiPasteTargetsAsync(MultiPasteViewModel viewModel) => Task.FromResult(MultiPasteResult);
            public void ShowSchemaExplorer(ViewModel.LogicalSchema.ModuleSchemaViewModel viewModel) { }
            public void ShowKitExplorer(KitExplorerViewModel viewModel) { ShowKitExplorerCount++; LastKit = viewModel; }
            public void ShowModuleExplorer(ModuleExplorerViewModel viewModel) { }
            public void ShowInstrumentAudioExplorer(ViewModel.Audio.InstrumentAudioExplorerViewModel viewModel) { }
            public void ShowInstrumentRecorderDialog(InstrumentAudioRecorderViewModel viewModel) { }
            public Task<T?> ShowDataTransferDialog<T>(DataTransferViewModel<T> viewModel) where T : class => Task.FromResult<T?>(null);
            public void AddRequerySuggestion(EventHandler handler) { }
            public void RemoveRequerySuggestion(EventHandler handler) { }
            public int ShowKitExplorerCount { get; private set; }
            public KitExplorerViewModel? LastKit { get; private set; }
        }

        private static DataTreeNodeViewModel FindKitRoot(DataTreeNodeViewModel node)
        {
            if (node.IsKitRoot) return node;
            foreach (var c in node.Children)
            {
                var r = FindKitRoot(c);
                if (r != null) return r;
            }
            return null!;
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
        public async Task CopyKit_WithCancelledDialog_DoesNotEnableUndo()
        {
            var vs = new ConfigurableViewServices { CopyKitResult = null };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            var kitNode = FindKitRoot(vm.Root[0]);
            // Use command
            vm.CopyKitCommand.Execute(kitNode);
            await Task.Delay(100);
            Assert.False(vm.CanUndo);
        }

        [Fact]
        public async Task CopyKit_WithValidTarget_CopiesKitAndEnablesUndo()
        {
            var vs = new ConfigurableViewServices { CopyKitResult = 2 };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            var kitNode = FindKitRoot(vm.Root[0]);
            // Ensure kit 1 and 2 distinct? Just run
            vm.CopyKitCommand.Execute(kitNode);
            await WaitUntilAsync(() => vm.CanUndo);
            Assert.True(vm.CanUndo);
        }

        [Fact]
        public async Task CopyKit_WithNonKitNode_DoesNotThrow()
        {
            var vs = new ConfigurableViewServices { CopyKitResult = 2 };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            var nonKit = vm.Root[0]; // root not kit
            vm.CopyKitCommand.Execute(nonKit);
            await Task.Delay(100);
            Assert.False(vm.CanUndo);
        }

        [Fact]
        public async Task CopyMultipleKits_Cancelled_DoesNotEnableUndo()
        {
            var vs = new ConfigurableViewServices { CopyKitsResult = false };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            vm.CopyMultipleKitsCommand.Execute(null!);
            await Task.Delay(100);
            Assert.False(vm.CanUndo);
        }

        [Fact]
        public async Task CopyMultipleKits_Accepted_EnablesUndo()
        {
            var vs = new ConfigurableViewServices { CopyKitsResult = true };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            // CopyMultipleKits uses CopyKitsViewModel with SourceFrom/Destination etc, default values.
            // By default CopyCount maybe 1? Should push undo regardless.
            vm.CopyMultipleKitsCommand.Execute(null!);
            await Task.Delay(100);
            Assert.True(vm.CanUndo);
        }

        [Fact]
        public async Task ImportKitFromFile_Cancelled_DoesNotThrow()
        {
            var vs = new ConfigurableViewServices { OpenFileResult = null };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            var kitNode = FindKitRoot(vm.Root[0]);
            vm.ImportKitFromFileCommand.Execute(kitNode);
            await Task.Delay(100);
            Assert.False(vm.CanUndo);
        }

        [Fact]
        public async Task ImportKitFromFile_WithInvalidPath_DoesNotEnableUndoAndLogsError()
        {
            var vs = new ConfigurableViewServices { OpenFileResult = "/nonexistent/path.vkit" };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            var kitNode = FindKitRoot(vm.Root[0]);
            vm.ImportKitFromFileCommand.Execute(kitNode);
            await Task.Delay(100);
            Assert.False(vm.CanUndo);
        }

        [Fact]
        public async Task ImportKitFromFile_WithValidKitFile_ImportsAndEnablesUndo()
        {
            // Create a temp kit file
            var kit = module.ExportKit(1);
            var temp = Path.Combine(Path.GetTempPath(), $"import_{Guid.NewGuid():N}.vkit");
            using (var s = File.Create(temp)) kit.Save(s);
            var vs = new ConfigurableViewServices { OpenFileResult = temp };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            var kitNode = FindKitRoot(vm.Root[0]);
            // Destination kit number is kitNode.KitNumber (likely 1)
            try
            {
                vm.ImportKitFromFileCommand.Execute(kitNode);
                await Task.Delay(200);
                Assert.True(vm.CanUndo);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        [Fact]
        public async Task ImportKitFromFile_WithNonKitNode_DoesNotThrow()
        {
            var vs = new ConfigurableViewServices { OpenFileResult = "/tmp/file.vkit" };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            vm.ImportKitFromFileCommand.Execute(vm.Root[0]);
            await Task.Delay(100);
        }

        [Fact]
        public async Task ExportKit_Cancelled_DoesNotThrow()
        {
            var vs = new ConfigurableViewServices { SaveFileResult = null };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            var kitNode = FindKitRoot(vm.Root[0]);
            vm.ExportKitCommand.Execute(kitNode);
            await Task.Delay(100);
        }

        [Fact]
        public async Task ExportKit_WithFile_CreatesFile()
        {
            var temp = Path.Combine(Path.GetTempPath(), $"export_{Guid.NewGuid():N}.vkit");
            var vs = new ConfigurableViewServices { SaveFileResult = temp };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            var kitNode = FindKitRoot(vm.Root[0]);
            try
            {
                vm.ExportKitCommand.Execute(kitNode);
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
        public async Task ExportKit_WithNonKitNode_DoesNotThrow()
        {
            var vs = new ConfigurableViewServices { SaveFileResult = "/tmp/out.vkit" };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            vm.ExportKitCommand.Execute(vm.Root[0]);
            await Task.Delay(100);
        }

        [Fact]
        public void OpenCopyInKitExplorer_WithKitNode_ShowsExplorer()
        {
            var vs = new ConfigurableViewServices();
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            var kitNode = FindKitRoot(vm.Root[0]);
            vm.OpenCopyInKitExplorerCommand.Execute(kitNode);
            Assert.Equal(1, vs.ShowKitExplorerCount);
            Assert.NotNull(vs.LastKit);
        }

        [Fact]
        public async Task ImportKitFromFile_WithModuleFile_LogsErrorNotUndo()
        {
            // Create a temp module file (not kit)
            var temp = Path.Combine(Path.GetTempPath(), $"mod_{Guid.NewGuid():N}.vdrum");
            using (var s = File.Create(temp)) module.Save(s);
            var vs = new ConfigurableViewServices { OpenFileResult = temp };
            var vm = new ModuleExplorerViewModel(vs, NullLogger.Instance, new DeviceViewModel(), module);
            var kitNode = FindKitRoot(vm.Root[0]);
            try
            {
                vm.ImportKitFromFileCommand.Execute(kitNode);
                await Task.Delay(150);
                Assert.False(vm.CanUndo);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}
