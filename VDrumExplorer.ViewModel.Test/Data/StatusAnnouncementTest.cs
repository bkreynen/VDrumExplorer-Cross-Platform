// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VDrumExplorer.Model;
using VDrumExplorer.ViewModel.Data;
using VDrumExplorer.ViewModel.Home;
using VDrumExplorer.ViewModel.Logging;
using VDrumExplorer.ViewModel.Status;
using VDrumExplorer.ViewModel.Test.Fakes;
using VDrumExplorer.ViewModel.Test.Helpers;
using Xunit;

namespace VDrumExplorer.ViewModel.Test.Data
{
    /// <summary>
    /// Tests that operation outcomes are announced on the per-window
    /// <see cref="StatusViewModel"/> live regions (docs/accessibility.md §5):
    /// successful operations populate <see cref="StatusViewModel.Message"/>
    /// (polite) and failures populate <see cref="StatusViewModel.ErrorMessage"/>
    /// (assertive).
    /// </summary>
    [Collection("Clipboard")]
    public class StatusAnnouncementTest
    {
        /// <summary>
        /// Creates a KitExplorerViewModel for a TD-27 kit with a connected fake TD-27
        /// device and a view services whose transfer dialog completes "successfully"
        /// (returning the boxed transfer result) without executing device I/O.
        /// </summary>
        private static KitExplorerViewModel CreateKitExplorer(FakeViewServices fake, string transferResult)
        {
            var module = TestData.LoadTD27Module();
            var kit = module.ExportKit(1);
            fake.DataTransferFunc = _ => Task.FromResult<object?>(transferResult);
            var deviceViewModel = ViewModelTestHelpers.CreateDeviceViewModel();
            return new KitExplorerViewModel(fake, NullLogger.Instance, deviceViewModel, kit);
        }

        [Fact]
        public async Task CopyKitToDevice_Success_AnnouncesTargetSlot()
        {
            var fake = new FakeViewServices();
            var viewModel = CreateKitExplorer(fake, transferResult: "");
            viewModel.KitCopyTargetNumber = 3;
            viewModel.CopyDataToDeviceCommand.Execute(null!);
            await ViewModelTestHelpers.WaitUntilAsync(() => viewModel.Status.Message.Length > 0);
            Assert.Equal("Copied kit to slot 3", viewModel.Status.Message);
            Assert.Equal("", viewModel.Status.ErrorMessage);
        }

        [Fact]
        public async Task ModuleCopyToDevice_Success_AnnouncesDefaultMessage()
        {
            // CopyDataToTemporaryStudioSet is not exercised here: it resolves a
            // TemporaryStudioSet container which only exists in the AE-10 schema,
            // so it cannot run with the TD-27 test data. Its announcement flows
            // through the same CopyDataToDevice base path as the tests below.
            var module = TestData.LoadTD27Module();
            var fake = new FakeViewServices { DataTransferFunc = _ => Task.FromResult<object?>("") };
            var deviceViewModel = ViewModelTestHelpers.CreateDeviceViewModel();
            var viewModel = new ModuleExplorerViewModel(fake, NullLogger.Instance, deviceViewModel, module);
            viewModel.CopyDataToDeviceCommand.Execute(null!);
            await ViewModelTestHelpers.WaitUntilAsync(() => viewModel.Status.Message.Length > 0);
            Assert.Equal("Copied data to device", viewModel.Status.Message);
            Assert.Equal("", viewModel.Status.ErrorMessage);
        }

        [Fact]
        public async Task SaveFile_UnwritablePath_AnnouncesError()
        {
            var fake = new FakeViewServices();
            var module = TestData.LoadTD27Module();
            var kit = module.ExportKit(1);
            var viewModel = new KitExplorerViewModel(
                fake, NullLogger.Instance, new DeviceViewModel(), kit)
            {
                FileName = Path.Combine(Path.GetTempPath(), "no-such-dir-a11y", "test.vkit"),
            };
            viewModel.SaveFileCommand.Execute(null!);
            await ViewModelTestHelpers.WaitUntilAsync(() => viewModel.Status.ErrorMessage.Length > 0, 500);
            Assert.StartsWith("Could not save file:", viewModel.Status.ErrorMessage);
            Assert.Equal("", viewModel.Status.Message);
        }

        [Fact]
        public void LoadModuleFromDevice_NoDevice_AnnouncesError()
        {
            var fake = new FakeViewServices();
            var home = new ExplorerHomeViewModel(
                fake, new LogViewModel(), new DeviceViewModel(), new FakeAudioDeviceManager());
            home.LoadModuleFromDeviceCommand.Execute(null!);
            Assert.Equal("No device connected; cannot load module data", home.Status.ErrorMessage);
            Assert.Equal("", home.Status.Message);
        }

        [Fact]
        public async Task LoadKitFromDevice_Success_AnnouncesKitNumber()
        {
            var module = TestData.LoadTD27Module();
            var kit = module.ExportKit(1);
            var fake = new FakeViewServices { DataTransferFunc = _ => Task.FromResult<object?>(kit) };
            var deviceViewModel = ViewModelTestHelpers.CreateDeviceViewModel();
            var home = new ExplorerHomeViewModel(
                fake, new LogViewModel(), deviceViewModel, new FakeAudioDeviceManager())
            {
                LoadKitFromDeviceNumber = 1,
            };
            home.LoadKitFromDeviceCommand.Execute(null!);
            await ViewModelTestHelpers.WaitUntilAsync(() => home.Status.Message.Length > 0);
            Assert.Equal("Loaded kit 1 from device", home.Status.Message);
            Assert.Equal("", home.Status.ErrorMessage);
        }

        [Fact]
        public async Task LoadFile_InvalidPath_AnnouncesError()
        {
            var fake = new FakeViewServices { OpenFileFunc = _ => Task.FromResult<string?>("/no/such/file.vdrum") };
            var home = new ExplorerHomeViewModel(
                fake, new LogViewModel(), new DeviceViewModel(), new FakeAudioDeviceManager());
            home.LoadFileCommand.Execute(null!);
            await ViewModelTestHelpers.WaitUntilAsync(() => home.Status.ErrorMessage.Length > 0, 500);
            Assert.StartsWith("Load failed:", home.Status.ErrorMessage);
            Assert.Equal("", home.Status.Message);
        }
    }
}
